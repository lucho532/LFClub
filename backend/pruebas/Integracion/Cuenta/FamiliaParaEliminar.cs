using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Una cuenta de familia con tres jugadores en dos clubes, sembrada para las pruebas de la
/// eliminación de cuentas (spec 014): dos en el club A (uno de ellos retirado) y uno en espera en
/// el club B; cada club con su presidente y una categoría 2014.
/// </summary>
public record FamiliaParaEliminar(
    Usuario Cuenta,
    UsuarioRol JugadorA1,
    UsuarioRol JugadorA2Retirado,
    UsuarioRol JugadorBEnEspera,
    Club ClubA,
    Club ClubB,
    Categoria CategoriaA,
    Persona PresidenteA,
    Persona PresidenteB)
{
    public const string Ruta = "/api/cuenta/eliminacion";

    public IEnumerable<UsuarioRol> Jugadores => [JugadorA1, JugadorA2Retirado, JugadorBEnEspera];

    public static async Task<FamiliaParaEliminar> CrearAsync(FabricaApi fabrica)
    {
        var sembrador = fabrica.Sembrador;
        var categorias = fabrica.Categorias;
        var clubA = await sembrador.CrearClubAsync();
        var clubB = await sembrador.CrearClubAsync();
        var categoriaA = await categorias.CrearCategoriaAsync(clubA, 2014);
        var categoriaB = await categorias.CrearCategoriaAsync(clubB, 2014);

        var presidenteA = await PersonaAsync(fabrica, await sembrador.CrearIntegranteAsync(clubA, Rol.PRESIDENTE));
        var presidenteB = await PersonaAsync(fabrica, await sembrador.CrearIntegranteAsync(clubB, Rol.PRESIDENTE));

        var (cuenta, a1) = await categorias.CrearJugadorAsync(clubA, 2014, categoriaA, nombres: "Lucía", apellidos: "Familia");
        var a2 = await sembrador.CrearIntegranteAsync(clubA, cuenta, Rol.JUGADOR, nombres: "Mateo");
        var b1 = await sembrador.CrearIntegranteAsync(
            clubB, cuenta, Rol.JUGADOR, nombres: "Sara", estadoIngreso: EstadoIngreso.EN_ESPERA);

        await fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(fila => fila.Id == a2.Id)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(fila => fila.CategoriaId, categoriaA.Id)
                .SetProperty(fila => fila.Activo, false)
                .SetProperty(fila => fila.RetiradoEn, DateTime.UtcNow)));
        a2.CategoriaId = categoriaA.Id;
        a2.Activo = false;

        return new FamiliaParaEliminar(cuenta, a1, a2, b1, clubA, clubB, categoriaA, presidenteA, presidenteB);
    }

    public static async Task<Persona> PersonaAsync(FabricaApi fabrica, (Usuario Usuario, UsuarioRol Integrante) par) =>
        new(par.Usuario, par.Integrante, await fabrica.CrearClienteDePrueba().ConSesionDeAsync(par.Usuario));

    public Task<ClienteDePrueba> ClienteAsync(FabricaApi fabrica) =>
        fabrica.CrearClienteDePrueba().ConSesionDeAsync(Cuenta);

    public static Task<bool> ExisteCuentaAsync(FabricaApi fabrica, Guid usuarioId) =>
        fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(fila => fila.Id == usuarioId));

    public static Task<bool> ExisteIntegranteAsync(FabricaApi fabrica, Guid integranteId) =>
        fabrica.ConContextoAsync(contexto =>
            contexto.UsuariosRol.IgnoreQueryFilters().AnyAsync(fila => fila.Id == integranteId));

    public static async Task<JsonElement> CuerpoAsync(HttpResponseMessage respuesta) =>
        await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
}
