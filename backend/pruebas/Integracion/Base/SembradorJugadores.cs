using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Arma el club de la guía de validación de la 007 ("Datos de partida") directamente en la base de
/// datos, y escribe fotos de jugadores sin pasar por la API, para las pruebas que solo las leen.
/// </summary>
public class SembradorJugadores
{
    private readonly FabricaApi _fabrica;

    public SembradorJugadores(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Club con presidente, un directivo que entrena la 2015, E1 (solo la 2014, dirige "A") y E2 (sin
    /// categorías); categorías 2014 (equipos "A" y "B") y 2015 (equipo "C") activas, y 2016
    /// desactivada con el equipo activo "D"; jugadores Ana (2014, A y B), Beto (2014, sin equipo),
    /// Caro (2015, C), Dani (sin categoría), Eva (retirada) y Luis, hermano de Ana en espera.
    /// </summary>
    public async Task<ClubDeJugadores> CrearClubAsync()
    {
        var sembrador = _fabrica.Sembrador;
        var categorias = _fabrica.Categorias;
        var club = await sembrador.CrearClubAsync();

        var presidente = await sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var directivo = await sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var e1 = await sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR);
        var e2 = await sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR);

        var c2014 = await categorias.CrearCategoriaAsync(club, 2014);
        var c2015 = await categorias.CrearCategoriaAsync(club, 2015);
        var c2016 = await categorias.CrearCategoriaAsync(club, 2016, activa: false, usada: true);
        var equipoA = await categorias.CrearEquipoAsync(c2014, "A");
        var equipoB = await categorias.CrearEquipoAsync(c2014, "B");
        var equipoC = await categorias.CrearEquipoAsync(c2015, "C");
        await categorias.CrearEquipoAsync(c2016, "D");

        var asignacionE1 = await categorias.AsignarEntrenadorAsync(c2014, e1.Integrante);
        await categorias.DirigirEquipoAsync(asignacionE1, equipoA);
        await categorias.AsignarEntrenadorAsync(c2015, directivo.Integrante);

        var ana = await categorias.CrearJugadorAsync(club, 2014, c2014, "Ana", $"Arango {Sembrador.Unico()}");
        var beto = await categorias.CrearJugadorAsync(club, 2014, c2014, "Beto", $"Bermúdez {Sembrador.Unico()}");
        var caro = await categorias.CrearJugadorAsync(club, 2015, c2015, "Caro", $"Cárdenas {Sembrador.Unico()}");
        var dani = await categorias.CrearJugadorAsync(club, 2017, null, "Dani", $"Duque {Sembrador.Unico()}");
        var eva = await categorias.CrearJugadorAsync(club, 2014, c2014, "Eva", $"Echeverri {Sembrador.Unico()}");
        await categorias.PonerEnEquipoAsync(ana.Integrante, equipoA);
        await categorias.PonerEnEquipoAsync(ana.Integrante, equipoB);
        await categorias.PonerEnEquipoAsync(caro.Integrante, equipoC);
        await categorias.RetirarAsync(eva.Integrante, presidente.Integrante);
        var luis = await _fabrica.Hermanos.CrearHermanoAsync(ana.Integrante);

        return new ClubDeJugadores(
            club, presidente, directivo, e1, e2, c2014, c2015, c2016, equipoA, equipoB, equipoC, asignacionE1,
            ana, beto, caro, dani, eva, luis);
    }

    /// <summary>Escribe la foto de ese jugador, sustituyendo la que tuviera, guardada ahora.</summary>
    public Task<FotoJugador> PonerFotoAsync(UsuarioRol jugador, byte[] contenido, string tipoContenido = "image/png") =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var foto = new FotoJugador
            {
                UsuarioRolId = jugador.Id,
                ClubId = jugador.ClubId,
                Contenido = contenido,
                TipoContenido = tipoContenido,
                GuardadaEn = DateTime.UtcNow,
            };
            contexto.FotosJugador.Add(foto);
            await contexto.SaveChangesAsync();
            return foto;
        });

    /// <summary>Cliente con la sesión de esa cuenta y, si se indica, con ese jugador elegido.</summary>
    public Task<ClienteDePrueba> ClienteAsync(Usuario cuenta, UsuarioRol? elegido = null) =>
        _fabrica.CrearClienteDePrueba().ElegirJugador(elegido?.Id).ConSesionDeAsync(cuenta);
}

/// <summary>El club de "Datos de partida" de la guía de la 007, con todo lo que crea el sembrador.</summary>
public record ClubDeJugadores(
    Club Club,
    (Usuario Usuario, UsuarioRol Integrante) Presidente,
    (Usuario Usuario, UsuarioRol Integrante) Directivo,
    (Usuario Usuario, UsuarioRol Integrante) E1,
    (Usuario Usuario, UsuarioRol Integrante) E2,
    Categoria C2014,
    Categoria C2015,
    Categoria C2016,
    Equipo EquipoA,
    Equipo EquipoB,
    Equipo EquipoC,
    AsignacionEntrenadorCategoria AsignacionE1,
    (Usuario Usuario, UsuarioRol Integrante) Ana,
    (Usuario Usuario, UsuarioRol Integrante) Beto,
    (Usuario Usuario, UsuarioRol Integrante) Caro,
    (Usuario Usuario, UsuarioRol Integrante) Dani,
    (Usuario Usuario, UsuarioRol Integrante) Eva,
    UsuarioRol Luis)
{
    /// <summary>Ruta de la API del club, con lo que siga.</summary>
    public string Ruta(string resto = "") => $"/api/clubes/{Club.Id}{resto}";
}
