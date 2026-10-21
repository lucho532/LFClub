namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una cuenta por la que el club recibe pagos (spec 015, RF-026 a RF-028).
/// Su responsabilidad es llevar los datos que ven el PRESIDENTE, el DIRECTIVO y las familias.
/// No lleva dinero ni pagos.
/// </summary>
/// <param name="Id">Identificador del medio.</param>
/// <param name="Entidad">Nequi, Bancolombia, Daviplata u otra.</param>
/// <param name="Titular">Nombre del titular.</param>
/// <param name="Numero">Número de cuenta, celular o llave.</param>
/// <param name="Nota">Nota opcional.</param>
/// <param name="Activo">Falso significa que no se muestra a las familias.</param>
public record MedioPagoDto(Guid Id, string Entidad, string Titular, string Numero, string? Nota, bool Activo);
