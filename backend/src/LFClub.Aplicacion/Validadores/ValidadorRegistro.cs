using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los datos del registro con invitación (constitución §12.1).
/// Su responsabilidad es exigir nombres, apellidos, tipo y número de documento, fecha de
/// nacimiento no futura, celular y una contraseña válida. Cuando la invitación es de JUGADOR exige
/// además el nombre del padre, madre o responsable si la persona es menor de 18 años el día del
/// registro, y lo admite, opcional, si es adulta. Con cualquier otro rol, también PRESIDENTE, ese
/// dato ni se exige ni se valida: no se guarda (RF-013).
/// La identidad la valida <see cref="ValidadorIdentidad"/>, con las mismas reglas que aplica la ficha
/// del jugador. No valida el correo (sale de la invitación) ni comprueba si el documento ya existe. No decide
/// qué roles piden el responsable: lo dice la regla de ingreso por invitación.
/// </summary>
public static class ValidadorRegistro
{
    /// <summary>Longitud máxima del nombre del responsable.</summary>
    public const int MaximoResponsable = 160;

    /// <summary>
    /// Lanza <c>datos_invalidos</c> si los datos no son válidos.
    /// <paramref name="rolDeLaInvitacion"/> decide si se pide el responsable. <paramref name="hoy"/>
    /// es la fecha UTC del servidor: con ella se rechazan las fechas futuras y se decide quién es
    /// menor.
    /// </summary>
    public static void Validar(RegistrarConInvitacionDto datos, Rol rolDeLaInvitacion, DateOnly hoy)
    {
        var errores = new ErroresDeValidacion();
        var pideResponsable = ReglaIngresoPorInvitacion.PideResponsable(rolDeLaInvitacion);

        ValidadorIdentidad.NombresYApellidos(errores, datos.Nombres, datos.Apellidos);
        errores.Obligatorio("celular", datos.Celular, 20, "El celular");
        ValidadorIdentidad.Documento(errores, datos.TipoDocumento, datos.NumeroDocumento);

        if (ValidadorIdentidad.FechaNacimiento(errores, datos.FechaNacimiento, hoy)
            && string.IsNullOrWhiteSpace(datos.NombreResponsable)
            && ReglaIngresoPorInvitacion.ExigeResponsable(rolDeLaInvitacion, datos.FechaNacimiento!.Value, hoy))
        {
            errores.Agregar(
                "nombreResponsable",
                "El nombre del padre, madre o responsable es obligatorio para una persona menor de 18 años.");
        }

        if (pideResponsable)
        {
            errores.Maximo("nombreResponsable", datos.NombreResponsable, MaximoResponsable, "El nombre del responsable");
        }

        ValidadorContrasena.Validar(errores, "contrasena", datos.Contrasena);
        errores.LanzarSiHayErrores();
    }
}
