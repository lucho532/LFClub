using System.Globalization;
using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>
/// Cuerpos de las peticiones de entrenamientos y partidos, y lecturas de sus respuestas, para que
/// las pruebas de la 009 digan solo lo que cambia en cada caso.
/// </summary>
public static class Actividades
{
    /// <summary>Fecha como la pide el contrato.</summary>
    public static string Texto(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Cuerpo de un entrenamiento de 4:00 a 5:30 p. m. en "Cancha de Minitas".</summary>
    public static object Entrenamiento(
        DateOnly fecha,
        IEnumerable<Categoria> categorias,
        string horaInicio = "16:00",
        string horaFin = "17:30",
        string? lugar = "Cancha de Minitas",
        string? nota = "Traer peto") => new
        {
            fecha = Texto(fecha),
            horaInicio,
            horaFin,
            lugar,
            nota,
            categoriaIds = categorias.Select(categoria => categoria.Id).ToList(),
        };

    /// <summary>Cuerpo de un partido amistoso y local contra "Equipo C" a las 9:00 a. m.</summary>
    public static object Partido(
        DateOnly fecha,
        IEnumerable<Categoria> categorias,
        string hora = "09:00",
        string rival = "Equipo C",
        string tipo = "AMISTOSO",
        string? nombreTorneo = null,
        string condicion = "LOCAL") => new
        {
            rival,
            fecha = Texto(fecha),
            hora,
            lugar = "Cancha de Minitas",
            condicion,
            tipo,
            nombreTorneo,
            categoriaIds = categorias.Select(categoria => categoria.Id).ToList(),
        };

    /// <summary>Lee el cuerpo JSON de una respuesta que debe tener ese estado.</summary>
    public static async Task<JsonElement> LeerAsync(HttpResponseMessage respuesta, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.True(respuesta.StatusCode == esperado, $"{(int)respuesta.StatusCode}: {texto}");
        return JsonDocument.Parse(texto).RootElement.Clone();
    }

    /// <summary>Comprueba el estado y el código de un error.</summary>
    public static async Task ErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.True(respuesta.StatusCode == estado, $"{(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
    }

    /// <summary>Los identificadores de los elementos de una lista, por la propiedad indicada.</summary>
    public static List<Guid> Ids(JsonElement lista, string propiedad) =>
        lista.EnumerateArray().Select(elemento => elemento.GetProperty(propiedad).GetGuid()).ToList();

    /// <summary>Los años de las categorías de una actividad.</summary>
    public static List<int> Anios(JsonElement actividad) =>
        actividad.GetProperty("categorias").EnumerateArray().Select(categoria => categoria.GetProperty("anio").GetInt32()).ToList();
}
