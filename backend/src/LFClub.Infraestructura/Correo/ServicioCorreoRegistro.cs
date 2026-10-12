using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Enumeraciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LFClub.Infraestructura.Correo;

/// <summary>
/// Representa el servicio de correo de desarrollo, que se usa cuando no hay llave de Brevo.
/// Su responsabilidad es escribir cada correo, con su enlace, en el registro de la API para poder
/// probar todo el flujo sin una cuenta de Brevo (research §9).
/// No envía nada y no debe usarse en producción: el registro contiene enlaces de un solo uso.
/// </summary>
public class ServicioCorreoRegistro : IServicioCorreo
{
    private readonly OpcionesCorreo _opciones;
    private readonly ILogger<ServicioCorreoRegistro> _registro;

    /// <summary>Crea el servicio con la configuración del correo.</summary>
    public ServicioCorreoRegistro(IOptions<OpcionesCorreo> opciones, ILogger<ServicioCorreoRegistro> registro)
    {
        _opciones = opciones.Value;
        _registro = registro;
    }

    /// <inheritdoc />
    public Task<bool> EnviarInvitacionAsync(
        string correo, string nombreClub, Rol rol, string token, CancellationToken cancelacion = default) =>
        Registrar(correo, PlantillasCorreo.Invitacion(_opciones.UrlBaseFrontend, nombreClub, rol, token));

    /// <inheritdoc />
    public Task<bool> EnviarRecuperacionAsync(string correo, string token, CancellationToken cancelacion = default) =>
        Registrar(correo, PlantillasCorreo.Recuperacion(_opciones.UrlBaseFrontend, token));

    private Task<bool> Registrar(string correo, MensajeCorreo mensaje)
    {
        _registro.LogInformation(
            "Correo para {Destinatario} | {Asunto} | {Enlace}", correo, mensaje.Asunto, mensaje.Enlace);
        return Task.FromResult(true);
    }
}
