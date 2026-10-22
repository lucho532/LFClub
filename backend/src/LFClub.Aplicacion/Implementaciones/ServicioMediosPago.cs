using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de los medios de pago del club.
/// Su responsabilidad es validar y guardar los medios que publica el PRESIDENTE, y listarlos: todos
/// para el PRESIDENTE y el DIRECTIVO, solo los activos para la cuenta de un jugador (RF-027). Lo que
/// no existe o es de otro club responde 404.
/// No toca los pagos ya hechos con esa entidad, no comprueba el rol de quien cambia y no accede al
/// contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioMediosPago : IServicioMediosPago
{
    private readonly IRepositorioMediosPago _medios;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoClub _contextoClub;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioMediosPago(
        IRepositorioMediosPago medios, IUnidadDeTrabajo unidadDeTrabajo, IContextoClub contextoClub, IReloj reloj)
    {
        _medios = medios;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contextoClub = contextoClub;
        _reloj = reloj;
    }

    /// <summary>Convierte un medio en su DTO.</summary>
    public static MedioPagoDto ADto(MedioPagoClub medio) =>
        new(medio.Id, medio.Entidad, medio.Titular, medio.Numero, medio.Nota, medio.Activo);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MedioPagoDto>> ListarAsync(
        UsuarioRol quienPregunta, CancellationToken cancelacion = default) =>
        (await _medios.ListarAsync(soloActivos: quienPregunta.Rol == Rol.JUGADOR, cancelacion)).Select(ADto).ToList();

    /// <inheritdoc />
    public async Task<MedioPagoDto> CrearAsync(GuardarMedioPagoDto datos, CancellationToken cancelacion = default)
    {
        var (entidad, titular, numero, nota) = ValidadorMedioPago.Validar(datos);
        var medio = new MedioPagoClub
        {
            ClubId = _contextoClub.ClubId ?? throw new InvalidOperationException("No hay club en la petición."),
            Entidad = entidad,
            Titular = titular,
            Numero = numero,
            Nota = nota,
            Activo = datos.Activo,
            CreadoEn = _reloj.AhoraUtc,
        };
        _medios.Agregar(medio);
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
        return ADto(medio);
    }

    /// <inheritdoc />
    public async Task<MedioPagoDto> EditarAsync(
        Guid medioId, GuardarMedioPagoDto datos, CancellationToken cancelacion = default)
    {
        var (entidad, titular, numero, nota) = ValidadorMedioPago.Validar(datos);
        var medio = await _medios.ObtenerAsync(medioId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        medio.Entidad = entidad;
        medio.Titular = titular;
        medio.Numero = numero;
        medio.Nota = nota;
        medio.Activo = datos.Activo;
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
        return ADto(medio);
    }

    /// <inheritdoc />
    public async Task QuitarAsync(Guid medioId, CancellationToken cancelacion = default)
    {
        var medio = await _medios.ObtenerAsync(medioId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        _medios.Quitar(medio);
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
    }
}
