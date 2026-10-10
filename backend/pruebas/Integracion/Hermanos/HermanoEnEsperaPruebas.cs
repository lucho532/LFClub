using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Pruebas.Integracion.Aislamiento;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Ficha;
using LaPecosa.Pruebas.Integracion.Ingresos;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Mientras el hermano está en espera no aparece en ninguna lista de jugadores del club ni obtiene
/// nada de él, y los demás jugadores de la cuenta siguen con normalidad (historia 1, escenario 4;
/// RF-012 a RF-014; CE-003).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class HermanoEnEsperaPruebas
{
    private readonly FabricaApi _fabrica;

    public HermanoEnEsperaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private List<Endpoint> DeClub => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => endpoint.Ruta.StartsWith("/api/clubes/{clubId}", StringComparison.Ordinal))
        .ToList();

    /// <summary>Un hermano de Ana agregado por la API, con un nombre que no tiene nadie más.</summary>
    private async Task<(EscenarioHermanos E, UsuarioRol Hermano)> ConHermanoEnEsperaAsync()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var cuerpo = EscenarioHermanos.DatosDeHermano(nombreResponsable: "Marta Gómez");
        cuerpo["nombres"] = "Zacarías";
        var respuesta = await (await e.ClienteDeLaFamiliaAsync()).PostAsync(e.Hermanos(e.Ana.Id), cuerpo);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        return (e, (await e.JugadoresDeLaFamiliaAsync()).Last());
    }

    // Escenario 1.4, RF-012 y CE-003. Nació en el año de una categoría activa y aun así no está en ella.
    [Fact]
    public async Task No_aparece_en_ninguna_lista_de_jugadores_del_club_para_ningun_rol()
    {
        var (e, hermano) = await ConHermanoEnEsperaAsync();
        string[] listas =
        [
            e.Base.Categorias,
            e.Base.RutaCategoria(e.CategoriaDeAna.Id),
            e.Base.Candidatos(e.CategoriaDeAna.Id),
            e.Base.SinCategoria,
            e.Base.Retirados,
            EscenarioIngresos.Aprobados(e.Club),
            e.Base.MiCategoria,
        ];
        (string Rol, ClienteDePrueba Cliente)[] quienes =
        [
            ("PRESIDENTE", e.Presidente),
            ("DIRECTIVO", e.Directivo),
            ("ENTRENADOR", e.Entrenador),
            ("su familia", await e.ClienteDeLaFamiliaAsync(e.Ana.Id)),
        ];
        var vistas = 0;

        foreach (var (rol, cliente) in quienes)
        {
            foreach (var lista in listas)
            {
                var respuesta = await cliente.GetAsync(lista);
                var cuerpo = await respuesta.Content.ReadAsStringAsync();
                vistas += respuesta.IsSuccessStatusCode ? 1 : 0;

                Assert.DoesNotContain(hermano.Id.ToString(), cuerpo);
                Assert.DoesNotContain(hermano.NumeroDocumento, cuerpo);
                Assert.False(cuerpo.Contains("Zacarías", StringComparison.Ordinal), $"{rol} {lista}");
            }
        }

        // El presidente ve las seis listas del club; cada uno de los demás, al menos una.
        Assert.True(vistas >= 9, $"Listas respondidas: {vistas}");

        // Solo aparece en la sala de espera.
        Assert.Contains("Zacarías", await (await e.Presidente.GetAsync(EscenarioIngresos.EnEspera(e.Club))).Content.ReadAsStringAsync());
    }

    // RF-013: elegido, no obtiene nada del club; tampoco de un endpoint futuro.
    [Fact]
    public async Task Elegido_recibe_403_en_todos_los_endpoints_del_club_y_ningun_dato()
    {
        var (e, hermano) = await ConHermanoEnEsperaAsync();
        var familia = await e.ClienteDeLaFamiliaAsync(hermano.Id);
        Assert.NotEmpty(DeClub);

        foreach (var endpoint in DeClub)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(familia, endpoint, e.Club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(e.Club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }

        // Tampoco su propia ficha.
        var ficha = await familia.GetAsync(EscenarioFicha.Ficha(e.Club.Id, hermano.Id));
        Assert.Equal(HttpStatusCode.Forbidden, ficha.StatusCode);
    }

    // RF-014.
    [Fact]
    public async Task Mientras_tanto_el_primer_hijo_sigue_con_normalidad()
    {
        var (e, _) = await ConHermanoEnEsperaAsync();
        var conAna = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        var fichaDeAna = EscenarioFicha.Ficha(e.Club.Id, e.Ana.Id);

        Assert.Equal(HttpStatusCode.OK, (await conAna.GetAsync(e.InicioDelClub)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await conAna.GetAsync(e.Base.MiCategoria)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await conAna.GetAsync(fichaDeAna)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await conAna.PutAsync(fichaDeAna, EscenarioFicha.Cuerpo())).StatusCode);

        var guardada = (await e.JugadoresDeLaFamiliaAsync())[0];
        Assert.Equal((e.Ana.Id, e.CategoriaDeAna.Id, true), (guardada.Id, guardada.CategoriaId, guardada.Activo));
    }

    // RF-013 y el supuesto de la 005: en espera no hay ficha, tampoco para el club.
    [Fact]
    public async Task Ni_el_presidente_ni_nadie_del_club_ve_la_ficha_del_hermano_en_espera()
    {
        var (e, hermano) = await ConHermanoEnEsperaAsync();
        var ficha = EscenarioFicha.Ficha(e.Club.Id, hermano.Id);

        foreach (var cliente in new[] { e.Presidente, e.Directivo, e.Entrenador, await e.ClienteDeLaFamiliaAsync(e.Ana.Id) })
        {
            var respuesta = await cliente.GetAsync(ficha);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        var cambio = await e.Presidente.PutAsync(ficha, EscenarioFicha.Cuerpo());
        Assert.Equal(HttpStatusCode.NotFound, cambio.StatusCode);
    }
}
