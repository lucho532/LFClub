using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Infraestructura.Imagenes;

/// <summary>
/// Representa el plano común de la carta (contracts/carta.md) en unidades del lienzo de 750 × 1050:
/// los mismos números que <c>plano.ts</c> en la pantalla.
/// Su responsabilidad es fijar el lienzo, las cajas de cada capa, los tamaños de letra, los colores
/// de la rareza, las abreviaturas de la posición y cómo se ajusta el nombre.
/// No dibuja nada: lo hace <see cref="GeneradorImagenCarta"/>. Un cambio de diseño se hace primero
/// en el contrato y después aquí y en la pantalla.
/// </summary>
public static class PlanoCarta
{
    /// <summary>Ancho del lienzo.</summary>
    public const int Ancho = 750;

    /// <summary>Alto del lienzo.</summary>
    public const int Alto = 1050;

    /// <summary>Radio de las esquinas.</summary>
    public const float Radio = 48;

    /// <summary>Grosor del marco, hacia dentro.</summary>
    public const float Marco = 18;

    /// <summary>Caja de la valoración.</summary>
    public static readonly Caja Valoracion = new(60, 70, 160, 160);

    /// <summary>Caja de la posición, debajo de la valoración.</summary>
    public static readonly Caja Posicion = new(60, 235, 160, 50);

    /// <summary>Caja del escudo.</summary>
    public static readonly Caja Escudo = new(70, 300, 130, 130);

    /// <summary>Caja de la foto o de la figura genérica.</summary>
    public static readonly Caja Foto = new(260, 60, 430, 460);

    /// <summary>Caja del dorsal, en la esquina inferior derecha de la foto.</summary>
    public static readonly Caja Dorsal = new(590, 440, 100, 80);

    /// <summary>Caja del nombre.</summary>
    public static readonly Caja Nombre = new(60, 540, 630, 100);

    /// <summary>Línea bajo el nombre (3 de alto).</summary>
    public static readonly Caja Linea = new(120, 660, 510, 3);

    /// <summary>Caja de la rareza escrita.</summary>
    public static readonly Caja Rareza = new(0, 960, 750, 50);

    /// <summary>X y ancho de las dos columnas de estadísticas.</summary>
    public static readonly (float X, float Ancho)[] Columnas = [(110, 250), (400, 250)];

    /// <summary>Y de las tres filas de estadísticas.</summary>
    public static readonly float[] Filas = [700, 790, 880];

    /// <summary>Alto de cada fila de estadísticas.</summary>
    public const float AltoFila = 80;

    /// <summary>Separación entre el valor y la sigla, como en la pantalla (6 % de la columna).</summary>
    public const float SeparacionEstadistica = 15;

    /// <summary>Siglas, por columna y fila: RIT, TIR, PAS a la izquierda; REG, DEF, FÍS a la derecha.</summary>
    public static readonly string[][] Siglas = [["RIT", "TIR", "PAS"], ["REG", "DEF", "FÍS"]];

    /// <summary>Tamaños de letra.</summary>
    public const float LetraValoracion = 120, LetraPosicion = 44, LetraDorsal = 72, LetraNombreMaxima = 64,
        LetraNombreMinima = 36, LetraValorEstadistica = 60, LetraSiglaEstadistica = 44, LetraRareza = 36;

    /// <summary>Halo del texto cuando hace falta: contorno, desenfoque y opacidad.</summary>
    public const float ContornoHalo = 2, DesenfoqueHalo = 6, OpacidadHalo = 0.85f;

    /// <summary>Óvalo del escudo (opaco hasta esa fracción del radio) y fundido de la foto (alto opaco y lados).</summary>
    public const float EscudoOpaco = 0.86f, FotoOpacaHasta = 0.62f, FotoLados = 0.08f;

    /// <summary>Ángulo CSS (en grados) de las bandas del reflejo.</summary>
    public const float AnguloBrillo = 115;

    /// <summary>Bandas diagonales del reflejo: inicio, máximo, fin y opacidad de cada una (las de plano.ts).</summary>
    public static readonly (float Inicio, float Maximo, float Fin, float Opacidad)[] BandasBrillo =
        [(0.2f, 0.38f, 0.55f, 0.35f), (0.6f, 0.68f, 0.76f, 0.2f)];

    /// <summary>Halo del reflejo arriba a la izquierda: centro, radio y opacidad.</summary>
    public const float HaloBrilloX = 225, HaloBrilloY = 0, HaloBrilloRadio = 630, OpacidadHaloBrillo = 0.35f;

