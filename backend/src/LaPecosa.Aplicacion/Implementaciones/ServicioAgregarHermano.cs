using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que agrega un hermano desde la ficha de un jugador (research §6 de la
/// 006).
/// Su responsabilidad es comprobar que la ficha es la del jugador que hace la petición, validar la
/// identidad con las reglas del registro y, con el club bloqueado, crear otro integrante de la
/// misma cuenta en el mismo club: rol JUGADOR, en espera, sin categoría y con el jugador de origen.
/// Impide el documento que ya tiene otro integrante del club, en cualquier estado, y el que otra
/// cuenta tiene en un rol que no es JUGADOR (RF-039 de la 006). Admite el de la misma cuenta en
/// otro club, que es el mismo niño (§10), y el de un jugador de otra cuenta en otro club, con el
/// aviso de si aprobarlo lo retirará de allí (RF-034, <see cref="ReglaDocumentoCompartido"/>). Si
/// ese documento ya es de un jugador en espera de la misma cuenta, lo devuelve sin crear ni cambiar
/// nada, para que confirmar dos veces deje un solo jugador (RF-009), con el aviso calculado en ese
/// momento. Guarda el responsable en la cuenta solo si no lo tenía y el hermano es menor.
/// No escribe nada en el otro club (RF-035): eso lo hace la aprobación. No crea la ficha ni sus documentos, que nacen con su primer cambio; no envía correos; no cuenta
/// cuántos jugadores tiene la cuenta (RF-010); no aprueba al hermano ni lo ubica en una categoría.
/// No comprueba el rol ni el estado de quien agrega: lo hizo la autorización. No accede al
/// contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioAgregarHermano : IServicioAgregarHermano
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAgregarHermano(
        IRepositorioClub club,
        IRepositorioPertenencias pertenencias,
        IRepositorioUsuarios usuarios,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _club = club;
        _pertenencias = pertenencias;
        _usuarios = usuarios;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<(HermanoAgregadoDto Hermano, bool Creado)> AgregarAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, AgregarHermanoDto datos, CancellationToken cancelacion = default)
    {
        // Solo desde la ficha propia: la de un hermano y la de un jugador ajeno no existen (RF-029).
        if (usuarioRolId != quienPregunta.Id)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var ahora = _reloj.AhoraUtc;
        var hoy = DateOnly.FromDateTime(ahora);
        var cuenta = await _usuarios.ObtenerPorIdAsync(quienPregunta.UsuarioId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();
        ValidadorHermano.Validar(datos, !string.IsNullOrWhiteSpace(cuenta.NombreResponsable), hoy);

        var numero = NormalizadorTexto.Documento(datos.NumeroDocumento);
        var fechaNacimiento = datos.FechaNacimiento!.Value;

        try
        {
            return await _unidadDeTrabajo.EnTransaccionAsync(
                async () =>
                {
                    // Con el club bloqueado, la segunda de dos confirmaciones ve siempre a la primera.
                    var clubId = await _club.BloquearAsync(cancelacion);

                    var conEseDocumento = await _pertenencias.ObtenerPorDocumentoEnClubAsync(clubId, numero, cancelacion);
                    var enOtrasCuentas = ReglaDocumentoCompartido.Evaluar(
                        await _pertenencias.ListarPorDocumentosAsync([numero], cancelacion), quienPregunta.UsuarioId);
                    var retiraDeOtroClub = enOtrasCuentas == DocumentoEnOtraCuenta.ConBaja;

                    if (conEseDocumento is not null)
                    {
                        return conEseDocumento.UsuarioId == quienPregunta.UsuarioId
                            && conEseDocumento.EstadoIngreso == EstadoIngreso.EN_ESPERA
                            ? (Agregado(conEseDocumento, retiraDeOtroClub), false)
                            : throw ErroresDeFicha.DocumentoRepetidoEnClub();
                    }

                    if (enOtrasCuentas == DocumentoEnOtraCuenta.NoAdmitido)
                    {
                        throw ErroresDeFicha.DocumentoEnOtraCuenta();
                    }

                    var paraCambiar = await _usuarios.ObtenerParaCambiarAsync(quienPregunta.UsuarioId, cancelacion)
                        ?? throw ExcepcionDeAplicacion.NoEncontrado();
                    var tieneResponsable = !string.IsNullOrWhiteSpace(paraCambiar.NombreResponsable);
                    if (ValidadorHermano.GuardaResponsable(fechaNacimiento, tieneResponsable, hoy)
                        && !string.IsNullOrWhiteSpace(datos.NombreResponsable))
                    {
                        paraCambiar.NombreResponsable = NormalizadorTexto.SinEspaciosSobrantes(datos.NombreResponsable);
                    }

                    var hermano = new UsuarioRol
                    {
                        ClubId = clubId,
                        UsuarioId = quienPregunta.UsuarioId,
                        Rol = Rol.JUGADOR,
                        EstadoIngreso = EstadoIngreso.EN_ESPERA,
                        Nombres = NormalizadorTexto.SinEspaciosSobrantes(datos.Nombres),
                        Apellidos = NormalizadorTexto.SinEspaciosSobrantes(datos.Apellidos),
                        TipoDocumento = datos.TipoDocumento!.Value,
                        NumeroDocumento = numero,
                        FechaNacimiento = fechaNacimiento,
                        AgregadoDesdeUsuarioRolId = quienPregunta.Id,
                        CreadoEn = ahora,
                    };
                    _pertenencias.Agregar(hermano);
                    await _unidadDeTrabajo.GuardarAsync(cancelacion);

                    return (Agregado(hermano, retiraDeOtroClub), true);
                },
                cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.DocumentoEnClub)
        {
            // Lo garantiza el índice único, como en el registro y en el cambio de documento.
            throw ErroresDeFicha.DocumentoRepetidoEnClub();
        }
    }

    private static HermanoAgregadoDto Agregado(UsuarioRol hermano, bool retiraDeOtroClub)
    {
        var jugador = MapperSesion.AJugadorDeSesion(hermano);
        return new HermanoAgregadoDto(
            jugador.UsuarioRolId, jugador.Nombres, jugador.Apellidos, jugador.EstadoIngreso, jugador.Retirado, retiraDeOtroClub);
    }
}
