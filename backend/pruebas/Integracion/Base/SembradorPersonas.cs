using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>
/// Arma los "Datos de partida" de la guía de validación de la 008 directamente en la base de datos,
/// y escribe fotos de perfil sin pasar por la API, para las pruebas que solo las leen.
/// </summary>
public class SembradorPersonas
{
    private readonly FabricaApi _fabrica;

    public SembradorPersonas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Club A con el presidente P (sin categorías, con foto), el directivo D (entrena la 2015, sin
    /// foto), E1 (la 2014 con los equipos "A" y "B", con foto) y E2 (sin categorías); la 2014 con
    /// un equipo "C" desactivado que E1 también dirige, y la 2016 desactivada con una asignación de
    /// E1. Jugadores Ana (2014) y Caro (2015), Eva retirada por P, Luis hermano de Ana en espera, y
    /// la ficha de Ana cambiada por última vez por P. Club B con su presidente, un jugador y la
    /// cuenta de E1 como PRESIDENTE.
    /// </summary>
    public async Task<ClubDePersonas> CrearAsync()
    {
        var sembrador = _fabrica.Sembrador;
        var categorias = _fabrica.Categorias;
        var club = await sembrador.CrearClubAsync();

        var p = await CrearPersonaAsync(club, Rol.PRESIDENTE, "Pedro", "Presidente");
        var d = await CrearPersonaAsync(club, Rol.DIRECTIVO, "Diana", "Directiva");
        var e1 = await CrearPersonaAsync(club, Rol.ENTRENADOR, "Ernesto", "Entrenador");
        var e2 = await CrearPersonaAsync(club, Rol.ENTRENADOR, "Elena", "Entrenadora");
        await PonerFotoPerfilAsync(p.Usuario, Imagenes.Png());
        await PonerFotoPerfilAsync(e1.Usuario, Imagenes.Jpeg(), "image/jpeg");

        var c2014 = await categorias.CrearCategoriaAsync(club, 2014);
        var c2015 = await categorias.CrearCategoriaAsync(club, 2015);
        var c2016 = await categorias.CrearCategoriaAsync(club, 2016);
        var equipoA = await categorias.CrearEquipoAsync(c2014, "A");
        var equipoB = await categorias.CrearEquipoAsync(c2014, "B");
        var equipoC = await categorias.CrearEquipoAsync(c2014, "C");

        // E1 dirige "B" antes que "A" para que el orden por nombre no sea casual.
        var asignacionE1 = await categorias.AsignarEntrenadorAsync(c2014, e1.Integrante);
        await categorias.DirigirEquipoAsync(asignacionE1, equipoB);
        await categorias.DirigirEquipoAsync(asignacionE1, equipoA);
        await categorias.DirigirEquipoAsync(asignacionE1, equipoC);
        await DesactivarEquipoAsync(equipoC);
        await categorias.AsignarEntrenadorAsync(c2016, e1.Integrante);
        await DesactivarCategoriaAsync(c2016);
        await categorias.AsignarEntrenadorAsync(c2015, d.Integrante);

        var ana = await categorias.CrearJugadorAsync(club, 2014, c2014, "Ana", $"Arango {Sembrador.Unico()}");
        var caro = await categorias.CrearJugadorAsync(club, 2015, c2015, "Caro", $"Cárdenas {Sembrador.Unico()}");
        var eva = await categorias.CrearJugadorAsync(club, 2014, c2014, "Eva", $"Echeverri {Sembrador.Unico()}");
        await categorias.PonerEnEquipoAsync(ana.Integrante, equipoA);
        await categorias.RetirarAsync(eva.Integrante, p.Integrante);
        var luis = await _fabrica.Hermanos.CrearHermanoAsync(ana.Integrante);
        await _fabrica.Fichas.CrearFichaAsync(ana.Integrante, p.Integrante);

        var clubB = await sembrador.CrearClubAsync();
        var presidenteB = await CrearPersonaAsync(clubB, Rol.PRESIDENTE, "Beatriz", "Presidenta");
        var e1EnB = await sembrador.CrearIntegranteAsync(clubB, e1.Usuario, Rol.PRESIDENTE, nombres: "Ernesto");
        var jugadorB = await categorias.CrearJugadorAsync(clubB, 2014, null, "Bruno", $"Bravo {Sembrador.Unico()}");

        return new ClubDePersonas(
            club, p, d, e1, e2, c2014, c2015, c2016, equipoA, equipoB, equipoC, asignacionE1,
            ana, caro, eva, luis, clubB, presidenteB, e1EnB, jugadorB);
    }

    /// <summary>Una cuenta nueva como integrante aprobado de ese club, con nombres y apellidos únicos.</summary>
    public async Task<(Usuario Usuario, UsuarioRol Integrante)> CrearPersonaAsync(
        Club club,
        Rol rol,
        string nombres,
        string apellidos,
        string? documento = null,
        EstadoIngreso estado = EstadoIngreso.APROBADO)
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var integrante = new UsuarioRol
        {
            ClubId = club.Id,
            UsuarioId = usuario.Id,
            Rol = rol,
            EstadoIngreso = estado,
            Nombres = nombres,
            Apellidos = $"{apellidos} {Sembrador.Unico()}",
            TipoDocumento = TipoDocumento.CEDULA_CIUDADANIA,
            NumeroDocumento = NormalizadorTexto.Documento($"{documento ?? "doc"}{Sembrador.Unico()}"),
            FechaNacimiento = new DateOnly(1985, 3, 14),
            CreadoEn = DateTime.UtcNow,
        };

        return await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.UsuariosRol.Add(integrante);
            await contexto.SaveChangesAsync();
            return (usuario, integrante);
        });
    }

    /// <summary>
    /// Escribe la foto de perfil de la cuenta, sustituyendo la que tuviera, y sube su
    /// <see cref="Usuario.VersionFoto"/>, como lo hace <c>PUT /api/cuenta/foto</c>.
    /// </summary>
    public Task<int> PonerFotoPerfilAsync(Usuario usuario, byte[] contenido, string tipoContenido = "image/png") =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            await contexto.FotosPerfil.Where(foto => foto.UsuarioId == usuario.Id).ExecuteDeleteAsync();
            contexto.FotosPerfil.Add(new FotoPerfil { UsuarioId = usuario.Id, Contenido = contenido, TipoContenido = tipoContenido });
            await contexto.SaveChangesAsync();
            await contexto.Usuarios.Where(cuenta => cuenta.Id == usuario.Id)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(cuenta => cuenta.VersionFoto, cuenta => cuenta.VersionFoto + 1));
            return await contexto.Usuarios.Where(cuenta => cuenta.Id == usuario.Id).Select(cuenta => cuenta.VersionFoto).FirstAsync();
        });

    /// <summary>Quita al integrante del club, como al eliminarlo del club desde la plataforma.</summary>
    public Task QuitarDelClubAsync(UsuarioRol integrante) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().Where(fila => fila.Id == integrante.Id).ExecuteDeleteAsync());

    /// <summary>Cambia el rol del integrante directamente, como lo dejará la operación de §12.2.</summary>
    public Task CambiarRolAsync(UsuarioRol integrante, Rol rol) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().Where(fila => fila.Id == integrante.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Rol, rol)));

    public Task DesactivarEquipoAsync(Equipo equipo) => _fabrica.ConContextoAsync(contexto =>
        contexto.Equipos.IgnoreQueryFilters().Where(fila => fila.Id == equipo.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Activo, false)));

    public Task DesactivarCategoriaAsync(Categoria categoria) => _fabrica.ConContextoAsync(contexto =>
        contexto.Categorias.IgnoreQueryFilters().Where(fila => fila.Id == categoria.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Activa, false)));

    public Task DesactivarAsignacionAsync(AsignacionEntrenadorCategoria asignacion) => _fabrica.ConContextoAsync(contexto =>
        contexto.AsignacionesEntrenadorCategoria.IgnoreQueryFilters().Where(fila => fila.Id == asignacion.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Activa, false)));
}

/// <summary>Los "Datos de partida" de la guía de la 008, con todo lo que crea el sembrador.</summary>
public record ClubDePersonas(
    Club Club,
    (Usuario Usuario, UsuarioRol Integrante) P,
    (Usuario Usuario, UsuarioRol Integrante) D,
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
    (Usuario Usuario, UsuarioRol Integrante) Caro,
    (Usuario Usuario, UsuarioRol Integrante) Eva,
    UsuarioRol Luis,
    Club ClubB,
    (Usuario Usuario, UsuarioRol Integrante) PresidenteB,
    UsuarioRol E1EnB,
    (Usuario Usuario, UsuarioRol Integrante) JugadorB)
{
    /// <summary>Ruta de la API del club A, con lo que siga.</summary>
    public string Ruta(string resto = "") => $"/api/clubes/{Club.Id}{resto}";

    /// <summary>Ruta de la ficha básica de ese integrante en el club A.</summary>
    public string Ficha(UsuarioRol integrante) => Ruta($"/personas/{integrante.Id}");

    /// <summary>Ruta de la foto de ese integrante en el club A.</summary>
    public string Foto(UsuarioRol integrante) => Ruta($"/personas/{integrante.Id}/foto");
}
