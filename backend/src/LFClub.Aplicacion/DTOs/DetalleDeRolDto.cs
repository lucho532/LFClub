using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una línea del detalle de un cobro de la plataforma: un rol (spec 017, RF-004).
/// Su responsabilidad es llevar cuántos integrantes había, el precio y el subtotal congelados al
/// generar el cobro.
/// No recalcula nada: sale del cobro guardado.
/// </summary>
/// <param name="Rol">Rol de la línea.</param>
/// <param name="Integrantes">Integrantes activos de ese rol al generar el cobro.</param>
/// <param name="Precio">Precio por integrante vigente al generar el cobro.</param>
/// <param name="Subtotal">Integrantes por precio.</param>
public record DetalleDeRolDto(Rol Rol, int Integrantes, decimal Precio, decimal Subtotal);