    /// <summary>Destellos de luz (efecto premium): centro y tamaño, los de plano.ts.</summary>
    public static readonly (float X, float Y, float Tamano)[] Destellos =
        [(120, 44, 22), (375, 10, 16), (712, 92, 30), (40, 512, 18), (726, 700, 20), (86, 1004, 26), (664, 1040, 16)];

    /// <summary>Forma de un destello: cintura de la estrella, radio y opacidad del resplandor.</summary>
    public const float CinturaDestello = 0.16f, ResplandorDestello = 1.1f, OpacidadResplandorDestello = 0.55f;

    /// <summary>Los tres tonos del marco metálico de una rareza, de arriba abajo (los de plano.ts).</summary>
    public static (string Claro, string Medio, string Oscuro) TonosDelMarco(RarezaCarta rareza)
    {
        var color = ColorDeRareza(rareza);
        return (ContrasteDeColor.Mezclar(color, "#FFFFFF", 0.45), color, ContrasteDeColor.Mezclar(color, "#000000", 0.6));
    }

    /// <summary>Opacidad de la línea y de la figura genérica.</summary>
    public const float OpacidadLinea = 0.5f, OpacidadFigura = 0.3f;

    /// <summary>Silueta de la figura genérica, en coordenadas de la caja de la foto (la de FiguraGenerica.tsx).</summary>
    public const string Silueta = "M40 460c0-110 78-190 175-190s175 80 175 190z";

    /// <summary>Cabeza de la figura genérica: centro y radio, en coordenadas de la caja de la foto.</summary>
    public static readonly (float X, float Y, float Radio) Cabeza = (215, 150, 92);

    /// <summary>Color del marco por rareza.</summary>
    public static string ColorDeRareza(RarezaCarta rareza) => rareza switch
    {
        RarezaCarta.ORO => "#D9A62E",
        RarezaCarta.PLATA => "#BFC5CC",
        _ => "#B0793F",
    };

    /// <summary>La rareza escrita.</summary>
    public static string TextoDeRareza(RarezaCarta rareza) => rareza.ToString();

    /// <summary>POR, DEF, MED o DEL.</summary>
    public static string Abreviatura(PosicionJugador posicion) => posicion switch
    {
        PosicionJugador.PORTERO => "POR",
        PosicionJugador.DEFENSA => "DEF",
        PosicionJugador.MEDIOCAMPISTA => "MED",
        _ => "DEL",
    };

    /// <summary>
    /// "Nombres Apellidos" en mayúsculas: de 64 baja de 2 en 2 hasta 36 para caber en una línea; si
    /// aún no cabe, dos líneas a 36, y lo que sobre termina en "…". <paramref name="medir"/> da el ancho
    /// de un texto con un tamaño de letra.
    /// </summary>
    public static (IReadOnlyList<string> Lineas, float Tamano) AjustarNombre(
        string nombres, string apellidos, Func<string, float, float> medir)
    {
        var texto = string.Join(' ', $"{nombres} {apellidos}".Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        for (var tamano = LetraNombreMaxima; tamano >= LetraNombreMinima; tamano -= 2)
        {
            if (medir(texto, tamano) <= Nombre.Ancho)
            {
                return ([texto], tamano);
            }
        }

        var palabras = new Queue<string>(texto.Split(' '));
        var primera = string.Empty;
        while (palabras.Count > 0 && medir(primera.Length > 0 ? $"{primera} {palabras.Peek()}" : palabras.Peek(), LetraNombreMinima) <= Nombre.Ancho)
        {
            primera = primera.Length > 0 ? $"{primera} {palabras.Dequeue()}" : palabras.Dequeue();
        }

        var segunda = string.Join(' ', palabras);
        if (primera.Length == 0)
        {
            return ([Recortar(segunda, medir)], LetraNombreMinima);
        }

        if (segunda.Length > 0 && medir(segunda, LetraNombreMinima) > Nombre.Ancho)
        {
            segunda = Recortar(segunda, medir);
        }

        return (segunda.Length > 0 ? [primera, segunda] : [primera], LetraNombreMinima);
    }

    private static string Recortar(string texto, Func<string, float, float> medir)
    {
        var corto = texto;
        while (corto.Length > 1 && medir($"{corto}…", LetraNombreMinima) > Nombre.Ancho)
        {
            corto = corto[..^1].TrimEnd();
        }

        return $"{corto}…";
    }
}

/// <summary>
/// Representa una caja del plano de la carta, en unidades del lienzo.
/// Su responsabilidad es llevar su posición y su tamaño.
/// No sabe qué se dibuja en ella.
/// </summary>
/// <param name="X">Izquierda.</param>
/// <param name="Y">Arriba.</param>
/// <param name="Ancho">Ancho.</param>
/// <param name="Alto">Alto.</param>
public readonly record struct Caja(float X, float Y, float Ancho, float Alto);
