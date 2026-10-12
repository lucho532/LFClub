using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de categorías, equipos, asignaciones y jugadores en los DTO del
/// apartado "Categorías" y de la tarjeta "Mi categoría".
/// Su responsabilidad es que ninguna entidad salga por la API (constitución §5), que cada lista
/// salga ordenada y que de un jugador o de un entrenador solo salga lo que cada quien puede ver:
/// en las listas el club nunca recibe documento, correo ni celular (RF-032), solo el PRESIDENTE y
/// los DIRECTIVOS reciben el estado de la documentación de cada jugador, y la familia recibe un tipo
/// distinto, solo con el identificador de cada entrenador para abrir su ficha básica (RF-035 de la
/// 003, RF-014 de la 008, §23).
/// No consulta la base de datos ni decide el alcance de quien pregunta. Solo muestra los equipos
/// activos y las asignaciones activas, aunque la categoría venga cargada con más.
/// </summary>
public static class MapperCategorias
{
    /// <summary>Categoría para la lista, con el número de jugadores que le corresponde.</summary>
    public static CategoriaDto ACategoria(Categoria categoria, int numeroJugadores) => new(
        categoria.Id,
        categoria.Anio,
        categoria.Activa,
        !categoria.Usada,
        numeroJugadores,
        Equipos(categoria),
        Entrenadores(categoria));

    /// <summary>
    /// Categoría con sus jugadores, que deben venir con sus equipos cargados.
    /// <paramref name="documentosEntregados"/> dice cuántos documentos tiene entregados cada
    /// jugador; con nulo, ningún jugador lleva el estado de su documentación.
    /// </summary>
    public static CategoriaDetalleDto ADetalle(
        Categoria categoria,
        IReadOnlyList<UsuarioRol> jugadores,
        IReadOnlyDictionary<Guid, int>? documentosEntregados) => new(
        categoria.Id,
        categoria.Anio,
        categoria.Activa,
        !categoria.Usada,
        jugadores.Count,
        Equipos(categoria),
        Entrenadores(categoria),
        jugadores.PorApellidos()
            .Select(jugador => AJugador(
                jugador,
                categoria,
                documentosEntregados is null ? null : DocumentosPendientes(documentosEntregados, jugador.Id)))
            .ToList());

    /// <summary>
    /// Jugador de una lista. Con categoría, lleva sus equipos activos y la marca de fuera de su
    /// año (RF-016); sin ella, es un jugador de "Sin categoría". <paramref name="documentosPendientes"/>
    /// es nulo para quien no puede ver los documentos.
    /// </summary>
    public static JugadorDeCategoriaDto AJugador(UsuarioRol jugador, Categoria? categoria, int? documentosPendientes) => new(
        jugador.Id,
        jugador.Nombres,
        jugador.Apellidos,
        jugador.FechaNacimiento.Year,
        categoria is not null && jugador.FechaNacimiento.Year != categoria.Anio,
        categoria is null ? [] : Referencias(categoria, jugador.Equipos.Select(fila => fila.EquipoId)),
        documentosPendientes);

    /// <summary>
    /// Cuántos documentos pedidos le faltan a ese jugador, dado cuántos tiene entregados cada uno.
    /// Se calcula al consultar; no se guarda ningún contador (CE-009 de la 005).
    /// </summary>
    public static int DocumentosPendientes(IReadOnlyDictionary<Guid, int> documentosEntregados, Guid usuarioRolId) =>
        Enum.GetValues<DocumentoPedido>().Length - documentosEntregados.GetValueOrDefault(usuarioRolId);

    /// <summary>Integrante que se puede asignar como entrenador.</summary>
    public static CandidatoEntrenadorDto ACandidato(UsuarioRol integrante) =>
        new(integrante.Id, integrante.Nombres, integrante.Apellidos, integrante.Rol);

