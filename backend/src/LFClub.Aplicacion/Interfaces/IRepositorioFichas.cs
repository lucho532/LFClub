using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las fichas de los jugadores del club de la petición: lo que la ficha
/// añade a cada jugador y el sello de su último cambio.
/// Su responsabilidad es leer la ficha de un jugador y sellar su último cambio, creando la fila si
/// todavía no existe: una ficha que nadie ha cambiado no tiene fila.
/// No recibe un identificador de club ni ve fichas de otro club. No decide quién puede ver o
/// cambiar una ficha, no bloquea el club y no lee ni guarda los archivos.
/// </summary>
public interface IRepositorioFichas
{
    /// <summary>La ficha de ese jugador, sin seguimiento; nulo si nadie la ha cambiado.</summary>
    Task<FichaJugador?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Sella el último cambio de la ficha de ese jugador con la fecha y con la cuenta y el nombre
    /// de quien cambia, copiado en ese momento (§13), sustituyendo al sello anterior (RF-038). Si
    /// la ficha no tiene fila, la crea. Devuelve la ficha, con seguimiento, para que quien llama
    /// escriba en ella lo que cambió; se guarda con la unidad de trabajo. Debe llamarse con el
    /// club ya bloqueado.
    /// </summary>
    Task<FichaJugador> SellarCambioAsync(
        UsuarioRol jugador, UsuarioRol quienCambia, DateTime ahoraUtc, CancellationToken cancelacion = default);
}
