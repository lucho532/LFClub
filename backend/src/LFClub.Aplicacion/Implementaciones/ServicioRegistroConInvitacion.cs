using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de registro con invitación.
/// Su responsabilidad es comprobar que la invitación está vigente, que el correo no tiene cuenta y
/// que el documento no se repite en el club ni pertenece a ninguna otra cuenta, aunque esté en dos
/// (una persona tiene un único inicio de sesión; solo el hermano de la 006 comparte número), y
/// crear la cuenta y el integrante en la misma transacción en que la invitación queda usada.
/// Quien se registra entra directamente al club con el rol de su invitación, aprobado y sin sala
/// de espera (RF-008); si es JUGADOR queda además, en esa misma
/// transacción y con el club bloqueado, en la categoría activa de su año de nacimiento, o sin
/// categoría si el club no la tiene (RF-011); si entra como JUGADOR, se le cobra desde el mes siguiente
/// (spec 015, RF-008).
/// No acepta un correo, un club ni un rol enviados por quien se registra, y no puede crear una
/// cuenta DESARROLLADOR. No registra ninguna aprobación: nadie aprobó a quien fue invitado. Para
/// leer las categorías fija en el contexto el club de la invitación válida, que es lo único que
/// liga la petición a un club (constitución §7.1, tercera excepción); la respuesta no devuelve
/// ningún dato de ese club. No accede al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioRegistroConInvitacion : IServicioRegistroConInvitacion
{
    private readonly IRepositorioInvitacionesPorToken _invitaciones;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioClub _club;
    private readonly IContextoClub _contextoClub;
    private readonly UbicadorDeJugadores _ubicador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IHashContrasena _hash;
    private readonly IEmisorTokenSesion _emisor;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRegistroConInvitacion(
        IRepositorioInvitacionesPorToken invitaciones,
        IRepositorioUsuarios usuarios,
        IRepositorioPertenencias pertenencias,
        IRepositorioClub club,
        IContextoClub contextoClub,
        UbicadorDeJugadores ubicador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IHashContrasena hash,
        IEmisorTokenSesion emisor,
        IReloj reloj)
    {
        _invitaciones = invitaciones;
        _usuarios = usuarios;
        _pertenencias = pertenencias;
        _club = club;
        _contextoClub = contextoClub;
        _ubicador = ubicador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _hash = hash;
        _emisor = emisor;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<InvitacionVigenteDto> ConsultarAsync(TokenDto datos, CancellationToken cancelacion = default)
    {
        var invitacion = await VigenteAsync(datos.Token, cancelacion);
        var cuenta = await _usuarios.ObtenerPorCorreoAsync(invitacion.Correo, cancelacion);

        return new InvitacionVigenteDto(
            invitacion.Club!.Nombre,
            invitacion.Rol,
            invitacion.Correo,
            cuenta is not null,
            ReglaIngresoPorInvitacion.PideResponsable(invitacion.Rol),
            cuenta is not null && await FaltaResponsableAsync(cuenta, invitacion.Rol, cancelacion),
            MapperIdentidadClub.AIdentidad(invitacion.Club));
    }

    /// <summary>
    /// Indica si la aceptación va a pedir el responsable a esa cuenta: no lo tiene y, por la fecha
    /// de nacimiento de su integrante más reciente, entra como JUGADOR menor de edad (RF-026).
    /// </summary>
    private async Task<bool> FaltaResponsableAsync(Usuario cuenta, Rol rolDeLaInvitacion, CancellationToken cancelacion)
    {
        if (!string.IsNullOrWhiteSpace(cuenta.NombreResponsable)
            || !ReglaIngresoPorInvitacion.PideResponsable(rolDeLaInvitacion))
        {
            return false;
        }

        var identidad = (await _pertenencias.ListarDeUsuarioAsync(cuenta.Id, cancelacion))
            .OrderByDescending(integrante => integrante.CreadoEn)
            .FirstOrDefault();
        return identidad is not null && ReglaIngresoPorInvitacion.ExigeResponsable(
            rolDeLaInvitacion, identidad.FechaNacimiento, DateOnly.FromDateTime(_reloj.AhoraUtc));
    }

    /// <inheritdoc />
    public async Task<TokenSesionDto> RegistrarAsync(
        RegistrarConInvitacionDto datos, CancellationToken cancelacion = default)
    {
        var invitacion = await VigenteAsync(datos.Token, cancelacion);

        // Desde aquí la petición queda limitada al club de la invitación (research §4).
        _contextoClub.Fijar(invitacion.ClubId);

        var ahora = _reloj.AhoraUtc;
        ValidadorRegistro.Validar(datos, invitacion.Rol, DateOnly.FromDateTime(ahora));

        var documento = NormalizadorTexto.Documento(datos.NumeroDocumento);
        if (await _usuarios.ObtenerPorCorreoAsync(invitacion.Correo, cancelacion) is not null)
        {
            throw ErroresDeInvitacion.CorreoYaRegistrado();
        }

        if (await _pertenencias.ExisteDocumentoEnClubAsync(invitacion.ClubId, documento, cancelacion))
        {
            // El documento de un jugador retirado sigue ocupado, pero el mensaje es otro (RF-046 de la 003).
            throw await _pertenencias.EsDocumentoDeRetiradoAsync(invitacion.ClubId, documento, cancelacion)
                ? ErroresDeInvitacion.PersonaRetirada()
                : ErroresDeInvitacion.DocumentoRepetidoEnClub();
        }

        if (await _pertenencias.ObtenerCuentaPorDocumentoAsync(documento, cancelacion) is not null)
        {
            throw ExcepcionDeAplicacion.Conflicto(
                "documento_en_otra_cuenta",
                "Ese documento ya está registrado con otra cuenta. Inicia sesión con esa cuenta para aceptar la invitación.");
        }

        var (usuario, integrante) = Nuevos(datos, invitacion, documento, ahora);

        try
        {
            await _unidadDeTrabajo.EnTransaccionAsync(
                async () =>
                {
                    // Con el club bloqueado, registrarse y crear la categoría del año a la vez deja
                    // siempre al jugador dentro de ella (research §4).
                    await _club.BloquearAsync(cancelacion);

                    if (!await _invitaciones.MarcarUsadaAsync(invitacion.Id, ahora, cancelacion))
                    {
                        throw ErroresDeInvitacion.NoValida();
                    }

                    _usuarios.Agregar(usuario);
                    _pertenencias.Agregar(integrante);
                    await _unidadDeTrabajo.GuardarAsync(cancelacion);

                    // Solo el jugador tiene categoría: a un entrenador o a un directivo no se les ubica (RF-012).
                    if (integrante.Rol == Rol.JUGADOR)
                    {
                        await _ubicador.UbicarAUnoAsync(integrante, cancelacion);
                    }
                },
                cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && ErroresDeInvitacion.DeIndiceUnico(indice) is { } conflicto)
        {
            // Dos registros simultáneos: el índice único decide y se responde el mismo 409.
            throw conflicto;
        }

        return MapperSesion.AToken(_emisor.Emitir(usuario.Id, usuario.SelloSeguridad));
    }

    /// <summary>
    /// La cuenta y el integrante que nacen del registro. El correo, el club y el rol son siempre los
    /// de la invitación (RF-009). El integrante nace aprobado y sin datos de aprobación, y el
    /// responsable solo se guarda cuando la invitación es de JUGADOR (RF-013).
    /// </summary>
    private (Usuario Usuario, UsuarioRol Integrante) Nuevos(
        RegistrarConInvitacionDto datos, Invitacion invitacion, string documento, DateTime ahora)
    {
        var responsable = ReglaIngresoPorInvitacion.PideResponsable(invitacion.Rol)
            ? NormalizadorTexto.SinEspaciosSobrantes(datos.NombreResponsable)
            : string.Empty;
        var usuario = new Usuario
        {
            Correo = invitacion.Correo,
            CorreoNormalizado = invitacion.Correo,
            ContrasenaHash = _hash.Calcular(datos.Contrasena!),
            Celular = datos.Celular!.Trim(),
            NombreResponsable = responsable.Length == 0 ? null : responsable,
            EsDesarrollador = false,
            CreadoEn = ahora,
        };
        var integrante = new UsuarioRol
        {
            ClubId = invitacion.ClubId,
            UsuarioId = usuario.Id,
            Rol = invitacion.Rol,
            EstadoIngreso = EstadoIngreso.APROBADO,
            Nombres = NormalizadorTexto.SinEspaciosSobrantes(datos.Nombres),
            Apellidos = NormalizadorTexto.SinEspaciosSobrantes(datos.Apellidos),
            TipoDocumento = datos.TipoDocumento!.Value,
            NumeroDocumento = documento,
            FechaNacimiento = datos.FechaNacimiento!.Value,
            CreadoEn = ahora,
            CobraDesde = invitacion.Rol == Rol.JUGADOR ? ReglaMesDeCobro.PrimerMesDeCobro(ahora) : null,
        };

        return (usuario, integrante);
    }

    private async Task<Invitacion> VigenteAsync(string? token, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw ErroresDeInvitacion.NoValida();
        }

        var invitacion = await _invitaciones.ObtenerPorHashAsync(GeneradorTokens.Hash(token), cancelacion);
        if (invitacion?.Club is null
            || !invitacion.EstaVigente(_reloj.AhoraUtc)
            || !ReglaIngresoPorInvitacion.ElClubPermiteUsarla(invitacion.Rol, invitacion.Club.Estado))
        {
            throw ErroresDeInvitacion.NoValida();
        }

        return invitacion;
    }
}
