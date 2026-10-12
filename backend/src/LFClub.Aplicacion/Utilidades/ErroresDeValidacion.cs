namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores por campo que reúne un validador escrito a mano (research §13).
/// Su responsabilidad es acumularlos y convertirlos en el error 400 <c>datos_invalidos</c>.
/// No contiene reglas de negocio: cada validador decide qué comprueba.
/// </summary>
public class ErroresDeValidacion
{
    private readonly Dictionary<string, List<string>> _errores = [];

    /// <summary>Indica si se ha añadido algún error.</summary>
    public bool HayErrores => _errores.Count > 0;

    /// <summary>Añade un mensaje al campo, con el nombre que tiene en el contrato.</summary>
    public void Agregar(string campo, string mensaje)
    {
        if (!_errores.TryGetValue(campo, out var mensajes))
        {
            _errores[campo] = mensajes = [];
        }

        mensajes.Add(mensaje);
    }

    /// <summary>Exige un texto no vacío y de longitud máxima; devuelve si es válido.</summary>
    public bool Obligatorio(string campo, string? valor, int maximo, string nombreVisible)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            Agregar(campo, $"{nombreVisible} es obligatorio.");
            return false;
        }

        return Maximo(campo, valor, maximo, nombreVisible);
    }

    /// <summary>Exige una longitud máxima en un texto opcional; devuelve si es válido.</summary>
    public bool Maximo(string campo, string? valor, int maximo, string nombreVisible)
    {
        if (valor is not null && valor.Trim().Length > maximo)
        {
            Agregar(campo, $"{nombreVisible} admite como máximo {maximo} caracteres.");
            return false;
        }

        return true;
    }

    /// <summary>Lanza <c>datos_invalidos</c> si hay errores.</summary>
    public void LanzarSiHayErrores()
    {
        if (HayErrores)
        {
            throw new ExcepcionDeAplicacion(
                "datos_invalidos",
                400,
                "Revisa los datos marcados.",
                _errores.ToDictionary(par => par.Key, par => par.Value.ToArray()));
        }
    }
}
