using LFClub.Aplicacion.Utilidades;

namespace LFClub.Pruebas.Unitarias.Utilidades;

public class GeneradorTokensPruebas
{
    [Fact]
    public void Nuevo_devuelve_un_token_base64url_de_32_bytes()
    {
        var (token, _) = GeneradorTokens.Nuevo();

        Assert.Equal(43, token.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", token);
    }

    [Fact]
    public void Nuevo_devuelve_el_hash_del_token_en_hexadecimal_de_64_caracteres()
    {
        var (token, hash) = GeneradorTokens.Nuevo();

        Assert.Matches("^[0-9a-f]{64}$", hash);
        Assert.Equal(GeneradorTokens.Hash(token), hash);
        Assert.NotEqual(token, hash);
    }

    [Fact]
    public void Nuevo_no_repite_tokens() =>
        Assert.NotEqual(GeneradorTokens.Nuevo().Token, GeneradorTokens.Nuevo().Token);

    [Fact]
    public void Hash_es_el_sha256_conocido() =>
        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            GeneradorTokens.Hash("abc"));
}
