using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa quién publicó un comunicado (spec 011, RF-035).
/// Su responsabilidad es llevar el nombre y el rol copiados al publicar (§13) y, si esa persona
/// sigue en el club, su integrante, para abrir su ficha básica (spec 008).
/// No concede acceso a la ficha: abrirla pasa por sus propias reglas.
/// </summary>
/// <param name="UsuarioRolId">Integrante que lo publicó; nulo si ya no es del club.</param>
/// <param name="Nombre">Nombres y apellidos copiados al publicar.</param>
/// <param name="Rol">Rol que tenía al publicar.</param>
public record AutorDeComunicadoDto(Guid? UsuarioRolId, string Nombre, Rol Rol);
