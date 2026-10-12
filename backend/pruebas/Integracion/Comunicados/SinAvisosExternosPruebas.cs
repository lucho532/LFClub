using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.Extensions.DependencyInjection;
using static LaPecosa.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LaPecosa.Pruebas.Integracion.Comunicados;

/// <summary>
/// Publicar y retirar un comunicado no envía nada fuera de la aplicación: ni correo ni ningún otro
/// aviso (spec 011, CE-006; RF-028 y RF-029).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SinAvisosExternosPruebas
{
    private readonly FabricaApi _fabrica;

    public SinAvisosExternosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Publicar_y_retirar_no_envian_ningun_correo()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var antes = _fabrica.Correo.Enviados.Count;

        var publicado = await e.PublicarAsync(e.P, Cuerpo("CLUB"));
        await Actividades.LeerAsync(await e.P.PostAsync(e.Ruta($"/{publicado.GetProperty("id").GetGuid()}/retiro")));

        Assert.Equal(antes, _fabrica.Correo.Enviados.Count);
    }

    [Fact]
    public async Task Publicar_y_retirar_funcionan_aunque_el_servicio_de_correo_falle()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        _fabrica.Correo.FallarEnvios = true;
        try
        {
            var publicado = await e.PublicarAsync(e.P, Cuerpo("CLUB"));
            await Actividades.LeerAsync(await e.P.PostAsync(e.Ruta($"/{publicado.GetProperty("id").GetGuid()}/retiro")));
        }
        finally
        {
            _fabrica.Correo.FallarEnvios = false;
        }
    }

    [Fact]
    public void El_servicio_de_publicacion_no_depende_del_servicio_de_correo()
    {
        var constructores = typeof(LaPecosa.Aplicacion.Implementaciones.ServicioPublicacionComunicados).GetConstructors();

        var parametros = constructores.SelectMany(constructor => constructor.GetParameters()).Select(p => p.ParameterType);
        Assert.DoesNotContain(typeof(IServicioCorreo), parametros);
        Assert.NotNull(_fabrica.Services.GetRequiredService<IServicioCorreo>());
    }
}
