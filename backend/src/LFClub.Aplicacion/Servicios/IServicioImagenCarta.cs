using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de exportar la carta de un jugador como imagen (spec 010, historia 4).
/// Su responsabilidad es que solo la obtengan la cuenta del jugador (su jugador) y el PRESIDENTE,
/// también la de un retirado, y que la barrera la ponga el servidor (RF-031, §15).
/// No conoce HTTP.
/// </summary>
public interface IServicioImagenCarta
{
    /// <summary>La imagen PNG de la carta y el nombre de su archivo.</summary>
    Task<ImagenCarta> GenerarAsync(Guid usuarioRolId, UsuarioRol quienPide, CancellationToken cancelacion = default);
}

/// <summary>
/// Representa la imagen exportada de una carta.
/// Su responsabilidad es llevar los bytes del PNG y el nombre con el que se descarga.
/// No lleva nada más.
/// </summary>
/// <param name="Contenido">Bytes del PNG.</param>
/// <param name="NombreArchivo"><c>carta-&lt;apellidos&gt;-&lt;nombres&gt;.png</c>, sin tildes ni espacios.</param>
public record ImagenCarta(byte[] Contenido, string NombreArchivo);
