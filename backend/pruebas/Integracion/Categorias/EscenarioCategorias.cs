using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Un club con su presidente, un directivo, un entrenador y un jugador nacido en 2014, cada uno con
/// su cliente con sesión, más un segundo club con su presidente. Reúne también las rutas de la
/// funcionalidad y la lectura directa de lo guardado, para que las pruebas no repitan el montaje.
/// </summary>
internal sealed class EscenarioCategorias
{
    public const int AnioDelJugador = 2014;

    private EscenarioCategorias()
    {
    }

    public required FabricaApi Fabrica { get; init; }

    public required Club Club { get; init; }

    public required ClienteDePrueba Presidente { get; init; }

    public required UsuarioRol IntegrantePresidente { get; init; }

    public required ClienteDePrueba Directivo { get; init; }

    public required UsuarioRol IntegranteDirectivo { get; init; }

    public required ClienteDePrueba Entrenador { get; init; }

    public required UsuarioRol IntegranteEntrenador { get; init; }

    public required ClienteDePrueba Jugador { get; init; }

    public required UsuarioRol IntegranteJugador { get; init; }

    public required Club OtroClub { get; init; }

    public required ClienteDePrueba PresidenteDeOtroClub { get; init; }

    public SembradorCategorias Sembrar => Fabrica.Categorias;

    /// <summary>Los tres roles del club que nunca pueden cambiar nada, con su nombre para los mensajes.</summary>
    public IEnumerable<(string Rol, ClienteDePrueba Cliente)> QuienesNoGestionan =>
        [("DIRECTIVO", Directivo), ("ENTRENADOR", Entrenador), ("JUGADOR", Jugador)];

    public string Categorias => RutaCategorias(Club);

    public string SinCategoria => $"/api/clubes/{Club.Id}/jugadores/sin-categoria";

    public string Retirados => $"/api/clubes/{Club.Id}/jugadores/retirados";

    public string MiCategoria => $"/api/clubes/{Club.Id}/mi-categoria";

    public static async Task<EscenarioCategorias> CrearAsync(FabricaApi fabrica)
    {
        var club = await fabrica.Sembrador.CrearClubAsync();
        var otroClub = await fabrica.Sembrador.CrearClubAsync();
        var (presidente, integrantePresidente) = await fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (directivo, integranteDirectivo) = await fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var (entrenador, integranteEntrenador) = await fabrica.Sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR);
        var (jugador, integranteJugador) = await fabrica.Categorias.CrearJugadorAsync(club, AnioDelJugador);
        var (ajeno, _) = await fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.PRESIDENTE);

        return new EscenarioCategorias
        {
            Fabrica = fabrica,
            Club = club,
            Presidente = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente),
            IntegrantePresidente = integrantePresidente,
            Directivo = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(directivo),
            IntegranteDirectivo = integranteDirectivo,
            Entrenador = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(entrenador),
            IntegranteEntrenador = integranteEntrenador,
            Jugador = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(jugador),
            IntegranteJugador = integranteJugador,
            OtroClub = otroClub,
            PresidenteDeOtroClub = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(ajeno),
        };
    }

    public static string RutaCategorias(Club club) => $"/api/clubes/{club.Id}/categorias";

    public string RutaCategoria(Guid categoriaId) => $"{Categorias}/{categoriaId}";

    public string Desactivacion(Guid categoriaId) => $"{RutaCategoria(categoriaId)}/desactivacion";

    public string Reactivacion(Guid categoriaId) => $"{RutaCategoria(categoriaId)}/reactivacion";

    public string Candidatos(Guid categoriaId) => $"{RutaCategoria(categoriaId)}/entrenadores/candidatos";

    public string EntrenadorDe(Guid categoriaId, Guid usuarioRolId) =>
        $"{RutaCategoria(categoriaId)}/entrenadores/{usuarioRolId}";

    public string EquiposQueDirige(Guid categoriaId, Guid usuarioRolId) =>
        $"{EntrenadorDe(categoriaId, usuarioRolId)}/equipos";

    public string Equipos(Guid categoriaId) => $"{RutaCategoria(categoriaId)}/equipos";

    public string RutaEquipo(Guid categoriaId, Guid equipoId) => $"{Equipos(categoriaId)}/{equipoId}";

    public string DesactivacionDeEquipo(Guid categoriaId, Guid equipoId) =>
        $"{RutaEquipo(categoriaId, equipoId)}/desactivacion";

    public string JugadorDeEquipo(Guid categoriaId, Guid equipoId, Guid usuarioRolId) =>
        $"{RutaEquipo(categoriaId, equipoId)}/jugadores/{usuarioRolId}";

    public string CategoriaDe(Guid usuarioRolId) => $"/api/clubes/{Club.Id}/jugadores/{usuarioRolId}/categoria";

    public string Retiro(Guid usuarioRolId) => $"/api/clubes/{Club.Id}/jugadores/{usuarioRolId}/retiro";

    public string Reincorporacion(Guid usuarioRolId) =>
        $"/api/clubes/{Club.Id}/jugadores/{usuarioRolId}/reincorporacion";

    /// <summary>Crea la categoría por la API como presidente y devuelve su identificador.</summary>
    public async Task<Guid> CrearCategoriaPorApiAsync(int anio)
    {
        var respuesta = await Presidente.PostAsync(Categorias, new { anio });
        respuesta.EnsureSuccessStatusCode();
        return (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta))
            .GetProperty("categoria").GetProperty("categoriaId").GetGuid();
    }

    /// <summary>El detalle de una categoría tal como lo ve el presidente.</summary>
    public async Task<JsonElement> DetalleAsync(Guid categoriaId) =>
        await ClienteDePrueba.LeerAsync<JsonElement>(await Presidente.GetAsync(RutaCategoria(categoriaId)));

    /// <summary>La categoría tal como está guardada, o nulo si ya no existe.</summary>
    public Task<Categoria?> CategoriaGuardadaAsync(Guid categoriaId) => Fabrica.ConContextoAsync(contexto =>
        contexto.Categorias.IgnoreQueryFilters().AsNoTracking()
            .Include(categoria => categoria.Equipos)
            .Include(categoria => categoria.Asignaciones)
            .FirstOrDefaultAsync(categoria => categoria.Id == categoriaId));

    /// <summary>El integrante tal como está guardado, con sus equipos.</summary>
    public Task<UsuarioRol?> IntegranteGuardadoAsync(Guid usuarioRolId) => Fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking()
            .Include(integrante => integrante.Equipos)
            .FirstOrDefaultAsync(integrante => integrante.Id == usuarioRolId));
}

/// <summary>Lecturas de JSON que repiten las pruebas de categorías.</summary>
internal static class JsonDeCategorias
{
    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage respuesta) =>
        await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);

    public static List<JsonElement> Lista(this JsonElement elemento, string propiedad) =>
        elemento.GetProperty(propiedad).EnumerateArray().ToList();

    public static List<Guid> Ids(this IEnumerable<JsonElement> elementos, string propiedad) =>
        elementos.Select(elemento => elemento.GetProperty(propiedad).GetGuid()).ToList();

    public static async Task<List<JsonElement>> ListaAsync(this ClienteDePrueba cliente, string ruta) =>
        (await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(ruta))).EnumerateArray().ToList();
}
