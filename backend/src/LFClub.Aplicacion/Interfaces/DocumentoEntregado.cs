using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa lo que se sabe de un documento entregado sin leer su archivo.
/// Su responsabilidad es llevar qué documento pedido es, cuándo se subió, de qué tipo es y cuánto
/// pesa, para mostrar su estado en la ficha.
/// No lleva el contenido del archivo: ninguna consulta de estado lo lee.
/// </summary>
/// <param name="Documento">Documento pedido.</param>
/// <param name="SubidoEn">Fecha del archivo vigente, en UTC.</param>
/// <param name="TipoContenido">Tipo de contenido del archivo.</param>
/// <param name="TamanoBytes">Tamaño del archivo.</param>
public record DocumentoEntregado(DocumentoPedido Documento, DateTime SubidoEn, string TipoContenido, int TamanoBytes);
