using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Ayudas comunes a las pruebas de invitaciones, registro, aprobación, rechazo y consulta de
/// ingresos: rutas, clientes con la sesión de un integrante nuevo, registro con una invitación y
/// lectura directa de un integrante en la base de datos.
/// </summary>
internal static class EscenarioIngresos
{
    /// <summary>Contraseña de quien se registra con <see cref="DatosDeRegistro"/>.</summary>
    public const string ContrasenaDeRegistro = "mi-contrasena-propia";

    public static string Invitaciones(Club club) => $"/api/clubes/{club.Id}/invitaciones";

    public static string Reenvio(Club club, Guid invitacionId) => $"{Invitaciones(club)}/{invitacionId}/reenvio";

    public static string Cancelacion(Club club, Guid invitacionId) => $"{Invitaciones(club)}/{invitacionId}/cancelacion";

    public static string EnEspera(Club club) => $"/api/clubes/{club.Id}/ingresos/en-espera";

    public static string Aprobados(Club club) => $"/api/clubes/{club.Id}/ingresos/aprobados";

    public static string Aprobacion(Club club, Guid usuarioRolId) =>
        $"/api/clubes/{club.Id}/ingresos/{usuarioRolId}/aprobacion";

    public static string Rechazo(Club club, Guid usuarioRolId) =>
        $"/api/clubes/{club.Id}/ingresos/{usuarioRolId}/rechazo";

    /// <summary>Crea un integrante aprobado con ese rol y devuelve un cliente con su sesión.</summary>
    public static async Task<ClienteDePrueba> ClienteDeAsync(this FabricaApi fabrica, Club club, Rol rol)
    {
        var (usuario, _) = await fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        return await fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
    }

    /// <summary>Invita a ese correo con ese rol y devuelve la respuesta de la API.</summary>
    public static Task<HttpResponseMessage> InvitarAsync(this ClienteDePrueba cliente, Club club, string correo, Rol rol) =>
        cliente.PostAsync(Invitaciones(club), new { correo, rol });

    /// <summary>
    /// Cuerpo de <c>POST /api/invitaciones/registro</c> con valores válidos por defecto: un adulto
    /// con un documento único.
    /// </summary>
    public static object DatosDeRegistro(
        string token, string? numeroDocumento = null, DateOnly? fechaNacimiento = null, string? nombreResponsable = null) => new
        {
            token,
            nombres = "Ana",
            apellidos = "Pérez",
            tipoDocumento = "CEDULA_CIUDADANIA",
            numeroDocumento = numeroDocumento ?? Sembrador.Unico("doc"),
            fechaNacimiento = (fechaNacimiento ?? new DateOnly(1988, 3, 15)).ToString("yyyy-MM-dd"),
            celular = "3001234567",
            nombreResponsable,
            contrasena = ContrasenaDeRegistro,
        };

    /// <summary>
    /// Siembra una invitación de ese rol, registra a la persona por la API y devuelve su cliente
    /// con la sesión iniciada y su integrante tal como quedó guardado.
    /// </summary>
    public static async Task<(ClienteDePrueba Cliente, UsuarioRol Integrante)> RegistrarConInvitacionAsync(
        this FabricaApi fabrica, Club club, Rol rol, DateOnly? fechaNacimiento = null)
    {
        var (_, token) = await fabrica.Sembrador.CrearInvitacionAsync(club, rol: rol);
        var documento = Sembrador.Unico("doc");
        var cliente = fabrica.CrearClienteDePrueba();

        // El responsable solo se pide al jugador, y es obligatorio si es menor.
        var respuesta = await cliente.PostAsync(
            "/api/invitaciones/registro",
            DatosDeRegistro(token, documento, fechaNacimiento, rol == Rol.JUGADOR ? "Marta Gómez" : null));
        Assert.Equal(System.Net.HttpStatusCode.Created, respuesta.StatusCode);
        cliente.UsarToken((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("token").GetString()!);

        return (cliente, await fabrica.IntegranteDeDocumentoAsync(club, documento));
    }

    /// <summary>El integrante de ese club con ese documento, tal como está guardado.</summary>
    public static Task<UsuarioRol> IntegranteDeDocumentoAsync(this FabricaApi fabrica, Club club, string documento) =>
        fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(fila => fila.ClubId == club.Id && fila.NumeroDocumento == documento));

    /// <summary>La cuenta tal como está guardada.</summary>
    public static Task<Usuario> CuentaAsync(this FabricaApi fabrica, Guid usuarioId) =>
        fabrica.ConContextoAsync(contexto => contexto.Usuarios.AsNoTracking().SingleAsync(usuario => usuario.Id == usuarioId));

    /// <summary>El integrante tal como está guardado, o nulo si ya no existe.</summary>
    public static Task<UsuarioRol?> IntegranteAsync(this FabricaApi fabrica, Guid usuarioRolId) =>
        fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(integrante => integrante.Id == usuarioRolId));

    /// <summary>Indica si la cuenta sigue existiendo.</summary>
    public static Task<bool> ExisteCuentaAsync(this FabricaApi fabrica, Guid usuarioId) =>
        fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(usuario => usuario.Id == usuarioId));

    /// <summary>Lee una lista JSON de la API.</summary>
    public static async Task<List<JsonElement>> ListaAsync(this ClienteDePrueba cliente, string ruta) =>
        (await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(ruta))).EnumerateArray().ToList();
}
