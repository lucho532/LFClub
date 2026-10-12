using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa, con Entity Framework, la única escritura en un club desde otro club (constitución
/// §7.1, cuarta excepción): el retiro por número de documento del jugador de otra cuenta.
/// Su responsabilidad es bloquear varios clubes en orden y retirar con sentencias condicionadas,
/// borrando también la foto propia de los que retira (§14.2, spec 007), sus convocatorias a partidos
/// que no están jugados y las respuestas de su familia a entrenamientos que no han empezado (RF-033
/// de la 009). Su asistencia y lo que jugó se conservan.
/// Por eso se salta el filtro de aislamiento, y cada sentencia va acotada a un número de documento
/// o a los clubes que se le indican.
/// No decide a quién retirar ni lee datos para devolverlos: solo cuenta cuántos retiró.
/// </summary>
public class RepositorioRetiroEntreClubes : IRepositorioRetiroEntreClubes
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioRetiroEntreClubes(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task BloquearClubesAsync(IReadOnlyCollection<Guid> clubIds, CancellationToken cancelacion = default)
    {
        var ordenados = clubIds.Distinct().Order().ToArray();
        if (ordenados.Length == 0)
        {
            return;
        }

        // Una sola sentencia, en orden de identificador: nunca dos bloqueos cruzados.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Clubes\" WHERE \"Id\" = ANY({ordenados}) ORDER BY \"Id\" FOR UPDATE", cancelacion);
    }

    /// <inheritdoc />
    public async Task<int> RetirarDeOtrasCuentasAsync(
        string numeroDocumento, Guid cuentaQueQueda, DateTime retiradoEn, CancellationToken cancelacion = default)
    {
        var aRetirar = _contexto.UsuariosRol
            .IgnoreQueryFilters()
            .Where(integrante => integrante.NumeroDocumento == numeroDocumento
                && integrante.UsuarioId != cuentaQueQueda
                && integrante.Rol == Rol.JUGADOR
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO
                && integrante.Activo);

        await _contexto.JugadoresEquipo
            .IgnoreQueryFilters()
            .Where(fila => aRetirar.Any(integrante => integrante.Id == fila.UsuarioRolId))
            .ExecuteDeleteAsync(cancelacion);

        // Antes de retirarlos: después ya no estarían activos y la condición no los encontraría.
        await _contexto.FotosJugador
            .IgnoreQueryFilters()
            .Where(foto => aRetirar.Any(integrante => integrante.Id == foto.UsuarioRolId))
            .ExecuteDeleteAsync(cancelacion);

        // Lo pendiente en las actividades: hoy y ahora en hora de Colombia, la de las actividades.
        var (hoy, ahora) = HoraDeColombia.De(retiradoEn);
        await _contexto.PartidosJugador
            .IgnoreQueryFilters()
            .Where(fila => aRetirar.Any(integrante => integrante.Id == fila.UsuarioRolId)
                && fila.Partido!.Estado != EstadoPartido.JUGADO)
            .ExecuteDeleteAsync(cancelacion);
        await _contexto.RespuestasEntrenamiento
            .IgnoreQueryFilters()
            .Where(fila => aRetirar.Any(integrante => integrante.Id == fila.UsuarioRolId)
                && (fila.Entrenamiento!.Fecha > hoy || (fila.Entrenamiento.Fecha == hoy && fila.Entrenamiento.HoraInicio > ahora)))
            .ExecuteDeleteAsync(cancelacion);

        return await aRetirar.ExecuteUpdateAsync(
            cambios => cambios
                .SetProperty(integrante => integrante.Activo, false)
                .SetProperty(integrante => integrante.CategoriaId, (Guid?)null)
                .SetProperty(integrante => integrante.RetiradoEn, retiradoEn)
                .SetProperty(integrante => integrante.RetiradoPorUsuarioId, (Guid?)null)
                .SetProperty(integrante => integrante.RetiradoPorNombre, (string?)null),
            cancelacion);
    }
}
