using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el estado de uno de los documentos que pide la ficha (RF-028).
/// Su responsabilidad es decir si está entregado o pendiente y, si está entregado, cuándo se subió
/// el archivo vigente, de qué tipo es y cuánto pesa.
/// No lleva el contenido del archivo ni su nombre original.
/// </summary>
/// <param name="Documento">Documento pedido.</param>
/// <param name="Entregado">Verdadero si hay un archivo; falso si está pendiente.</param>
/// <param name="SubidoEn">Fecha del archivo vigente, en UTC; nulo si está pendiente.</param>
/// <param name="TipoContenido">Tipo de contenido del archivo; nulo si está pendiente.</param>
/// <param name="TamanoBytes">Tamaño del archivo; nulo si está pendiente.</param>
public record DocumentoDeFichaDto(
    DocumentoPedido Documento,
    bool Entregado,
    DateTime? SubidoEn,
    string? TipoContenido,
    int? TamanoBytes);
