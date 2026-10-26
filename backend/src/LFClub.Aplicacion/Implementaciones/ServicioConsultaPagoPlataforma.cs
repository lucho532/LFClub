using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio con el que el PRESIDENTE consulta el cobro de la plataforma a su club
/// (spec 017, RF-007 a RF-011).
/// Su responsabilidad es separar lo pendiente del historial, sumar lo pendiente, indicar si el pago en
/// línea está disponible y si el club está suspendido por impago, y marcar «en verificación» el cobro
/// pendiente que tuvo un intento de pago en los últimos 30 minutos, salvo que la pasarela ya haya dicho que
/// ese último intento no fue aprobado (entonces lo indica aparte). Añade el último pago y una estimación del próximo cobro. El detalle sale del cobro guardado,
/// nunca de un cálculo nuevo.
/// No cambia nada y no devuelve llaves, secretos ni datos personales.
/// </summary>
public class ServicioConsultaPagoPlataforma : IServicioConsultaPagoPlataforma
{
    private static readonly TimeSpan VentanaDeVerificacion = TimeSpan.FromMinutes(30);

    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IRepositorioConfiguracionPlataforma _configuracion;
    private readonly IRepositorioConteoDeIntegrantes _conteo;
    private readonly IPasarelaDePago _pasarela;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaPagoPlataforma(
        IRepositorioClubesPlataforma clubes,
        IRepositorioCobrosPlataforma cobros,
        IRepositorioConfiguracionPlataforma configuracion,
        IRepositorioConteoDeIntegrantes conteo,
        IPasarelaDePago pasarela,
        IReloj reloj)
    {
        _clubes = clubes;
        _cobros = cobros;
        _configuracion = configuracion;
        _conteo = conteo;
        _pasarela = pasarela;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PagoPlataformaDto> ObtenerAsync(Guid clubId, CancellationToken cancelacion = default)
    {
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var cobros = await _cobros.ListarDeClubAsync(clubId, cancelacion);
        var pagos = await _cobros.ListarPagosAsync(cobros.Select(cobro => cobro.Id), cancelacion);

        var pendientes = new List<CobroPlataformaDto>();
        foreach (var cobro in cobros.Where(cobro => cobro.Estado == EstadoCobroPlataforma.PENDIENTE).OrderBy(cobro => cobro.Periodo))
        {
            pendientes.Add(await ACobroAsync(cobro, null, cancelacion));
        }

        var historial = cobros
            .Where(cobro => cobro.Estado != EstadoCobroPlataforma.PENDIENTE)
            .Select(cobro => MapperCobroPlataforma.ACobro(cobro, pagos.GetValueOrDefault(cobro.Id)))
            .ToList();

        return new PagoPlataformaDto(
            _pasarela.Disponible,
            club.Estado == EstadoClub.SUSPENDIDO && club.MotivoSuspension == MotivoSuspension.IMPAGO,
            pendientes,
            pendientes.Sum(cobro => cobro.Total),
            historial,
            historial.Where(cobro => cobro.Estado == EstadoCobroPlataforma.PAGADO).MaxBy(cobro => cobro.PagadoEn),
            await EstimarProximoCobroAsync(club, cancelacion),
            MapperCobroPlataforma.AEstadoDePago(
                club, cobros.Where(cobro => cobro.Estado == EstadoCobroPlataforma.PENDIENTE).ToList(),
                ReglaMesDeCobro.DiaEnColombia(_reloj.AhoraUtc)).EstadoDePago);
    }

    /// <summary>
    /// El siguiente cobro: el del mes que viene o, si al club se le empieza a cobrar más adelante, el de ese
    /// mes. Nulo si no se le cobra, si está dado de baja o si con sus jugadores de hoy no le tocaría cobro.
    /// </summary>
    private async Task<ProximoCobroDto?> EstimarProximoCobroAsync(Club club, CancellationToken cancelacion)
    {
        if (club.MesInicioCobro is not { } inicio || club.Estado == EstadoClub.DADO_DE_BAJA)
        {
            return null;
        }

        var siguiente = ReglaMesDeCobro.MesEnCurso(_reloj.AhoraUtc).AddMonths(1);
        var periodo = inicio > siguiente ? inicio : siguiente;
        var configuracion = await _configuracion.ObtenerAsync(cancelacion);
        var calculo = ReglaCobroPlataforma.Calcular(await _conteo.ContarPorRolAsync(club.Id, cancelacion), configuracion.Rangos);

        return calculo is null
            ? null
            : new ProximoCobroDto(
                MapperCobroPlataforma.APeriodo(periodo),
                calculo.Conteo.Jugadores,
                new RangoTarifaDto(calculo.Rango.Desde, calculo.Rango.Hasta, calculo.Rango.Precio),
                calculo.Total);
    }

    /// <inheritdoc />
    public async Task<CobroPlataformaDto> ObtenerCobroAsync(
        Guid clubId, Guid cobroId, CancellationToken cancelacion = default)
    {
        var cobro = await _cobros.ObtenerDeClubAsync(clubId, cobroId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var pago = await _cobros.ObtenerPagoDeCobroAsync(cobro.Id, cancelacion);

        return await ACobroAsync(cobro, pago, cancelacion);
    }

    private async Task<CobroPlataformaDto> ACobroAsync(
        CobroPlataforma cobro, PagoPlataforma? pago, CancellationToken cancelacion)
    {
        if (cobro.Estado != EstadoCobroPlataforma.PENDIENTE)
        {
            return MapperCobroPlataforma.ACobro(cobro, pago);
        }

        var enVerificacion = await _cobros.HayIntentoRecienteAsync(
            cobro.Id, _reloj.AhoraUtc - VentanaDeVerificacion, cancelacion);
        var noAprobado = await _cobros.UltimoIntentoNoAprobadoAsync(cobro.Id, cancelacion);

        return MapperCobroPlataforma.ACobro(cobro, pago, enVerificacion, noAprobado);
    }
}
