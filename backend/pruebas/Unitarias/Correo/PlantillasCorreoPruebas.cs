using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Correo;

namespace LFClub.Pruebas.Unitarias.Correo;

public class PlantillasCorreoPruebas
{
    private const string UrlBase = "http://localhost:5173";
    private const string Club = "Valfor F.C.";
    private const string Token = "token-de-prueba";

    [Fact]
    public void La_invitacion_de_presidente_nombra_su_rol()
    {
        var mensaje = PlantillasCorreo.Invitacion(UrlBase, Club, Rol.PRESIDENTE, Token);

        Assert.Contains("como presidente", mensaje.Texto);
        Assert.DoesNotContain("revisará", mensaje.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Club, mensaje.Texto);
        Assert.Equal($"{UrlBase}/invitacion#{Token}", mensaje.Enlace);
        Assert.Contains(mensaje.Enlace, mensaje.Texto);
    }

    [Theory]
    [InlineData(Rol.JUGADOR, "jugador")]
    [InlineData(Rol.ENTRENADOR, "entrenador")]
    [InlineData(Rol.DIRECTIVO, "directivo")]
    public void La_invitacion_del_club_nombra_el_club_y_el_rol_y_no_anuncia_ninguna_revision(Rol rol, string nombre)
    {
        var mensaje = PlantillasCorreo.Invitacion($"{UrlBase}/", Club, rol, Token);

        Assert.Contains(Club, mensaje.Texto);
        Assert.Contains(Club, mensaje.Asunto);
        Assert.Equal($"{UrlBase}/invitacion#{Token}", mensaje.Enlace);
        Assert.Contains(mensaje.Enlace, mensaje.Texto);

        // Nombra su rol y ningún otro, y ya no dice que el club revisará el ingreso (RF-006).
        Assert.Contains($"como {nombre}", mensaje.Texto);
        foreach (var otro in new[] { "jugador", "presidente", "entrenador", "directivo", "desarrollador" }.Where(otro => otro != nombre))
        {
            Assert.DoesNotContain(otro, mensaje.Texto, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("revisará", mensaje.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Las_dos_invitaciones_avisan_de_que_el_enlace_sirve_una_vez_y_vence()
    {
        foreach (var rol in new[] { Rol.PRESIDENTE, Rol.JUGADOR })
        {
            var mensaje = PlantillasCorreo.Invitacion(UrlBase, Club, rol, Token);

            Assert.Contains("una sola vez", mensaje.Texto);
            Assert.Contains("7 días", mensaje.Texto);
        }
    }
}
