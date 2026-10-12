using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la comprobación de acceso que comparten todas las operaciones de la carta de un
/// jugador (spec 010, RF-020; research §8).
/// Su responsabilidad es decidir si quien pregunta ve la carta con <see cref="AccesoAFicha"/>, sin
/// cambiarlo: la ven exactamente quienes ven la ficha, y los demás reciben el mismo
/// <c>404 no_encontrado</c> que con un identificador inventado. Después averigua, en cada petición,
/// si quien pregunta tiene una asignación activa en la categoría activa del jugador, sea cual sea su
/// rol, y aplica <see cref="ReglaAccesoACarta"/>.
/// No decide qué puede hacer cada rol (eso es de la regla), no arma la respuesta ni cambia nada.
/// </summary>
public class AccesoACarta
{
    private readonly AccesoAFicha _ficha;
    private readonly IRepositorioAsignaciones _asignaciones;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AccesoACarta(AccesoAFicha ficha, IRepositorioAsignaciones asignaciones)
    {
        _ficha = ficha;
        _asignaciones = asignaciones;
    }

    /// <summary>
    /// El jugador de la carta y lo que quien pregunta puede hacer con ella. Lanza
    /// <c>404 no_encontrado</c> si no puede ver su ficha.
    /// </summary>
    public async Task<(UsuarioRol Jugador, AlcanceDeCarta Alcance)> ResolverAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var (jugador, _) = await _ficha.ResolverAsync(usuarioRolId, quienPregunta, cancelacion);

        // La asignación se lee en cada petición: quien la pierde deja de cambiar la carta al instante.
        var entrena = jugador.Categoria is { Activa: true } categoria
            && await _asignaciones.TieneActivaAsync(categoria.Id, quienPregunta.Id, cancelacion);
        var alcance = ReglaAccesoACarta.Alcance(quienPregunta.Rol, jugador.Id == quienPregunta.Id, entrena, jugador.Activo);
        return (jugador, alcance);
    }
}
