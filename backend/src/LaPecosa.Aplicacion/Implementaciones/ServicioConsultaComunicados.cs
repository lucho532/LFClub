using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta los comunicados (spec 011, RF-012 a RF-019 y RF-024).
/// Su responsabilidad es listar lo publicado con las familias alcanzadas y los destinos según el
/// rol (PRESIDENTE y DIRECTIVO todo, ENTRENADOR lo suyo), y abrir lo recibido por
/// la cuenta de un jugador, sin la audiencia ni cuántas familias lo recibieron. Pagina por
/// cursor: el límite va de 1 a 50 y es 30 por defecto (research §9). La lista y el contador de lo
/// recibido pasaron a la campana, <see cref="ServicioConsultaCampana"/> (spec 012).
/// No modifica datos, no marca la lectura y responde el mismo <c>404</c> a un comunicado que no le
/// llegó a la cuenta, está retirado o es de otro club (RF-032).
/// </summary>
public class ServicioConsultaComunicados : IServicioConsultaComunicados
{
    private const int LimitePorDefecto = 30;
    private const int LimiteMaximo = 50;

    private readonly IRepositorioComunicados _comunicados;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaComunicados(IRepositorioComunicados comunicados)
    {
        _comunicados = comunicados;
    }

    /// <inheritdoc />
    public async Task<PaginaDePublicadosDto> ListarPublicadosAsync(
        int? limite, Guid? antesDe, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        if (quienPregunta.Rol is not (Rol.PRESIDENTE or Rol.DIRECTIVO or Rol.ENTRENADOR))
        {
            throw ErroresDeComunicados.RolNoAutorizado();
        }

        var tamano = Limite(limite);
        var autor = quienPregunta.Rol == Rol.ENTRENADOR ? quienPregunta.Id : (Guid?)null;
        var filas = await _comunicados.ListarPublicadosAsync(autor, antesDe, tamano + 1, cancelacion);
        var pagina = filas.Take(tamano).ToList();
        var familias = await _comunicados.ContarFamiliasAsync(pagina.Select(fila => fila.Id).ToList(), cancelacion);

        return new PaginaDePublicadosDto(
            pagina
                .Select(fila => MapperComunicados.APublicado(
                    fila, familias.GetValueOrDefault(fila.Id), quienPregunta.Rol, quienPregunta.Id))
                .ToList(),
            filas.Count > tamano);
    }

    /// <inheritdoc />
    public async Task<ComunicadoRecibidoDto> ObtenerRecibidoAsync(
        Guid comunicadoId,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default)
    {
        ExigirJugador(quienPregunta);
        var recibido = await _comunicados.ObtenerRecibidoAsync(
            quienPregunta.UsuarioId, limitacion, comunicadoId, cancelacion);
        return recibido is null ? throw ExcepcionDeAplicacion.NoEncontrado() : MapperComunicados.ARecibido(recibido);
    }

    /// <summary>Solo la cuenta de un jugador recibe comunicados.</summary>
    internal static void ExigirJugador(UsuarioRol quienPregunta)
    {
        if (quienPregunta.Rol != Rol.JUGADOR)
        {
            throw ErroresDeComunicados.RolNoAutorizado();
        }
    }

    private static int Limite(int? limite) => Math.Clamp(limite ?? LimitePorDefecto, 1, LimiteMaximo);
}
