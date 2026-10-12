using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de los avisos y de los elementos de la campana a sus DTOs (spec 012).
/// Su responsabilidad es armar lo que ve la cuenta de un jugador: el aviso con su texto, su lectura y
/// la actividad con el dato de si sigue disponible, y el elemento con exactamente uno de los dos
/// contenidos.
/// No calcula la disponibilidad (la trae el repositorio) y no lleva datos de la actividad más allá de
/// su tipo e identificador (RF-017).
/// </summary>
public static class MapperCampana
{
    /// <summary>El aviso como lo ve la cuenta, con si su actividad sigue disponible para ella.</summary>
    public static AvisoRecibidoDto AAviso(AvisoDeCuenta recibido, bool disponible) =>
        new(
            recibido.Aviso.Id,
            recibido.Aviso.Texto,
            recibido.Aviso.GeneradoEn,
            new ActividadDeAvisoDto(recibido.Aviso.ActividadTipo, recibido.Aviso.ActividadId, disponible),
            recibido.Leido,
            recibido.Leido ? recibido.LeidoEn : null);

    /// <summary>Un elemento de la campana que trae un comunicado.</summary>
    public static ElementoDeCampanaDto ADeComunicado(ComunicadoRecibidoDto comunicado) =>
        new(TipoDeElementoDeCampana.COMUNICADO, comunicado, null);

    /// <summary>Un elemento de la campana que trae un aviso.</summary>
    public static ElementoDeCampanaDto ADeAviso(AvisoRecibidoDto aviso) =>
        new(TipoDeElementoDeCampana.AVISO, null, aviso);

    /// <summary>El identificador del contenido del elemento, que es su cursor.</summary>
    public static Guid Id(ElementoDeCampanaDto elemento) =>
        elemento.Comunicado?.Id ?? elemento.Aviso!.Id;
}
