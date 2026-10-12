using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Datos;

/// <summary>
/// Representa la creación de la única cuenta DESARROLLADOR al arrancar (research §6, RF-001).
/// Su responsabilidad es crearla, si no existe ninguna, con el correo de la configuración, sin
/// contraseña y sin ningún integrante; el DESARROLLADOR crea su contraseña con "olvidé mi
/// contraseña".
/// No asigna contraseñas, no modifica una cuenta DESARROLLADOR que ya existe y no hay ningún
/// endpoint que cree o cambie esa marca.
/// </summary>
public class CuentaInicial
{
    private readonly ContextoLFClub _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio sobre el contexto de datos.</summary>
    public CuentaInicial(ContextoLFClub contexto, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj)
    {
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <summary>Crea la cuenta DESARROLLADOR si todavía no existe ninguna.</summary>
    public async Task CrearSiFaltaAsync(string? correoDesarrollador, CancellationToken cancelacion = default)
    {
        if (await _contexto.Usuarios.AnyAsync(usuario => usuario.EsDesarrollador, cancelacion))
        {
            return;
        }

        var correo = (correoDesarrollador ?? string.Empty).Trim();
        if (correo.Length == 0)
        {
            throw new InvalidOperationException(
                "Falta 'Plataforma:CorreoDesarrollador': es el correo de la cuenta DESARROLLADOR.");
        }

        _contexto.Usuarios.Add(new Usuario
        {
            Correo = correo,
            CorreoNormalizado = NormalizadorTexto.Correo(correo),
            EsDesarrollador = true,
            CreadoEn = _reloj.AhoraUtc,
        });

        try
        {
            await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.UnicoDesarrollador)
        {
            // Otra instancia de la API la creó a la vez: ya existe, que es lo que se quería.
            _contexto.ChangeTracker.Clear();
        }
    }
}
