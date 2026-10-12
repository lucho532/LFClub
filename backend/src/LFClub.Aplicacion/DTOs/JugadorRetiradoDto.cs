namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador retirado en la lista del club (RF-044 de la 003).
/// Su responsabilidad es decir quién es, su año de nacimiento, quién lo retiró y cuándo, tal como
/// quedó registrado en ese momento, y cuántos documentos de su ficha le faltan por entregar
/// (RF-032 de la 005). La lista solo la ven el PRESIDENTE y los DIRECTIVOS.
/// No refleja el nombre actual de quien lo retiró (§13) ni lleva ningún dato de contacto: esos
/// datos están en su ficha, que el club conserva. Sí lleva el enlace a la ficha básica de hoy de
/// quien lo retiró, si sigue en el club (spec 008).
/// </summary>
/// <param name="UsuarioRolId">Identificador del integrante en este club.</param>
/// <param name="Nombres">Nombres del jugador.</param>
/// <param name="Apellidos">Apellidos del jugador.</param>
/// <param name="AnioNacimiento">Año de nacimiento.</param>
/// <param name="RetiradoPor">Nombre de quien lo retiró, tal como era en ese momento.</param>
/// <param name="RetiradoEn">Fecha y hora del retiro, en UTC.</param>
/// <param name="DocumentosPendientes">Cuántos documentos pedidos le faltan; 0 es documentación completa.</param>
/// <param name="RetiradoPorFicha">
/// Ficha básica de quien lo retiró; <c>null</c> si fue la familia, si quedó retirado sin autor o si
/// esa persona ya no está en el club (RF-011 de la 008).
/// </param>
public record JugadorRetiradoDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    int AnioNacimiento,
    string RetiradoPor,
    DateTime RetiradoEn,
    int DocumentosPendientes,
    EnlaceFichaDto? RetiradoPorFicha);
