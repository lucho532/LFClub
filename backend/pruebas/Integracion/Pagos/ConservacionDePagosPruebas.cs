using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// Lo que pasa con los cargos y los pagos cuando alguien deja el club o elimina su cuenta (spec 015,
/// historia 4; data-model; spec 014, RF-007; constitución §12.4 y §13).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConservacionDePagosPruebas
{
    private readonly FabricaApi _fabrica;

    public ConservacionDePagosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static object Contrasena() => new { contrasena = Sembrador.Contrasena };

    private async Task<(ClubDePagos Club, Cargo Cargo, Pago Pago)> ClubConUnPagoDeAnaAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        return (c, cargo, pago);
    }

    private Task<Pago?> PagoAsync(Guid id) => _fabrica.ConContextoAsync(contexto =>
        contexto.Pagos.IgnoreQueryFilters().SingleOrDefaultAsync(fila => fila.Id == id));

    private Task<int> CargosAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Cargos.IgnoreQueryFilters().CountAsync(fila => fila.UsuarioRolId == usuarioRolId));

    [Fact]
    public async Task Al_eliminar_la_cuenta_de_un_jugador_el_pago_se_conserva_sin_jugador_con_nombre_y_documento()
    {
        var (c, cargo, pago) = await ClubConUnPagoDeAnaAsync();
        var ana = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Ana.Usuario);

        var respuesta = await ana.PostAsync("/api/cuenta/eliminacion", Contrasena());

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var conservado = await PagoAsync(pago.Id);
        Assert.NotNull(conservado);
        Assert.Null(conservado.UsuarioRolId);
        Assert.Equal($"{c.Ana.Integrante.Nombres} {c.Ana.Integrante.Apellidos}", conservado.JugadorNombre);
        Assert.Equal(c.Ana.Integrante.NumeroDocumento, conservado.JugadorNumeroDocumento);
        Assert.Equal(65000m, conservado.Valor);
        Assert.NotNull(conservado.NumeroRecibo);
        // Los cargos desaparecen con el jugador y dejan de contar; el pago ya no tiene cargo al que apuntar.
        Assert.Equal(0, await CargosAsync(c.Ana.Integrante.Id));
        Assert.Equal(0m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task Un_pago_sin_jugador_lo_ven_el_presidente_y_el_directivo_pero_no_tiene_resguardo()
    {
        var (c, _, pago) = await ClubConUnPagoDeAnaAsync();
        await (await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Ana.Usuario))
            .PostAsync("/api/cuenta/eliminacion", Contrasena());

        var respuesta = await (await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario))
            .GetAsync(c.Ruta($"/pagos/{pago.Id}"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("\"tieneResguardo\":false", texto);
        Assert.Contains("\"usuarioRolId\":null", texto);
    }

    [Fact]
    public async Task Al_eliminar_a_un_presidente_su_nombre_pasa_a_persona_eliminada_en_los_pagos_que_registro()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var (otroUsuario, otro) = await _fabrica.Sembrador.CrearIntegranteAsync(c.Club, Rol.PRESIDENTE);
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync("/api/cuenta/eliminacion", Contrasena());

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var conservado = await PagoAsync(pago.Id);
        Assert.NotNull(conservado);
        Assert.Equal("Persona eliminada", conservado.CreadoPorNombre);
        Assert.Equal("Persona eliminada", conservado.ConfirmadoPorNombre);
        Assert.Null(conservado.ConfirmadoPorUsuarioId);
        Assert.Equal(c.Ana.Integrante.Id, conservado.UsuarioRolId);
        Assert.NotNull(otroUsuario);
        Assert.NotNull(otro);
    }

    [Fact]
    public async Task Un_jugador_retirado_conserva_sus_cargos_y_sus_pagos()
    {
        var (c, _, pago) = await ClubConUnPagoDeAnaAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 11, 1));

        await _fabrica.Categorias.RetirarAsync(c.Ana.Integrante, c.Presidente.Integrante);

        Assert.Equal(2, await CargosAsync(c.Ana.Integrante.Id));
        Assert.Equal(c.Ana.Integrante.Id, (await PagoAsync(pago.Id))!.UsuarioRolId);
    }

    [Fact]
    public async Task Eliminar_el_club_borra_cargos_pagos_aplicaciones_soportes_y_medios()
    {
        var (c, cargo, pago) = await ClubConUnPagoDeAnaAsync();
        var enRevision = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Beto.Integrante, 65000m, conSoporte: true);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.MediosPagoClub.Add(new MedioPagoClub
            {
                ClubId = c.Club.Id, Entidad = "Nequi", Titular = "Club", Numero = "3001234567", CreadoEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        });
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await desarrollador.PutAsync($"/api/plataforma/clubes/{c.Club.Id}/estado", new { estado = "DADO_DE_BAJA" });

        var respuesta = await desarrollador.PostAsync(
            $"/api/plataforma/clubes/{c.Club.Id}/eliminacion", new { nombreDeConfirmacion = c.Club.Nombre });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            Assert.False(await contexto.Cargos.IgnoreQueryFilters().AnyAsync(fila => fila.Id == cargo.Id));
            Assert.False(await contexto.Pagos.IgnoreQueryFilters().AnyAsync(fila => fila.Id == pago.Id || fila.Id == enRevision.Id));
            Assert.False(await contexto.AplicacionesPago.IgnoreQueryFilters().AnyAsync(fila => fila.ClubId == c.Club.Id));
            Assert.False(await contexto.SoportesPago.IgnoreQueryFilters().AnyAsync(fila => fila.ClubId == c.Club.Id));
            Assert.False(await contexto.MediosPagoClub.IgnoreQueryFilters().AnyAsync(fila => fila.ClubId == c.Club.Id));
            return 0;
        });
    }

    [Fact]
    public async Task Cambiar_el_nombre_o_el_documento_del_jugador_despues_no_altera_los_pagos_anteriores()
    {
        var (c, _, pago) = await ClubConUnPagoDeAnaAsync();
        var antes = await PagoAsync(pago.Id);
        await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(fila => fila.Id == c.Ana.Integrante.Id)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(fila => fila.Nombres, "Otro")
                .SetProperty(fila => fila.NumeroDocumento, Sembrador.Unico("nuevo"))));

        var despues = await PagoAsync(pago.Id);

        Assert.Equal(antes!.JugadorNombre, despues!.JugadorNombre);
        Assert.Equal(antes.JugadorNumeroDocumento, despues.JugadorNumeroDocumento);
    }
}
