using LFClub.Aplicacion.Interfaces;

namespace LFClub.Infraestructura.Datos;

/// <summary>
/// Representa el club de la petición en curso; hay una instancia por petición.
/// Su responsabilidad es guardar el club que fija la autorización para que lo use el filtro de
/// aislamiento.
/// No comprueba la pertenencia del usuario ni el estado del club.
/// </summary>
public class ContextoClub : IContextoClub
{
    /// <inheritdoc />
    public Guid? ClubId { get; private set; }

    /// <inheritdoc />
    public void Fijar(Guid clubId) => ClubId = clubId;
}
