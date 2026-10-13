using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que busca jugadores que se pueden sumar a una actividad (spec 013,
/// RF-013, RF-016; research §9).
/// Su responsabilidad es exigir un texto de al menos dos letras, limitar la búsqueda al PRESIDENTE y
/// a quien está asignado a alguna categoría (sin asignación, lista vacía) y devolver hasta veinte
/// jugadores activos del club con categoría, por apellidos, solo con nombres, apellidos y categoría.
/// No devuelve foto, documento, equipos ni enlace a la ficha: que aparezcan aquí no da acceso a ella.
/// </summary>
public class ServicioJugadoresSumables : IServicioJugadoresSumables
{
    /// <summary>Letras mínimas del texto de búsqueda.</summary>
    public const int MinimoDeLetras = 2;

    /// <summary>Longitud máxima del texto de búsqueda.</summary>
    public const int MaximoDeLetras = 60;

    /// <summary>Resultados como máximo.</summary>
    public const int MaximoDeResultados = 20;

    private readonly IRepositorioAudiencia _audiencia;
    private readonly IRepositorioAsignaciones _asignaciones;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioJugadoresSumables(IRepositorioAudiencia audiencia, IRepositorioAsignaciones asignaciones)
    {
        _audiencia = audiencia;
        _asignaciones = asignaciones;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JugadorSumableDto>> BuscarAsync(
        string? busqueda, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var texto = Validar(busqueda);

        if (quienPregunta.Rol != Rol.PRESIDENTE)
        {
            var entrena = await _asignaciones.CategoriasDeAsync(quienPregunta.Id, cancelacion);
            if (entrena.Count == 0)
            {
                return [];
            }
        }

        return await _audiencia.BuscarSumablesAsync(texto, MaximoDeResultados, cancelacion);
    }

    private static string Validar(string? busqueda)
    {
        var texto = (busqueda ?? string.Empty).Trim();
        var errores = new ErroresDeValidacion();
        if (texto.Length < MinimoDeLetras)
        {
            errores.Agregar("busqueda", "Escribe al menos 2 letras del nombre o del apellido.");
        }
        else if (texto.Length > MaximoDeLetras)
        {
            errores.Agregar("busqueda", "La búsqueda es de 60 caracteres como máximo.");
        }

        errores.LanzarSiHayErrores();
        return texto;
    }
}
