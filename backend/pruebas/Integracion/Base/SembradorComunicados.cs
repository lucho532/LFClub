using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Escribe directamente en la base lo que necesitan las pruebas de la spec 011: comunicados con sus
/// destinos y destinatarios como los dejaría el servicio, su retiro y su lectura, para preparar
/// escenarios sin pasar por la API.
/// </summary>
public class SembradorComunicados
{
    private readonly FabricaApi _fabrica;

    public SembradorComunicados(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Publica un comunicado como lo haría el servicio: con los destinos indicados (categorías o
    /// equipos, según el alcance) y una fila de destinatario por cada jugador.
    /// </summary>
    public Task<Comunicado> PublicarAsync(
        Club club,
        UsuarioRol autor,
        string titulo,
        string texto,
        AlcanceComunicado alcance,
        IReadOnlyCollection<Categoria>? categorias = null,
        IReadOnlyCollection<Equipo>? equipos = null,
        IReadOnlyCollection<UsuarioRol>? destinatarios = null) => _fabrica.ConContextoAsync(async contexto =>
    {
        var comunicado = new Comunicado
        {
            ClubId = club.Id,
            Titulo = titulo,
            Texto = texto,
            Alcance = alcance,
            Clave = Guid.NewGuid(),
            PublicadoEn = DateTime.UtcNow,
            PublicadoPorUsuarioRolId = autor.Id,
            PublicadoPorNombre = $"{autor.Nombres} {autor.Apellidos}",
            PublicadoPorRol = autor.Rol,
        };

        foreach (var categoria in categorias ?? [])
        {
            comunicado.Destinos.Add(new ComunicadoDestino
            {
                ClubId = club.Id, CategoriaId = categoria.Id, CategoriaNombre = categoria.Anio.ToString(),
            });
        }

        foreach (var equipo in equipos ?? [])
        {
            var categoria = await contexto.Categorias.IgnoreQueryFilters().FirstAsync(fila => fila.Id == equipo.CategoriaId);
            comunicado.Destinos.Add(new ComunicadoDestino
            {
                ClubId = club.Id,
                CategoriaId = categoria.Id,
                CategoriaNombre = categoria.Anio.ToString(),
                EquipoId = equipo.Id,
                EquipoNombre = equipo.Nombre,
            });
        }

        foreach (var jugador in destinatarios ?? [])
        {
            comunicado.Destinatarios.Add(new ComunicadoDestinatario
            {
                ClubId = club.Id, UsuarioRolId = jugador.Id, UsuarioId = jugador.UsuarioId,
            });
        }

        contexto.Comunicados.Add(comunicado);
        await contexto.SaveChangesAsync();
        return comunicado;
    });

    /// <summary>Retira el comunicado, con el nombre de quien lo retira y la hora de ahora.</summary>
    public Task RetirarAsync(Comunicado comunicado, UsuarioRol por) => _fabrica.ConContextoAsync(contexto =>
        contexto.Comunicados.IgnoreQueryFilters()
            .Where(fila => fila.Id == comunicado.Id)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(fila => fila.RetiradoEn, DateTime.UtcNow)
                .SetProperty(fila => fila.RetiradoPorUsuarioRolId, por.Id)
                .SetProperty(fila => fila.RetiradoPorNombre, $"{por.Nombres} {por.Apellidos}")));

    /// <summary>Marca el comunicado como leído en todas las filas de esa cuenta.</summary>
    public Task LeerAsync(Comunicado comunicado, Usuario cuenta) => _fabrica.ConContextoAsync(contexto =>
        contexto.ComunicadosDestinatario.IgnoreQueryFilters()
            .Where(fila => fila.ComunicadoId == comunicado.Id && fila.UsuarioId == cuenta.Id && fila.LeidoEn == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.LeidoEn, DateTime.UtcNow)));

    /// <summary>Crea un equipo activo de la categoría con esos jugadores dentro.</summary>
    public async Task<Equipo> AgregarEquipoAsync(
        Categoria categoria, string nombre, params UsuarioRol[] jugadores)
    {
        var equipo = await _fabrica.Categorias.CrearEquipoAsync(categoria, nombre);
        foreach (var jugador in jugadores)
        {
            await _fabrica.Categorias.PonerEnEquipoAsync(jugador, equipo);
        }

        return equipo;
    }

    /// <summary>Deja al jugador en la sala de espera: ingreso en espera, sin retiro.</summary>
    public Task PonerEnEsperaAsync(UsuarioRol jugador) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(fila => fila.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.EstadoIngreso, EstadoIngreso.EN_ESPERA)));

    /// <summary>Reincorpora al jugador retirado: activo, sin los datos del retiro.</summary>
    public Task ReincorporarAsync(UsuarioRol jugador, Categoria? categoria = null) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(fila => fila.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(fila => fila.Activo, true)
                .SetProperty(fila => fila.RetiradoEn, (DateTime?)null)
                .SetProperty(fila => fila.RetiradoPorUsuarioId, (Guid?)null)
                .SetProperty(fila => fila.RetiradoPorNombre, (string?)null)
                .SetProperty(fila => fila.CategoriaId, categoria == null ? (Guid?)null : categoria.Id)));

    /// <summary>Cambia la categoría actual del jugador.</summary>
    public Task MoverACategoriaAsync(UsuarioRol jugador, Categoria categoria) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(fila => fila.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.CategoriaId, (Guid?)categoria.Id)));
}
