using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Infraestructura.Datos;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LFClub.Pruebas.Integracion.Aislamiento;

/// <summary>Solo existe una cuenta DESARROLLADOR (constitución §20, RF-001).</summary>
[Collection(ColeccionApi.Nombre)]
public class CuentaDesarrolladorPruebas
{
    private readonly FabricaApi _fabrica;

    public CuentaDesarrolladorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Tras_arrancar_existe_exactamente_una_con_el_correo_de_la_configuracion()
    {
        var desarrolladores = await _fabrica.ConContextoAsync(contexto =>
            contexto.Usuarios.Where(usuario => usuario.EsDesarrollador).ToListAsync());

        var cuenta = Assert.Single(desarrolladores);
        Assert.Equal(FabricaApi.CorreoDesarrollador, cuenta.CorreoNormalizado);

        var integrantes = await _fabrica.ConContextoAsync(contexto =>
            contexto.UsuariosRol.IgnoreQueryFilters().CountAsync(integrante => integrante.UsuarioId == cuenta.Id));
        Assert.Equal(0, integrantes);
    }

    [Fact]
    public async Task La_cuenta_inicial_se_crea_sin_contrasena_y_sin_integrantes()
    {
        // Otras pruebas le crean una contraseña a la cuenta real; aquí se recrea dentro de una
        // transacción que se deshace, para ver cómo nace.
        await using var ambito = _fabrica.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<ContextoLFClub>();
        await using var transaccion = await contexto.Database.BeginTransactionAsync();

        await contexto.Usuarios.Where(usuario => usuario.EsDesarrollador).ExecuteDeleteAsync();
        await ambito.ServiceProvider.GetRequiredService<CuentaInicial>().CrearSiFaltaAsync(" Nuevo@LFClub.test ");

        var cuenta = await contexto.Usuarios.AsNoTracking().SingleAsync(usuario => usuario.EsDesarrollador);
        Assert.Null(cuenta.ContrasenaHash);
        Assert.False(cuenta.PuedeIniciarSesion);
        Assert.Equal("nuevo@lfclub.test", cuenta.CorreoNormalizado);
        Assert.False(await contexto.UsuariosRol.IgnoreQueryFilters().AnyAsync(integrante => integrante.UsuarioId == cuenta.Id));

        await transaccion.RollbackAsync();
    }

    [Fact]
    public async Task Ejecutar_la_cuenta_inicial_otra_vez_no_crea_otra()
    {
        await using (var ambito = _fabrica.Services.CreateAsyncScope())
        {
            await ambito.ServiceProvider.GetRequiredService<CuentaInicial>().CrearSiFaltaAsync("otro@lfclub.test");
        }

        var cantidad = await _fabrica.ConContextoAsync(contexto =>
            contexto.Usuarios.CountAsync(usuario => usuario.EsDesarrollador));
        Assert.Equal(1, cantidad);
    }

    [Fact]
    public async Task Insertar_una_segunda_cuenta_desarrollador_viola_el_indice_unico()
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => _fabrica.ConContextoAsync(async contexto =>
        {
            var correo = Sembrador.CorreoUnico();
            contexto.Usuarios.Add(new Usuario
            {
                Correo = correo,
                CorreoNormalizado = correo,
                EsDesarrollador = true,
                CreadoEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        }));

        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(IndicesUnicos.UnicoDesarrollador, postgres.ConstraintName);
    }
}
