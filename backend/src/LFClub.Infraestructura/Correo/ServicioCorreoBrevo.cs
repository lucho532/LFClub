using System.Net.Http.Json;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Enumeraciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LFClub.Infraestructura.Correo;

/// <summary>
/// Representa el envío de correos con la API transaccional de Brevo, por HTTP y sin su SDK
/// (constitución §6.3, research §9).
/// Su responsabilidad es enviar el correo y decir si Brevo lo aceptó.
/// No reintenta, no encola y nunca escribe el enlace ni el token en el registro.
/// </summary>
public class ServicioCorreoBrevo : IServicioCorreo
{
    private const string UrlEnvio = "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _http;
    private readonly OpcionesCorreo _opciones;
    private readonly ILogger<ServicioCorreoBrevo> _registro;

    /// <summary>Crea el servicio con su cliente HTTP y la configuración del correo.</summary>
    public ServicioCorreoBrevo(
        HttpClient http, IOptions<OpcionesCorreo> opciones, ILogger<ServicioCorreoBrevo> registro)
    {
        _http = http;
        _opciones = opciones.Value;
        _registro = registro;
    }

    /// <inheritdoc />
    public Task<bool> EnviarInvitacionAsync(
        string correo, string nombreClub, Rol rol, string token, CancellationToken cancelacion = default) =>
        EnviarAsync(correo, PlantillasCorreo.Invitacion(_opciones.UrlBaseFrontend, nombreClub, rol, token), cancelacion);

    /// <inheritdoc />
    public Task<bool> EnviarRecuperacionAsync(string correo, string token, CancellationToken cancelacion = default) =>
        EnviarAsync(correo, PlantillasCorreo.Recuperacion(_opciones.UrlBaseFrontend, token), cancelacion);

    private async Task<bool> EnviarAsync(string correo, MensajeCorreo mensaje, CancellationToken cancelacion)
    {
        try
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, UrlEnvio);
            peticion.Headers.Add("api-key", _opciones.Llave);
            peticion.Content = JsonContent.Create(new
            {
                sender = new { email = _opciones.Remitente, name = "LFClub" },
                to = new[] { new { email = correo } },
                subject = mensaje.Asunto,
                textContent = mensaje.Texto,
            });

            using var respuesta = await _http.SendAsync(peticion, cancelacion);
            if (respuesta.IsSuccessStatusCode)
            {
                return true;
            }

            _registro.LogWarning("Brevo rechazó un correo con el estado {Estado}", (int)respuesta.StatusCode);
            return false;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            _registro.LogWarning(error, "No se pudo enviar un correo con Brevo");
            return false;
        }
    }
}
