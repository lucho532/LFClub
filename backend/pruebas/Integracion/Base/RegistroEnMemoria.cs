using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Proveedor de registro de eventos de las pruebas: guarda en memoria cada mensaje (con su excepción)
/// que escribe la API, para comprobar que no contiene secretos ni cuerpos de notificaciones.
/// </summary>
public sealed class RegistroEnMemoria : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _mensajes = new();

    /// <summary>Todo lo escrito hasta ahora.</summary>
    public IReadOnlyCollection<string> Mensajes => [.. _mensajes];

    public ILogger CreateLogger(string categoria) => new Registrador(_mensajes);

    public void Dispose()
    {
    }

    private sealed class Registrador(ConcurrentQueue<string> mensajes) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState estado)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel nivel) => true;

        public void Log<TState>(
            LogLevel nivel, EventId evento, TState estado, Exception? excepcion, Func<TState, Exception?, string> formato)
        {
            mensajes.Enqueue(formato(estado, excepcion) + (excepcion is null ? string.Empty : " " + excepcion));
        }
    }
}
