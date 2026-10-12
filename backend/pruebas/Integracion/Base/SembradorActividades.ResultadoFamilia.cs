using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>Las ayudas de la spec 010: lo que publica la familia y convocar a un partido existente.</summary>
public partial class SembradorActividades
{
    /// <summary>
    /// Escribe lo que publica o indica la familia de un convocado, como lo haría el servicio: si se
    /// pasa marcador y el partido no lo tiene, lo publica a nombre del jugador y lo deja jugado; y
    /// la fila del jugador queda indicada por la familia.
    /// </summary>
    public Task PublicarPorFamiliaAsync(
        Partido partido, UsuarioRol jugador, int? golesClub, int? golesRival, bool jugo, int goles) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var ahora = DateTime.UtcNow;
            var nombre = $"{jugador.Nombres} {jugador.Apellidos}";
            if (golesClub is not null && golesRival is not null)
            {
                await contexto.Partidos.IgnoreQueryFilters()
                    .Where(fila => fila.Id == partido.Id && fila.GolesClub == null)
                    .ExecuteUpdateAsync(cambios => cambios
                        .SetProperty(fila => fila.Estado, EstadoPartido.JUGADO)
                        .SetProperty(fila => fila.GolesClub, golesClub)
                        .SetProperty(fila => fila.GolesRival, golesRival)
                        .SetProperty(fila => fila.ResultadoCambiadoEn, ahora)
                        .SetProperty(fila => fila.ResultadoCambiadoPorUsuarioId, jugador.UsuarioId)
                        .SetProperty(fila => fila.ResultadoCambiadoPorNombre, nombre)
                        .SetProperty(fila => fila.OrigenResultado, OrigenResultado.FAMILIA)
                        .SetProperty(fila => fila.PublicadoEn, ahora)
                        .SetProperty(fila => fila.PublicadoPorUsuarioId, jugador.UsuarioId)
                        .SetProperty(fila => fila.PublicadoPorNombre, nombre)
                        .SetProperty(fila => fila.PublicadoPorFamilia, true));
            }

            return await contexto.PartidosJugador.IgnoreQueryFilters()
                .Where(fila => fila.PartidoId == partido.Id && fila.UsuarioRolId == jugador.Id)
                .ExecuteUpdateAsync(cambios => cambios
                    .SetProperty(fila => fila.Jugo, jugo)
                    .SetProperty(fila => fila.Goles, goles)
                    .SetProperty(fila => fila.IndicadoPor, OrigenResultado.FAMILIA)
                    .SetProperty(fila => fila.IndicadoEn, ahora)
                    .SetProperty(fila => fila.IndicadoPorUsuarioId, jugador.UsuarioId)
                    .SetProperty(fila => fila.IndicadoPorNombre, nombre));
        });

    /// <summary>Añade esos convocados a un partido existente, con su categoría actual copiada.</summary>
    public Task ConvocarAsync(Partido partido, params UsuarioRol[] jugadores) => _fabrica.ConContextoAsync(async contexto =>
    {
        contexto.PartidosJugador.AddRange(jugadores.Select(jugador => new PartidoJugador
        {
            PartidoId = partido.Id,
            UsuarioRolId = jugador.Id,
            ClubId = jugador.ClubId,
            CategoriaId = jugador.CategoriaId!.Value,
            ConvocadoEn = DateTime.UtcNow,
        }));
        return await contexto.SaveChangesAsync();
    });
}
