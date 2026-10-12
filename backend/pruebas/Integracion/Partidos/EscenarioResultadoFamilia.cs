using System.Text.Json;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Partidos;

/// <summary>
/// Los datos de partida de la guía de la 010: el club de la 009 con un partido X de la 2014 que
/// ya empezó (J1, J3, J4 y J5 convocados) y un partido Y del día siguiente con J3 convocado.
/// </summary>
public record EscenarioResultadoFamilia(EscenarioActividades E, Partido X, Partido Y)
{
    public string RutaX => E.Ruta($"/partidos/{X.Id}");

    public string RutaY => E.Ruta($"/partidos/{Y.Id}");

    public static string MiJugador(string rutaPartido) => $"{rutaPartido}/resultado/mi-jugador";

    public static object Familia(int? club, int? rival, bool jugo, int goles) =>
        new { golesClub = club, golesRival = rival, jugo, goles };

    public static object DeGestion(int club, int rival, params (Persona Jugador, bool Jugo, int Goles)[] filas) => new
    {
        golesClub = club,
        golesRival = rival,
        jugadores = filas.Select(fila => new { usuarioRolId = fila.Jugador.Id, jugo = fila.Jugo, goles = fila.Goles }),
    };

    /// <summary>Crea el escenario con el reloj a las 10:00 del día de X (X empezó a las 9:00).</summary>
    public static async Task<EscenarioResultadoFamilia> CrearAsync(FabricaApi fabrica)
    {
        var e = await fabrica.Actividades.CrearClubAsync();
        var dia = fabrica.Reloj.Hoy.AddDays(3);
        var x = await fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], dia, new TimeOnly(9, 0), new[] { 1, 3, 4, 5 }.Select(numero => e.Jn(numero).Integrante));
        var y = await fabrica.Actividades.CrearPartidoAsync([e.C2014], dia.AddDays(1), new TimeOnly(9, 0), [e.Jn(3).Integrante]);
        fabrica.Reloj.FijarEnColombia(dia, new TimeOnly(10, 0));
        return new EscenarioResultadoFamilia(e, x, y);
    }

    /// <summary>Otro partido de la 2014 que ya empezó, con esos convocados.</summary>
    public async Task<(Partido Partido, string Ruta)> OtroPartidoAsync(FabricaApi fabrica, params Persona[] convocados)
    {
        var partido = await fabrica.Actividades.CrearPartidoAsync(
            [E.C2014], X.Fecha, new TimeOnly(8, 0), convocados.Select(persona => persona.Integrante));
        return (partido, E.Ruta($"/partidos/{partido.Id}"));
    }

    /// <summary>La fila de un convocado en la planilla, vista por E1.</summary>
    public async Task<JsonElement> FilaAsync(string rutaPartido, Persona jugador)
    {
        var planilla = await Actividades.LeerAsync(await E.E1.Cliente.GetAsync($"{rutaPartido}/jugadores"));
        return planilla.GetProperty("convocados").EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == jugador.Id);
    }
}
