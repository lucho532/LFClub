using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que crea, cambia, cancela y reactiva partidos (spec 009, RF-007, RF-008,
/// RF-014, RF-021 y RF-038).
/// Su responsabilidad es validar los datos, exigir categorías activas del club y que quien pregunta
/// gestione el partido con <see cref="ReglaGestionDeActividad"/>, marcar cada categoría como usada
/// (research §11) y permitir cada cambio solo desde el estado que admite
/// <see cref="ReglaTransicionPartido"/>: un partido jugado no se cancela ni cambia sus datos, y un
/// cancelado se reactiva mientras no haya empezado, con su convocatoria como estaba. Crear bloquea el
/// club, igual que desactivar una categoría. Cambiar, cancelar y reactivar viven en
/// <c>ServicioPartidos.Cambios.cs</c>: avisan a las familias en su misma transacción (spec 012).
/// Crear no avisa a nadie: un partido nace sin convocados (RF-003).
/// No toca la convocatoria ni el resultado, no decide quién lo ve, no borra nunca un partido y no
/// accede al contexto de Entity Framework.
/// </summary>
public partial class ServicioPartidos : IServicioPartidos
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioPartidos _partidos;
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioClub _club;
    private readonly IServicioConsultaPartidos _consulta;
    private readonly IServicioAvisosDeActividad _avisos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioPartidos(
        AccesoAActividad acceso,
        IRepositorioPartidos partidos,
        IRepositorioCategorias categorias,
        IRepositorioClub club,
        IServicioConsultaPartidos consulta,
        IServicioAvisosDeActividad avisos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _partidos = partidos;
        _categorias = categorias;
        _club = club;
        _consulta = consulta;
        _avisos = avisos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PartidoDto> CrearAsync(GuardarPartidoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var validos = ValidadorPartido.Validar(datos);

        var partidoId = await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var clubId = await _club.BloquearAsync(cancelacion);
                await ExigirActivasAsync(validos.CategoriaIds, cancelacion);
                var quien = await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion);
                if (!ReglaGestionDeActividad.PuedeGestionar(quien.Rol, validos.CategoriaIds, quien.CategoriasQueEntrena))
                {
                    throw ErroresDeActividades.NoGestionaActividad();
                }

                var partido = Con(validos, new Partido
                {
                    ClubId = clubId,
                    Estado = EstadoPartido.PROXIMO,
                    CreadoEn = _reloj.AhoraUtc,
                    CreadoPorUsuarioRolId = quienPregunta.Id,
                    Categorias = validos.CategoriaIds
                        .Select(categoriaId => new PartidoCategoria { CategoriaId = categoriaId, ClubId = clubId })
                        .ToList(),
                });

                foreach (var categoriaId in validos.CategoriaIds)
                {
                    await _categorias.MarcarUsadaAsync(categoriaId, cancelacion);
                }

                await _partidos.AgregarAsync(partido, cancelacion);
                return partido.Id;
            },
            cancelacion);

        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PartidoDto> CorregirRivalAsync(
        Guid partidoId, CorregirRivalDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var errores = new ErroresDeValidacion();
        var rival = CamposDeActividad.Limpio(datos.Rival);
        errores.Maximo("rival", rival, Partido.LongitudMaximaDeTexto, "El rival");
        errores.LanzarSiHayErrores();

        var partido = await GestionadoAsync(partidoId, quienPregunta, cancelacion);
        if (partido.Estado != EstadoPartido.JUGADO)
        {
            // Uno próximo se cambia con el formulario completo; uno cancelado, tras reactivarlo.
            throw partido.Estado == EstadoPartido.CANCELADO
                ? ErroresDeActividades.ActividadCancelada()
                : ErroresDeActividades.PartidoNoJugado();
        }

        await _partidos.CambiarRivalAsync(partidoId, rival, cancelacion);
        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }

    /// <summary>El 409 de un partido que no admite el cambio por su estado: cancelado o jugado.</summary>
    public static ExcepcionDeAplicacion ErrorDeEstado(EstadoPartido estado) => estado == EstadoPartido.JUGADO
        ? ErroresDeActividades.PartidoJugado()
        : ErroresDeActividades.ActividadCancelada();

    private static Partido Con(DatosDePartido datos, Partido partido)
    {
        partido.Rival = datos.Rival;
        partido.Fecha = datos.Fecha;
        partido.Hora = datos.Hora;
        partido.Lugar = datos.Lugar;
        partido.Condicion = datos.Condicion;
        partido.Tipo = datos.Tipo;
        partido.NombreTorneo = datos.NombreTorneo;
        return partido;
    }

    /// <summary>El partido, si quien pregunta lo ve (si no, 404) y lo gestiona (si no, 403).</summary>
    private async Task<Partido> GestionadoAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion)
    {
        var visible = await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion);
        AccesoAActividad.ExigirGestion(visible.Permisos);
        return visible.Partido;
    }

    private async Task ExigirActivasAsync(IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion)
    {
        if (await _categorias.ContarActivasAsync(categoriaIds, cancelacion) != categoriaIds.Count)
        {
            throw ErroresDeActividades.CategoriaInactiva();
        }
    }
}
