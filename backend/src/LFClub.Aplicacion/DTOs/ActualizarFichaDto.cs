using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se guarda desde el formulario de la ficha: el contacto, el contacto de
/// emergencia, la seguridad social y los datos clínicos.
/// Su responsabilidad es llevar esos doce datos. Reemplaza el formulario entero: un dato vacío o
/// ausente queda vacío. Todo es opcional salvo el celular y, si el jugador es menor de 18 años, el
/// responsable.
/// No lleva nombres, apellidos, fecha de nacimiento, documento ni correo: si llegan en el cuerpo
/// se ignoran (RF-017, RF-018).
/// </summary>
/// <param name="Celular">Celular de la cuenta, hasta 20 caracteres. Obligatorio.</param>
/// <param name="NombreResponsable">Nombre del responsable, hasta 160 caracteres.</param>
/// <param name="EmergenciaNombre">Contacto de emergencia: nombre, hasta 160 caracteres.</param>
/// <param name="EmergenciaParentesco">Contacto de emergencia: parentesco, hasta 40 caracteres.</param>
/// <param name="EmergenciaCelular">Contacto de emergencia: celular, hasta 20 caracteres.</param>
/// <param name="EntidadSalud">Entidad de salud, hasta 120 caracteres.</param>
/// <param name="LugarAtencion">Lugar de atención, hasta 200 caracteres.</param>
/// <param name="GrupoSanguineo">Grupo sanguíneo.</param>
/// <param name="Alergias">Alergias, hasta 1000 caracteres.</param>
/// <param name="Enfermedades">Enfermedades o condiciones, hasta 1000 caracteres.</param>
/// <param name="Medicamentos">Medicamentos, hasta 1000 caracteres.</param>
/// <param name="Observaciones">Observaciones, hasta 1000 caracteres.</param>
public record ActualizarFichaDto(
    string? Celular,
    string? NombreResponsable,
    string? EmergenciaNombre,
    string? EmergenciaParentesco,
    string? EmergenciaCelular,
    string? EntidadSalud,
    string? LugarAtencion,
    GrupoSanguineo? GrupoSanguineo,
    string? Alergias,
    string? Enfermedades,
    string? Medicamentos,
    string? Observaciones);
