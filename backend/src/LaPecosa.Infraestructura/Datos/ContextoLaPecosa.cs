using System.Reflection;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Datos;

/// <summary>
/// Representa el contexto de Entity Framework de la plataforma.
/// Su responsabilidad es exponer las tablas y aplicar a toda entidad de club el filtro global por
/// el club de la petición (constitución §7.1). Sin club en el contexto, el filtro no devuelve
/// ninguna fila: falla cerrado.
/// No contiene consultas ni reglas de negocio, y no decide cuál es el club de la petición.
/// </summary>
public class ContextoLaPecosa : DbContext
{
    private static readonly MethodInfo MetodoFiltro = typeof(ContextoLaPecosa)
        .GetMethod(nameof(AplicarFiltroDeClub), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly IContextoClub _contextoClub;

    /// <summary>Crea el contexto con sus opciones y el club de la petición.</summary>
    public ContextoLaPecosa(DbContextOptions<ContextoLaPecosa> opciones, IContextoClub contextoClub)
        : base(opciones)
    {
        _contextoClub = contextoClub;
    }

    /// <summary>Clubes de la plataforma.</summary>
    public DbSet<Club> Clubes => Set<Club>();

    /// <summary>Cuentas.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Fotos de perfil de las cuentas.</summary>
    public DbSet<FotoPerfil> FotosPerfil => Set<FotoPerfil>();

    /// <summary>Integrantes de los clubes.</summary>
    public DbSet<UsuarioRol> UsuariosRol => Set<UsuarioRol>();

    /// <summary>Escudos de los clubes.</summary>
    public DbSet<EscudoClub> EscudosClub => Set<EscudoClub>();

    /// <summary>Invitaciones para registrarse en un club.</summary>
    public DbSet<Invitacion> Invitaciones => Set<Invitacion>();

    /// <summary>Solicitudes de recuperación de contraseña.</summary>
    public DbSet<SolicitudRecuperacion> SolicitudesRecuperacion => Set<SolicitudRecuperacion>();

    /// <summary>Categorías de los clubes.</summary>
    public DbSet<Categoria> Categorias => Set<Categoria>();

    /// <summary>Equipos de las categorías.</summary>
    public DbSet<Equipo> Equipos => Set<Equipo>();

    /// <summary>Asignaciones de entrenadores a categorías.</summary>
    public DbSet<AsignacionEntrenadorCategoria> AsignacionesEntrenadorCategoria =>
        Set<AsignacionEntrenadorCategoria>();

    /// <summary>Equipos que dirige cada asignación.</summary>
    public DbSet<EntrenadorEquipo> EntrenadoresEquipo => Set<EntrenadorEquipo>();

    /// <summary>Equipos en los que juega cada jugador.</summary>
    public DbSet<JugadorEquipo> JugadoresEquipo => Set<JugadorEquipo>();

    /// <summary>Lo que la ficha añade a cada jugador.</summary>
    public DbSet<FichaJugador> FichasJugador => Set<FichaJugador>();

    /// <summary>Archivos entregados para los documentos que pide la ficha.</summary>
    public DbSet<DocumentoJugador> DocumentosJugador => Set<DocumentoJugador>();

    /// <summary>Fotos propias de los jugadores.</summary>
    public DbSet<FotoJugador> FotosJugador => Set<FotoJugador>();

    /// <summary>Entrenamientos de los clubes.</summary>
    public DbSet<Entrenamiento> Entrenamientos => Set<Entrenamiento>();

    /// <summary>Categorías a las que va dirigido cada entrenamiento.</summary>
    public DbSet<EntrenamientoCategoria> EntrenamientosCategoria => Set<EntrenamientoCategoria>();

    /// <summary>Asistencia registrada a cada entrenamiento.</summary>
    public DbSet<AsistenciaEntrenamiento> AsistenciasEntrenamiento => Set<AsistenciaEntrenamiento>();

    /// <summary>Respuestas de las familias a cada entrenamiento.</summary>
    public DbSet<RespuestaEntrenamiento> RespuestasEntrenamiento => Set<RespuestaEntrenamiento>();

    /// <summary>Partidos de los clubes.</summary>
    public DbSet<Partido> Partidos => Set<Partido>();

    /// <summary>Categorías de cada partido.</summary>
    public DbSet<PartidoCategoria> PartidosCategoria => Set<PartidoCategoria>();

    /// <summary>Convocados de cada partido, con lo que jugaron y la respuesta de su familia.</summary>
    public DbSet<PartidoJugador> PartidosJugador => Set<PartidoJugador>();

    /// <summary>Cartas de los jugadores: dorsal, posición y estadísticas (spec 010).</summary>
    public DbSet<CartaJugador> CartasJugador => Set<CartaJugador>();

    /// <summary>Club de la petición; lo lee el filtro global en cada consulta.</summary>
    private Guid? ClubDeLaPeticion => _contextoClub.ClubId;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoLaPecosa).Assembly);

        var entidadesDeClub = modelBuilder.Model.GetEntityTypes()
            .Where(tipo => typeof(IPerteneceAClub).IsAssignableFrom(tipo.ClrType))
            .Select(tipo => tipo.ClrType)
            .ToList();

        foreach (var entidad in entidadesDeClub)
        {
            MetodoFiltro.MakeGenericMethod(entidad).Invoke(this, [modelBuilder]);
        }
    }

    private void AplicarFiltroDeClub<TEntidad>(ModelBuilder modelBuilder)
        where TEntidad : class, IPerteneceAClub
    {
        modelBuilder.Entity<TEntidad>().HasQueryFilter(entidad => entidad.ClubId == ClubDeLaPeticion);
    }
}
