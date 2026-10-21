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
/// Representa el servicio que acepta una invitación con una cuenta existente.
/// Su responsabilidad es comprobar que la invitación está vigente y es del correo de la sesión, y
/// en una transacción marcarla como usada y crear el integrante, copiando la identidad del
/// integrante más reciente de la cuenta. El integrante nuevo entra directamente con el rol de la
/// invitación, aprobado y sin sala de espera (RF-010); si es JUGADOR queda, en esa transacción y
/// con el club bloqueado, en la categoría activa de su año de nacimiento, o sin categoría si el
/// club no la tiene. Con una invitación del club, si la cuenta ya está en ese club no cambia nada
/// (409). Solo una invitación de presidente reemplaza el rol de quien ya era integrante; si era
/// jugador, sale de su categoría y de sus equipos y deja de estar retirado. A un jugador retirado
/// no lo devuelve al club una invitación del club (409): se le reincorpora. Si entra como JUGADOR, se le
/// cobra desde el mes siguiente (spec 015, RF-008).
/// No crea cuentas ni pide de nuevo los datos de la persona. El responsable es la única excepción:
/// lo exige, y lo guarda en la cuenta, cuando entra como JUGADOR una persona menor de 18 años cuya
/// cuenta no lo tiene (RF-026); en cualquier otro caso la cuenta conserva el que tenga. No cambia
/// nada en los otros clubes de la persona ni registra ninguna aprobación. Para leer las categorías fija en el contexto el club de la invitación válida
/// (constitución §7.1, tercera excepción). No accede al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioAceptacionInvitacion : IServicioAceptacionInvitacion
{
    private readonly IRepositorioInvitacionesPorToken _invitaciones;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioClub _club;
    private readonly IContextoClub _contextoClub;
    private readonly UbicadorDeJugadores _ubicador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAceptacionInvitacion(
        IRepositorioInvitacionesPorToken invitaciones,
        IRepositorioUsuarios usuarios,
        IRepositorioPertenencias pertenencias,
        IRepositorioClub club,
        IContextoClub contextoClub,
        UbicadorDeJugadores ubicador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _invitaciones = invitaciones;
        _usuarios = usuarios;
        _pertenencias = pertenencias;
        _club = club;
        _contextoClub = contextoClub;
        _ubicador = ubicador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<(ClubDeSesionDto Club, bool Creada)> AceptarAsync(
        AceptarInvitacionDto datos, Guid usuarioId, CancellationToken cancelacion = default)
    {
        var ahora = _reloj.AhoraUtc;
        var invitacion = string.IsNullOrWhiteSpace(datos.Token)
            ? null
            : await _invitaciones.ObtenerPorHashAsync(GeneradorTokens.Hash(datos.Token), cancelacion);
        if (invitacion?.Club is null
            || !invitacion.EstaVigente(ahora)
            || !ReglaIngresoPorInvitacion.ElClubPermiteUsarla(invitacion.Rol, invitacion.Club.Estado))
        {
            throw ErroresDeInvitacion.NoValida();
        }

        // Desde aquí la petición queda limitada al club de la invitación (research §4).
        _contextoClub.Fijar(invitacion.ClubId);

        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion);
        if (usuario is null || usuario.CorreoNormalizado != invitacion.Correo)
        {
            throw new ExcepcionDeAplicacion(
                "invitacion_de_otro_correo",
                403,
                "Esta invitación se envió a otro correo. Cierra sesión y entra con la cuenta de ese correo.");
        }

        var existente = await _pertenencias.ObtenerAsync(usuarioId, invitacion.ClubId, cancelacion);
        if (existente is not null && ReglaIngresoPorInvitacion.EsDelClub(invitacion.Rol))
        {
            // Una invitación del club no cambia a quien ya está en él: no se gasta ni cambia nada.
            // A un jugador retirado tampoco lo devuelve: eso es reincorporarlo (RF-046 de la 003).
            throw existente.Activo ? ErroresDeInvitacion.YaPertenecesAlClub() : ErroresDeInvitacion.PersonaRetirada();
        }

        var integrante = existente ?? await NuevoIntegranteAsync(usuario, invitacion, ahora, cancelacion);
        var responsable = existente is null ? ResponsableQueFaltaba(datos, usuario, integrante, ahora) : null;

        try
        {
            await _unidadDeTrabajo.EnTransaccionAsync(
                async () =>
                {
                    // Con el club bloqueado, aceptar y crear la categoría del año a la vez deja
                    // siempre al jugador dentro de ella (research §4).
                    await _club.BloquearAsync(cancelacion);

                    if (!await _invitaciones.MarcarUsadaAsync(invitacion.Id, ahora, cancelacion))
                    {
                        throw ErroresDeInvitacion.NoValida();
                    }

                    if (existente is null)
                    {
                        _pertenencias.Agregar(integrante);
                        usuario.NombreResponsable = responsable ?? usuario.NombreResponsable;
                    }
                    else
                    {
                        // Ya era integrante de este club y la invitación es de presidente: su rol
                        // pasa a ser el de la invitación (RF-018 de la 001).
                        existente.Rol = invitacion.Rol;
                        existente.EstadoIngreso = EstadoIngreso.APROBADO;

                        // Si era jugador deja de serlo: solo los jugadores tienen categoría y equipos
                        // y solo a ellos se les retira (RF-012 y RF-041 de la 003).
                        existente.CategoriaId = null;
                        existente.Activo = true;
                        existente.RetiradoEn = null;
                        existente.RetiradoPorUsuarioId = null;
                        existente.RetiradoPorNombre = null;
                        await _pertenencias.SacarDeSusEquiposAsync(existente.Id, cancelacion);
                    }

                    await _unidadDeTrabajo.GuardarAsync(cancelacion);

                    // Quien entra como jugador se ubica igual que al registrarse (RF-011).
                    if (existente is null && integrante.Rol == Rol.JUGADOR)
                    {
                        await _ubicador.UbicarAUnoAsync(integrante, cancelacion);
                    }
                },
                cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && ErroresDeInvitacion.DeIndiceUnico(indice) is { } conflicto)
        {
            throw conflicto;
        }

        integrante.Club = invitacion.Club;
        return (MapperSesion.AClubDeSesion(integrante), existente is null);
    }

    /// <summary>
    /// El nombre del responsable que hay que guardar en la cuenta, o nulo si no hace falta. Un
    /// JUGADOR menor de 18 años no entra sin responsable, igual que al registrarse: si su cuenta no
    /// lo tiene, se exige aquí (RF-026). En cualquier otro caso lo que llegue se ignora.
    /// </summary>
    private static string? ResponsableQueFaltaba(
        AceptarInvitacionDto datos, Usuario usuario, UsuarioRol integrante, DateTime ahora)
    {
        if (!string.IsNullOrWhiteSpace(usuario.NombreResponsable)
            || !ReglaIngresoPorInvitacion.ExigeResponsable(
                integrante.Rol, integrante.FechaNacimiento, DateOnly.FromDateTime(ahora)))
        {
            return null;
        }

        var errores = new ErroresDeValidacion();
        if (string.IsNullOrWhiteSpace(datos.NombreResponsable))
        {
            errores.Agregar(
                "nombreResponsable",
                "El nombre del padre, madre o responsable es obligatorio para una persona menor de 18 años.");
        }

        errores.Maximo(
            "nombreResponsable", datos.NombreResponsable, ValidadorRegistro.MaximoResponsable, "El nombre del responsable");
        errores.LanzarSiHayErrores();

        return NormalizadorTexto.SinEspaciosSobrantes(datos.NombreResponsable);
    }

    private async Task<UsuarioRol> NuevoIntegranteAsync(
        Usuario usuario, Invitacion invitacion, DateTime ahora, CancellationToken cancelacion)
    {
        // La persona es la misma en todos sus clubes: se copia su identidad del integrante más
        // reciente de la cuenta (research §7).
        var identidad = (await _pertenencias.ListarDeUsuarioAsync(usuario.Id, cancelacion))
            .OrderByDescending(integrante => integrante.CreadoEn)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Una cuenta con invitación debe tener algún integrante.");

        return new UsuarioRol
        {
            ClubId = invitacion.ClubId,
            UsuarioId = usuario.Id,
            Rol = invitacion.Rol,
            EstadoIngreso = EstadoIngreso.APROBADO,
            Nombres = identidad.Nombres,
            Apellidos = identidad.Apellidos,
            TipoDocumento = identidad.TipoDocumento,
            NumeroDocumento = identidad.NumeroDocumento,
            FechaNacimiento = identidad.FechaNacimiento,
            CreadoEn = ahora,
            CobraDesde = invitacion.Rol == Rol.JUGADOR ? ReglaMesDeCobro.PrimerMesDeCobro(ahora) : null,
        };
    }
}
