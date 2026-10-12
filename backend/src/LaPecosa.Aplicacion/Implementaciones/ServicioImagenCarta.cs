using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que exporta la carta de un jugador como imagen (spec 010, RF-029 a
/// RF-031; research §11).
/// Su responsabilidad es pedir la carta con <see cref="IServicioConsultaCarta"/> (quien no ve la
/// ficha recibe <c>404</c>, y así la imagen lleva exactamente lo que la carta en pantalla), negar a
/// quien no exporta con <c>403 rol_no_autorizado</c> aunque la vea (DIRECTIVO y ENTRENADOR), leer la
/// foto del jugador (la de un retirado ya se borró: figura genérica), el escudo y los colores del
/// club, y pedir el dibujo a <see cref="IGeneradorImagenCarta"/>.
/// No dibuja, no guarda la imagen ni la deja en caché, y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioImagenCarta : IServicioImagenCarta
{
    private readonly IServicioConsultaCarta _consulta;
    private readonly IRepositorioFotosJugador _fotos;
    private readonly IRepositorioEscudos _escudos;
    private readonly IRepositorioClub _club;
    private readonly IGeneradorImagenCarta _generador;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioImagenCarta(
        IServicioConsultaCarta consulta,
        IRepositorioFotosJugador fotos,
        IRepositorioEscudos escudos,
        IRepositorioClub club,
        IGeneradorImagenCarta generador)
    {
        _consulta = consulta;
        _fotos = fotos;
        _escudos = escudos;
        _club = club;
        _generador = generador;
    }

    /// <inheritdoc />
    public async Task<ImagenCarta> GenerarAsync(Guid usuarioRolId, UsuarioRol quienPide, CancellationToken cancelacion = default)
    {
        if (quienPide.Rol is not (Rol.PRESIDENTE or Rol.JUGADOR))
        {
            throw RolNoAutorizado();
        }

        var carta = await _consulta.ObtenerAsync(usuarioRolId, quienPide, cancelacion);
        if (!carta.Permisos.Exporta)
        {
            throw RolNoAutorizado();
        }

        var foto = carta.FotoActualizadaEn is null ? null : await _fotos.ObtenerAsync(usuarioRolId, cancelacion);
        var escudo = await _escudos.ObtenerAsync(cancelacion);
        var club = await _club.ObtenerAsync(cancelacion);
        var datos = new DatosImagenCarta(
            carta.Nombres,
            carta.Apellidos,
            carta.Dorsal,
            carta.Posicion,
            carta.Estadisticas,
            carta.Valoracion,
            carta.Rareza,
            foto?.Contenido,
            escudo?.Contenido,
            club?.ColorPrincipal,
            club?.ColorAcento);
        var nombre = $"carta-{NormalizadorTexto.ParaArchivo(carta.Apellidos)}-{NormalizadorTexto.ParaArchivo(carta.Nombres)}.png";
        return new ImagenCarta(_generador.Generar(datos), nombre);
    }

    private static ExcepcionDeAplicacion RolNoAutorizado() =>
        new("rol_no_autorizado", 403, "Solo la familia del jugador y el presidente descargan la carta.");
}
