using System.Collections.Concurrent;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Sustituye el servicio de correo en las pruebas: guarda lo enviado para poder leer los tokens de
/// los enlaces y permite simular que el envío falla.
/// </summary>
public class CorreoEnMemoria : IServicioCorreo
{
    private readonly ConcurrentQueue<CorreoEnviado> _enviados = new();

    /// <summary>Mientras sea verdadero, todo envío falla.</summary>
    public bool FallarEnvios { get; set; }

    public IReadOnlyList<CorreoEnviado> Enviados => [.. _enviados];

    public Task<bool> EnviarInvitacionAsync(
        string correo, string nombreClub, Rol rol, string token, CancellationToken cancelacion = default) =>
        Registrar(new CorreoEnviado("invitacion", correo, token, nombreClub, rol));

    public Task<bool> EnviarRecuperacionAsync(string correo, string token, CancellationToken cancelacion = default) =>
        Registrar(new CorreoEnviado("recuperacion", correo, token, null, null));

    /// <summary>Token del último correo de ese tipo enviado a esa dirección.</summary>
    public string UltimoToken(string tipo, string correo) =>
        Enviados.Last(enviado => enviado.Tipo == tipo && EsPara(enviado, correo)).Token;

    public int Contar(string tipo, string correo) =>
        Enviados.Count(enviado => enviado.Tipo == tipo && EsPara(enviado, correo));

    private static bool EsPara(CorreoEnviado enviado, string correo) =>
        string.Equals(enviado.Destinatario, correo.Trim(), StringComparison.OrdinalIgnoreCase);

    private Task<bool> Registrar(CorreoEnviado enviado)
    {
        if (FallarEnvios)
        {
            return Task.FromResult(false);
        }

        _enviados.Enqueue(enviado);
        return Task.FromResult(true);
    }
}

public record CorreoEnviado(string Tipo, string Destinatario, string Token, string? NombreClub, Rol? Rol);
