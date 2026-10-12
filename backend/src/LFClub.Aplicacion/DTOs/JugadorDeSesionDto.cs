using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a uno de los jugadores de una cuenta en un club, tal como lo ve su propia familia.
/// Su responsabilidad es llevar lo necesario para la lista en la que la familia elige con cuál
/// continuar: quién es y si está activo, pendiente de aprobación o retirado (RF-022 de la 006). Lo
/// que responde agregar un hermano es <see cref="HermanoAgregadoDto"/>, que añade un aviso.
/// No contiene el documento, la categoría ni ningún dato de la ficha, y nunca sale hacia una
/// cuenta que no sea la del jugador.
/// </summary>
/// <param name="UsuarioRolId">Identificador del jugador en este club; es el que se envía como jugador elegido.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="EstadoIngreso">Estado de ingreso: en espera o aprobado.</param>
/// <param name="Retirado">Verdadero si está retirado del club, lo retirara quien lo retirara.</param>
public record JugadorDeSesionDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    EstadoIngreso EstadoIngreso,
    bool Retirado);
