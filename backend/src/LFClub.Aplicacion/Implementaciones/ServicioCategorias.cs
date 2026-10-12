using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio con el que el PRESIDENTE crea, desactiva, reactiva y borra categorías.
/// Su responsabilidad es ejecutar cada operación en una transacción que empieza bloqueando el
/// club, para que dos altas del mismo año, o un alta y una aprobación simultáneas, den siempre un
/// resultado coherente (research §4), y aplicar las reglas: un año válido, una sola categoría por
/// año, no desactivar con jugadores y borrar solo lo que nunca se usó.
/// No accede al contexto de Entity Framework ni conoce HTTP, y no decide quién puede llamarlo.
/// </summary>
public class ServicioCategorias : IServicioCategorias
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioCategorias _categorias;
    private readonly UbicadorDeJugadores _ubicador;
    private readonly LectorDeCategorias _lector;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCategorias(
        IRepositorioClub club,
        IRepositorioCategorias categorias,
        UbicadorDeJugadores ubicador,
        LectorDeCategorias lector,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _club = club;
        _categorias = categorias;
        _ubicador = ubicador;
        _lector = lector;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<CategoriaConUbicadosDto> CrearAsync(CrearCategoriaDto datos, CancellationToken cancelacion = default)
    {
        var anio = AnioValido(datos.Anio);

        try
        {
            return await _unidadDeTrabajo.EnTransaccionAsync(
                async () =>
                {
                    var clubId = await _club.BloquearAsync(cancelacion);
                    var existente = await _categorias.BuscarPorAnioAsync(anio, cancelacion);
                    if (existente is not null)
                    {
                        throw existente.Activa
                            ? ErroresDeCategorias.CategoriaYaExiste(anio)
                            : ErroresDeCategorias.CategoriaInactivaYaExiste(anio);
                    }

                    var categoria = new Categoria { ClubId = clubId, Anio = anio, CreadaEn = _reloj.AhoraUtc };
                    await _categorias.AgregarAsync(categoria, cancelacion);
                    var ubicados = await _ubicador.RecogerAsync(categoria, cancelacion);

                    return new CategoriaConUbicadosDto(await _lector.ResumenAsync(categoria.Id, cancelacion), ubicados);
                },
                cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.CategoriaEnClub)
        {
            throw ErroresDeCategorias.CategoriaYaExiste(anio);
        }
    }

    /// <inheritdoc />
    public Task<CategoriaDto> DesactivarAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var categoria = await ExistenteAsync(categoriaId, cancelacion);
                if (!categoria.Activa)
                {
                    return await _lector.ResumenAsync(categoriaId, cancelacion);
                }

                if (await _categorias.ContarJugadoresAsync(categoriaId, cancelacion) > 0)
                {
                    throw ErroresDeCategorias.CategoriaConJugadores();
                }

                await _categorias.RetirarEntrenadoresAsync(categoriaId, cancelacion);
                await _categorias.CambiarActivaAsync(categoriaId, false, cancelacion);
                return await _lector.ResumenAsync(categoriaId, cancelacion);
            },
            cancelacion);

    /// <inheritdoc />
    public Task<CategoriaConUbicadosDto> ReactivarAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var categoria = await ExistenteAsync(categoriaId, cancelacion);
                var ubicados = 0;
                if (!categoria.Activa)
                {
                    await _categorias.CambiarActivaAsync(categoriaId, true, cancelacion);
                    ubicados = await _ubicador.RecogerAsync(categoria, cancelacion);
                }

                return new CategoriaConUbicadosDto(await _lector.ResumenAsync(categoriaId, cancelacion), ubicados);
            },
            cancelacion);

    /// <inheritdoc />
    public Task BorrarAsync(Guid categoriaId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                await ExistenteAsync(categoriaId, cancelacion);
                if (!await _categorias.BorrarSiNuncaSeUsoAsync(categoriaId, cancelacion))
                {
                    throw ErroresDeCategorias.CategoriaConHistorial();
                }
            },
            cancelacion);

    private async Task<Categoria> ExistenteAsync(Guid categoriaId, CancellationToken cancelacion) =>
        await _categorias.ObtenerAsync(categoriaId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();

    /// <summary>Un año de cuatro cifras no posterior al año en curso, que es el de la fecha UTC (RF-002).</summary>
    private int AnioValido(int? anio)
    {
        var anioEnCurso = _reloj.AhoraUtc.Year;
        var errores = new ErroresDeValidacion();
        if (anio is null)
        {
            errores.Agregar("anio", "Escribe el año de nacimiento de la categoría.");
        }
        else if (anio > anioEnCurso)
        {
            errores.Agregar("anio", $"El año no puede ser posterior a {anioEnCurso}.");
        }
        else if (!ReglaAnioDeCategoria.EsValido(anio.Value, anioEnCurso))
        {
            errores.Agregar("anio", "El año debe tener cuatro cifras, por ejemplo 2014.");
        }

        errores.LanzarSiHayErrores();
        return anio!.Value;
    }
}