    /// <summary>
    /// Jugador retirado. Quién lo retiró sale de lo copiado al retirar, nunca del nombre actual (§13);
    /// <paramref name="retiradoPorFicha"/> es el enlace ya resuelto a su ficha básica de hoy, o nulo.
    /// </summary>
    public static JugadorRetiradoDto ARetirado(
        UsuarioRol jugador, int documentosPendientes, EnlaceFichaDto? retiradoPorFicha) => new(
        jugador.Id,
        jugador.Nombres,
        jugador.Apellidos,
        jugador.FechaNacimiento.Year,
        jugador.RetiradoPorNombre ?? string.Empty,
        jugador.RetiradoEn ?? throw new InvalidOperationException("Un jugador retirado debe tener la fecha de su retiro."),
        documentosPendientes,
        retiradoPorFicha);

    /// <summary>
    /// Lo que ve la familia: la categoría de su jugador, los nombres de sus equipos y, de cada
    /// entrenador, solo su identificador, su nombre, sus apellidos, los equipos que dirige y la
    /// versión de su foto. Las asignaciones deben venir con la cuenta de cada entrenador.
    /// </summary>
    public static MiCategoriaDto AMiCategoria(UsuarioRol jugador, Categoria? categoria)
    {
        if (categoria is null)
        {
            return new MiCategoriaDto(null);
        }

        var versiones = categoria.Asignaciones
            .Where(asignacion => asignacion.Activa)
            .Select(asignacion => asignacion.UsuarioRol!)
            .DistinctBy(integrante => integrante.Id)
            .ToDictionary(integrante => integrante.Id, integrante => integrante.Usuario?.VersionFoto ?? 0);

        var entrenadores = Entrenadores(categoria)
            .Select(entrenador => new EntrenadorParaFamiliaDto(
                entrenador.UsuarioRolId,
                entrenador.Nombres,
                entrenador.Apellidos,
                entrenador.Equipos.Select(equipo => equipo.Nombre).ToList(),
                versiones.GetValueOrDefault(entrenador.UsuarioRolId)))
            .ToList();

        var equipos = Referencias(categoria, jugador.Equipos.Select(fila => fila.EquipoId))
            .Select(equipo => equipo.Nombre)
            .ToList();

        return new MiCategoriaDto(new CategoriaDeMiJugadorDto(categoria.Anio, equipos, entrenadores));
    }

    private static List<EquipoDto> Equipos(Categoria categoria) => categoria.Equipos
        .Where(equipo => equipo.Activo)
        .OrderBy(equipo => equipo.Nombre, OrdenDePersonas.Comparador)
        .Select(equipo => new EquipoDto(equipo.Id, equipo.Nombre, equipo.Jugadores.Count, !equipo.Usado))
        .ToList();

    private static List<EntrenadorDeCategoriaDto> Entrenadores(Categoria categoria) => categoria.Asignaciones
        .Where(asignacion => asignacion.Activa)
        .Select(asignacion => (
            Asignacion: asignacion,
            Integrante: asignacion.UsuarioRol
                ?? throw new InvalidOperationException("La asignación debe venir con su integrante cargado.")))
        .OrderBy(par => par.Integrante.Apellidos, OrdenDePersonas.Comparador)
        .ThenBy(par => par.Integrante.Nombres, OrdenDePersonas.Comparador)
        .Select(par => new EntrenadorDeCategoriaDto(
            par.Integrante.Id,
            par.Integrante.Nombres,
            par.Integrante.Apellidos,
            par.Integrante.Rol,
            Referencias(categoria, par.Asignacion.Equipos.Select(fila => fila.EquipoId))))
        .ToList();

    /// <summary>Los equipos activos de la categoría que están entre esos identificadores, por nombre.</summary>
    private static List<EquipoDeReferenciaDto> Referencias(Categoria categoria, IEnumerable<Guid> equipoIds)
    {
        var buscados = equipoIds.ToHashSet();
        return categoria.Equipos
            .Where(equipo => equipo.Activo && buscados.Contains(equipo.Id))
            .OrderBy(equipo => equipo.Nombre, OrdenDePersonas.Comparador)
            .Select(equipo => new EquipoDeReferenciaDto(equipo.Id, equipo.Nombre))
            .ToList();
    }
}
