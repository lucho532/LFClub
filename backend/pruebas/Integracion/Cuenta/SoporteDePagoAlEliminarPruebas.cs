using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Cuenta.FamiliaParaEliminar;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Guardián de los soportes de pago al eliminar una cuenta (spec 015, research §13 y data-model):
/// el pago se conserva sin jugador, pero ninguna imagen de soporte sobrevive a su familia.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SoporteDePagoAlEliminarPruebas
{
    private readonly FabricaApi _fabrica;

    public SoporteDePagoAlEliminarPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Ningun_soporte_sobrevive_a_la_eliminacion_de_la_cuenta_de_su_jugador_y_el_pago_se_conserva()
    {
        var f = await CrearAsync(_fabrica);
        var pago = await _fabrica.Pagos.CrearPagoEnRevisionAsync(f.ClubA, f.JugadorA1, 65000m, conSoporte: true);
        var cliente = await f.ClienteAsync(_fabrica);

        var respuesta = await cliente.PostAsync(Ruta, new { contrasena = Sembrador.Contrasena });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var (soportes, pagos) = await _fabrica.ConContextoAsync(async contexto => (
            await contexto.SoportesPago.IgnoreQueryFilters().CountAsync(soporte => soporte.PagoId == pago.Id),
            await contexto.Pagos.IgnoreQueryFilters().SingleAsync(fila => fila.Id == pago.Id)));
        Assert.Equal(0, soportes);
        Assert.Null(pagos.UsuarioRolId);
        Assert.Equal(pago.JugadorNombre, pagos.JugadorNombre);
        Assert.Equal(EstadoPago.EN_REVISION, pagos.Estado);
    }
}
