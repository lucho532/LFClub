using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa un cargo leído junto con lo que se le ha aplicado de pagos confirmados (spec 015).
/// Su responsabilidad es llevar al servicio lo que necesita para calcular lo pendiente, el saldo y
/// el estado de cuenta, o los totales de las finanzas, sin que el repositorio decida nada.
/// No lleva el pago que se aplicó ni los pagos anulados o en revisión, que no cuentan.
/// </summary>
/// <param name="CargoId">Identificador del cargo.</param>
/// <param name="UsuarioRolId">Jugador que lo debe.</param>
/// <param name="Tipo">Tipo de cargo.</param>
/// <param name="Mes">Mes que cobra; nulo si no es una mensualidad.</param>
/// <param name="Valor">Valor cobrado.</param>
/// <param name="Aplicado">Suma de lo aplicado por pagos confirmados.</param>
/// <param name="CategoriaId">Categoría del jugador cuando se generó.</param>
public record CargoConAplicado(
    Guid CargoId, Guid UsuarioRolId, TipoCargo Tipo, DateOnly? Mes, decimal Valor, decimal Aplicado, Guid? CategoriaId);
