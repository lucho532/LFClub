using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa la redacción de las líneas que el sistema escribe en la campana (spec 012, RF-012 a
/// RF-017; research §10).
/// Su responsabilidad es armar en español el texto plano de cada hecho: la fecha se dice "hoy",
/// "mañana", "el sábado" (en los próximos seis días, contados en hora de Colombia) o "el 18 de
/// octubre"; la hora, "a las 9:00" (24 horas, sin cero a la izquierda). Los nombres de los días y de
/// los meses van en una tabla propia, no en la cultura, porque la imagen del contenedor puede no
/// traerla. El texto nunca pasa de 300 caracteres: recorta el lugar y el rival a 120 cada uno.
/// El aviso de asistencia no dice quién asistió ni quién faltó (RF-016). No consulta la base de
/// datos ni decide quién recibe el aviso.
/// </summary>
public static class RedaccionDeAvisos
{
    private const int LongitudMaximaDeTexto = 300;
    private const int LongitudMaximaDeLugarYRival = 120;
    private const int LongitudMaximaDeNombre = 40;
    private const string RivalPorDefinir = "Por definir";

    private static readonly string[] Dias =
        ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre",
        "octubre", "noviembre", "diciembre",
    ];

    /// <summary>"Nuevo entrenamiento el sábado a las 9:00".</summary>
    public static string EntrenamientoProgramado(DateOnly fecha, TimeOnly inicio, DateOnly hoy) =>
        Acotar($"Nuevo entrenamiento {Fecha(fecha, hoy)} a las {Hora(inicio)}");

    /// <summary>"Cambió la hora del entrenamiento: ahora el sábado a las 10:00".</summary>
    public static string EntrenamientoCambiado(
        CambiosDeActividad cambios, DateOnly fecha, TimeOnly inicio, string lugar, DateOnly hoy) =>
        Acotar($"{QueCambio(cambios)} del entrenamiento: ahora {ComoQueda(cambios, fecha, inicio, lugar, hoy)}");

    /// <summary>"Entrenamiento del sábado cancelado".</summary>
    public static string EntrenamientoCancelado(DateOnly fecha, DateOnly hoy) =>
        Acotar($"Entrenamiento {DeLaFecha(fecha, hoy)} cancelado");

    /// <summary>"El entrenamiento del sábado sigue en pie".</summary>
    public static string EntrenamientoReactivado(DateOnly fecha, DateOnly hoy) =>
        Acotar($"El entrenamiento {DeLaFecha(fecha, hoy)} sigue en pie");

    /// <summary>"Se registró la asistencia del entrenamiento del sábado"; sin nombres ni estados.</summary>
    public static string AsistenciaRegistrada(DateOnly fecha, DateOnly hoy) =>
        Acotar($"Se registró la asistencia del entrenamiento {DeLaFecha(fecha, hoy)}");

    /// <summary>"Cambió el lugar del partido contra Rival: ahora en Cancha Minitas".</summary>
    public static string PartidoCambiado(
        CambiosDeActividad cambios, string? rival, DateOnly fecha, TimeOnly hora, string lugar, DateOnly hoy) =>
        Acotar($"{QueCambio(cambios)} del partido{ContraRival(rival)}: ahora {ComoQueda(cambios, fecha, hora, lugar, hoy)}");

    /// <summary>"Partido contra Rival del sábado cancelado".</summary>
    public static string PartidoCancelado(string? rival, DateOnly fecha, DateOnly hoy) =>
        Acotar($"Partido{ContraRival(rival)} {DeLaFecha(fecha, hoy)} cancelado");

    /// <summary>"El partido contra Rival del sábado sigue en pie".</summary>
    public static string PartidoReactivado(string? rival, DateOnly fecha, DateOnly hoy) =>
        Acotar($"El partido{ContraRival(rival)} {DeLaFecha(fecha, hoy)} sigue en pie");

    /// <summary>"Mateo fue convocado al partido contra Rival el sábado a las 15:00".</summary>
    public static string JugadorConvocado(
        IReadOnlyList<string> nombres, string? rival, DateOnly fecha, TimeOnly hora, DateOnly hoy)
    {
        var lista = Enumerar(nombres.Select(nombre => Recortar(nombre.Trim(), LongitudMaximaDeNombre)).ToList());
        var verbo = nombres.Count == 1 ? "fue convocado" : "fueron convocados";
        return Acotar($"{lista} {verbo} al partido{ContraRival(rival)} {Fecha(fecha, hoy)} a las {Hora(hora)}");
    }

    /// <summary>"Resultado del partido contra Rival: La Pecosa 3 - 1 Rival".</summary>
    public static string ResultadoPublicado(
        string? rival, CondicionPartido condicion, string nombreDelClub, int golesClub, int golesRival)
    {
        var nombreRival = NombreDelRival(rival) ?? RivalPorDefinir;
        var marcador = condicion == CondicionPartido.LOCAL
            ? $"{nombreDelClub} {golesClub} - {golesRival} {nombreRival}"
            : $"{nombreRival} {golesRival} - {golesClub} {nombreDelClub}";
        return Acotar($"Resultado del partido{ContraRival(rival)}: {marcador}");
    }

    private static string QueCambio(CambiosDeActividad cambios)
    {
        var elementos = new List<string>();
        if (cambios.HasFlag(CambiosDeActividad.Fecha))
        {
            elementos.Add("la fecha");
        }

        if (cambios.HasFlag(CambiosDeActividad.Hora))
        {
            elementos.Add("la hora");
        }

        if (cambios.HasFlag(CambiosDeActividad.Lugar))
        {
            elementos.Add("el lugar");
        }

        return $"{(elementos.Count == 1 ? "Cambió" : "Cambiaron")} {Enumerar(elementos)}";
    }

    private static string ComoQueda(CambiosDeActividad cambios, DateOnly fecha, TimeOnly hora, string lugar, DateOnly hoy)
    {
        var cambioElMomento = cambios.HasFlag(CambiosDeActividad.Fecha) || cambios.HasFlag(CambiosDeActividad.Hora);
        var enElLugar = $"en {Recortar(lugar.Trim(), LongitudMaximaDeLugarYRival)}";
        if (!cambioElMomento)
        {
            return enElLugar;
        }

        var momento = $"{Fecha(fecha, hoy)} a las {Hora(hora)}";
        return cambios.HasFlag(CambiosDeActividad.Lugar) ? $"{momento} {enElLugar}" : momento;
    }

    private static string ContraRival(string? rival)
    {
        var nombre = NombreDelRival(rival);
        return nombre is null ? string.Empty : $" contra {nombre}";
    }

    private static string? NombreDelRival(string? rival)
    {
        var nombre = rival?.Trim();
        return string.IsNullOrEmpty(nombre) || nombre == RivalPorDefinir
            ? null
            : Recortar(nombre, LongitudMaximaDeLugarYRival);
    }

    private static string Fecha(DateOnly fecha, DateOnly hoy)
    {
        var dias = fecha.DayNumber - hoy.DayNumber;
        return dias switch
        {
            0 => "hoy",
            1 => "mañana",
            >= 2 and <= 6 => $"el {Dias[(int)fecha.DayOfWeek]}",
            _ => $"el {fecha.Day} de {Meses[fecha.Month - 1]}",
        };
    }

    private static string DeLaFecha(DateOnly fecha, DateOnly hoy)
    {
        var frase = Fecha(fecha, hoy);
        return frase.StartsWith("el ", StringComparison.Ordinal) ? $"del {frase[3..]}" : $"de {frase}";
    }

    private static string Hora(TimeOnly hora) => $"{hora.Hour}:{hora.Minute:00}";

    private static string Enumerar(IReadOnlyList<string> elementos) => elementos.Count switch
    {
        0 => string.Empty,
        1 => elementos[0],
        _ => $"{string.Join(", ", elementos.Take(elementos.Count - 1))} y {elementos[^1]}",
    };

    private static string Recortar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..(maximo - 1)] + "…";

    private static string Acotar(string texto) => Recortar(texto, LongitudMaximaDeTexto);
}
