using System.Net;
using System.Text;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Nadie de otro club ve ni cambia una categoría, un equipo o un jugador de este, ni siquiera con
/// sus identificadores reales (constitución §7.1, RF-037, CE-008), y dentro del club solo el
/// PRESIDENTE cambia algo (RF-036). El presidente del otro club llama en SU club, donde sí tiene
/// permiso, con identificadores de este: el caso en que solo protege el filtro de aislamiento.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AislamientoCategoriasPruebas
{
    private readonly FabricaApi _fabrica;

    public AislamientoCategoriasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_presidente_de_otro_club_recibe_404_con_el_identificador_real_de_una_categoria()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);
        var enSuClub = $"/api/clubes/{e.OtroClub.Id}/categorias";

        await ComprobarAsync(e.PresidenteDeOtroClub, HttpStatusCode.NotFound, "no_encontrado",
            ("GET", $"{enSuClub}/{categoria.Id}", null),
            ("POST", $"{enSuClub}/{categoria.Id}/desactivacion", null),
            ("POST", $"{enSuClub}/{inactiva.Id}/reactivacion", null),
            ("DELETE", $"{enSuClub}/{categoria.Id}", null));

        Assert.True((await e.CategoriaGuardadaAsync(categoria.Id))!.Activa);
        Assert.False((await e.CategoriaGuardadaAsync(inactiva.Id))!.Activa);
    }

    [Fact]
    public async Task Un_directivo_un_entrenador_y_un_jugador_no_desactivan_reactivan_ni_borran()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);

        foreach (var (_, cliente) in e.QuienesNoGestionan)
        {
            await ComprobarAsync(cliente, HttpStatusCode.Forbidden, "rol_no_autorizado",
                ("POST", e.Desactivacion(categoria.Id), null),
                ("POST", e.Reactivacion(inactiva.Id), null),
                ("DELETE", e.RutaCategoria(categoria.Id), null));
        }

        Assert.True((await e.CategoriaGuardadaAsync(categoria.Id))!.Activa);
        Assert.False((await e.CategoriaGuardadaAsync(inactiva.Id))!.Activa);
    }

    /// <summary>Envía cada petición y comprueba que todas responden ese estado y ese código.</summary>
    internal static async Task ComprobarAsync(
        ClienteDePrueba cliente, HttpStatusCode estado, string codigo, params (string Metodo, string Ruta, string? Cuerpo)[] peticiones)
    {
        foreach (var (metodo, ruta, cuerpo) in peticiones)
        {
            var peticion = new HttpRequestMessage(new HttpMethod(metodo), ruta);
            if (metodo is "POST" or "PUT")
            {
                peticion.Content = new StringContent(cuerpo ?? "{}", Encoding.UTF8, "application/json");
            }

            var respuesta = await cliente.Http.SendAsync(peticion);

            Assert.True(respuesta.StatusCode == estado, $"{metodo} {ruta}: {(int)respuesta.StatusCode}");
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }
}
