using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que publica y retira comunicados del club (spec 011, RF-001 a RF-011 y
/// RF-024 a RF-027).
/// Su responsabilidad es validar los datos, devolver el comunicado ya publicado si la clave se
/// repite (idempotencia, research §6), comprobar la audiencia con
/// <see cref="ReglaAudienciaDeComunicado"/> contra las categorías y equipos activos del club y las
/// categorías que entrena quien publica, resolver los jugadores activos que lo reciben, guardar el
/// comunicado con sus destinos y destinatarios en una sola unidad de trabajo copiando los nombres
/// (§13), y retirarlo con <see cref="ReglaRetiroDeComunicado"/>.
/// No envía correos ni ningún aviso fuera de la aplicación (RF-028), no edita ni borra comunicados,
/// no crea destinatarios después de publicar y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioPublicacionComunicados : IServicioPublicacionComunicados
{
    private readonly IRepositorioComunicados _comunicados;
    private readonly IRepositorioAsignaciones _asignaciones;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioPublicacionComunicados(
        IRepositorioComunicados comunicados,
        IRepositorioAsignaciones asignaciones,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _comunicados = comunicados;
        _asignaciones = asignaciones;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<ResultadoDePublicacion> PublicarAsync(
        PublicarComunicadoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        if (quienPregunta.Rol is not (Rol.PRESIDENTE or Rol.DIRECTIVO or Rol.ENTRENADOR))
        {
            throw ErroresDeComunicados.RolNoAutorizado();
        }

        var validos = ValidadorPublicarComunicado.Validar(datos);
        if (await _comunicados.BuscarPorClaveAsync(validos.Clave, cancelacion) is { } existente)
        {
            return Existente(existente, quienPregunta, await ContarAsync(existente.Id, cancelacion));
        }

        var (categorias, equipos) = await ComprobarAudienciaAsync(validos, quienPregunta, cancelacion);
        var destinatarios = await _comunicados.ResolverDestinatariosAsync(
            validos.Alcance, validos.CategoriaIds, validos.EquipoIds, cancelacion);
        if (destinatarios.Count == 0)
        {
            throw ErroresDeComunicados.SinDestinatarios();
        }

        var comunicado = Nuevo(validos, quienPregunta, categorias, equipos, destinatarios);
        try
        {
            await _unidadDeTrabajo.EnTransaccionAsync(
                () => _comunicados.AgregarAsync(comunicado, cancelacion), cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.ClaveDeComunicado)
        {
            // Dos peticiones con la misma clave a la vez: la que pierde relee y devuelve la ganadora.
            var ganador = await _comunicados.BuscarPorClaveAsync(validos.Clave, cancelacion);
            if (ganador is null)
            {
                throw;
            }

            return Existente(ganador, quienPregunta, await ContarAsync(ganador.Id, cancelacion));
        }

        var familias = destinatarios.Select(fila => fila.UsuarioId).Distinct().Count();
        return new ResultadoDePublicacion(
            MapperComunicados.APublicado(comunicado, familias, quienPregunta.Rol, quienPregunta.Id), true);
    }

    /// <inheritdoc />
    public async Task<ComunicadoPublicadoDto> RetirarAsync(
        Guid comunicadoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var comunicado = await _comunicados.ObtenerAsync(comunicadoId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        var resultado = ReglaRetiroDeComunicado.Evaluar(
            quienPregunta.Rol, quienPregunta.Id, comunicado.PublicadoPorUsuarioRolId, comunicado.RetiradoEn is not null);
        switch (resultado)
        {
            case ResultadoRetiroDeComunicado.NoLoVe:
                throw ExcepcionDeAplicacion.NoEncontrado();
            case ResultadoRetiroDeComunicado.NoRetira:
                throw ErroresDeComunicados.NoRetiraComunicado();
            case ResultadoRetiroDeComunicado.YaRetirado:
                throw ErroresDeComunicados.ComunicadoRetirado();
        }

        await _comunicados.RetirarAsync(
            comunicadoId, quienPregunta.Id, Nombre(quienPregunta), _reloj.AhoraUtc, cancelacion);

        var retirado = await _comunicados.ObtenerAsync(comunicadoId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return MapperComunicados.APublicado(
            retirado, await ContarAsync(comunicadoId, cancelacion), quienPregunta.Rol, quienPregunta.Id);
    }

    private ResultadoDePublicacion Existente(Comunicado existente, UsuarioRol quienPregunta, int familias)
    {
        if (existente.PublicadoPorUsuarioRolId != quienPregunta.Id)
        {
            throw ErroresDeComunicados.ClaveEnUso();
        }

        return new ResultadoDePublicacion(
            MapperComunicados.APublicado(existente, familias, quienPregunta.Rol, quienPregunta.Id), false);
    }

    private async Task<int> ContarAsync(Guid comunicadoId, CancellationToken cancelacion) =>
        (await _comunicados.ContarFamiliasAsync([comunicadoId], cancelacion)).GetValueOrDefault(comunicadoId);

    /// <summary>
    /// Comprueba que cada categoría y cada equipo pedido exista, esté activo y sea del club, y que
    /// quien publica pueda dirigirse a la audiencia. Devuelve lo cargado, para copiar los nombres.
    /// </summary>
    private async Task<(IReadOnlyList<CategoriaDeAudiencia> Categorias, IReadOnlyList<EquipoDeAudiencia> Equipos)>
        ComprobarAudienciaAsync(DatosDeComunicado datos, UsuarioRol quienPregunta, CancellationToken cancelacion)
    {
        var categorias = datos.CategoriaIds.Count == 0
            ? []
            : await _comunicados.CategoriasActivasAsync(datos.CategoriaIds, cancelacion);
        var equipos = datos.EquipoIds.Count == 0
            ? []
            : await _comunicados.EquiposActivosAsync(datos.EquipoIds, cancelacion);
        if (categorias.Count != datos.CategoriaIds.Count || equipos.Count != datos.EquipoIds.Count)
        {
            throw ErroresDeComunicados.AudienciaInvalida();
        }

        var entrena = quienPregunta.Rol == Rol.ENTRENADOR
            ? await _asignaciones.CategoriasDeAsync(quienPregunta.Id, cancelacion)
            : [];
        var resultado = ReglaAudienciaDeComunicado.Validar(
            quienPregunta.Rol,
            datos.Alcance,
            categorias.Select(categoria => categoria.CategoriaId).ToList(),
            entrena,
            equipos.Select(equipo => equipo.CategoriaId).ToList());

        return resultado switch
        {
            ResultadoAudiencia.Permitida => (categorias, equipos),
            ResultadoAudiencia.FueraDeSuAlcance => throw ErroresDeComunicados.NoEntrenaCategoria(),
            ResultadoAudiencia.RolNoPublica => throw ErroresDeComunicados.RolNoAutorizado(),
            _ => throw ErroresDeComunicados.AudienciaInvalida(),
        };
    }

    private Comunicado Nuevo(
        DatosDeComunicado datos,
        UsuarioRol autor,
        IReadOnlyList<CategoriaDeAudiencia> categorias,
        IReadOnlyList<EquipoDeAudiencia> equipos,
        IReadOnlyList<DestinatarioDeComunicado> destinatarios)
    {
        var comunicado = new Comunicado
        {
            ClubId = autor.ClubId,
            Titulo = datos.Titulo,
            Texto = datos.Texto,
            Alcance = datos.Alcance,
            Clave = datos.Clave,
            PublicadoEn = _reloj.AhoraUtc,
            PublicadoPorUsuarioRolId = autor.Id,
            PublicadoPorNombre = Nombre(autor),
            PublicadoPorRol = autor.Rol,
        };

        // Con CATEGORIAS, una fila por categoría; con EQUIPOS, una por equipo con el nombre de su
        // categoría; con CLUB, ninguna.
        if (datos.Alcance == AlcanceComunicado.CATEGORIAS)
        {
            comunicado.Destinos.AddRange(categorias.Select(categoria => new ComunicadoDestino
            {
                ClubId = autor.ClubId, CategoriaId = categoria.CategoriaId, CategoriaNombre = categoria.Anio.ToString(),
            }));
        }
        else if (datos.Alcance == AlcanceComunicado.EQUIPOS)
        {
            comunicado.Destinos.AddRange(equipos.Select(equipo => new ComunicadoDestino
            {
                ClubId = autor.ClubId,
                CategoriaId = equipo.CategoriaId,
                CategoriaNombre = equipo.Anio.ToString(),
                EquipoId = equipo.EquipoId,
                EquipoNombre = equipo.Nombre,
            }));
        }

        comunicado.Destinatarios.AddRange(destinatarios.Select(destinatario => new ComunicadoDestinatario
        {
            ClubId = autor.ClubId, UsuarioRolId = destinatario.UsuarioRolId, UsuarioId = destinatario.UsuarioId,
        }));
        return comunicado;
    }

    private static string Nombre(UsuarioRol integrante) => $"{integrante.Nombres} {integrante.Apellidos}";
}
