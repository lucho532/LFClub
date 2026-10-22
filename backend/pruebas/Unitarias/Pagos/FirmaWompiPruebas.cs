using LFClub.Infraestructura.Pagos;

namespace LFClub.Pruebas.Unitarias.Pagos;

public class FirmaWompiPruebas
{
    // Vectores calculados aparte con SHA-256 y secretos inventados para la prueba.
    private const string FirmaDeIntegridadEsperada = "4f18f92dc61aeff50dcb5f6fe1b64a6962d7dcf6567ab9d488190e4ecfb178c3";
    private const string ChecksumDeEventoEsperado = "0986e67b5575ae94cc4b514169957065d028bb6d0744d618fbe1c02321e25a14";

    [Fact]
    public void La_firma_de_integridad_es_el_sha256_de_referencia_valor_moneda_y_secreto()
    {
        var firma = FirmaWompi.Integridad("LFC-abc12345", 4500000, "COP", "secreto-de-integridad-de-prueba");

        Assert.Equal(FirmaDeIntegridadEsperada, firma);
    }

    [Fact]
    public void El_checksum_del_evento_concatena_los_valores_la_marca_de_tiempo_y_el_secreto()
    {
        var checksum = FirmaWompi.ChecksumDeEvento(
            ["1234-5678", "APPROVED", "3000000"], "1530291411", "secreto-de-eventos-de-prueba");

        Assert.Equal(ChecksumDeEventoEsperado, checksum);
    }

    [Fact]
    public void Un_valor_con_un_solo_digito_cambiado_no_coincide()
    {
        var alterado = FirmaWompi.ChecksumDeEvento(
            ["1234-5678", "APPROVED", "3000001"], "1530291411", "secreto-de-eventos-de-prueba");

        Assert.False(FirmaWompi.SonIguales(ChecksumDeEventoEsperado, alterado));
    }

    [Fact]
    public void La_comparacion_ignora_mayusculas_y_no_se_rompe_con_cadenas_de_otro_largo()
    {
        Assert.True(FirmaWompi.SonIguales(ChecksumDeEventoEsperado, ChecksumDeEventoEsperado.ToUpperInvariant()));
        Assert.False(FirmaWompi.SonIguales(ChecksumDeEventoEsperado, "abc"));
        Assert.False(FirmaWompi.SonIguales(ChecksumDeEventoEsperado, string.Empty));
    }
}
