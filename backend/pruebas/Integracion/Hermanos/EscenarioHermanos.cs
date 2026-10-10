using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// El club de <see cref="EscenarioCategorias"/> (presidente, directivo, entrenador y otro club con
/// su presidente) con la categoría activa del año de Ana, que entrena el entrenador del club; Ana,
/// jugadora aprobada en ella, cuya cuenta es la de la familia; y Beto, jugador de otra cuenta.
/// Reúne la ruta para agregar un hermano, su cuerpo y los clientes de la familia.
/// </summary>
internal sealed class EscenarioHermanos
{
    public const int AnioDeAna = 2014;

    private EscenarioHermanos()
    {
    }

    public required EscenarioCategorias Base { get; init; }

    public required Categoria CategoriaDeAna { get; init; }

    /// <summary>La cuenta de la familia.</summary>
    public required Usuario Familia { get; init; }

    public required UsuarioRol Ana { get; init; }

    public required Usuario CuentaDeBeto { get; init; }

    public required UsuarioRol Beto { get; init; }

    public FabricaApi Fabrica => Base.Fabrica;

    public Club Club => Base.Club;

    public Club OtroClub => Base.OtroClub;

    public ClienteDePrueba Presidente => Base.Presidente;

    public ClienteDePrueba Directivo => Base.Directivo;

    public ClienteDePrueba Entrenador => Base.Entrenador;

    public SembradorHermanos Sembrar => Fabrica.Hermanos;

    public string InicioDelClub => $"/api/clubes/{Club.Id}";

    public static async Task<EscenarioHermanos> CrearAsync(FabricaApi fabrica)
    {
        var escenario = await EscenarioCategorias.CrearAsync(fabrica);
        var categoria = await fabrica.Categorias.CrearCategoriaAsync(escenario.Club, AnioDeAna);
        await fabrica.Categorias.AsignarEntrenadorAsync(categoria, escenario.IntegranteEntrenador);
        var (familia, ana) = await fabrica.Categorias.CrearJugadorAsync(escenario.Club, AnioDeAna, categoria, "Ana");
        var (cuentaDeBeto, beto) = await fabrica.Categorias.CrearJugadorAsync(escenario.Club, 2015, nombres: "Beto");

        return new EscenarioHermanos
        {
            Base = escenario,
            CategoriaDeAna = categoria,
            Familia = familia,
            Ana = ana,
            CuentaDeBeto = cuentaDeBeto,
            Beto = beto,
        };
    }

    public static string Hermanos(Club club, Guid usuarioRolId) =>
        $"/api/clubes/{club.Id}/jugadores/{usuarioRolId}/hermanos";

    /// <summary>La ruta para agregar un hermano desde la ficha de ese jugador, en el club del escenario.</summary>
    public string Hermanos(Guid usuarioRolId) => Hermanos(Club, usuarioRolId);

    /// <summary>
    /// Cuerpo de <c>AgregarHermanoDto</c> con valores válidos por defecto: un niño con un documento
    /// único. La cuenta sembrada no tiene responsable, así que para un menor hay que indicarlo.
    /// </summary>
    public static Dictionary<string, object?> DatosDeHermano(
        string? numeroDocumento = null, DateOnly? fechaNacimiento = null, string? nombreResponsable = null) => new()
        {
            ["nombres"] = "Luis",
            ["apellidos"] = "Gómez",
            ["tipoDocumento"] = "TARJETA_IDENTIDAD",
            ["numeroDocumento"] = numeroDocumento ?? Sembrador.Unico("doc"),
            ["fechaNacimiento"] = (fechaNacimiento ?? new DateOnly(AnioDeAna, 3, 9)).ToString("yyyy-MM-dd"),
            ["nombreResponsable"] = nombreResponsable,
        };

    /// <summary>
    /// Un cliente con la sesión de la cuenta de Ana, iniciada como con el correo, y opcionalmente
    /// con un jugador elegido.
    /// </summary>
    public async Task<ClienteDePrueba> ClienteDeLaFamiliaAsync(Guid? jugadorElegido = null) =>
        (await Fabrica.CrearClienteDePrueba().ConSesionDeAsync(Familia)).ElegirJugador(jugadorElegido);

    /// <summary>
    /// Un cliente con la sesión de la cuenta de Ana limitada a esos jugadores, como la que deja
    /// entrar con el documento de uno de ellos.
    /// </summary>
    public Task<ClienteDePrueba> ClienteLimitadoAsync(params Guid[] jugadores) =>
        Fabrica.CrearClienteDePrueba().ConSesionLimitadaAsync(Familia, jugadores);

    /// <summary>Los integrantes de la cuenta de la familia en ese club, del más antiguo al más reciente.</summary>
    public Task<List<UsuarioRol>> JugadoresDeLaFamiliaAsync(Club? club = null) => Fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking()
            .Include(integrante => integrante.Equipos)
            .Where(integrante => integrante.UsuarioId == Familia.Id && integrante.ClubId == (club ?? Club).Id)
            .OrderBy(integrante => integrante.CreadoEn)
            .ToListAsync());

    /// <summary>La cuenta de la familia tal como está guardada.</summary>
    public Task<Usuario> FamiliaGuardadaAsync() => Fabrica.ConContextoAsync(contexto =>
        contexto.Usuarios.AsNoTracking().FirstAsync(cuenta => cuenta.Id == Familia.Id));
}
