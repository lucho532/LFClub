using System.Text.Json;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Comunicados;

/// <summary>
/// El club de "Datos de partida" de la 007 (presidente, un directivo que entrena la 2015, E1 con la
/// 2014 y "A", E2 sin categorías; jugadores Ana, Beto, Caro, Dani, Eva retirada y Luis en espera) con
/// una sesión abierta para cada uno, para las pruebas de los comunicados de la spec 011.
/// </summary>
public class EscenarioComunicados
{
    private readonly FabricaApi _fabrica;

    private EscenarioComunicados(FabricaApi fabrica, ClubDeJugadores club)
    {
        _fabrica = fabrica;
        Club = club;
    }

    public ClubDeJugadores Club { get; }

    public ClienteDePrueba P { get; private set; } = null!;

    public ClienteDePrueba D { get; private set; } = null!;

    public ClienteDePrueba E1 { get; private set; } = null!;

    public ClienteDePrueba E2 { get; private set; } = null!;

    /// <summary>La cuenta de Ana (2014, equipos A y B), con ella elegida porque su hermano Luis está en espera.</summary>
    public ClienteDePrueba Ana { get; private set; } = null!;

    public ClienteDePrueba Beto { get; private set; } = null!;

    public ClienteDePrueba Caro { get; private set; } = null!;

    public ClienteDePrueba Dani { get; private set; } = null!;

    public ClienteDePrueba Eva { get; private set; } = null!;

    public UsuarioRol AnaJugador => Club.Ana.Integrante;

    public static async Task<EscenarioComunicados> CrearAsync(FabricaApi fabrica)
    {
        var club = await fabrica.Jugadores.CrearClubAsync();
        var escenario = new EscenarioComunicados(fabrica, club)
        {
            P = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Presidente.Usuario),
            D = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Directivo.Usuario),
            E1 = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.E1.Usuario),
            E2 = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.E2.Usuario),
            Ana = (await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Ana.Usuario)).ElegirJugador(club.Ana.Integrante.Id),
            Beto = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Beto.Usuario),
            Caro = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Caro.Usuario),
            Dani = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Dani.Usuario),
            Eva = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Eva.Usuario),
        };

        return escenario;
    }

    /// <summary>Ruta de los comunicados del club, con lo que siga.</summary>
    public string Ruta(string resto = "") => Club.Ruta("/comunicados" + resto);

    /// <summary>Cuerpo de publicación; sin clave indicada genera una nueva.</summary>
    public static object Cuerpo(
        string alcance,
        IEnumerable<Categoria>? categorias = null,
        IEnumerable<Equipo>? equipos = null,
        string titulo = "Cambio de horario",
        string texto = "El sábado entrenamos a las 9.",
        Guid? clave = null) => new
        {
            clave = clave ?? Guid.NewGuid(),
            titulo,
            texto,
            alcance,
            categoriaIds = categorias?.Select(categoria => categoria.Id).ToList(),
            equipoIds = equipos?.Select(equipo => equipo.Id).ToList(),
        };

    /// <summary>Publica y devuelve el cuerpo de la respuesta, que debe ser 201.</summary>
    public async Task<JsonElement> PublicarAsync(ClienteDePrueba cliente, object cuerpo) =>
        await Actividades.LeerAsync(await cliente.PostAsync(Ruta(), cuerpo), System.Net.HttpStatusCode.Created);

    /// <summary>Ruta de la campana del club (spec 012), con lo que siga.</summary>
    public string RutaCampana(string resto = "") => Club.Ruta("/campana" + resto);

    /// <summary>
    /// Identificadores de los comunicados que ve una cuenta de jugador en la campana, del más
    /// reciente al más antiguo (los avisos, si hay, no cuentan).
    /// </summary>
    public async Task<List<Guid>> RecibidosAsync(ClienteDePrueba familia) =>
        ComunicadosDe(await Actividades.LeerAsync(await familia.GetAsync(RutaCampana())));

    /// <summary>Los identificadores de los elementos de tipo COMUNICADO de una página de la campana.</summary>
    public static List<Guid> ComunicadosDe(JsonElement pagina) => pagina.GetProperty("elementos").EnumerateArray()
        .Where(elemento => elemento.GetProperty("tipo").GetString() == "COMUNICADO")
        .Select(elemento => elemento.GetProperty("comunicado").GetProperty("id").GetGuid())
        .ToList();

    /// <summary>Cuántas filas de destinatario tiene ese jugador para el comunicado, leídas de la base.</summary>
    public Task<int> FilasDeAsync(Guid comunicadoId, UsuarioRol jugador) => _fabrica.ConContextoAsync(contexto =>
        contexto.ComunicadosDestinatario.IgnoreQueryFilters()
            .CountAsync(fila => fila.ComunicadoId == comunicadoId && fila.UsuarioRolId == jugador.Id));

    /// <summary>El contador de sin leer de una cuenta de jugador.</summary>
    public async Task<int> SinLeerAsync(ClienteDePrueba familia) =>
        (await Actividades.LeerAsync(await familia.GetAsync(RutaCampana("/contador")))).GetProperty("sinLeer").GetInt32();
}
