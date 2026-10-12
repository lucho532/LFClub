using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que cambia el documento de identidad de un jugador y corrige su
/// identidad.
/// Su responsabilidad es resolver el acceso, validar con las reglas del registro y, con el club
/// bloqueado, actualizar la misma fila del jugador y sellar el último cambio solo si algo quedó
/// distinto. Lo usan el PRESIDENTE y la cuenta del jugador (spec 010, RF-013). Al cambiar el
/// documento impide el número que ya tiene otro integrante del club o que usa alguna otra cuenta, y traduce la violación del índice único al mismo 409. Al corregir la
/// fecha de nacimiento, si corrige el PRESIDENTE, ubica al jugador activo que no tiene categoría, con
/// la regla de la 003, y nunca mueve a quien ya tiene una (RF-025); si corrige la familia, la
/// categoría no cambia nunca, tenga o no (RF-015 de la 010).
/// No cambia la cuenta, la contraseña ni las sesiones abiertas; no toca los nombres ya copiados en
/// una aprobación o un retiro (§13); no exige el responsable aunque el jugador pase a ser menor;
/// no accede al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioIdentidadJugador : IServicioIdentidadJugador
{
    private readonly AccesoAFicha _acceso;
    private readonly IRepositorioClub _club;
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioFichas _fichas;
    private readonly UbicadorDeJugadores _ubicador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaFicha _consulta;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioIdentidadJugador(
        AccesoAFicha acceso,
        IRepositorioClub club,
        IRepositorioJugadores jugadores,
        IRepositorioPertenencias pertenencias,
        IRepositorioFichas fichas,
        UbicadorDeJugadores ubicador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaFicha consulta,
        IReloj reloj)
    {
        _acceso = acceso;
        _club = club;
        _jugadores = jugadores;
        _pertenencias = pertenencias;
        _fichas = fichas;
        _ubicador = ubicador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<FichaJugadorDto> CambiarDocumentoAsync(
        Guid usuarioRolId,
        CambiarDocumentoIdentidadDto datos,
        UsuarioRol quienCambia,
        CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienCambia, cancelacion);
        AccesoAFicha.ExigirCambio(alcance);

        var errores = new ErroresDeValidacion();
        ValidadorIdentidad.Documento(errores, datos.TipoDocumento, datos.NumeroDocumento);
        errores.LanzarSiHayErrores();
        var tipo = datos.TipoDocumento!.Value;
        var numero = NormalizadorTexto.Documento(datos.NumeroDocumento);

        try
        {
            await _unidadDeTrabajo.EnTransaccionAsync(
                async () =>
                {
                    await _club.BloquearAsync(cancelacion);
                    var actual = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion)
                        ?? throw ExcepcionDeAplicacion.NoEncontrado();

                    // El mismo documento que ya tiene no es un error ni un cambio (research §5).
                    if (actual.TipoDocumento == tipo && actual.NumeroDocumento == numero)
                    {
                        return;
                    }

                    if (actual.NumeroDocumento != numero)
                    {
                        await ExigirNumeroLibreAsync(jugador, numero, cancelacion);
                    }

                    await _jugadores.CambiarDocumentoAsync(usuarioRolId, tipo, numero, cancelacion);
                    await _fichas.SellarCambioAsync(jugador, quienCambia, _reloj.AhoraUtc, cancelacion);
                    await _unidadDeTrabajo.GuardarAsync(cancelacion);
                },
                cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.DocumentoEnClub)
        {
            // Lo garantiza el índice único, como en el registro.
            throw ErroresDeFicha.DocumentoRepetidoEnClub();
        }

        return await _consulta.ObtenerAsync(usuarioRolId, quienCambia, cancelacion);
    }

    /// <inheritdoc />
    public async Task<FichaJugadorDto> CorregirAsync(
        Guid usuarioRolId, CorregirIdentidadDto datos, UsuarioRol quienCorrige, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienCorrige, cancelacion);
        AccesoAFicha.ExigirCorreccionDeIdentidad(alcance);

        var ahora = _reloj.AhoraUtc;
        var errores = new ErroresDeValidacion();
        ValidadorIdentidad.NombresYApellidos(errores, datos.Nombres, datos.Apellidos);
        ValidadorIdentidad.FechaNacimiento(errores, datos.FechaNacimiento, DateOnly.FromDateTime(ahora));
        errores.LanzarSiHayErrores();
        var nombres = NormalizadorTexto.SinEspaciosSobrantes(datos.Nombres);
        var apellidos = NormalizadorTexto.SinEspaciosSobrantes(datos.Apellidos);
        var fechaNacimiento = datos.FechaNacimiento!.Value;

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                // Con el club bloqueado, corregir la fecha y crear la categoría de ese año a la vez
                // deja siempre al jugador dentro de ella (research §4 de la 003).
                await _club.BloquearAsync(cancelacion);
                var actual = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();

                if (actual.Nombres == nombres && actual.Apellidos == apellidos && actual.FechaNacimiento == fechaNacimiento)
                {
                    return;
                }

                await _jugadores.CorregirIdentidadAsync(usuarioRolId, nombres, apellidos, fechaNacimiento, cancelacion);

                // Solo el PRESIDENTE ubica a quien no tiene categoría; a un retirado no se le ubica
                // (RF-025, RF-026 de la 005). Cuando corrige la familia, la categoría no se toca (RF-015).
                if (UbicaAlCorregir(quienCorrige, actual))
                {
                    actual.FechaNacimiento = fechaNacimiento;
                    await _ubicador.UbicarAUnoAsync(actual, cancelacion);
                }

                await _fichas.SellarCambioAsync(jugador, quienCorrige, ahora, cancelacion);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(usuarioRolId, quienCorrige, cancelacion);
    }

    /// <summary>
    /// Si al corregir la identidad se ubica al jugador en la categoría de su año: solo cuando corrige
    /// el PRESIDENTE y el jugador está activo y sin categoría.
    /// </summary>
    private static bool UbicaAlCorregir(UsuarioRol quienCorrige, UsuarioRol jugador) =>
        quienCorrige.Rol == Rol.PRESIDENTE && jugador.EsJugadorDelClub && jugador.CategoriaId is null;

    /// <summary>
    /// Lanza el 409 que corresponde si ese número ya lo tiene otro integrante del club, activo o
    /// retirado (RF-023), o si lo usa alguna otra cuenta en otro club, aunque también lo tenga la
    /// misma. Si solo es de la misma cuenta en otro club se admite: es la misma persona (§10).
    /// </summary>
    private async Task ExigirNumeroLibreAsync(UsuarioRol jugador, string numero, CancellationToken cancelacion)
    {
        if (await _jugadores.ExisteDocumentoEnOtroAsync(jugador.Id, numero, cancelacion))
        {
            throw ErroresDeFicha.DocumentoRepetidoEnClub();
        }

        var conEseNumero = await _pertenencias.ListarPorDocumentosAsync([numero], cancelacion);
        if (conEseNumero.Any(integrante => integrante.UsuarioId != jugador.UsuarioId))
        {
            throw ErroresDeFicha.DocumentoEnOtraCuenta();
        }
    }
}
