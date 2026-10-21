using LFClub.Aplicacion.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LFClub.Infraestructura.Trabajos;

/// <summary>
/// Representa el trabajo en segundo plano que genera las mensualidades (spec 015, RF-005).
/// Su responsabilidad es llamar a <see cref="IServicioGeneracionCargos"/> al arrancar la API y cada
/// 30 minutos, de modo que el primer arranque de cada mes en Colombia cree los cargos aunque la API
/// estuviera caída el día 1. Es seguro repetirlo y correrlo en varias instancias a la vez: la
/// generación es idempotente por el índice único de jugador y mes. Captura y registra cualquier
/// error sin tumbar la API; el siguiente ciclo lo reintenta.
/// No decide qué cargos nacen ni calcula nada: es solo el reloj que dispara la generación.
/// </summary>
public class TrabajoCargosMensuales : BackgroundService
{
    /// <summary>Cada cuánto se repite la generación.</summary>
    public static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _ambitos;
    private readonly ILogger<TrabajoCargosMensuales> _registro;

    /// <summary>Crea el trabajo con la fábrica de ámbitos, porque el servicio vive por petición.</summary>
    public TrabajoCargosMensuales(IServiceScopeFactory ambitos, ILogger<TrabajoCargosMensuales> registro)
    {
        _ambitos = ambitos;
        _registro = registro;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var temporizador = new PeriodicTimer(Intervalo);
        do
        {
            await GenerarAsync(parada);
        }
        while (await EsperarAsync(temporizador, parada));
    }

    private async Task GenerarAsync(CancellationToken parada)
    {
        try
        {
            await using var ambito = _ambitos.CreateAsyncScope();
            var creados = await ambito.ServiceProvider.GetRequiredService<IServicioGeneracionCargos>()
                .GenerarAsync(parada);
            if (creados > 0)
            {
                _registro.LogInformation("Se generaron {Creados} cargos de mensualidad.", creados);
            }
        }
        catch (OperationCanceledException) when (parada.IsCancellationRequested)
        {
            // La API se está apagando.
        }
        catch (Exception error)
        {
            _registro.LogError(error, "No se pudieron generar las mensualidades; se reintenta en el próximo ciclo.");
        }
    }

    private static async Task<bool> EsperarAsync(PeriodicTimer temporizador, CancellationToken parada)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(parada);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
