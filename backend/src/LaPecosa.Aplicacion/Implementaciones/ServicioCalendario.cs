using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio del calendario.
/// Su responsabilidad es validar el rango con <see cref="ValidadorRangoCalendario"/>, resolver
/// quién pregunta y pedir a los repositorios solo los entrenamientos visibles del rango, con la
/// respuesta propia solo para la cuenta de un jugador.
/// No decide quién ve qué (los repositorios aplican la regla en SQL), no modifica datos y no
/// accede al contexto de Entity Framework.
/// </summary>
public class ServicioCalendario : IServicioCalendario
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEntrenamientos _entrenamientos;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCalendario(AccesoAActividad acceso, IRepositorioEntrenamientos entrenamientos)
    {
        _acceso = acceso;
        _entrenamientos = entrenamientos;
    }

    /// <inheritdoc />
    public async Task<CalendarioDto> ObtenerAsync(
        string? desde, string? hasta, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var rango = ValidadorRangoCalendario.Validar(desde, hasta);
        var quien = await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion);

        var entrenamientos = await _entrenamientos.ListarVisiblesAsync(rango.Desde, rango.Hasta, quien, cancelacion);
        var resumenes = await ServicioConsultaEntrenamientos.ResumenesAsync(_entrenamientos, entrenamientos, quien, cancelacion);
        return new CalendarioDto(rango.Desde, rango.Hasta, resumenes, []);
    }
}
