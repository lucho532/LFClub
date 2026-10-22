using System.Globalization;
using System.Text.Json;
using LFClub.Aplicacion.Interfaces;

namespace LFClub.Infraestructura.Pagos;

/// <summary>
/// Representa la pasarela Wompi detrás de <see cref="IPasarelaDePago"/> (spec 017, research §2).
/// Su responsabilidad es armar la dirección del checkout con la firma de integridad y comprobar el
/// checksum de las notificaciones <c>transaction.updated</c> contra el secreto de eventos y, si
/// viene, contra la cabecera <c>X-Event-Checksum</c>.
/// No hace llamadas salientes a Wompi, no escribe secretos ni cuerpos en el registro y no decide si
/// un cobro está pagado.
/// </summary>
public class PasarelaWompi : IPasarelaDePago
{
    private const string Moneda = "COP";

    private readonly OpcionesWompi _opciones;

    /// <summary>Crea la pasarela con su configuración.</summary>
    public PasarelaWompi(OpcionesWompi opciones)
    {
        _opciones = opciones;
    }

    /// <inheritdoc />
    public bool Disponible => _opciones.Completa;

    /// <inheritdoc />
    public Uri? CrearEnlaceDePago(SolicitudDePago solicitud)
    {
        if (!Disponible)
        {
            return null;
        }

        var centavos = (long)(solicitud.ValorEnPesos * 100);
        var firma = FirmaWompi.Integridad(solicitud.Referencia, centavos, Moneda, _opciones.SecretoIntegridad);
        var baseUrl = string.IsNullOrWhiteSpace(_opciones.UrlCheckout)
            ? OpcionesWompi.UrlCheckoutPorDefecto
            : _opciones.UrlCheckout;

        var consulta = string.Join(
            "&",
            Par("public-key", _opciones.LlavePublica),
            Par("currency", Moneda),
            Par("amount-in-cents", centavos.ToString(CultureInfo.InvariantCulture)),
            Par("reference", solicitud.Referencia),
            Par("signature:integrity", firma),
            Par("redirect-url", solicitud.DireccionDeRetorno));

        return new Uri($"{baseUrl}?{consulta}");
    }

    /// <inheritdoc />
    public NotificacionVerificada? VerificarNotificacion(string cuerpo, string? checksumDeLaCabecera)
    {
        if (!Disponible)
        {
            return null;
        }

        try
        {
            using var documento = JsonDocument.Parse(cuerpo);
            var raiz = documento.RootElement;

            if (!TieneFirmaValida(raiz, checksumDeLaCabecera)
                || !raiz.TryGetProperty("data", out var datos)
                || !datos.TryGetProperty("transaction", out var transaccion))
            {
                return null;
            }

            var id = Texto(transaccion, "id");
            var estado = Texto(transaccion, "status");
            var referencia = Texto(transaccion, "reference");
            var centavos = transaccion.TryGetProperty("amount_in_cents", out var monto) && monto.TryGetInt64(out var valor)
                ? valor
                : (long?)null;

            if (id is null || estado is null || referencia is null || centavos is null)
            {
                return null;
            }

            return new NotificacionVerificada(
                id, estado, referencia, centavos.Value / 100m, Texto(transaccion, "payment_method_type"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private bool TieneFirmaValida(JsonElement raiz, string? checksumDeLaCabecera)
    {
        if (!raiz.TryGetProperty("signature", out var firma)
            || !firma.TryGetProperty("properties", out var propiedades)
            || propiedades.ValueKind != JsonValueKind.Array
            || Texto(firma, "checksum") is not { } checksumDelCuerpo
            || !raiz.TryGetProperty("data", out var datos)
            || raiz.TryGetProperty("timestamp", out var marca) is false)
        {
            return false;
        }

        var valores = new List<string>();
        foreach (var propiedad in propiedades.EnumerateArray())
        {
            if (propiedad.GetString() is not { } ruta || ValorEnRuta(datos, ruta) is not { } valor)
            {
                return false;
            }

            valores.Add(valor);
        }

        var esperado = FirmaWompi.ChecksumDeEvento(valores, ComoTexto(marca) ?? string.Empty, _opciones.SecretoEventos);
        var coincideElCuerpo = FirmaWompi.SonIguales(esperado, checksumDelCuerpo);
        var coincideLaCabecera = string.IsNullOrWhiteSpace(checksumDeLaCabecera)
            || FirmaWompi.SonIguales(esperado, checksumDeLaCabecera);

        return coincideElCuerpo && coincideLaCabecera;
    }

    private static string? ValorEnRuta(JsonElement datos, string ruta)
    {
        var actual = datos;
        foreach (var parte in ruta.Split('.'))
        {
            if (actual.ValueKind != JsonValueKind.Object || !actual.TryGetProperty(parte, out actual))
            {
                return null;
            }
        }

        return ComoTexto(actual);
    }

    private static string? Texto(JsonElement elemento, string nombre) =>
        elemento.ValueKind == JsonValueKind.Object && elemento.TryGetProperty(nombre, out var valor)
            ? ComoTexto(valor)
            : null;

    private static string? ComoTexto(JsonElement valor) => valor.ValueKind switch
    {
        JsonValueKind.String => valor.GetString(),
        JsonValueKind.Number => valor.GetRawText(),
        _ => null,
    };

    private static string Par(string nombre, string valor) =>
        $"{nombre}={Uri.EscapeDataString(valor)}";
}
