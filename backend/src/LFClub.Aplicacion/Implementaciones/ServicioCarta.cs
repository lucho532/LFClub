using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que cambia la carta de un jugador (spec 010, RF-021, RF-022, RF-027 y
/// RF-028; research §8 y §12.2).
/// Su responsabilidad es resolver el acceso con <see cref="AccesoACarta"/> (quien no ve la ficha,
/// <c>404</c>); negar las estadísticas a la cuenta de un jugador (<c>403 rol_no_autorizado</c>); negar
/// cualquier cambio sobre un jugador retirado (<c>409 jugador_retirado</c>) y a quien ve la carta pero
/// no puede cambiar esa parte (<c>403 no_entrena_categoria</c>); validar los valores; y, con el club
/// bloqueado, crear la carta con el primer cambio o guardarla, con el sello de quien la cambió y su
/// nombre copiado (§13). Si nada cambió, no crea la fila ni mueve el sello.
/// No calcula la valoración ni la rareza, no toca las estadísticas de partidos y entrenamientos
/// (RF-024) y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioCarta : IServicioCarta
{
    private readonly AccesoACarta _acceso;
    private readonly IRepositorioCartas _cartas;
    private readonly IRepositorioClub _club;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaCarta _consulta;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCarta(
        AccesoACarta acceso,
        IRepositorioCartas cartas,
        IRepositorioClub club,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaCarta consulta,
        IReloj reloj)
    {
        _acceso = acceso;
        _cartas = cartas;
        _club = club;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<CartaDto> CambiarDorsalYPosicionAsync(
        Guid usuarioRolId, DorsalYPosicionDto datos, UsuarioRol quienCambia, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienCambia, cancelacion);
        Exigir(jugador, alcance.CambiaDorsalYPosicion);
        if (datos.Dorsal is { } dorsal && (dorsal < CartaJugador.ValorMinimo || dorsal > CartaJugador.ValorMaximo))
        {
            var errores = new ErroresDeValidacion();
            errores.Agregar("dorsal", "El dorsal va de 1 a 99.");
            errores.LanzarSiHayErrores();
        }

        await GuardarAsync(
            jugador,
            carta => carta.Dorsal == datos.Dorsal && carta.Posicion == datos.Posicion,
            carta =>
            {
                carta.Dorsal = datos.Dorsal;
                carta.Posicion = datos.Posicion;
                carta.DorsalCambiadoEn = _reloj.AhoraUtc;
                carta.DorsalCambiadoPorUsuarioId = quienCambia.UsuarioId;
                carta.DorsalCambiadoPorNombre = Nombre(quienCambia);
            },
            cancelacion);
        return await _consulta.ObtenerAsync(usuarioRolId, quienCambia, cancelacion);
    }

    /// <inheritdoc />
    public async Task<CartaDto> CambiarEstadisticasAsync(
        Guid usuarioRolId, EstadisticasCartaDto datos, UsuarioRol quienCambia, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienCambia, cancelacion);
        if (quienCambia.Rol == Rol.JUGADOR)
        {
            throw new ExcepcionDeAplicacion("rol_no_autorizado", 403, "Tu rol en este club no permite hacer esto.");
        }

        Exigir(jugador, alcance.CambiaEstadisticas);
        if (ReglaEstadisticasDeCarta.Validar(datos.Ritmo, datos.Tiro, datos.Pase, datos.Regate, datos.Defensa, datos.Fisico) is { } campo)
        {
            var errores = new ErroresDeValidacion();
            errores.Agregar(campo, "Las seis estadísticas van de 1 a 99, o ninguna.");
            errores.LanzarSiHayErrores();
        }

        await GuardarAsync(
            jugador,
            carta => carta.Ritmo == datos.Ritmo && carta.Tiro == datos.Tiro && carta.Pase == datos.Pase
                && carta.Regate == datos.Regate && carta.Defensa == datos.Defensa && carta.Fisico == datos.Fisico,
            carta =>
            {
                (carta.Ritmo, carta.Tiro, carta.Pase) = (datos.Ritmo, datos.Tiro, datos.Pase);
                (carta.Regate, carta.Defensa, carta.Fisico) = (datos.Regate, datos.Defensa, datos.Fisico);
                carta.EstadisticasCambiadasEn = _reloj.AhoraUtc;
                carta.EstadisticasCambiadasPorUsuarioId = quienCambia.UsuarioId;
                carta.EstadisticasCambiadasPorNombre = Nombre(quienCambia);
            },
            cancelacion);
        return await _consulta.ObtenerAsync(usuarioRolId, quienCambia, cancelacion);
    }

    // Supuesto 2: la carta de un retirado no se cambia, aunque sea el PRESIDENTE quien lo pida.
    private static void Exigir(UsuarioRol jugador, bool puede)
    {
        if (!jugador.Activo)
        {
            throw ErroresDeCarta.JugadorRetirado();
        }

        if (!puede)
        {
            throw ErroresDeCarta.NoEntrenaCategoria();
        }
    }

    private async Task GuardarAsync(
        UsuarioRol jugador, Func<CartaJugador, bool> sinCambios, Action<CartaJugador> cambiar, CancellationToken cancelacion) =>
        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var guardada = await _cartas.ObtenerAsync(jugador.Id, cancelacion);
                var carta = guardada ?? new CartaJugador { UsuarioRolId = jugador.Id, ClubId = jugador.ClubId };
                if (sinCambios(carta))
                {
                    return;
                }

                cambiar(carta);
                await _cartas.GuardarAsync(carta, guardada is null, cancelacion);
            },
            cancelacion);

    private static string Nombre(UsuarioRol integrante) => $"{integrante.Nombres} {integrante.Apellidos}";
}
