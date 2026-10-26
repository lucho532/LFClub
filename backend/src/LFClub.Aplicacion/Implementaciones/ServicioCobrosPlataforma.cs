using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que genera el cobro mensual de la plataforma (spec 017, RF-003 a RF-006).
/// Su responsabilidad es, para cada club al que ya le toca y que aún no tiene el cobro del mes en
/// curso, contar sus integrantes por rol, aplicar <see cref="ReglaCobroPlataforma"/> con los precios
/// vigentes y guardar el cobro con ese detalle, los días de gracia y la fecha desde la que se suspende
/// (el día de Colombia en que se genera más los días de gracia). Es idempotente: el índice único de
/// club y periodo evita duplicados aunque dos instancias coincidan.
/// No genera cobros de meses anteriores, no cobra a clubes dados de baja y no suspende a nadie.
/// </summary>
public class ServicioCobrosPlataforma : IServicioCobrosPlataforma
{
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IRepositorioConfiguracionPlataforma _configuracion;
    private readonly IRepositorioConteoDeIntegrantes _conteo;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCobrosPlataforma(
        IRepositorioCobrosPlataforma cobros,
        IRepositorioConfiguracionPlataforma configuracion,
        IRepositorioConteoDeIntegrantes conteo,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _cobros = cobros;
        _configuracion = configuracion;
        _conteo = conteo;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<int> GenerarAsync(CancellationToken cancelacion = default)
    {
        var configuracion = await _configuracion.ObtenerAsync(cancelacion);
        var ahora = _reloj.AhoraUtc;
        var periodo = ReglaMesDeCobro.MesEnCurso(ahora);
        var suspendeDesde = ReglaMesDeCobro.DiaEnColombia(ahora).AddDays(configuracion.DiasDeGracia);

        var pendientes = await _cobros.ListarClubesSinCobroAsync(periodo, cancelacion);
        if (pendientes.Count == 0)
        {
            return 0;
        }

        var conteos = await _conteo.ContarPorRolDeClubesAsync(pendientes, cancelacion);
        var creados = 0;
        foreach (var clubId in pendientes)
        {
            var calculo = ReglaCobroPlataforma.Calcular(conteos[clubId], configuracion.Rangos);
            if (calculo is null)
            {
                continue;
            }

            if (await GuardarAsync(clubId, periodo, calculo, configuracion.DiasDeGracia, suspendeDesde, ahora, cancelacion))
            {
                creados++;
            }
        }

        return creados;
    }

    private async Task<bool> GuardarAsync(
        Guid clubId,
        DateOnly periodo,
        CalculoDeCobro calculo,
        int diasDeGracia,
        DateOnly suspendeDesde,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        try
        {
            await _unidadDeTrabajo.EnTransaccionAsync(
                async () =>
                {
                    _cobros.Agregar(new CobroPlataforma
                    {
                        ClubId = clubId,
                        Periodo = periodo,
                        Jugadores = calculo.Conteo.Jugadores,
                        Entrenadores = calculo.Conteo.Entrenadores,
                        Directivos = calculo.Conteo.Directivos,
                        Presidentes = calculo.Conteo.Presidentes,
                        RangoDesde = calculo.Rango.Desde,
                        RangoHasta = calculo.Rango.Hasta,
                        Total = calculo.Total,
                        GeneradoEn = ahora,
                        DiasDeGracia = diasDeGracia,
                        SuspendeDesde = suspendeDesde,
                    });
                    await _unidadDeTrabajo.GuardarAsync(cancelacion);
                },
                cancelacion);
            return true;
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out _))
        {
            // Otra instancia ya creó el cobro de este club y periodo: no es un error.
            return false;
        }
    }
}
