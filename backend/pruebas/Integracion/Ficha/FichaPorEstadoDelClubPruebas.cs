using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// En un club suspendido su PRESIDENTE sigue leyendo y cambiando fichas y los demás no entran; en
/// uno dado de baja nadie accede (constitución §7.4). Lo resuelve la autorización de la 001; aquí
/// se comprueba sobre las seis operaciones de la ficha.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FichaPorEstadoDelClubPruebas
{
    private const DocumentoPedido Copia = DocumentoPedido.COPIA_DOCUMENTO_IDENTIDAD;

    private readonly FabricaApi _fabrica;

    public FichaPorEstadoDelClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task En_un_club_suspendido_el_presidente_lee_y_cambia_fichas()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, SembradorFichas.Pdf(), "application/pdf");
        await CambiarEstadoAsync(e, EstadoClub.SUSPENDIDO);

        var respuestas = await LasSeisOperacionesAsync(e, e.Presidente);

        Assert.All(respuestas, par => Assert.True(par.Respuesta.StatusCode == HttpStatusCode.OK, $"{par.Operacion}: {(int)par.Respuesta.StatusCode}"));
        Assert.Equal("Nueva EPS", (await e.FichaGuardadaAsync(e.Ana.Id))!.EntidadSalud);
        Assert.Equal("Corregida", (await e.JugadorGuardadoAsync(e.Ana.Id)).Nombres);
    }

    [Fact]
    public async Task En_un_club_suspendido_los_demas_reciben_el_aviso_en_las_seis_operaciones()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, SembradorFichas.Pdf(), "application/pdf");
        await CambiarEstadoAsync(e, EstadoClub.SUSPENDIDO);

        // También el retirado: primero se mira el estado del club.
        foreach (var cliente in new[] { e.Directivo, e.Entrenador, e.CuentaDeAna, e.CuentaDeDani })
        {
            await NegadoEnLasSeisAsync(e, cliente, "club_suspendido");
        }

        Assert.Equal(e.Ana.Nombres, (await e.JugadorGuardadoAsync(e.Ana.Id)).Nombres);
        Assert.Equal(SembradorFichas.EntidadSalud, (await e.FichaGuardadaAsync(e.Ana.Id))!.EntidadSalud);
    }

    [Fact]
    public async Task En_un_club_dado_de_baja_nadie_accede_a_las_fichas()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, Copia, SembradorFichas.Pdf(), "application/pdf");
        await CambiarEstadoAsync(e, EstadoClub.DADO_DE_BAJA);

        foreach (var cliente in new[] { e.Presidente, e.Directivo, e.Entrenador, e.CuentaDeAna })
        {
            await NegadoEnLasSeisAsync(e, cliente, "club_dado_de_baja");
        }

        // Al revertir la baja, la ficha sigue como estaba.
        await CambiarEstadoAsync(e, EstadoClub.ACTIVO);
        var ficha = await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id);
        Assert.Equal(SembradorFichas.Alergias, ficha.GetProperty("datosClinicos").GetProperty("alergias").GetString());
        Assert.True(ficha.DocumentoDe(Copia).GetProperty("entregado").GetBoolean());
    }

    private static async Task NegadoEnLasSeisAsync(EscenarioFicha e, ClienteDePrueba cliente, string codigo)
    {
        var respuestas = await LasSeisOperacionesAsync(e, cliente);

        Assert.Equal(6, respuestas.Count);
        foreach (var (operacion, respuesta) in respuestas)
        {
            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{operacion}: {(int)respuesta.StatusCode}");
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            Assert.DoesNotContain(e.Ana.NumeroDocumento, cuerpo);
            Assert.DoesNotContain(SembradorFichas.Alergias, cuerpo);
        }
    }

    /// <summary>Las seis operaciones de la ficha sobre Ana, con cuerpos válidos.</summary>
    private static async Task<List<(string Operacion, HttpResponseMessage Respuesta)>> LasSeisOperacionesAsync(
        EscenarioFicha e, ClienteDePrueba cliente) =>
    [
        ("GET ficha", await cliente.GetAsync(e.Ficha(e.Ana.Id))),
        ("PUT ficha", await cliente.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo())),
        ("PUT documento-identidad", await cliente.PutAsync(
            e.DocumentoIdentidad(e.Ana.Id), new { tipoDocumento = "REGISTRO_CIVIL", numeroDocumento = Sembrador.Unico("rc") })),
        ("PUT identidad", await cliente.PutAsync(
            e.Identidad(e.Ana.Id), new { nombres = "Corregida", apellidos = e.Ana.Apellidos, fechaNacimiento = "2014-07-15" })),
        ("GET documento", await cliente.GetAsync(e.Documento(e.Ana.Id, Copia))),
        ("PUT documento", await cliente.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Png())),
    ];

    private Task<int> CambiarEstadoAsync(EscenarioFicha e, EstadoClub estado) => _fabrica.ConContextoAsync(contexto =>
        contexto.Clubes.Where(club => club.Id == e.Club.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(club => club.Estado, estado)));
}
