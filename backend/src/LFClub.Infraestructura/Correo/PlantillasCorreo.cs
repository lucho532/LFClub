using LFClub.Dominio.Enumeraciones;

namespace LFClub.Infraestructura.Correo;

/// <summary>
/// Representa los textos en español de los correos de la plataforma.
/// Su responsabilidad es redactar la invitación a un club y la recuperación de contraseña, con su
/// enlace. El token va en el fragmento del enlace para que no quede en los registros del servidor
/// web (research §8).
/// No envía correos ni guarda tokens.
/// </summary>
public static class PlantillasCorreo
{
    /// <summary>
    /// Redacta la invitación para registrarse en un club. Nombra siempre el club y el rol con el
    /// que entra la persona, y no anuncia ninguna revisión: quien se registra entra directamente
    /// (RF-006).
    /// </summary>
    public static MensajeCorreo Invitacion(string urlBaseFrontend, string nombreClub, Rol rol, string token)
    {
        var enlace = $"{urlBaseFrontend.TrimEnd('/')}/invitacion#{token}";
        var invitacion = rol == Rol.PRESIDENTE
            ? $"Te invitaron a LFClub como presidente de {nombreClub}.\n\n" +
              $"Para aceptar la invitación, abre este enlace:\n{enlace}\n\n"
            : $"{nombreClub} te invita a registrarte en LFClub como {NombreDelRol(rol)}.\n\n" +
              $"Para aceptar la invitación, abre este enlace:\n{enlace}\n\n";
        var texto =
            $"Hola:\n\n" +
            invitacion +
            "El enlace sirve una sola vez y vence en 7 días. Si no esperabas este correo, ignóralo.";

        return new MensajeCorreo($"Invitación a {nombreClub} en LFClub", texto, enlace);
    }

    private static string NombreDelRol(Rol rol) => rol switch
    {
        Rol.JUGADOR => "jugador",
        Rol.ENTRENADOR => "entrenador",
        Rol.DIRECTIVO => "directivo",
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "Una invitación del club no lleva ese rol."),
    };

    /// <summary>Redacta el correo de recuperación de contraseña.</summary>
    public static MensajeCorreo Recuperacion(string urlBaseFrontend, string token)
    {
        var enlace = $"{urlBaseFrontend.TrimEnd('/')}/restablecer#{token}";
        var texto =
            $"Hola:\n\n" +
            $"Para crear una contraseña nueva en LFClub, abre este enlace:\n{enlace}\n\n" +
            "El enlace sirve una sola vez y vence en 60 minutos. Si no lo pediste, ignora este correo: " +
            "tu contraseña no cambia.";

        return new MensajeCorreo("Crea tu contraseña de LFClub", texto, enlace);
    }
}
