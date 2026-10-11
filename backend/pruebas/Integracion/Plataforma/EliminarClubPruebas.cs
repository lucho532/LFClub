using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Plataforma;

/// <summary>
/// Eliminar un club borra toda su información y no afecta a ningún otro club (constitución §20,
/// §7.4, RF-029, CE-008).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EliminarClubPruebas
{
    private readonly FabricaApi _fabrica;

    public EliminarClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(EstadoClub.ACTIVO)]
    [InlineData(EstadoClub.SUSPENDIDO)]
    public async Task Un_club_que_no_esta_dado_de_baja_no_se_puede_eliminar(EstadoClub estado)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: estado);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id), new { nombreDeConfirmacion = club.Nombre });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("club_no_dado_de_baja", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.True(await ExisteClubAsync(club.Id));
    }

    [Theory]
    [InlineData("otro nombre")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Una_confirmacion_incorrecta_responde_400_y_no_borra_nada(string? confirmacion)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.DADO_DE_BAJA);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var antes = await FilasPorTablaAsync(club.Id);

        var respuesta = await cliente.PostAsync(Ruta(club.Id), new { nombreDeConfirmacion = confirmacion });
        var enMinusculas = await cliente.PostAsync(Ruta(club.Id), new { nombreDeConfirmacion = club.Nombre.ToLowerInvariant() });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("confirmacion_no_coincide", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(HttpStatusCode.BadRequest, enMinusculas.StatusCode);
        Assert.Equal(antes, await FilasPorTablaAsync(club.Id));
    }

    [Fact]
    public async Task Tras_eliminar_no_queda_ninguna_fila_del_club_y_los_demas_clubes_quedan_intactos()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (soloDeEste, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (deLosDos, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, deLosDos, Rol.PRESIDENTE);
        var (soloDelOtro, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.JUGADOR);
        await _fabrica.Sembrador.CrearInvitacionAsync(club);
        await _fabrica.Sembrador.CrearInvitacionAsync(otroClub);
        await SembrarCategoriaConEquipoAsync(club);
        await SembrarCategoriaConEquipoAsync(otroClub);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await cliente.PutArchivoAsync($"/api/plataforma/clubes/{club.Id}/escudo", Imagenes.Png());
        await cliente.PutArchivoAsync($"/api/plataforma/clubes/{otroClub.Id}/escudo", Imagenes.Png());
        await cliente.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "DADO_DE_BAJA" });
        var delQueTieneDos = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(deLosDos);
        var delOtroAntes = await FilasPorTablaAsync(otroClub.Id);
        Assert.All((await FilasPorTablaAsync(club.Id)).Values, filas => Assert.True(filas > 0));

        var respuesta = await cliente.PostAsync(Ruta(club.Id), new { nombreDeConfirmacion = $" {club.Nombre} " });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.All((await FilasPorTablaAsync(club.Id)).Values, filas => Assert.Equal(0, filas));
        Assert.Equal(delOtroAntes, await FilasPorTablaAsync(otroClub.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/plataforma/clubes/{club.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _fabrica.CrearClienteDePrueba().GetAsync($"/api/publico/clubes/{club.Id}/escudo")).StatusCode);

        // Quien solo pertenecía a ese club ya no tiene cuenta; quien tenía otro club sigue entrando.
        Assert.False(await ExisteCuentaAsync(soloDeEste.Id));
        Assert.True(await ExisteCuentaAsync(deLosDos.Id));
        Assert.True(await ExisteCuentaAsync(soloDelOtro.Id));
        Assert.Equal(HttpStatusCode.OK, (await delQueTieneDos.GetAsync($"/api/clubes/{otroClub.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(soloDeEste.Correo)).StatusCode);

        // La cuenta DESARROLLADOR sigue existiendo y el nombre del club queda libre.
        Assert.Equal(1, await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.CountAsync(u => u.EsDesarrollador)));
        var otraVez = await cliente.PostAsync("/api/plataforma/clubes", new { nombre = club.Nombre, correoPresidente = Sembrador.CorreoUnico() });
        Assert.Equal(HttpStatusCode.Created, otraVez.StatusCode);
    }

    [Fact]
    public async Task Solo_el_desarrollador_elimina_y_un_club_inexistente_responde_404()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.DADO_DE_BAJA);
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.PRESIDENTE);
        var delPresidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var cuerpo = new { nombreDeConfirmacion = club.Nombre };

        Assert.Equal(HttpStatusCode.Forbidden, (await delPresidente.PostAsync(Ruta(club.Id), cuerpo)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fabrica.CrearClienteDePrueba().PostAsync(Ruta(club.Id), cuerpo)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await desarrollador.PostAsync(Ruta(Guid.NewGuid()), cuerpo)).StatusCode);
        Assert.True(await ExisteClubAsync(club.Id));
    }

    private static string Ruta(Guid clubId) => $"/api/plataforma/clubes/{clubId}/eliminacion";

    /// <summary>
    /// Una categoría con un equipo, un jugador en él, con su ficha y un documento entregado, y un
    /// entrenador que lo dirige.
    /// </summary>
    private async Task SembrarCategoriaConEquipoAsync(Club club)
    {
        var categoria = await _fabrica.Categorias.CrearCategoriaAsync(club, 2014);
        var equipo = await _fabrica.Categorias.CrearEquipoAsync(categoria, "A");
        var (_, jugador) = await _fabrica.Categorias.CrearJugadorAsync(club, 2014, categoria);
        var (_, entrenador) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR);
        var asignacion = await _fabrica.Categorias.AsignarEntrenadorAsync(categoria, entrenador);
        await _fabrica.Categorias.PonerEnEquipoAsync(jugador, equipo);
        await _fabrica.Categorias.DirigirEquipoAsync(asignacion, equipo);
        await _fabrica.Fichas.CrearFichaAsync(jugador);
        await _fabrica.Fichas.CrearDocumentoAsync(
            jugador, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(), "application/pdf");
        await _fabrica.Jugadores.PonerFotoAsync(jugador, Imagenes.Png());

        // Spec 009: un entrenamiento con asistencia y respuesta, y un partido con el jugador convocado.
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([categoria], new DateOnly(2026, 10, 13));
        await _fabrica.Actividades.RegistrarAsistenciaAsync(entrenamiento, jugador, presente: true);
        await _fabrica.Actividades.ResponderAsync(entrenamiento, jugador, RespuestaFamilia.ASISTIRA);
        await _fabrica.Actividades.CrearPartidoAsync([categoria], new DateOnly(2026, 10, 17), convocados: [jugador]);
    }

    private Task<bool> ExisteClubAsync(Guid clubId) =>
        _fabrica.ConContextoAsync(contexto => contexto.Clubes.AnyAsync(club => club.Id == clubId));

    private Task<bool> ExisteCuentaAsync(Guid usuarioId) =>
        _fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(usuario => usuario.Id == usuarioId));

    /// <summary>
    /// Número de filas de ese club en cada tabla que pertenece a un club, recorriendo el modelo:
    /// así una tabla añadida por una funcionalidad futura queda cubierta sin tocar esta prueba.
    /// </summary>
    private Task<SortedDictionary<string, int>> FilasPorTablaAsync(Guid clubId) => _fabrica.ConContextoAsync(async contexto =>
    {
        var filas = new SortedDictionary<string, int>();
        var tablas = contexto.Model.GetEntityTypes()
            .Where(tipo => typeof(IPerteneceAClub).IsAssignableFrom(tipo.ClrType))
            .Select(tipo => tipo.GetTableName()!)
            .ToList();

        foreach (var tabla in tablas)
        {
#pragma warning disable EF1002 // El nombre de la tabla sale del modelo, no de la petición.
            filas[tabla] = await contexto.Database
                .SqlQueryRaw<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"{tabla}\" WHERE \"ClubId\" = {{0}}", clubId)
                .SingleAsync();
#pragma warning restore EF1002
        }

        filas["Clubes"] = await contexto.Clubes.CountAsync(club => club.Id == clubId);
        return filas;
    });
}
