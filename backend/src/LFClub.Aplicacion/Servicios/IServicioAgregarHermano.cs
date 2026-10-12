using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de agregar un hermano desde la ficha de un jugador ya registrado
/// (constitución §12.1.2; historia 1 de la 006).
/// Su responsabilidad es añadir a la cuenta de la familia, en el mismo club, un jugador nuevo que
/// queda en la sala de espera hasta que el PRESIDENTE lo apruebe.
/// No aprueba ni rechaza al hermano, no completa su ficha y no decide quién puede agregarlo: solo
/// llega aquí la cuenta de un jugador aprobado y activo.
/// </summary>
public interface IServicioAgregarHermano
{
    /// <summary>
    /// Agrega un hermano desde la ficha de <paramref name="usuarioRolId"/>, que debe ser el jugador
    /// que hace la petición; si no, <c>404 no_encontrado</c>. Devuelve el hermano y si se creó: es
    /// falso cuando ese documento ya era de un jugador en espera de la misma cuenta en el club, que
    /// se devuelve sin cambiar nada. Datos no válidos: <c>400 datos_invalidos</c>. Documento de
    /// otro integrante del club: <c>409 documento_repetido_en_club</c>. Documento que otra cuenta
    /// tiene en un rol que no es JUGADOR: <c>409 documento_en_otra_cuenta</c>. El de un jugador de
    /// otra cuenta se admite, y el hermano lleva el aviso de si aprobarlo lo retirará de otro club.
    /// </summary>
    Task<(HermanoAgregadoDto Hermano, bool Creado)> AgregarAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, AgregarHermanoDto datos, CancellationToken cancelacion = default);
}
