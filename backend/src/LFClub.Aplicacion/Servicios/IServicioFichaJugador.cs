using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de guardar el formulario de la ficha de un jugador: el contacto, el
/// contacto de emergencia, la seguridad social y los datos clínicos (RF-016, RF-018).
/// Su responsabilidad es reemplazar esos datos, dejando registrado quién hizo el cambio y cuándo
/// (RF-038).
/// No cambia nombres, apellidos, fecha de nacimiento, documento, correo ni archivos.
/// </summary>
public interface IServicioFichaJugador
{
    /// <summary>
    /// Guarda el formulario y devuelve la ficha ya guardada. Quien no puede ver o cambiar esa
    /// ficha recibe 404 <c>no_encontrado</c>; datos no válidos, 400 <c>datos_invalidos</c>.
    /// </summary>
    Task<FichaJugadorDto> ActualizarAsync(
        Guid usuarioRolId, ActualizarFichaDto datos, UsuarioRol quienCambia, CancellationToken cancelacion = default);
}
