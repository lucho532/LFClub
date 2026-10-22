using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que inicia el pago de un cobro de la plataforma (spec 017, RF-008 y RF-009).
/// Su responsabilidad es comprobar que el cobro es del club y sigue pendiente, que la pasarela está
/// disponible, guardar un <see cref="IntentoPagoPlataforma"/> con una referencia nueva y el valor del
/// cobro guardado (nunca uno que mande el cliente) y pedir a la pasarela la dirección de pago, con la
/// dirección de retorno a la pantalla que solo consulta.
/// No marca el cobro como pagado.
/// </summary>
public class ServicioPagoPlataforma : IServicioPagoPlataforma
{
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IPasarelaDePago _pasarela;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;
    private readonly OpcionesDeEnlaces _enlaces;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioPagoPlataforma(
        IRepositorioCobrosPlataforma cobros,
        IPasarelaDePago pasarela,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj,
        OpcionesDeEnlaces enlaces)
    {
        _cobros = cobros;
        _pasarela = pasarela;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
        _enlaces = enlaces;
    }

    /// <inheritdoc />
    public async Task<IntentoDePagoDto> IniciarAsync(
        Guid clubId, Guid cobroId, Guid usuarioId, CancellationToken cancelacion = default)
    {
        var cobro = await _cobros.ObtenerDeClubAsync(clubId, cobroId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        if (cobro.Estado != EstadoCobroPlataforma.PENDIENTE)
        {
            throw ExcepcionDeAplicacion.Conflicto("cobro_no_pendiente", "Este cobro ya no se puede pagar.");
        }

        var referencia = "LFC-" + Guid.CreateVersion7().ToString("N");
        var retorno = $"{_enlaces.UrlBaseFrontend}/club/{clubId}/pago-plataforma/retorno?cobro={cobroId}";
        var enlace = _pasarela.CrearEnlaceDePago(new SolicitudDePago(referencia, cobro.Total, retorno))
            ?? throw new ExcepcionDeAplicacion(
                "pago_no_disponible", 503, "El pago en línea no está disponible por ahora.");

        _cobros.AgregarIntento(new IntentoPagoPlataforma
        {
            CobroId = cobro.Id,
            Referencia = referencia,
            Valor = cobro.Total,
            CreadoPorUsuarioId = usuarioId,
            CreadoEn = _reloj.AhoraUtc,
        });
        await _unidadDeTrabajo.GuardarAsync(cancelacion);

        return new IntentoDePagoDto(enlace.ToString(), referencia);
    }
}
