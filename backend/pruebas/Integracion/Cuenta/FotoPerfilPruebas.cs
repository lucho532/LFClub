using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>Foto de perfil: cada cuenta la suya y nadie la de otra (RF-036 a RF-038).</summary>
[Collection(ColeccionApi.Nombre)]
public class FotoPerfilPruebas
{
    private const string Ruta = "/api/cuenta/foto";

    private readonly FabricaApi _fabrica;

    public FotoPerfilPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public static TheoryData<byte[], string> Formatos => new()
    {
        { Imagenes.Png(), "image/png" },
        { Imagenes.Jpeg(), "image/jpeg" },
        { Imagenes.Webp(), "image/webp" },
    };

    [Theory]
    [MemberData(nameof(Formatos))]
    public async Task Se_guarda_y_la_obtiene_su_dueno_con_el_tipo_detectado_por_su_firma(byte[] imagen, string tipo)
    {
        var cliente = await ClienteNuevoAsync();

        var respuesta = await cliente.PutArchivoAsync(Ruta, imagen, "foto.txt");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(1, (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("versionFoto").GetInt32());

        var foto = await cliente.GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.OK, foto.StatusCode);
        Assert.Equal(tipo, foto.Content.Headers.ContentType?.MediaType);
        Assert.Equal(imagen, await foto.Content.ReadAsByteArrayAsync());
        Assert.Contains("no-store", foto.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Lo_que_no_es_imagen_o_pasa_de_1_mb_se_rechaza_y_se_conserva_la_anterior()
    {
        var cliente = await ClienteNuevoAsync();
        var anterior = Imagenes.Jpeg(40);
        await cliente.PutArchivoAsync(Ruta, anterior);

        var texto = await cliente.PutArchivoAsync(Ruta, Imagenes.Texto(), "foto.png");
        var svg = await cliente.PutArchivoAsync(Ruta, Imagenes.Svg());
        var grande = await cliente.PutArchivoAsync(Ruta, Imagenes.PngDeMasDe1Mb());

        Assert.Equal(HttpStatusCode.BadRequest, texto.StatusCode);
        Assert.Equal("foto_no_es_imagen", await ClienteDePrueba.CodigoAsync(texto));
        Assert.Equal("foto_no_es_imagen", await ClienteDePrueba.CodigoAsync(svg));
        Assert.Equal(HttpStatusCode.BadRequest, grande.StatusCode);
        Assert.Equal("foto_demasiado_grande", await ClienteDePrueba.CodigoAsync(grande));

        Assert.Equal(anterior, await (await cliente.GetAsync(Ruta)).Content.ReadAsByteArrayAsync());
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        Assert.Equal(1, sesion.GetProperty("versionFoto").GetInt32());
    }

    [Fact]
    public async Task Sin_sesion_las_tres_operaciones_responden_401()
    {
        var cliente = _fabrica.CrearClienteDePrueba();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PutArchivoAsync(Ruta, Imagenes.Png())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.DeleteAsync(Ruta)).StatusCode);
    }

    [Fact]
    public async Task Cada_sesion_obtiene_solo_su_propia_foto()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (una, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (otra, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var (sinFoto, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        var deUna = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(una);
        var deOtra = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(otra);
        var deSinFoto = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(sinFoto);
        var fotoDeUna = Imagenes.Png(10);
        var fotoDeOtra = Imagenes.Webp(20);
        await deUna.PutArchivoAsync(Ruta, fotoDeUna);
        await deOtra.PutArchivoAsync(Ruta, fotoDeOtra);

        Assert.Equal(fotoDeUna, await (await deUna.GetAsync(Ruta)).Content.ReadAsByteArrayAsync());
        Assert.Equal(fotoDeOtra, await (await deOtra.GetAsync(Ruta)).Content.ReadAsByteArrayAsync());

        // Aunque comparten club, quien no tiene foto recibe 404, no la de otra persona; y no hay
        // ninguna ruta que acepte el identificador de otra cuenta.
        var propia = await deSinFoto.GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.NotFound, propia.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(propia));
        Assert.Equal(HttpStatusCode.NotFound, (await deSinFoto.GetAsync($"{Ruta}/{una.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await deSinFoto.GetAsync($"{Ruta}?usuarioId={una.Id}")).StatusCode);
    }

    [Fact]
    public async Task Cambiarla_sube_la_version_y_quitarla_la_deja_en_cero()
    {
        var cliente = await ClienteNuevoAsync();
        await cliente.PutArchivoAsync(Ruta, Imagenes.Png());

        var cambio = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.PutArchivoAsync(Ruta, Imagenes.Jpeg()));
        var quitar = await cliente.DeleteAsync(Ruta);
        var quitarOtraVez = await cliente.DeleteAsync(Ruta);

        Assert.Equal(2, cambio.GetProperty("versionFoto").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, quitar.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, quitarOtraVez.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync(Ruta)).StatusCode);
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        Assert.Equal(0, sesion.GetProperty("versionFoto").GetInt32());
    }

    [Fact]
    public async Task El_desarrollador_tambien_tiene_la_suya()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutArchivoAsync(Ruta, Imagenes.Png());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("esDesarrollador").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync(Ruta)).StatusCode);
    }

    [Fact]
    public async Task La_foto_se_borra_al_borrarse_la_cuenta()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        await cliente.PutArchivoAsync(Ruta, Imagenes.Png());
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        Assert.True(await TieneFotoAsync(usuario.Id));

        // Era su único club: al eliminarlo del club su cuenta deja de existir.
        await desarrollador.PostAsync(
            $"/api/plataforma/clubes/{club.Id}/presidentes/{integrante.Id}/retiro", new { accion = "ELIMINAR_DEL_CLUB" });

        Assert.False(await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(u => u.Id == usuario.Id)));
        Assert.False(await TieneFotoAsync(usuario.Id));
    }

    private async Task<ClienteDePrueba> ClienteNuevoAsync()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        return await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
    }

    private Task<bool> TieneFotoAsync(Guid usuarioId) =>
        _fabrica.ConContextoAsync(contexto => contexto.FotosPerfil.AnyAsync(foto => foto.UsuarioId == usuarioId));
}
