using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>
/// Arma el club de "Datos de partida" de la guía de la 009 directamente en la base de datos, con
/// un cliente con sesión para cada integrante, y escribe entrenamientos, partidos y resultados sin
/// pasar por la API, para las pruebas que solo los leen.
/// </summary>
public class SembradorActividades
{
    private readonly FabricaApi _fabrica;

    public SembradorActividades(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Club A con 2013 a 2016 activas; P sin asignación; E1 en la 2014; E2 en 2014 y 2015; D1 en la
    /// 2014; D2 sin asignar; J1 a J10 en la 2014, salvo J2, hermano de J1 en la misma cuenta, que
    /// está en la 2015; K1 en la 2015; M1 en la 2016. Club B con PB y un jugador.
    /// </summary>
    public async Task<EscenarioActividades> CrearClubAsync()
    {
        var sembrador = _fabrica.Sembrador;
        var categorias = _fabrica.Categorias;
        var club = await sembrador.CrearClubAsync();
        var c2013 = await categorias.CrearCategoriaAsync(club, 2013);
        var c2014 = await categorias.CrearCategoriaAsync(club, 2014);
        var c2015 = await categorias.CrearCategoriaAsync(club, 2015);
        var c2016 = await categorias.CrearCategoriaAsync(club, 2016);

        var p = await PersonaAsync(await sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE));
        var e1 = await PersonaAsync(await sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR));
        var e2 = await PersonaAsync(await sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR));
        var d1 = await PersonaAsync(await sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO));
        var d2 = await PersonaAsync(await sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO));
        await categorias.AsignarEntrenadorAsync(c2014, e1.Integrante);
        await categorias.AsignarEntrenadorAsync(c2014, e2.Integrante);
        await categorias.AsignarEntrenadorAsync(c2015, e2.Integrante);
        await categorias.AsignarEntrenadorAsync(c2014, d1.Integrante);

        var jugadores = new List<Persona>();
        for (var numero = 1; numero <= 10; numero++)
        {
            if (numero == 2)
            {
                var j1 = jugadores[0];
                var j2 = await _fabrica.Hermanos.CrearHermanoAsync(
                    j1.Integrante, EstadoIngreso.APROBADO, nombres: "J2", categoria: c2015);
                jugadores.Add(await PersonaAsync((j1.Cuenta, j2), elegido: true));
                jugadores[0] = await PersonaAsync((j1.Cuenta, j1.Integrante), elegido: true);
                continue;
            }

            jugadores.Add(await JugadorAsync(club, c2014, $"J{numero}", $"Apellido{numero:00}"));
        }

        var k1 = await JugadorAsync(club, c2015, "K1", "Kapellido");
        var m1 = await JugadorAsync(club, c2016, "M1", "Mapellido");

        var clubB = await sembrador.CrearClubAsync();
        var cB = await categorias.CrearCategoriaAsync(clubB, 2014);
        var pb = await PersonaAsync(await sembrador.CrearIntegranteAsync(clubB, Rol.PRESIDENTE));
        var jugadorB = await JugadorAsync(clubB, cB, "JB", "Bapellido");

        return new EscenarioActividades(
            club, c2013, c2014, c2015, c2016, p, e1, e2, d1, d2, jugadores, k1, m1, clubB, cB, pb, jugadorB);
    }

    /// <summary>Entrenamiento escrito en la base, para esas categorías.</summary>
    public Task<Entrenamiento> CrearEntrenamientoAsync(
        IEnumerable<Categoria> categorias,
        DateOnly fecha,
        TimeOnly? horaInicio = null,
        TimeOnly? horaFin = null,
        EstadoEntrenamiento estado = EstadoEntrenamiento.PROGRAMADO) => GuardarAsync(categorias, lista =>
        new Entrenamiento
        {
            ClubId = lista[0].ClubId,
            Fecha = fecha,
            HoraInicio = horaInicio ?? new TimeOnly(16, 0),
            HoraFin = horaFin ?? new TimeOnly(17, 30),
            Lugar = "Cancha de Minitas",
            Estado = estado,
            CreadoEn = DateTime.UtcNow,
            Categorias = lista
                .Select(categoria => new EntrenamientoCategoria { CategoriaId = categoria.Id, ClubId = categoria.ClubId })
                .ToList(),
        });

    /// <summary>Partido escrito en la base, con esos convocados y la categoría actual de cada uno copiada.</summary>
    public Task<Partido> CrearPartidoAsync(
        IEnumerable<Categoria> categorias,
        DateOnly fecha,
        TimeOnly? hora = null,
        IEnumerable<UsuarioRol>? convocados = null,
        EstadoPartido estado = EstadoPartido.PROXIMO) => GuardarAsync(categorias, lista =>
        new Partido
        {
            ClubId = lista[0].ClubId,
            Rival = "Equipo C",
            Fecha = fecha,
            Hora = hora ?? new TimeOnly(9, 0),
            Lugar = "Cancha de Minitas",
            Condicion = CondicionPartido.LOCAL,
            Tipo = TipoPartido.AMISTOSO,
            Estado = estado,
            CreadoEn = DateTime.UtcNow,
            Categorias = lista
                .Select(categoria => new PartidoCategoria { CategoriaId = categoria.Id, ClubId = categoria.ClubId })
                .ToList(),
            Jugadores = (convocados ?? [])
                .Select(jugador => new PartidoJugador
                {
                    UsuarioRolId = jugador.Id,
                    ClubId = jugador.ClubId,
                    CategoriaId = jugador.CategoriaId!.Value,
                    ConvocadoEn = DateTime.UtcNow,
                })
                .ToList(),
        });

    /// <summary>
    /// Deja el partido jugado con ese marcador: los jugadores indicados jugaron con sus goles; los
    /// demás convocados, no.
    /// </summary>
    public Task RegistrarJugadoAsync(
        Partido partido, int golesClub, int golesRival, params (UsuarioRol Jugador, int Goles)[] jugaron) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            await contexto.Partidos.IgnoreQueryFilters()
                .Where(fila => fila.Id == partido.Id)
                .ExecuteUpdateAsync(cambios => cambios
                    .SetProperty(fila => fila.Estado, EstadoPartido.JUGADO)
                    .SetProperty(fila => fila.GolesClub, golesClub)
                    .SetProperty(fila => fila.GolesRival, golesRival)
                    .SetProperty(fila => fila.ResultadoCambiadoEn, DateTime.UtcNow)
                    .SetProperty(fila => fila.ResultadoCambiadoPorNombre, "Sembrador"));

            foreach (var (jugador, goles) in jugaron)
            {
                await contexto.PartidosJugador.IgnoreQueryFilters()
                    .Where(fila => fila.PartidoId == partido.Id && fila.UsuarioRolId == jugador.Id)
                    .ExecuteUpdateAsync(cambios => cambios
                        .SetProperty(fila => fila.Jugo, true)
                        .SetProperty(fila => fila.Goles, goles));
            }

            return 0;
        });

    /// <summary>Asistencia escrita en la base, con la categoría actual del jugador copiada.</summary>
    public Task RegistrarAsistenciaAsync(Entrenamiento entrenamiento, UsuarioRol jugador, bool presente) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.AsistenciasEntrenamiento.Add(new AsistenciaEntrenamiento
            {
                EntrenamientoId = entrenamiento.Id,
                UsuarioRolId = jugador.Id,
                ClubId = jugador.ClubId,
                CategoriaId = jugador.CategoriaId!.Value,
                Presente = presente,
                RegistradaEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        });

    /// <summary>Respuesta de la familia escrita en la base.</summary>
    public Task ResponderAsync(Entrenamiento entrenamiento, UsuarioRol jugador, RespuestaFamilia respuesta) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.RespuestasEntrenamiento.Add(new RespuestaEntrenamiento
            {
                EntrenamientoId = entrenamiento.Id,
                UsuarioRolId = jugador.Id,
                ClubId = jugador.ClubId,
                Respuesta = respuesta,
                RespondidaEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        });

    /// <summary>Cambia al jugador de categoría directamente en la base (y la copia que lleva el objeto).</summary>
    public Task CambiarCategoriaAsync(UsuarioRol jugador, Categoria categoria) => _fabrica.ConContextoAsync(contexto =>
    {
        jugador.CategoriaId = categoria.Id;
        return contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(fila => fila.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.CategoriaId, categoria.Id));
    });

    private async Task<T> GuardarAsync<T>(IEnumerable<Categoria> categorias, Func<List<Categoria>, T> crear)
        where T : class
    {
        var entidad = crear(categorias.ToList());
        return await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.Add(entidad);
            await contexto.SaveChangesAsync();
            return entidad;
        });
    }

    private async Task<Persona> JugadorAsync(Club club, Categoria categoria, string nombres, string apellidos) =>
        await PersonaAsync(await _fabrica.Categorias.CrearJugadorAsync(
            club, categoria.Anio, categoria, nombres, $"{apellidos} {Sembrador.Unico()}"));

    private async Task<Persona> PersonaAsync((Usuario Cuenta, UsuarioRol Integrante) persona, bool elegido = false)
    {
        var cliente = await _fabrica.CrearClienteDePrueba()
            .ElegirJugador(elegido ? persona.Integrante.Id : null)
            .ConSesionDeAsync(persona.Cuenta);
        return new Persona(persona.Cuenta, persona.Integrante, cliente);
    }
}
