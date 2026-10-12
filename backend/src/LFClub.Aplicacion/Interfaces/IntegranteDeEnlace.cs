using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa lo mínimo de un integrante del club para decidir a qué ficha lleva su nombre (spec 008).
/// Su responsabilidad es llevar su identificador, su cuenta, su rol y su estado de ingreso de hoy.
/// No lleva nombres ni ningún dato personal: el nombre que se muestra es el que ya trae cada
/// respuesta.
/// </summary>
/// <param name="UsuarioRolId">Integrante del club.</param>
/// <param name="UsuarioId">Su cuenta.</param>
/// <param name="Rol">Su rol en el club.</param>
/// <param name="EstadoIngreso">Su estado de ingreso.</param>
public record IntegranteDeEnlace(Guid UsuarioRolId, Guid UsuarioId, Rol Rol, EstadoIngreso EstadoIngreso);
