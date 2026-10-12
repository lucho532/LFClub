using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de lo que se guarda desde el formulario de la ficha.
/// Su responsabilidad es exigir el celular, exigir el nombre del responsable solo cuando el jugador
/// de la ficha es menor de 18 años ese día (RF-020), limitar la longitud de cada texto y admitir
/// como grupo sanguíneo solo un valor de la lista. El contacto de emergencia, la seguridad social y
/// los datos clínicos son todos opcionales (RF-002).
/// No decide quién es menor (lo recibe ya resuelto), no valida nombres, apellidos, documento ni
/// fecha de nacimiento, que no se cambian aquí, y no comprueba permisos.
/// </summary>
public static class ValidadorFicha
{
    /// <summary>Longitud máxima de las alergias, las enfermedades, los medicamentos y las observaciones.</summary>
    public const int MaximoTextoClinico = 1000;

    /// <summary>Lanza <c>datos_invalidos</c>, con el detalle por campo, si los datos no son válidos.</summary>
    public static void Validar(ActualizarFichaDto datos, bool jugadorEsMenorDeEdad)
    {
        var errores = new ErroresDeValidacion();

        errores.Obligatorio("celular", datos.Celular, 20, "El celular");

        if (jugadorEsMenorDeEdad && string.IsNullOrWhiteSpace(datos.NombreResponsable))
        {
            errores.Agregar(
                "nombreResponsable",
                "El nombre del padre, madre o responsable es obligatorio para una persona menor de 18 años.");
        }

        errores.Maximo(
            "nombreResponsable", datos.NombreResponsable, ValidadorRegistro.MaximoResponsable, "El nombre del responsable");

        errores.Maximo("emergenciaNombre", datos.EmergenciaNombre, 160, "El nombre del contacto de emergencia");
        errores.Maximo("emergenciaParentesco", datos.EmergenciaParentesco, 40, "El parentesco");
        errores.Maximo("emergenciaCelular", datos.EmergenciaCelular, 20, "El celular del contacto de emergencia");
        errores.Maximo("entidadSalud", datos.EntidadSalud, 120, "La entidad de salud");
        errores.Maximo("lugarAtencion", datos.LugarAtencion, 200, "El lugar de atención");

        if (datos.GrupoSanguineo is { } grupo && !Enum.IsDefined(grupo))
        {
            errores.Agregar("grupoSanguineo", "Elige un grupo sanguíneo de la lista.");
        }

        errores.Maximo("alergias", datos.Alergias, MaximoTextoClinico, "Las alergias");
        errores.Maximo("enfermedades", datos.Enfermedades, MaximoTextoClinico, "Las enfermedades o condiciones");
        errores.Maximo("medicamentos", datos.Medicamentos, MaximoTextoClinico, "Los medicamentos");
        errores.Maximo("observaciones", datos.Observaciones, MaximoTextoClinico, "Las observaciones");

        errores.LanzarSiHayErrores();
    }
}
