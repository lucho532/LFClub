using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de las tarifas del cobro de la plataforma (spec 017, RF-001 y RF-002).
/// Su responsabilidad es validar los precios (nulos o pesos enteros no negativos) y los días de gracia
/// (0 a 30), guardarlos con quién y cuándo, y decir si la pasarela está configurada sin mostrar
/// ningún valor de su configuración.
/// No modifica cobros ya generados.
/// </summary>
public class ServicioTarifas : IServicioTarifas
{
    private const int MaximoDiasDeGracia = 30;

    private readonly IRepositorioConfiguracionPlataforma _configuracion;
    private readonly IPasarelaDePago _pasarela;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioTarifas(
        IRepositorioConfiguracionPlataforma configuracion,
        IPasarelaDePago pasarela,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _configuracion = configuracion;
        _pasarela = pasarela;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<TarifasDto> ObtenerAsync(CancellationToken cancelacion = default) =>
        ADto(await _configuracion.ObtenerAsync(cancelacion));

    /// <inheritdoc />
    public async Task<TarifasDto> GuardarAsync(
        GuardarTarifasDto datos, Guid usuarioId, CancellationToken cancelacion = default)
    {
        Validar(datos);

        return await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var configuracion = await _configuracion.ObtenerBloqueandoAsync(cancelacion);
                configuracion.PrecioJugador = datos.PrecioJugador;
                configuracion.PrecioEntrenador = datos.PrecioEntrenador;
                configuracion.PrecioDirectivo = datos.PrecioDirectivo;
                configuracion.PrecioPresidente = datos.PrecioPresidente;
                configuracion.DiasDeGracia = datos.DiasDeGracia;
                configuracion.ActualizadoPorUsuarioId = usuarioId;
                configuracion.ActualizadoEn = _reloj.AhoraUtc;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                return ADto(configuracion);
            },
            cancelacion);
    }

    private static void Validar(GuardarTarifasDto datos)
    {
        var errores = new ErroresDeValidacion();
        ValidarPrecio(errores, "precioJugador", datos.PrecioJugador, "El precio por jugador");
        ValidarPrecio(errores, "precioEntrenador", datos.PrecioEntrenador, "El precio por entrenador");
        ValidarPrecio(errores, "precioDirectivo", datos.PrecioDirectivo, "El precio por directivo");
        ValidarPrecio(errores, "precioPresidente", datos.PrecioPresidente, "El precio por presidente");

        if (datos.DiasDeGracia is < 0 or > MaximoDiasDeGracia)
        {
            errores.Agregar("diasDeGracia", $"Los días de gracia van de 0 a {MaximoDiasDeGracia}.");
        }

        errores.LanzarSiHayErrores();
    }

    private static void ValidarPrecio(ErroresDeValidacion errores, string campo, decimal? precio, string nombre)
    {
        if (precio is null)
        {
            return;
        }

        if (precio < 0)
        {
            errores.Agregar(campo, $"{nombre} no puede ser negativo.");
        }
        else if (precio != decimal.Truncate(precio.Value))
        {
            errores.Agregar(campo, $"{nombre} debe ser un valor en pesos enteros, sin centavos.");
        }
        else if (precio > 9_999_999_999m)
        {
            errores.Agregar(campo, $"{nombre} es demasiado grande.");
        }
    }

    private TarifasDto ADto(ConfiguracionPlataforma configuracion) => new(
        configuracion.PrecioJugador,
        configuracion.PrecioEntrenador,
        configuracion.PrecioDirectivo,
        configuracion.PrecioPresidente,
        configuracion.DiasDeGracia,
        _pasarela.Disponible,
        configuracion.ActualizadoPorUsuarioId is null && configuracion.ActualizadoEn == default
            ? null
            : configuracion.ActualizadoEn);
}
