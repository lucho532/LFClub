using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que crea, renombra, desactiva y borra los equipos de una categoría.
/// Su responsabilidad es aplicar las reglas de los equipos: solo se crean en una categoría activa,
/// su nombre no se repite entre los equipos activos de la categoría sin distinguir mayúsculas, y
/// solo se borra el que nunca se usó. Un equipo inactivo, inexistente o de otra categoría no
/// existe: 404.
/// No accede al contexto de Entity Framework ni conoce HTTP, y no decide quién juega en cada
/// equipo ni quién lo dirige.
/// </summary>
public class ServicioEquipos : IServicioEquipos
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioEquipos _equipos;
    private readonly LectorDeCategorias _lector;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioEquipos(
        IRepositorioClub club,
        IRepositorioCategorias categorias,
        IRepositorioEquipos equipos,
        LectorDeCategorias lector,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _club = club;
        _categorias = categorias;
        _equipos = equipos;
        _lector = lector;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> CrearAsync(
        Guid categoriaId, NombreEquipoDto datos, CancellationToken cancelacion = default)
    {
        var nombre = ValidadorNombreEquipo.Validar(datos.Nombre);

        return ConNombreUnicoAsync(
            async clubId =>
            {
                var categoria = await _categorias.ObtenerAsync(categoriaId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (!categoria.Activa)
                {
                    throw ErroresDeCategorias.CategoriaInactiva();
                }

                await ComprobarNombreLibreAsync(categoriaId, nombre, null, cancelacion);
                await _equipos.AgregarAsync(
                    new Equipo
                    {
                        ClubId = clubId,
                        CategoriaId = categoriaId,
                        Nombre = nombre.Nombre,
                        NombreNormalizado = nombre.Normalizado,
                        CreadoEn = _reloj.AhoraUtc,
                    },
                    cancelacion);
            },
            categoriaId,
            cancelacion);
    }

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> RenombrarAsync(
        Guid categoriaId, Guid equipoId, NombreEquipoDto datos, CancellationToken cancelacion = default)
    {
        var nombre = ValidadorNombreEquipo.Validar(datos.Nombre);

        return ConNombreUnicoAsync(
            async _ =>
            {
                await ExistenteAsync(categoriaId, equipoId, cancelacion);
                await ComprobarNombreLibreAsync(categoriaId, nombre, equipoId, cancelacion);
                await _equipos.RenombrarAsync(equipoId, nombre.Nombre, nombre.Normalizado, cancelacion);
            },
            categoriaId,
            cancelacion);
    }

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> DesactivarAsync(
        Guid categoriaId, Guid equipoId, CancellationToken cancelacion = default) =>
        EnElClubAsync(
            async _ =>
            {
                await ExistenteAsync(categoriaId, equipoId, cancelacion);
                await _equipos.DesactivarAsync(equipoId, cancelacion);
            },
            categoriaId,
            cancelacion);

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> BorrarAsync(Guid categoriaId, Guid equipoId, CancellationToken cancelacion = default) =>
        EnElClubAsync(
            async _ =>
            {
                await ExistenteAsync(categoriaId, equipoId, cancelacion);
                if (!await _equipos.BorrarSiNuncaSeUsoAsync(equipoId, cancelacion))
                {
                    throw ErroresDeCategorias.EquipoConHistorial();
                }
            },
            categoriaId,
            cancelacion);

    /// <summary>Ejecuta el cambio con el club bloqueado y responde con la categoría actualizada.</summary>
    private Task<CategoriaDetalleDto> EnElClubAsync(
        Func<Guid, Task> cambio, Guid categoriaId, CancellationToken cancelacion) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await cambio(await _club.BloquearAsync(cancelacion));
                return await _lector.DetalleAsync(categoriaId, conDocumentacion: true, cancelacion);
            },
            cancelacion);

    /// <summary>Como <see cref="EnElClubAsync"/>, traduciendo la violación del índice del nombre.</summary>
    private async Task<CategoriaDetalleDto> ConNombreUnicoAsync(
        Func<Guid, Task> cambio, Guid categoriaId, CancellationToken cancelacion)
    {
        try
        {
            return await EnElClubAsync(cambio, categoriaId, cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.EquipoEnCategoria)
        {
            throw ErroresDeCategorias.EquipoYaExiste();
        }
    }

    private async Task ComprobarNombreLibreAsync(
        Guid categoriaId, NombreDeEquipo nombre, Guid? exceptoEquipoId, CancellationToken cancelacion)
    {
        if (await _equipos.ExisteNombreAsync(categoriaId, nombre.Normalizado, exceptoEquipoId, cancelacion))
        {
            throw ErroresDeCategorias.EquipoYaExiste();
        }
    }

    private async Task<Equipo> ExistenteAsync(Guid categoriaId, Guid equipoId, CancellationToken cancelacion) =>
        await _equipos.ObtenerActivoAsync(categoriaId, equipoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
}
