using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa al hermano que la familia acaba de agregar, tal como se lo devuelve la API (RF-034 de
/// la 006).
/// Su responsabilidad es llevar lo mismo que <see cref="JugadorDeSesionDto"/> más el aviso de que,
/// si el club lo aprueba, dejará de estar en otro club donde está inscrito con otra cuenta.
/// No lleva el nombre ni el identificador de ese otro club, ni datos de la otra cuenta (RF-038). No
/// viaja en la sesión: ahí el aviso no tiene sentido.
/// </summary>
/// <param name="UsuarioRolId">Identificador del hermano en este club.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="EstadoIngreso">Estado de ingreso: en espera.</param>
/// <param name="Retirado">Siempre falso: un hermano recién agregado no está retirado.</param>
/// <param name="RetiraDeOtroClub">Si aprobarlo retirará a un jugador de otra cuenta en otro club, calculado en el momento.</param>
public record HermanoAgregadoDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    EstadoIngreso EstadoIngreso,
    bool Retirado,
    bool RetiraDeOtroClub);
