using LFClub.Infraestructura.Seguridad;

namespace LFClub.Pruebas.Unitarias.Seguridad;

public class HashContrasenaPruebas
{
    private readonly HashContrasena _hash = new();

    [Fact]
    public void El_hash_no_contiene_la_contrasena()
    {
        var hash = _hash.Calcular("contrasena-segura");

        Assert.DoesNotContain("contrasena-segura", hash);
    }

    [Fact]
    public void Verifica_la_contrasena_correcta() =>
        Assert.True(_hash.Verificar(_hash.Calcular("contrasena-segura"), "contrasena-segura"));

    [Theory]
    [InlineData("contrasena-segurA")]
    [InlineData(" contrasena-segura")]
    [InlineData("contrasena-segura ")]
    [InlineData("")]
    public void Rechaza_una_contrasena_distinta_sin_normalizarla(string otra) =>
        Assert.False(_hash.Verificar(_hash.Calcular("contrasena-segura"), otra));

    [Fact]
    public void Dos_hashes_de_la_misma_contrasena_son_distintos() =>
        Assert.NotEqual(_hash.Calcular("contrasena-segura"), _hash.Calcular("contrasena-segura"));

    [Fact]
    public void Un_hash_mal_formado_no_verifica() => Assert.False(_hash.Verificar("no-es-un-hash", "x"));
}
