using System.Web;
using LFClub.Aplicacion.Interfaces;
using LFClub.Infraestructura.Pagos;

namespace LFClub.Pruebas.Unitarias.Pagos;

public class PasarelaWompiPruebas
{
    private const string SecretoDeEventos = "secreto-de-eventos-de-prueba";

    private static PasarelaWompi Pasarela(bool completa = true) => new(new OpcionesWompi
    {
        LlavePublica = completa ? "llave-publica-de-prueba" : string.Empty,
        SecretoIntegridad = completa ? "secreto-de-integridad-de-prueba" : string.Empty,
        SecretoEventos = completa ? SecretoDeEventos : string.Empty,
    });

    private static string Evento(string estado = "APPROVED", long centavos = 3000000, string? checksum = null)
    {
        var verdadero = FirmaWompi.ChecksumDeEvento(["1234-5678", estado, centavos.ToString()], "1530291411", SecretoDeEventos);
        return "{\"event\":\"transaction.updated\",\"data\":{\"transaction\":{\"id\":\"1234-5678\",\"status\":\""
            + estado + "\",\"amount_in_cents\":" + centavos
            + ",\"reference\":\"LFC-abc\",\"payment_method_type\":\"PSE\"}},"
            + "\"signature\":{\"properties\":[\"transaction.id\",\"transaction.status\",\"transaction.amount_in_cents\"],"
            + "\"checksum\":\"" + (checksum ?? verdadero) + "\"},\"timestamp\":1530291411}";
    }

    [Fact]
    public void El_enlace_lleva_la_llave_publica_el_valor_en_centavos_la_referencia_y_la_firma()
    {
        var enlace = Pasarela().CrearEnlaceDePago(new SolicitudDePago("LFC-abc12345", 45000m, "http://localhost/retorno?cobro=1"));

        Assert.NotNull(enlace);
        var consulta = HttpUtility.ParseQueryString(enlace.Query);
        Assert.Equal("llave-publica-de-prueba", consulta["public-key"]);
        Assert.Equal("4500000", consulta["amount-in-cents"]);
        Assert.Equal("COP", consulta["currency"]);
        Assert.Equal("LFC-abc12345", consulta["reference"]);
        Assert.Equal("http://localhost/retorno?cobro=1", consulta["redirect-url"]);
        Assert.Equal(
            FirmaWompi.Integridad("LFC-abc12345", 4500000, "COP", "secreto-de-integridad-de-prueba"),
            consulta["signature:integrity"]);
        Assert.DoesNotContain("secreto", enlace.ToString());
    }

    [Fact]
    public void Sin_configuracion_completa_no_esta_disponible_y_no_arma_enlaces()
    {
        var pasarela = Pasarela(completa: false);

        Assert.False(pasarela.Disponible);
        Assert.Null(pasarela.CrearEnlaceDePago(new SolicitudDePago("LFC-abc", 1000m, "http://x")));
        Assert.Null(pasarela.VerificarNotificacion(Evento(), null));
    }

    [Fact]
    public void Una_notificacion_bien_firmada_se_lee()
    {
        var notificacion = Pasarela().VerificarNotificacion(Evento(), null);

        Assert.NotNull(notificacion);
        Assert.Equal("1234-5678", notificacion.TransaccionId);
        Assert.Equal("APPROVED", notificacion.Estado);
        Assert.Equal("LFC-abc", notificacion.Referencia);
        Assert.Equal(30000m, notificacion.ValorEnPesos);
        Assert.Equal("PSE", notificacion.MedioDePago);
    }

    [Fact]
    public void Una_notificacion_con_el_checksum_de_la_cabecera_distinto_se_rechaza()
    {
        var verdadero = FirmaWompi.ChecksumDeEvento(["1234-5678", "APPROVED", "3000000"], "1530291411", SecretoDeEventos);

        Assert.NotNull(Pasarela().VerificarNotificacion(Evento(), verdadero));
        Assert.Null(Pasarela().VerificarNotificacion(Evento(), new string('a', 64)));
    }

    [Fact]
    public void Una_notificacion_con_el_valor_alterado_o_el_checksum_falso_se_rechaza()
    {
        var firmadaConOtroValor = Evento(centavos: 3000000).Replace("3000000", "100");

        Assert.Null(Pasarela().VerificarNotificacion(firmadaConOtroValor, null));
        Assert.Null(Pasarela().VerificarNotificacion(Evento(checksum: new string('0', 64)), null));
        Assert.Null(Pasarela().VerificarNotificacion("no es json", null));
        Assert.Null(Pasarela().VerificarNotificacion("{}", null));
    }
}
