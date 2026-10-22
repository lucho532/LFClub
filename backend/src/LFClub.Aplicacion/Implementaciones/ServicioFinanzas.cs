using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de las finanzas del club.
/// Su responsabilidad es calcular, con las cargas agrupadas de los repositorios: el recaudo del mes (lo
/// aplicado por pagos confirmados cuya confirmación cae en el mes de Colombia, por la categoría que tenía
/// el jugador cuando se generó cada cargo, §13), la cartera (la suma de lo pendiente de todos los cargos,
/// también de jugadores retirados; los cargos de cuentas eliminadas ya no existen), los totales por
/// categoría, quiénes deben con su estado y cuántos pagos esperan revisión. Un cambio de categoría no
/// reordena las cifras anteriores.
/// No guarda totales, no cuenta pagos en revisión, rechazados ni anulados, no comprueba el rol y no
/// accede al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioFinanzas : IServicioFinanzas
{
    private const string SinCategoria = "Sin categoría";

    private readonly IRepositorioFinanzas _finanzas;
    private readonly IRepositorioCargos _cargos;
    private readonly IRepositorioPagos _pagos;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioFinanzas(
        IRepositorioFinanzas finanzas, IRepositorioCargos cargos, IRepositorioPagos pagos, IReloj reloj)
    {
        _finanzas = finanzas;
        _cargos = cargos;
        _pagos = pagos;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<FinanzasDto> ObtenerAsync(DateOnly? mes, CancellationToken cancelacion = default)
    {
        var elegido = mes ?? ReglaMesDeCobro.MesEnCurso(_reloj.AhoraUtc);
        if (elegido.Day != 1)
        {
            throw new ExcepcionDeAplicacion(
                "datos_invalidos",
                400,
                "Revisa los datos marcados.",
                new Dictionary<string, string[]> { ["mes"] = ["El mes debe ser el primer día del mes (AAAA-MM-01)."] });
        }

        var (desde, hasta) = RangoDelMes(elegido);
        var aplicaciones = await _finanzas.AplicacionesConfirmadasAsync(desde, hasta, cancelacion);
        var cargos = await _cargos.ListarConAplicadoAsync(cancelacion: cancelacion);

        // Sin categoría es la clave vacía: un diccionario no admite la clave nula.
        var recaudoPorCategoria = aplicaciones
            .GroupBy(fila => fila.CategoriaId ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Valor));
        var carteraPorCategoria = cargos
            .GroupBy(cargo => cargo.CategoriaId ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => g.Sum(cargo => ReglaEstadoDeCuenta.Pendiente(cargo.Valor, cargo.Aplicado)));
        var porJugador = cargos.GroupBy(cargo => cargo.UsuarioRolId)
            .Select(grupo => (Id: grupo.Key, Cuenta: ReglaEstadoDeCuenta.Calcular(
                grupo.Select(cargo => new ReglaEstadoDeCuenta.ValorDeCargo(cargo.Valor, cargo.Aplicado)))))
            .Where(fila => fila.Cuenta.Saldo > 0m)
            .ToList();

        var categorias = recaudoPorCategoria.Keys.Concat(carteraPorCategoria.Keys).Where(id => id != Guid.Empty)
            .Distinct().ToList();
        var anios = await _finanzas.AniosDeCategoriasAsync(categorias, cancelacion);
        var jugadores = await _finanzas.JugadoresAsync(porJugador.Select(fila => fila.Id).ToList(), cancelacion);

        var porCategoria = recaudoPorCategoria.Keys.Concat(carteraPorCategoria.Keys).Distinct()
            .Select(id => new TotalPorCategoriaDto(
                id == Guid.Empty ? null : id,
                anios.TryGetValue(id, out var anio) ? $"Categoría {anio}" : SinCategoria,
                recaudoPorCategoria.GetValueOrDefault(id),
                carteraPorCategoria.GetValueOrDefault(id)))
            .OrderBy(fila => fila.Nombre == SinCategoria)
            .ThenBy(fila => fila.Nombre, StringComparer.Ordinal)
            .ToList();

        var deben = porJugador
            .Where(fila => jugadores.ContainsKey(fila.Id))
            .Select(fila => new JugadorQueDebeDto(
                fila.Id, jugadores[fila.Id].Nombre, fila.Cuenta.Saldo, fila.Cuenta.Estado, !jugadores[fila.Id].Activo))
            .OrderByDescending(fila => fila.Saldo)
            .ThenBy(fila => fila.NombreCompleto, OrdenDePersonas.Comparador)
            .ToList();

        return new FinanzasDto(
            elegido,
            aplicaciones.Sum(fila => fila.Valor),
            carteraPorCategoria.Values.Sum(),
            await _pagos.ContarEnRevisionAsync(cancelacion),
            porCategoria,
            deben);
    }

    // El mes de Colombia (UTC−5 fijo) como rango de instantes UTC: del día 1 a las 00:00 del mes siguiente.
    private static (DateTime Desde, DateTime Hasta) RangoDelMes(DateOnly mes)
    {
        DateTime Utc(DateOnly dia) =>
            DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue) - HoraDeColombia.Desfase, DateTimeKind.Utc);

        return (Utc(mes), Utc(mes.AddMonths(1)));
    }
}
