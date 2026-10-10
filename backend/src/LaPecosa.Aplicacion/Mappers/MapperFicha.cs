using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de un jugador, su cuenta, su ficha y sus documentos en la ficha que
/// recibe quien la consulta.
/// Su responsabilidad es que ninguna entidad salga por la API (constitución §5) y que solo viaje
/// lo que el <see cref="AlcanceDeFicha"/> permite: sin datos clínicos para quien no puede verlos
/// (RF-010) y sin documentos para quien no puede verlos (RF-007). Esos grupos quedan nulos, y el
/// DTO los omite del JSON.
/// No consulta la base de datos ni decide el alcance de quien pregunta: lo recibe ya resuelto. No
/// entrega nunca el contenido de un archivo.
/// </summary>
public static class MapperFicha
{
    /// <summary>
    /// La ficha de ese jugador, que debe venir con su cuenta, su categoría y sus equipos cargados.
    /// <paramref name="ficha"/> es nulo si nadie la ha cambiado; <paramref name="entregados"/> solo
    /// se usa si el alcance permite ver los documentos; <paramref name="hoy"/> decide si es menor;
    /// <paramref name="fotoActualizadaEn"/> es la fecha de su foto, o nula si no tiene.
    /// </summary>
    public static FichaJugadorDto AFicha(
        UsuarioRol jugador,
        FichaJugador? ficha,
        IReadOnlyList<DocumentoEntregado> entregados,
        AlcanceDeFicha alcance,
        DateOnly hoy,
        DateTime? fotoActualizadaEn)
    {
        var cuenta = jugador.Usuario
            ?? throw new InvalidOperationException("El jugador debe venir con su cuenta cargada.");

        return new FichaJugadorDto(
            jugador.Id,
            jugador.Nombres,
            jugador.Apellidos,
            jugador.TipoDocumento,
            jugador.NumeroDocumento,
            jugador.FechaNacimiento,
            ReglaMayoriaDeEdad.EsMenorDeEdad(jugador.FechaNacimiento, hoy),
            !jugador.Activo,
            jugador.CategoriaId,
            jugador.Categoria?.Anio,
            Equipos(jugador),
            new ContactoDeFichaDto(cuenta.Correo, cuenta.Celular, cuenta.NombreResponsable),
            new ContactoEmergenciaDto(ficha?.EmergenciaNombre, ficha?.EmergenciaParentesco, ficha?.EmergenciaCelular),
            new SeguridadSocialDto(ficha?.EntidadSalud, ficha?.LugarAtencion),
            alcance.VeDatosClinicos ? DatosClinicos(ficha) : null,
            alcance.VeDocumentos ? Documentos(entregados) : null,
            ficha is null ? null : UltimoCambio(ficha, jugador),
            new PermisosDeFichaDto(alcance.Cambia, alcance.CorrigeIdentidad),
            fotoActualizadaEn);
    }

    private static List<string> Equipos(UsuarioRol jugador) => jugador.Equipos
        .Select(fila => fila.Equipo
            ?? throw new InvalidOperationException("Los equipos del jugador deben venir cargados."))
        .Where(equipo => equipo.Activo)
        .Select(equipo => equipo.Nombre)
        .Order(OrdenDePersonas.Comparador)
        .ToList();

    private static DatosClinicosDto DatosClinicos(FichaJugador? ficha) => new(
        ficha?.GrupoSanguineo, ficha?.Alergias, ficha?.Enfermedades, ficha?.Medicamentos, ficha?.Observaciones);

    /// <summary>Siempre los documentos pedidos, en su orden, cada uno entregado o pendiente.</summary>
    private static List<DocumentoDeFichaDto> Documentos(IReadOnlyList<DocumentoEntregado> entregados) =>
        Enum.GetValues<DocumentoPedido>()
            .Select(pedido => entregados.FirstOrDefault(entregado => entregado.Documento == pedido) is { } archivo
                ? new DocumentoDeFichaDto(pedido, true, archivo.SubidoEn, archivo.TipoContenido, archivo.TamanoBytes)
                : new DocumentoDeFichaDto(pedido, false, null, null, null))
            .ToList();

    /// <summary>
    /// El autor sale de lo copiado al cambiar, nunca del nombre actual (§13). Lo hizo la cuenta del
    /// jugador cuando la cuenta que cambió es la suya.
    /// </summary>
    private static UltimoCambioDto UltimoCambio(FichaJugador ficha, UsuarioRol jugador) => new(
        ficha.UltimoCambioEn, ficha.UltimoCambioPorNombre, ficha.UltimoCambioPorUsuarioId == jugador.UsuarioId);
}
