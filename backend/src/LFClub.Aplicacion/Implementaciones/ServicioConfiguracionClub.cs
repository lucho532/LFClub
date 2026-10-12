using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que edita los datos de un club.
/// Su responsabilidad es validar, aplicar el cambio recalculando el nombre normalizado y traducir
/// la violación del índice único del nombre al error del contrato.
/// No consulta otros clubes para saber si el nombre está libre (el presidente no puede cruzar
/// clubes): lo decide el índice único. No toca la identidad ni el estado.
/// </summary>
public class ServicioConfiguracionClub : IServicioConfiguracionClub
{
    private readonly IRepositorioClub _clubPropio;
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaClubes _consulta;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConfiguracionClub(
        IRepositorioClub clubPropio,
        IRepositorioClubesPlataforma clubes,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaClubes consulta)
    {
        _clubPropio = clubPropio;
        _clubes = clubes;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
    }

    /// <inheritdoc />
    public async Task<ClubDto> ActualizarElPropioAsync(
        ActualizarConfiguracionClubDto datos, UsuarioRol quienEdita, CancellationToken cancelacion = default)
    {
        var club = await _clubPropio.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        await AplicarAsync(club, datos, cancelacion);
        return MapperClub.AClub(club, quienEdita);
    }

    /// <inheritdoc />
    public async Task<ClubDetalleDto> ActualizarDesdeElPanelAsync(
        Guid clubId, ActualizarConfiguracionClubDto datos, CancellationToken cancelacion = default)
    {
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        await AplicarAsync(club, datos, cancelacion);
        return await _consulta.ObtenerDetalleAsync(clubId, cancelacion);
    }

    private static string? Opcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : NormalizadorTexto.SinEspaciosSobrantes(valor);

    private async Task AplicarAsync(Club club, ActualizarConfiguracionClubDto datos, CancellationToken cancelacion)
    {
        ValidadorConfiguracionClub.Validar(datos);

        club.Nombre = NormalizadorTexto.SinEspaciosSobrantes(datos.Nombre);
        club.NombreNormalizado = NormalizadorTexto.NombreClub(datos.Nombre);
        club.Sede = Opcional(datos.Sede);
        club.Direccion = Opcional(datos.Direccion);
        club.CorreoContacto = Opcional(datos.CorreoContacto);
        club.TelefonoContacto = Opcional(datos.TelefonoContacto);

        try
        {
            await _unidadDeTrabajo.EnTransaccionAsync(() => _unidadDeTrabajo.GuardarAsync(cancelacion), cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.NombreDeClub)
        {
            throw ServicioCreacionClub.NombreRepetido();
        }
    }
}
