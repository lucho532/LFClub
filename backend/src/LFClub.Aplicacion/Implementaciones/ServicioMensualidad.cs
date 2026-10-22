using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que define el valor de la mensualidad.
/// Su responsabilidad es validar el importe (entero de pesos y mayor que cero), guardar el valor del
/// club y, tras guardarlo, pedir la generación del mes en curso para que un club que no tenía valor
/// empiece a cobrar de inmediato (repetirla es inofensivo); y fijar o quitar el valor propio de varios
/// jugadores en una sola transacción de todo o nada, porque si alguno no es un jugador activo del club
/// no se cambia ninguno.
/// No cambia los cargos ya generados (RF-013), no comprueba el rol de quien llama y no accede al
/// contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioMensualidad : IServicioMensualidad
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioCargos _cargos;
    private readonly IServicioGeneracionCargos _generacion;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioMensualidad(
        IRepositorioClub club,
        IRepositorioCargos cargos,
        IServicioGeneracionCargos generacion,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _club = club;
        _cargos = cargos;
        _generacion = generacion;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public async Task<MensualidadDto> ObtenerAsync(CancellationToken cancelacion = default)
    {
        var club = await _club.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return await ArmarAsync(club.ValorMensualidad, cancelacion);
    }

    /// <inheritdoc />
    public async Task<MensualidadDto> FijarValorDelClubAsync(
        FijarValorMensualidadDto datos, CancellationToken cancelacion = default)
    {
        ValidadorPago.ValidarValorDeMensualidad(datos.Valor);
        var club = await _club.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();

        club.ValorMensualidad = datos.Valor;
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
        await _generacion.GenerarDelClubAsync(club.Id, cancelacion);

        return await ArmarAsync(club.ValorMensualidad, cancelacion);
    }

    /// <inheritdoc />
    public async Task<MensualidadDto> FijarValorPropioAsync(
        FijarValorPropioDto datos, CancellationToken cancelacion = default)
    {
        var ids = datos.UsuarioRolIds?.Distinct().ToList() ?? [];
        var errores = new ErroresDeValidacion();
        if (ids.Count == 0)
        {
            errores.Agregar("usuarioRolIds", "Elige al menos un jugador.");
        }

        if (datos.Valor is { } valor)
        {
            ValidadorPago.ValidarValorDeMensualidad(valor);
        }

        errores.LanzarSiHayErrores();

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                if (await _cargos.FijarValorPropioAsync(ids, datos.Valor, cancelacion) != ids.Count)
                {
                    throw ExcepcionDeAplicacion.NoEncontrado();
                }
            },
            cancelacion);

        var club = await _club.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return await ArmarAsync(club.ValorMensualidad, cancelacion);
    }

    private async Task<MensualidadDto> ArmarAsync(decimal? valorClub, CancellationToken cancelacion)
    {
        var propios = await _cargos.ListarConValorPropioAsync(cancelacion);
        return new MensualidadDto(
            valorClub,
            propios.PorApellidos()
                .Select(jugador => new JugadorConValorPropioDto(
                    jugador.Id, $"{jugador.Nombres} {jugador.Apellidos}", jugador.ValorMensualidadPropio!.Value))
                .ToList());
    }
}
