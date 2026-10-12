using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de los datos propios de "Mi perfil" (spec 008).
/// Su responsabilidad es leer los integrantes de la cuenta con sesión, quedarse con los de rol
/// PRESIDENTE, DIRECTIVO o ENTRENADOR, mostrar los datos del más antiguo con el correo y el celular
/// de la cuenta, y, tras validarlos, escribir el cambio en todos ellos y en la cuenta en una sola
/// transacción.
/// No accede al contexto de Entity Framework, no cambia el documento ni el correo y no toca los
/// integrantes JUGADOR de la cuenta.
/// </summary>
public class ServicioDatosPropios : IServicioDatosPropios
{
    private static readonly Rol[] RolesConDatosPropios = [Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR];

    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioDatosPropios(
        IRepositorioPertenencias pertenencias,
        IRepositorioUsuarios usuarios,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _pertenencias = pertenencias;
        _usuarios = usuarios;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<DatosPropiosDto> ObtenerAsync(Guid usuarioId, CancellationToken cancelacion = default)
    {
        var integrante = await MasAntiguoAsync(usuarioId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var cuenta = await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();

        return new DatosPropiosDto(
            integrante.Nombres,
            integrante.Apellidos,
            integrante.TipoDocumento,
            integrante.NumeroDocumento,
            integrante.FechaNacimiento,
            cuenta.Correo,
            cuenta.Celular ?? string.Empty);
    }

    /// <inheritdoc />
    public async Task<DatosPropiosDto> CambiarAsync(
        Guid usuarioId, CambiarDatosPropiosDto datos, CancellationToken cancelacion = default)
    {
        // Primero si la cuenta tiene datos propios: a la de un jugador no se le dice qué falta.
        if (await MasAntiguoAsync(usuarioId, cancelacion) is null)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        ValidadorDatosPropios.Validar(datos, DateOnly.FromDateTime(_reloj.AhoraUtc));

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _pertenencias.CambiarDatosPropiosAsync(
                    usuarioId,
                    NormalizadorTexto.SinEspaciosSobrantes(datos.Nombres),
                    NormalizadorTexto.SinEspaciosSobrantes(datos.Apellidos),
                    datos.FechaNacimiento!.Value,
                    datos.Celular!.Trim(),
                    cancelacion);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await ObtenerAsync(usuarioId, cancelacion);
    }

    /// <summary>El integrante PRESIDENTE, DIRECTIVO o ENTRENADOR más antiguo de la cuenta, o nulo.</summary>
    private async Task<UsuarioRol?> MasAntiguoAsync(Guid usuarioId, CancellationToken cancelacion) =>
        (await _pertenencias.ListarDeUsuarioAsync(usuarioId, cancelacion))
            .Where(integrante => RolesConDatosPropios.Contains(integrante.Rol))
            .OrderBy(integrante => integrante.CreadoEn)
            .FirstOrDefault();
}
