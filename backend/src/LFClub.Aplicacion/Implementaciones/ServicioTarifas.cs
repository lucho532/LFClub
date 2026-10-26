using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de las tarifas del cobro de la plataforma (spec 017, RF-001 y RF-002).
/// Su responsabilidad es validar los rangos de jugadores con su precio (seguidos desde 1, el último sin
/// límite, precios en pesos enteros no negativos) y los días de gracia (0 a 30), guardarlos con quién y
/// cuándo, decir qué le corresponde pagar a cada club con esa tarifa y si la pasarela está configurada,
/// sin mostrar ningún valor de su configuración.
/// No modifica cobros ya generados.
/// </summary>
public class ServicioTarifas : IServicioTarifas
{
    private const int MaximoDiasDeGracia = 30;
    private const int MaximoDeRangos = 10;
    private const decimal PrecioMaximo = 9_999_999_999m;

    private readonly IRepositorioConfiguracionPlataforma _configuracion;
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IPasarelaDePago _pasarela;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioTarifas(
        IRepositorioConfiguracionPlataforma configuracion,
        IRepositorioClubesPlataforma clubes,
        IPasarelaDePago pasarela,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _configuracion = configuracion;
        _clubes = clubes;
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
                configuracion.Rangos = datos.Rangos.Select(r => new RangoTarifa(r.Desde, r.Hasta, r.Precio)).ToList();
                configuracion.DiasDeGracia = datos.DiasDeGracia;
                configuracion.ActualizadoPorUsuarioId = usuarioId;
                configuracion.ActualizadoEn = _reloj.AhoraUtc;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                return ADto(configuracion);
            },
            cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TarifaDeClubDto>> ListarPorClubAsync(CancellationToken cancelacion = default)
    {
        var configuracion = await _configuracion.ObtenerAsync(cancelacion);
        var filas = await _clubes.ListarAsync(cancelacion);

        return filas
            .Select(fila =>
            {
                var rango = ReglaCobroPlataforma.BuscarRango(fila.Jugadores, configuracion.Rangos);
                return new TarifaDeClubDto(
                    fila.Club.Id,
                    fila.Club.Nombre,
                    fila.Jugadores,
                    rango is null ? null : new RangoTarifaDto(rango.Desde, rango.Hasta, rango.Precio),
                    rango?.Precio ?? 0,
                    fila.Club.MesInicioCobro is { } mes ? MapperCobroPlataforma.APeriodo(mes) : null);
            })
            .ToList();
    }

    private static void Validar(GuardarTarifasDto datos)
    {
        var errores = new ErroresDeValidacion();
        ValidarRangos(errores, datos.Rangos);

        if (datos.DiasDeGracia is < 0 or > MaximoDiasDeGracia)
        {
            errores.Agregar("diasDeGracia", $"Los días de gracia van de 0 a {MaximoDiasDeGracia}.");
        }

        errores.LanzarSiHayErrores();
    }

    private static void ValidarRangos(ErroresDeValidacion errores, IReadOnlyList<RangoTarifaDto> rangos)
    {
        if (rangos.Count > MaximoDeRangos)
        {
            errores.Agregar("rangos", $"Puede haber como máximo {MaximoDeRangos} rangos.");
            return;
        }

        for (var i = 0; i < rangos.Count; i++)
        {
            var rango = rangos[i];
            var esElUltimo = i == rangos.Count - 1;
            var esperado = i == 0 ? 1 : (rangos[i - 1].Hasta ?? 0) + 1;

            if (rango.Desde != esperado)
            {
                errores.Agregar("rangos", i == 0
                    ? "El primer rango empieza en 1 jugador."
                    : $"El rango {i + 1} debe empezar en {esperado}, justo después del anterior.");
            }

            if (esElUltimo && rango.Hasta is not null)
            {
                errores.Agregar("rangos", "El último rango no tiene límite: déjalo sin «hasta».");
            }
            else if (!esElUltimo && (rango.Hasta is null || rango.Hasta < rango.Desde))
            {
                errores.Agregar("rangos", $"El rango {i + 1} debe terminar en un número igual o mayor que donde empieza.");
            }

            ValidarPrecio(errores, rango.Precio, i + 1);
        }
    }

    private static void ValidarPrecio(ErroresDeValidacion errores, decimal precio, int numero)
    {
        if (precio < 0)
        {
            errores.Agregar("rangos", $"El precio del rango {numero} no puede ser negativo.");
        }
        else if (precio != decimal.Truncate(precio))
        {
            errores.Agregar("rangos", $"El precio del rango {numero} debe ser en pesos enteros, sin centavos.");
        }
        else if (precio > PrecioMaximo)
        {
            errores.Agregar("rangos", $"El precio del rango {numero} es demasiado grande.");
        }
    }

    private TarifasDto ADto(ConfiguracionPlataforma configuracion) => new(
        configuracion.Rangos.Select(r => new RangoTarifaDto(r.Desde, r.Hasta, r.Precio)).ToList(),
        configuracion.DiasDeGracia,
        _pasarela.Disponible,
        configuracion.ActualizadoPorUsuarioId is null && configuracion.ActualizadoEn == default
            ? null
            : configuracion.ActualizadoEn);
}
