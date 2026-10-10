using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa la comprobación de acceso que comparten todas las operaciones de la ficha del
/// jugador (constitución §7.5 y §15; RF-012).
/// Su responsabilidad es leer al jugador de la ficha, averiguar si es el jugador de la petición
/// (el integrante que pregunta, y no un hermano de su misma cuenta: RF-030 de la 006) y si quien
/// pregunta entrena su categoría, aplicar <see cref="ReglaAccesoAFicha"/> y
/// devolver el jugador con su alcance. Si el jugador no existe o quien pregunta no puede ver su
/// ficha, responde el mismo <c>404 no_encontrado</c>, sin revelar cuál de los dos es el caso.
/// No decide quién ve qué: eso es de la regla. No arma la respuesta ni cambia nada, y no comprueba
/// la pertenencia al club ni el rol mínimo de cada operación, que ya hizo la autorización.
/// </summary>
public class AccesoAFicha
{
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioAsignaciones _asignaciones;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AccesoAFicha(IRepositorioJugadores jugadores, IRepositorioAsignaciones asignaciones)
    {
        _jugadores = jugadores;
        _asignaciones = asignaciones;
    }

    /// <summary>
    /// El jugador de esa ficha, con su cuenta, su categoría y sus equipos, y lo que quien pregunta
    /// puede hacer con ella. Lanza <c>404 no_encontrado</c> si no es un jugador aprobado del club
    /// o si quien pregunta no puede ver su ficha.
    /// </summary>
    public async Task<(UsuarioRol Jugador, AlcanceDeFicha Alcance)> ResolverAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var jugador = await _jugadores.ObtenerParaFichaAsync(usuarioRolId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        // Se compara el integrante y no la cuenta: con un hijo elegido, la familia no llega a la
        // ficha de su hermano hasta que cambie de jugador (RF-030).
        var esElJugadorDeLaPeticion = jugador.Id == quienPregunta.Id;

        // La asignación se consulta en cada petición: quien la pierde deja de ver la ficha al instante.
        var entrenaSuCategoria = quienPregunta.Rol == Rol.ENTRENADOR
            && jugador.Categoria is { Activa: true } categoria
            && await _asignaciones.TieneActivaAsync(categoria.Id, quienPregunta.Id, cancelacion);

        var alcance = ReglaAccesoAFicha.Evaluar(
            quienPregunta.Rol, esElJugadorDeLaPeticion, jugador.Activo, entrenaSuCategoria);
        if (!alcance.Ve)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        return (jugador, alcance);
    }

    /// <summary>Lanza el mismo <c>404</c> si ese alcance no permite cambiar la ficha.</summary>
    public static void ExigirCambio(AlcanceDeFicha alcance)
    {
        if (!alcance.Cambia)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }
    }

    /// <summary>
    /// Lanza el mismo <c>404</c> si ese alcance no permite corregir los nombres, los apellidos y la
    /// fecha de nacimiento.
    /// </summary>
    public static void ExigirCorreccionDeIdentidad(AlcanceDeFicha alcance)
    {
        if (!alcance.CorrigeIdentidad)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }
    }
}
