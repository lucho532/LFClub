using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Crea jugadores con su año de nacimiento, categorías, equipos, asignaciones y retiros
/// directamente en la base de datos, para que cada prueba de categorías parta del estado que
/// necesita sin depender de los endpoints de otra historia.
/// </summary>
public class SembradorCategorias
{
    private readonly FabricaApi _fabrica;

    public SembradorCategorias(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>Jugador aprobado y activo nacido ese año, opcionalmente ya en una categoría.</summary>
    public Task<(Usuario Usuario, UsuarioRol Integrante)> CrearJugadorAsync(
        Club club, int anioNacimiento, Categoria? categoria = null, string nombres = "Ana", string? apellidos = null) =>
        CrearAsync(club, anioNacimiento, EstadoIngreso.APROBADO, categoria, nombres, apellidos);

    /// <summary>Persona en la sala de espera nacida ese año.</summary>
    public Task<(Usuario Usuario, UsuarioRol Integrante)> CrearJugadorEnEsperaAsync(Club club, int anioNacimiento) =>
        CrearAsync(club, anioNacimiento, EstadoIngreso.EN_ESPERA, null, "Ana", null);

    public async Task<Categoria> CrearCategoriaAsync(Club club, int anio, bool activa = true, bool usada = false)
    {
        var categoria = new Categoria
        {
            ClubId = club.Id, Anio = anio, Activa = activa, Usada = usada, CreadaEn = DateTime.UtcNow,
        };

        return await GuardarAsync(categoria);
    }

    public async Task<Equipo> CrearEquipoAsync(Categoria categoria, string nombre, bool activo = true, bool usado = false)
    {
        var equipo = new Equipo
        {
            ClubId = categoria.ClubId,
            CategoriaId = categoria.Id,
            Nombre = nombre,
            NombreNormalizado = nombre.ToLowerInvariant(),
            Activo = activo,
            Usado = usado,
            CreadoEn = DateTime.UtcNow,
        };

        return await GuardarAsync(equipo);
    }

    /// <summary>Asigna al integrante como entrenador de la categoría y la marca como usada.</summary>
    public async Task<AsignacionEntrenadorCategoria> AsignarEntrenadorAsync(
        Categoria categoria, UsuarioRol integrante, bool activa = true)
    {
        var asignacion = new AsignacionEntrenadorCategoria
        {
            ClubId = categoria.ClubId,
            CategoriaId = categoria.Id,
            UsuarioRolId = integrante.Id,
            Activa = activa,
            CreadaEn = DateTime.UtcNow,
        };

        await GuardarAsync(asignacion);
        await MarcarUsadaAsync(categoria.Id);
        return asignacion;
    }

    /// <summary>Pone al jugador en el equipo y marca el equipo como usado.</summary>
    public async Task PonerEnEquipoAsync(UsuarioRol jugador, Equipo equipo)
    {
        await GuardarAsync(new JugadorEquipo { ClubId = equipo.ClubId, UsuarioRolId = jugador.Id, EquipoId = equipo.Id });
        await MarcarUsadoAsync(equipo.Id);
    }

    /// <summary>Indica que la asignación dirige el equipo y marca el equipo como usado.</summary>
    public async Task DirigirEquipoAsync(AsignacionEntrenadorCategoria asignacion, Equipo equipo)
    {
        await GuardarAsync(new EntrenadorEquipo
        {
            ClubId = equipo.ClubId, AsignacionEntrenadorCategoriaId = asignacion.Id, EquipoId = equipo.Id,
        });
        await MarcarUsadoAsync(equipo.Id);
    }

    /// <summary>Deja al jugador retirado: inactivo, sin categoría ni equipos y con los datos del retiro.</summary>
    public Task RetirarAsync(UsuarioRol jugador, UsuarioRol quienRetira) => _fabrica.ConContextoAsync(async contexto =>
    {
        await contexto.JugadoresEquipo.IgnoreQueryFilters()
            .Where(fila => fila.UsuarioRolId == jugador.Id).ExecuteDeleteAsync();

        return await contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(integrante => integrante.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(integrante => integrante.Activo, false)
                .SetProperty(integrante => integrante.CategoriaId, (Guid?)null)
                .SetProperty(integrante => integrante.RetiradoEn, DateTime.UtcNow)
                .SetProperty(integrante => integrante.RetiradoPorUsuarioId, quienRetira.UsuarioId)
                .SetProperty(integrante => integrante.RetiradoPorNombre, $"{quienRetira.Nombres} {quienRetira.Apellidos}"));
    });

    private async Task<(Usuario Usuario, UsuarioRol Integrante)> CrearAsync(
        Club club, int anioNacimiento, EstadoIngreso estadoIngreso, Categoria? categoria, string nombres, string? apellidos)
    {
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var integrante = new UsuarioRol
        {
            ClubId = club.Id,
            UsuarioId = usuario.Id,
            Rol = Rol.JUGADOR,
            EstadoIngreso = estadoIngreso,
            Nombres = nombres,
            Apellidos = apellidos ?? $"Gómez {Sembrador.Unico()}",
            TipoDocumento = TipoDocumento.TARJETA_IDENTIDAD,
            NumeroDocumento = NormalizadorTexto.Documento(Sembrador.Unico("doc")),
            FechaNacimiento = new DateOnly(anioNacimiento, 7, 15),
            CategoriaId = categoria?.Id,
            CreadoEn = DateTime.UtcNow,
        };

        await GuardarAsync(integrante);
        if (categoria is not null)
        {
            await MarcarUsadaAsync(categoria.Id);
        }

        return (usuario, integrante);
    }

    private Task<T> GuardarAsync<T>(T entidad)
        where T : class => _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.Add(entidad);
            await contexto.SaveChangesAsync();
            return entidad;
        });

    private Task<int> MarcarUsadaAsync(Guid categoriaId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Categorias.IgnoreQueryFilters()
            .Where(categoria => categoria.Id == categoriaId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(categoria => categoria.Usada, true)));

    private Task<int> MarcarUsadoAsync(Guid equipoId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Equipos.IgnoreQueryFilters()
            .Where(equipo => equipo.Id == equipoId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(equipo => equipo.Usado, true)));
}
