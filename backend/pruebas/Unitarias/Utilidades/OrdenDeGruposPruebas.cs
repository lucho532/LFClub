using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Pruebas.Unitarias.Utilidades;

/// <summary>
/// Orden de la lista "Jugadores" (spec 007, RF-002 y RF-005): grupos por año de menor a mayor,
/// "Sin categoría" siempre al final, jugadores por apellidos y nombres y sin grupos vacíos.
/// </summary>
public class OrdenDeGruposPruebas
{
    private static readonly IReadOnlyDictionary<Guid, DateTime> SinFotos = new Dictionary<Guid, DateTime>();

    [Fact]
    public void Los_anios_desordenados_salen_de_menor_a_mayor_y_sin_categoria_al_final()
    {
        var c2020 = Categoria(2020);
        var c2012 = Categoria(2012);
        var c2016 = Categoria(2016);
        var jugadores = new[]
        {
            Jugador("Ana", "Uno", null), Jugador("Beto", "Dos", c2020), Jugador("Caro", "Tres", c2012),
            Jugador("Dani", "Cuatro", c2016), Jugador("Eva", "Cinco", c2020),
        };

        var grupos = MapperListaJugadores.AGrupos(jugadores, SinFotos);

        Assert.Equal([2012, 2016, 2020, (int?)null], grupos.Select(grupo => grupo.Categoria?.Anio));
        Assert.Equal(2, grupos[2].Jugadores.Count);
    }

    [Fact]
    public void Sin_jugadores_no_hay_grupos_y_sin_jugadores_sin_categoria_no_hay_ese_grupo()
    {
        Assert.Empty(MapperListaJugadores.AGrupos([], SinFotos));

        var grupos = MapperListaJugadores.AGrupos([Jugador("Ana", "Uno", Categoria(2014))], SinFotos);

        Assert.DoesNotContain(grupos, grupo => grupo.Categoria is null);
    }

    [Fact]
    public void Los_jugadores_van_por_apellidos_y_nombres_sin_distinguir_mayusculas_ni_tildes()
    {
        var categoria = Categoria(2014);
        var jugadores = new[]
        {
            Jugador("Beto", "arango", categoria), Jugador("Zoe", "Bermúdez", categoria),
            Jugador("Ana", "Arango", categoria), Jugador("Luis", "Álvarez", categoria),
        };

        var grupo = Assert.Single(MapperListaJugadores.AGrupos(jugadores, SinFotos));

        Assert.Equal(["Luis", "Ana", "Beto", "Zoe"], grupo.Jugadores.Select(jugador => jugador.Nombres));
    }

    [Fact]
    public void Los_equipos_son_solo_los_activos_por_nombre_y_la_foto_sale_de_las_fechas()
    {
        var categoria = Categoria(2014);
        var jugador = Jugador("Ana", "Uno", categoria);
        jugador.Equipos =
        [
            new JugadorEquipo { Equipo = new Equipo { Nombre = "b", Activo = true } },
            new JugadorEquipo { Equipo = new Equipo { Nombre = "Viejo", Activo = false } },
            new JugadorEquipo { Equipo = new Equipo { Nombre = "A", Activo = true } },
        ];
        var guardadaEn = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

        var dto = MapperListaJugadores.AGrupos([jugador], new Dictionary<Guid, DateTime> { [jugador.Id] = guardadaEn })
            .Single().Jugadores.Single();

        Assert.Equal(["A", "b"], dto.Equipos);
        Assert.Equal(guardadaEn, dto.FotoActualizadaEn);
    }

    private static Categoria Categoria(int anio) => new() { Id = Guid.NewGuid(), Anio = anio, Activa = true };

    private static UsuarioRol Jugador(string nombres, string apellidos, Categoria? categoria) => new()
    {
        Rol = Rol.JUGADOR,
        EstadoIngreso = EstadoIngreso.APROBADO,
        Nombres = nombres,
        Apellidos = apellidos,
        Categoria = categoria,
        CategoriaId = categoria?.Id,
    };
}
