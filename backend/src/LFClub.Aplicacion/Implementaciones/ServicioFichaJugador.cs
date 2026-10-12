using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que guarda el formulario de la ficha de un jugador.
/// Su responsabilidad es resolver el acceso, validar y, con el club bloqueado, escribir el celular
/// y el responsable en la cuenta del jugador (RF-039) y los diez datos restantes en su ficha,
/// reemplazándolos todos: un texto vacío queda nulo. Si algún dato quedó distinto sella el último
/// cambio en esa misma transacción; si nada cambió, ni crea la fila ni mueve el sello.
/// No cambia la identidad, el documento, el correo ni los archivos; no decide quién puede cambiar
/// (eso es de la regla), no accede al contexto de Entity Framework y no conoce HTTP. No escribe
/// ningún dato de la ficha en el registro de la aplicación.
/// </summary>
public class ServicioFichaJugador : IServicioFichaJugador
{
    private readonly AccesoAFicha _acceso;
    private readonly IRepositorioClub _club;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioFichas _fichas;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaFicha _consulta;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioFichaJugador(
        AccesoAFicha acceso,
        IRepositorioClub club,
        IRepositorioUsuarios usuarios,
        IRepositorioFichas fichas,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaFicha consulta,
        IReloj reloj)
    {
        _acceso = acceso;
        _club = club;
        _usuarios = usuarios;
        _fichas = fichas;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<FichaJugadorDto> ActualizarAsync(
        Guid usuarioRolId, ActualizarFichaDto datos, UsuarioRol quienCambia, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienCambia, cancelacion);
        AccesoAFicha.ExigirCambio(alcance);

        var ahora = _reloj.AhoraUtc;
        ValidadorFicha.Validar(
            datos, ReglaMayoriaDeEdad.EsMenorDeEdad(jugador.FechaNacimiento, DateOnly.FromDateTime(ahora)));
        var nuevos = Limpios(datos);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                // Con el club bloqueado, dos guardados simultáneos van uno detrás de otro: queda el
                // último entero, sin mezclar datos de los dos (research §10).
                await _club.BloquearAsync(cancelacion);
                var cuenta = await _usuarios.ObtenerParaCambiarAsync(jugador.UsuarioId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                var actual = await _fichas.ObtenerAsync(usuarioRolId, cancelacion);

                if (cuenta.Celular == nuevos.Celular
                    && cuenta.NombreResponsable == nuevos.NombreResponsable
                    && DeLaFicha(actual) == DeLaFicha(nuevos))
                {
                    return;
                }

                cuenta.Celular = nuevos.Celular;
                cuenta.NombreResponsable = nuevos.NombreResponsable;
                Escribir(await _fichas.SellarCambioAsync(jugador, quienCambia, ahora, cancelacion), nuevos);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(usuarioRolId, quienCambia, cancelacion);
    }

    /// <summary>Los datos tal como se guardan: sin espacios sobrantes y con nulo en lugar de un texto vacío.</summary>
    private static ActualizarFichaDto Limpios(ActualizarFichaDto datos) => new(
        UnaLinea(datos.Celular),
        UnaLinea(datos.NombreResponsable),
        UnaLinea(datos.EmergenciaNombre),
        UnaLinea(datos.EmergenciaParentesco),
        UnaLinea(datos.EmergenciaCelular),
        UnaLinea(datos.EntidadSalud),
        UnaLinea(datos.LugarAtencion),
        datos.GrupoSanguineo,
        VariasLineas(datos.Alergias),
        VariasLineas(datos.Enfermedades),
        VariasLineas(datos.Medicamentos),
        VariasLineas(datos.Observaciones));

    private static string? UnaLinea(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : NormalizadorTexto.SinEspaciosSobrantes(texto);

    /// <summary>Los textos largos conservan sus saltos de línea; solo pierden los espacios de los extremos.</summary>
    private static string? VariasLineas(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static ActualizarFichaDto DeLaFicha(ActualizarFichaDto datos) =>
        datos with { Celular = null, NombreResponsable = null };

    /// <summary>Los diez datos de la ficha guardada, o todos vacíos si todavía no tiene fila.</summary>
    private static ActualizarFichaDto DeLaFicha(FichaJugador? ficha) => new(
        null,
        null,
        ficha?.EmergenciaNombre,
        ficha?.EmergenciaParentesco,
        ficha?.EmergenciaCelular,
        ficha?.EntidadSalud,
        ficha?.LugarAtencion,
        ficha?.GrupoSanguineo,
        ficha?.Alergias,
        ficha?.Enfermedades,
        ficha?.Medicamentos,
        ficha?.Observaciones);

    private static void Escribir(FichaJugador ficha, ActualizarFichaDto nuevos)
    {
        ficha.EmergenciaNombre = nuevos.EmergenciaNombre;
        ficha.EmergenciaParentesco = nuevos.EmergenciaParentesco;
        ficha.EmergenciaCelular = nuevos.EmergenciaCelular;
        ficha.EntidadSalud = nuevos.EntidadSalud;
        ficha.LugarAtencion = nuevos.LugarAtencion;
        ficha.GrupoSanguineo = nuevos.GrupoSanguineo;
        ficha.Alergias = nuevos.Alergias;
        ficha.Enfermedades = nuevos.Enfermedades;
        ficha.Medicamentos = nuevos.Medicamentos;
        ficha.Observaciones = nuevos.Observaciones;
    }
}
