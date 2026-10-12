using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa las cuentas del resumen del club con Entity Framework.
/// Su responsabilidad es hacer cada cuenta en la base de datos, sin traer filas: los entrenadores
/// como la unión de dos conjuntos de integrantes, para que quien tiene varias asignaciones, o
/// asignación y rol ENTRENADOR a la vez, cuente una sola vez. Trabaja siempre con el filtro de
/// aislamiento activo: sin club en el contexto, todo da cero.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioResumenClub : IRepositorioResumenClub
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioResumenClub(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<(int Jugadores, int Equipos, int Entrenadores, int Categorias)> ContarAsync(
        CancellationToken cancelacion = default)
    {
        var jugadores = await _contexto.UsuariosRol.Where(RepositorioJugadores.EsJugadorDelClub).CountAsync(cancelacion);
        var equipos = await _contexto.Equipos.CountAsync(equipo => equipo.Activo && equipo.Categoria!.Activa, cancelacion);
        var categorias = await _contexto.Categorias.CountAsync(categoria => categoria.Activa, cancelacion);

        var asignados = _contexto.AsignacionesEntrenadorCategoria
            .Where(asignacion => asignacion.Activa && asignacion.Categoria!.Activa)
            .Select(asignacion => asignacion.UsuarioRolId);
        var conRolDeEntrenador = _contexto.UsuariosRol
            .Where(integrante => integrante.Rol == Rol.ENTRENADOR
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO
                && integrante.Activo)
            .Select(integrante => integrante.Id);
        var entrenadores = await asignados.Union(conRolDeEntrenador).CountAsync(cancelacion);

        return (jugadores, equipos, entrenadores, categorias);
    }
}
