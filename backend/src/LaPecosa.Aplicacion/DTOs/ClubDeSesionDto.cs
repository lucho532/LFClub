using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa un club del desplegable de la sesión: una entrada por club, también cuando la cuenta
/// tiene varios jugadores en él.
/// Su responsabilidad es llevar lo necesario para pintar su identidad, saber el rol de la persona
/// en él y saber si su ingreso sigue en espera o si el club la retiró, para mostrarle la sala de
/// espera o el aviso de retiro sin pedir nada al club; y, cuando la cuenta tiene varios jugadores
/// en el club y puede elegir, la lista de ellos. Los datos del integrante describen al único que
/// tiene la cuenta en el club; si la sesión está limitada a uno, a ese; y si hay que elegir, al
/// más antiguo, hasta que la aplicación los sustituya por los del elegido.
/// No contiene la configuración completa del club ni datos de integrantes de otras cuentas, y a
/// una sesión limitada a un jugador no le entrega nada de sus hermanos.
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="Nombre">Nombre del club.</param>
/// <param name="Rol">Rol de la persona en ese club.</param>
/// <param name="Estado">Estado actual del club.</param>
/// <param name="EstadoIngreso">Estado de ingreso de la persona en ese club.</param>
/// <param name="Retirado">Verdadero si este jugador está retirado del club; no entra a él hasta que lo reincorporen.</param>
/// <param name="Identidad">Identidad visual del club.</param>
/// <param name="Nombres">Nombres del integrante en ese club.</param>
/// <param name="Apellidos">Apellidos del integrante en ese club.</param>
/// <param name="UsuarioRolId">Integrante al que se refieren el rol, el estado de ingreso, el retiro y el nombre.</param>
/// <param name="Jugadores">
/// Integrantes de la cuenta en este club entre los que hay que elegir, del más antiguo al más
/// reciente. Vacía si la cuenta tiene uno solo o si la sesión está limitada a uno.
/// </param>
public record ClubDeSesionDto(
    Guid ClubId,
    string Nombre,
    Rol Rol,
    EstadoClub Estado,
    EstadoIngreso EstadoIngreso,
    bool Retirado,
    IdentidadClubDto Identidad,
    string Nombres,
    string Apellidos,
    Guid UsuarioRolId,
    IReadOnlyList<JugadorDeSesionDto> Jugadores);
