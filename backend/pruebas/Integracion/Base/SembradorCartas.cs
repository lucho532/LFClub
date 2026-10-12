using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>
/// Escribe directamente en la base lo que necesita la carta de la spec 010: colores y escudo del
/// club, foto del jugador y la fila de la carta, para las pruebas que solo los leen.
/// </summary>
public class SembradorCartas
{
    private readonly FabricaApi _fabrica;

    public SembradorCartas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>Una imagen de verdad (que se puede decodificar), de un solo color.</summary>
    public static byte[] Imagen(SKEncodedImageFormat formato, SKColor color, int ancho = 40, int alto = 40)
    {
        using var mapa = new SKBitmap(ancho, alto);
        mapa.Erase(color);
        using var imagen = SKImage.FromBitmap(mapa);
        using var datos = imagen.Encode(formato, 90);
        return datos.ToArray();
    }

    /// <summary>Pone los colores del club y un escudo PNG pequeño.</summary>
    public Task PonerColoresYEscudoAsync(Club club, string? principal, string? acento) => _fabrica.ConContextoAsync(async contexto =>
    {
        await contexto.Clubes.Where(fila => fila.Id == club.Id).ExecuteUpdateAsync(cambios => cambios
            .SetProperty(fila => fila.ColorPrincipal, principal)
            .SetProperty(fila => fila.ColorAcento, acento)
            .SetProperty(fila => fila.VersionEscudo, fila => fila.VersionEscudo + 1));
        await contexto.EscudosClub.IgnoreQueryFilters().Where(fila => fila.ClubId == club.Id).ExecuteDeleteAsync();
        contexto.EscudosClub.Add(new EscudoClub
        {
            ClubId = club.Id,
            Contenido = Imagen(SKEncodedImageFormat.Png, SKColors.Red, 60, 80),
            TipoContenido = "image/png",
        });
        return await contexto.SaveChangesAsync();
    });

    /// <summary>Pone una foto JPEG pequeña al jugador.</summary>
    public Task PonerFotoAsync(UsuarioRol jugador) => _fabrica.ConContextoAsync(async contexto =>
    {
        contexto.FotosJugador.Add(new FotoJugador
        {
            UsuarioRolId = jugador.Id,
            ClubId = jugador.ClubId,
            Contenido = Imagen(SKEncodedImageFormat.Jpeg, SKColors.SteelBlue, 80, 100),
            TipoContenido = "image/jpeg",
            GuardadaEn = DateTime.UtcNow,
        });
        return await contexto.SaveChangesAsync();
    });

    /// <summary>
    /// Crea o reemplaza la carta del jugador con ese dorsal, esa posición y esas estadísticas
    /// (ritmo, tiro, pase, regate, defensa, físico), con sellos del sembrador.
    /// </summary>
    public Task PonerCartaAsync(
        UsuarioRol jugador, int? dorsal = null, PosicionJugador? posicion = null, int[]? estadisticas = null) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            await contexto.CartasJugador.IgnoreQueryFilters().Where(fila => fila.UsuarioRolId == jugador.Id).ExecuteDeleteAsync();
            var ahora = DateTime.UtcNow;
            var conDorsal = dorsal is not null || posicion is not null;
            contexto.CartasJugador.Add(new CartaJugador
            {
                UsuarioRolId = jugador.Id,
                ClubId = jugador.ClubId,
                Dorsal = dorsal,
                Posicion = posicion,
                Ritmo = estadisticas?[0],
                Tiro = estadisticas?[1],
                Pase = estadisticas?[2],
                Regate = estadisticas?[3],
                Defensa = estadisticas?[4],
                Fisico = estadisticas?[5],
                EstadisticasCambiadasEn = estadisticas is null ? null : ahora,
                EstadisticasCambiadasPorNombre = estadisticas is null ? null : "Sembrador",
                DorsalCambiadoEn = conDorsal ? ahora : null,
                DorsalCambiadoPorNombre = conDorsal ? "Sembrador" : null,
            });
            return await contexto.SaveChangesAsync();
        });
}
