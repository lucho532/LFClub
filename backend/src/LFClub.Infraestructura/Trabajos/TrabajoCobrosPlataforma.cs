using LFClub.Aplicacion.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LFClub.Infraestructura.Trabajos;

/// <summary>
/// Representa el trabajo en segundo plano del cobro de la plataforma (spec 017, research §7).
/// Su responsabilidad es, al arrancar la API y cada 30 minutos, llamar a
/// <see cref="IServicioCobrosPlataforma"/> para generar los cobros del mes y después a
/// <see cref="IServicioSuspensionPorImpago"/> para suspender a los clubes vencidos. Cada paso va en su
/// propio <c>try</c>: un fallo del primero no impide el segundo. Es seguro repetirlo y correrlo en varias
/// instancias; captura y registra los errores, solo con cantidades, sin tumbar la API.
/// No decide qué se cobra ni a quién se suspende: es solo el reloj.
/// </summary>
public class TrabajoCobrosPlataforma : BackgroundService
{
    /// <summary>Cada cuánto se repite el trabajo.</summary>
    public static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _ambitos;
    private readonly ILogger<TrabajoCobrosPlataforma> _registro;

    /// <summary>Crea el trabajo con la fábrica de ámbitos, porque los servicios viven por petición.</summary>
    public TrabajoCobrosPlataforma(IServiceScopeFactory ambitos, ILogger<TrabajoCobrosPlataforma> registro)
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
            await EjecutarAsync<IServicioCobrosPlataforma>(
                servicio => servicio.GenerarAsync(parada), "Se generaron {Cantidad} cobros de la plataforma.", parada);
            await EjecutarAsync<IServicioSuspensionPorImpago>(
                servicio => servicio.AplicarAsync(parada), "Se suspendieron {Cantidad} clubes por impago.", parada);
        }
        while (await EsperarAsync(temporizador, parada));
    }

    private async Task EjecutarAsync<TServicio>(
        Func<TServicio, Task<int>> paso, string mensaje, CancellationToken parada)
        where TServicio : notnull
    {
        try
        {
            await using var ambito = _ambitos.CreateAsyncScope();
            var cantidad = await paso(ambito.ServiceProvider.GetRequiredService<TServicio>());
            if (cantidad > 0)
            {
                _registro.LogInformation(mensaje, cantidad);
            }
        }
        catch (OperationCanceledException) when (parada.IsCancellationRequested)
        {
            // La API se está apagando.
        }
        catch (Exception error)
        {
            _registro.LogError(error, "Falló un paso del cobro de la plataforma; se reintenta en el próximo ciclo.");
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
