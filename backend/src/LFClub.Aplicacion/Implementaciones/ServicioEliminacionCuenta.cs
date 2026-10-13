using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa la eliminación de la propia cuenta (spec 014; constitución §12.4 y §8).
/// Su responsabilidad es comprobar la contraseña, con el contador de fallos de inicio de sesión
/// (research §4), y, dentro de una sola transacción, bloquear los clubes de la cuenta en orden de
/// identificador, aplicar <see cref="ReglaEliminacionDeCuenta"/> con los presidentes recontados y
/// ejecutar anonimizar, anular invitaciones y borrar (research §3). O se borra todo o nada.
/// No accede al contexto de Entity Framework, no conoce HTTP y no devuelve datos de la cuenta.
/// </summary>
public class ServicioEliminacionCuenta : IServicioEliminacionCuenta
{
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioEliminacionCuenta _eliminacion;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IHashContrasena _hash;
    private readonly IReloj _reloj;
    private readonly ServicioResumenEliminacionCuenta _resumen;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioEliminacionCuenta(
        IRepositorioUsuarios usuarios,
        IRepositorioPertenencias pertenencias,
        IRepositorioClubesPlataforma clubes,
        IRepositorioEliminacionCuenta eliminacion,
        IUnidadDeTrabajo unidadDeTrabajo,
        IHashContrasena hash,
        IReloj reloj,
        ServicioResumenEliminacionCuenta resumen)
    {
        _usuarios = usuarios;
        _pertenencias = pertenencias;
        _clubes = clubes;
        _eliminacion = eliminacion;
        _unidadDeTrabajo = unidadDeTrabajo;
        _hash = hash;
        _reloj = reloj;
        _resumen = resumen;
    }

    /// <inheritdoc />
    public Task<ResumenEliminacionCuentaDto> ObtenerResumenAsync(
        Guid usuarioId, CancellationToken cancelacion = default) => _resumen.ObtenerAsync(usuarioId, cancelacion);

    /// <inheritdoc />
    public async Task EliminarAsync(Guid usuarioId, EliminarCuentaDto datos, CancellationToken cancelacion = default)
    {
        var cuenta = await _usuarios.ObtenerParaCambiarAsync(usuarioId, cancelacion);
        if (cuenta is null || cuenta.EsDesarrollador)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var errores = new ErroresDeValidacion();
        if (string.IsNullOrEmpty(datos.Contrasena))
        {
            errores.Agregar("contrasena", "Escribe tu contraseña para confirmar.");
        }

        errores.LanzarSiHayErrores();

        // Una cuenta bloqueada no admite ni la contraseña correcta (§12.4).
        if (cuenta.Bloqueada)
        {
            throw new ExcepcionDeAplicacion(
                "cuenta_bloqueada",
                403,
                "Tu cuenta está bloqueada por intentos fallidos. Recupera tu contraseña para continuar.");
        }

        if (cuenta.ContrasenaHash is null || !_hash.Verificar(cuenta.ContrasenaHash, datos.Contrasena!))
        {
            // 400 y no 401: el cliente toma un 401 por sesión caducada y sacaría a la persona.
            await _usuarios.RegistrarFalloDeSesionAsync(usuarioId, cancelacion);
            throw new ExcepcionDeAplicacion("contrasena_incorrecta", 400, "La contraseña no es correcta.");
        }

        if (cuenta.IntentosFallidos > 0)
        {
            await _usuarios.ReiniciarFallosDeSesionAsync(usuarioId, cancelacion);
        }

        await _unidadDeTrabajo.EnTransaccionAsync(() => EliminarEnTransaccionAsync(usuarioId, cancelacion), cancelacion);
    }

    private async Task EliminarEnTransaccionAsync(Guid usuarioId, CancellationToken cancelacion)
    {
        var integrantes = await _pertenencias.ListarDeUsuarioAsync(usuarioId, cancelacion);

        // En orden estable, para que dos presidentes de los mismos clubes no se interbloqueen; el
        // segundo en llegar ve al primero ya borrado y se encuentra como único.
        var presidentes = new Dictionary<Guid, int>();
        foreach (var clubId in integrantes.Where(i => i.Rol == Rol.PRESIDENTE).Select(i => i.ClubId).Distinct().Order())
        {
            _ = await _clubes.ObtenerBloqueandoAsync(clubId, cancelacion);
            presidentes[clubId] = await _clubes.ContarPresidentesAsync(clubId, cancelacion);
        }

        var bloqueantes = ReglaEliminacionDeCuenta.ClubesBloqueantes(integrantes, presidentes);
        if (bloqueantes.Count > 0)
        {
            throw new ExcepcionDeAplicacion(
                "presidente_unico",
                409,
                "Eres el único presidente de un club. Nombra antes a otro presidente.",
                extras: new Dictionary<string, object>
                {
                    ["clubes"] = ServicioResumenEliminacionCuenta.ClubesQueImpiden(integrantes, bloqueantes),
                });
        }

        var integranteIds = integrantes.Select(i => i.Id).ToList();
        await _eliminacion.AnonimizarAutoriaAsync(usuarioId, integranteIds, cancelacion);
        await _eliminacion.AnularInvitacionesAsync(usuarioId, _reloj.AhoraUtc, cancelacion);
        await _eliminacion.EliminarCuentaYSusIntegrantesAsync(usuarioId, integranteIds, cancelacion);
    }
}
