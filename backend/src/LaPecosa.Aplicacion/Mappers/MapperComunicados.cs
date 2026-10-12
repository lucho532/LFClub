using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de los comunicados en los DTOs del contrato (spec 011).
/// Su responsabilidad es armar el comunicado publicado, con su audiencia, las familias que lo
/// recibieron y si quien pregunta puede retirarlo, y el comunicado recibido, que nunca lleva la
/// audiencia ni cuántas familias lo recibieron (RF-032).
/// No decide quién ve qué ni consulta nada.
/// </summary>
public static class MapperComunicados
{
    /// <summary>El comunicado como lo ve el equipo del club.</summary>
    public static ComunicadoPublicadoDto APublicado(
        Comunicado comunicado, int familiasAlcanzadas, Rol rolDeQuienPregunta, Guid integranteId)
    {
        var retiro = comunicado.RetiradoEn is { } en
            ? new RetiroDeComunicadoDto(en, comunicado.RetiradoPorNombre ?? string.Empty)
            : null;
        var puedeRetirar = ReglaRetiroDeComunicado.Evaluar(
            rolDeQuienPregunta, integranteId, comunicado.PublicadoPorUsuarioRolId, comunicado.RetiradoEn is not null)
            == ResultadoRetiroDeComunicado.Retira;

        return new ComunicadoPublicadoDto(
            comunicado.Id,
            comunicado.Titulo,
            comunicado.Texto,
            comunicado.PublicadoEn,
            Autor(comunicado),
            comunicado.Alcance,
            comunicado.Destinos
                .OrderBy(destino => destino.CategoriaNombre, StringComparer.Ordinal)
                .ThenBy(destino => destino.EquipoNombre, StringComparer.Ordinal)
                .Select(destino => new DestinoDeComunicadoDto(
                    destino.CategoriaId, destino.CategoriaNombre, destino.EquipoId, destino.EquipoNombre))
                .ToList(),
            familiasAlcanzadas,
            retiro,
            puedeRetirar);
    }

    /// <summary>El comunicado como lo ve la cuenta de un jugador: sin audiencia ni cuántos lo recibieron.</summary>
    public static ComunicadoRecibidoDto ARecibido(RecibidoDeCuenta recibido) =>
        new(
            recibido.Comunicado.Id,
            recibido.Comunicado.Titulo,
            recibido.Comunicado.Texto,
            recibido.Comunicado.PublicadoEn,
            Autor(recibido.Comunicado),
            recibido.Leido,
            recibido.Leido ? recibido.LeidoEn : null);

    private static AutorDeComunicadoDto Autor(Comunicado comunicado) =>
        new(comunicado.PublicadoPorUsuarioRolId, comunicado.PublicadoPorNombre, comunicado.PublicadoPorRol);
}
