using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa el armado de un entrenamiento nuevo a partir de sus datos validados y de su audiencia
/// ya armada (spec 009 y spec 013).
/// Su responsabilidad es crear la entidad con sus categorías, sus equipos y sus jugadores añadidos,
/// en estado programado y con quién y cuándo lo creó.
/// No valida nada, no guarda y no decide quién puede programarlo.
/// </summary>
public static class ConstructorDeEntrenamiento
{
    /// <summary>Arma el entrenamiento nuevo, sin guardarlo.</summary>
    public static Entrenamiento Armar(
        DatosDeEntrenamiento datos, AudienciaArmada audiencia, Guid clubId, Guid creadoPor, DateTime ahoraUtc) =>
        new()
        {
            ClubId = clubId,
            Fecha = datos.Fecha,
            HoraInicio = datos.HoraInicio,
            HoraFin = datos.HoraFin,
            Lugar = datos.Lugar,
            Nota = datos.Nota,
            Estado = EstadoEntrenamiento.PROGRAMADO,
            CreadoEn = ahoraUtc,
            CreadoPorUsuarioRolId = creadoPor,
            Categorias = datos.CategoriaIds
                .Select(categoriaId => new EntrenamientoCategoria { CategoriaId = categoriaId, ClubId = clubId })
                .ToList(),
            Equipos = audiencia.EquipoIds
                .Select(equipoId => new EntrenamientoEquipo { EquipoId = equipoId, ClubId = clubId })
                .ToList(),
            Jugadores = audiencia.JugadorAnadidoIds
                .Select(jugadorId => new EntrenamientoJugador
                {
                    UsuarioRolId = jugadorId,
                    ClubId = clubId,
                    Origen = OrigenDeJugadorEnEntrenamiento.ANADIDO,
                    AnadidoEn = ahoraUtc,
                })
                .ToList(),
        };
}
