using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Infraestructura.Imagenes;

/// <summary>
/// Representa el plano del resguardo de pago (spec 015, research §14) en unidades del lienzo de
/// 1080 × 1350: las medidas y las cajas de cada capa, los tamaños de letra, los colores del panel y los
/// textos con su formato.
/// Su responsabilidad es fijar dónde va cada cosa y cómo se escribe; el resguardo lleva un panel claro
/// de texto oscuro para que se lea con cualquier color de club, y solo el encabezado se escribe sobre el
/// degradado del club, con el color de texto de <see cref="ContrasteDeColor"/>.
/// No dibuja nada: lo hace <see cref="GeneradorImagenResguardo"/>.
/// </summary>
public static class PlanoResguardo
{
    /// <summary>Ancho del lienzo.</summary>
    public const int Ancho = 1080;

    /// <summary>Alto del lienzo.</summary>
    public const int Alto = 1350;

    /// <summary>Radio de las esquinas del lienzo.</summary>
    public const float Radio = 48;

    /// <summary>Radio de las esquinas del panel.</summary>
    public const float RadioDelPanel = 32;

    /// <summary>Caja del escudo, arriba a la izquierda.</summary>
    public static readonly Caja Escudo = new(60, 56, 150, 150);

    /// <summary>Caja del nombre del club, a la derecha del escudo.</summary>
    public static readonly Caja NombreDelClub = new(236, 56, 784, 150);

    /// <summary>Caja del nombre del club cuando no hay escudo: ocupa todo el ancho.</summary>
    public static readonly Caja NombreDelClubSinEscudo = new(60, 56, 960, 150);

    /// <summary>Panel claro con el recibo.</summary>
    public static readonly Caja Panel = new(48, 240, 984, 1062);

    /// <summary>Caja del título "Resguardo de pago n.º …".</summary>
    public static readonly Caja Titulo = new(96, 268, 888, 70);

    /// <summary>Caja del valor del pago.</summary>
    public static readonly Caja Valor = new(96, 340, 888, 170);

    /// <summary>Ancho disponible para el texto de cada fila.</summary>
    public const float AnchoDeFila = 888;

    /// <summary>Margen izquierdo del texto del panel.</summary>
    public const float MargenDeFila = 96;

    /// <summary>Línea de cada fila: arriba va su etiqueta y debajo su valor. Cada una con su alto.</summary>
    public static readonly (float Y, float Alto)[] Filas =
    [
        (530, 116),
        (646, 116),
        (762, 116),
        (878, 172),
        (1050, 116),
        (1166, 116),
    ];

    /// <summary>Tamaño de la etiqueta de cada fila.</summary>
    public const float LetraEtiqueta = 34;

    /// <summary>Tamaño del valor de cada fila.</summary>
    public const float LetraFila = 50;

    /// <summary>Tamaño del título.</summary>
    public const float LetraTitulo = 48;

    /// <summary>Tamaño del valor del pago.</summary>
    public const float LetraValor = 150;

    /// <summary>Tamaño del nombre del club.</summary>
    public const float LetraClub = 64;

    /// <summary>Líneas que ocupa como mucho lo que cubre el pago.</summary>
    public const int LineasDeConcepto = 2;

    /// <summary>Color del panel.</summary>
    public const string ColorDelPanel = "#FFFFFF";

    /// <summary>Color del texto del panel (casi negro; contraste 17:1 sobre el panel).</summary>
    public const string ColorDelTexto = "#141414";

    /// <summary>Color de las etiquetas (gris oscuro; contraste 6,5:1 sobre el panel).</summary>
    public const string ColorDeLaEtiqueta = "#5E5650";

    /// <summary>Color del sello ANULADO.</summary>
    public const string ColorDelSello = "#A11D1D";

    /// <summary>Inclinación del sello ANULADO, en grados.</summary>
    public const float AnguloDelSello = -22;

    /// <summary>Tamaño de la letra del sello.</summary>
    public const float LetraDelSello = 210;

    /// <summary>Opacidad del sello.</summary>
    public const double OpacidadDelSello = 0.85;

    /// <summary>"$ 65.000", con puntos de miles y sin decimales.</summary>
    public static string Pesos(decimal valor) => "$ " + TextoDePesos.ConPuntos(valor);

    /// <summary>"Resguardo de pago n.º 12".</summary>
    public static string TextoDelTitulo(int numero) => $"RESGUARDO DE PAGO N.º {numero}";

    /// <summary>"Efectivo" o "Transferencia (Nequi)".</summary>
    public static string TextoDelMedio(MedioPago medio, string? entidad) => medio == MedioPago.EFECTIVO
        ? "Efectivo"
        : string.IsNullOrWhiteSpace(entidad) ? "Transferencia" : $"Transferencia ({entidad})";

    /// <summary>"Al día" si no queda saldo, o "Saldo pendiente: $ 40.000".</summary>
    public static string TextoDelSaldo(decimal saldoTrasPago) =>
        saldoTrasPago <= 0m ? "Al día" : $"Saldo pendiente: {Pesos(saldoTrasPago)}";

    /// <summary>"13 de octubre de 2026".</summary>
    public static string TextoDeLaFecha(DateOnly fecha) => $"{fecha.Day} de {TextoDePesos.Meses[fecha.Month - 1]} de {fecha.Year}";
}
