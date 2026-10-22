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
/// Representa el servicio de las transferencias informadas por la familia.
/// Su responsabilidad es, al informar: validar los datos y la imagen, comprobar que el valor no supere
/// lo pendiente y guardar el pago en revisión con su soporte, sin aplicaciones, sin recibo y sin cambiar
/// ningún saldo; al confirmar: bloquear al jugador, aplicar el pago con <see cref="AplicadorDePagos"/>
/// (con el valor corregido y los cargos que el PRESIDENTE decida) y guardar lo que informó la familia si
/// el valor cambió; al rechazar: guardar quién, cuándo y por qué. Las transiciones las decide
/// <see cref="ReglaTransicionPago"/>.
/// No comprueba el rol de quien confirma o rechaza, no accede al contexto de Entity Framework ni
/// conoce HTTP.
/// </summary>
public class ServicioRevisionDePagos : IServicioRevisionDePagos
{
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioPagos _pagos;
    private readonly IRepositorioCargos _cargos;
    private readonly AplicadorDePagos _aplicador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRevisionDePagos(
        IRepositorioJugadores jugadores,
        IRepositorioPagos pagos,
        IRepositorioCargos cargos,
        AplicadorDePagos aplicador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _jugadores = jugadores;
        _pagos = pagos;
        _cargos = cargos;
        _aplicador = aplicador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PagoDto> InformarAsync(
        Guid usuarioRolId,
        InformarPagoDto datos,
        byte[]? soporte,
        UsuarioRol quienInforma,
        CancellationToken cancelacion = default)
    {
        // Solo sobre el propio jugador: cualquier otro responde lo mismo que uno que no existe.
        var jugador = usuarioRolId == quienInforma.Id ? await _jugadores.ObtenerAsync(usuarioRolId, cancelacion) : null;
        if (jugador is not { Rol: Rol.JUGADOR, EstadoIngreso: EstadoIngreso.APROBADO, Activo: true })
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var ahora = _reloj.AhoraUtc;
        var informado = ValidadorPago.ValidarInformado(datos, HoraDeColombia.De(ahora).Hoy);
        var (tipoContenido, rechazo) = ValidadorSoportePago.Validar(soporte ?? []);
        if (rechazo is not null)
        {
            throw ErroresDePagos.Soporte(rechazo);
        }

        var pendiente = (await _cargos.ListarConAplicadoAsync(jugador.Id, cancelacion))
            .Sum(cargo => ReglaEstadoDeCuenta.Pendiente(cargo.Valor, cargo.Aplicado));
        if (pendiente == 0m)
        {
            throw ErroresDePagos.SinDeuda();
        }

        if (informado.Valor > pendiente)
        {
            throw ErroresDePagos.PagoExcedeDeuda(pendiente);
        }

        var pago = new Pago
        {
            ClubId = jugador.ClubId,
            UsuarioRolId = jugador.Id,
            JugadorNombre = $"{jugador.Nombres} {jugador.Apellidos}",
            JugadorTipoDocumento = jugador.TipoDocumento.ToString(),
            JugadorNumeroDocumento = jugador.NumeroDocumento,
            Medio = MedioPago.TRANSFERENCIA,
            Entidad = informado.Entidad,
            Referencia = informado.Referencia,
            Fecha = informado.Fecha,
            Valor = informado.Valor,
            Estado = EstadoPago.EN_REVISION,
            CreadoEn = ahora,
            CreadoPorUsuarioId = quienInforma.UsuarioId,
            CreadoPorNombre = $"{quienInforma.Nombres} {quienInforma.Apellidos}",
        };
        _pagos.Agregar(pago);
        _pagos.AgregarSoporte(new SoportePago
        {
            PagoId = pago.Id,
            ClubId = pago.ClubId,
            Contenido = soporte!,
            TipoContenido = tipoContenido!,
            GuardadaEn = ahora,
        });
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
        return MapperPagos.APago(pago, tieneSoporte: true);
    }

    /// <inheritdoc />
    public async Task<PagosEnRevisionDto> ListarEnRevisionAsync(CancellationToken cancelacion = default)
    {
        var pagos = await _pagos.ListarEnRevisionAsync(cancelacion);
        var pendientesPorJugador = (await _cargos.ListarConAplicadoAsync(cancelacion: cancelacion))
            .Where(cargo => ReglaEstadoDeCuenta.Pendiente(cargo.Valor, cargo.Aplicado) > 0m)
            .OrderBy(cargo => cargo.Mes)
            .ToLookup(cargo => cargo.UsuarioRolId);
        var conSoporte = await _pagos.IdsConSoporteAsync(pagos.Select(pago => pago.Id).ToList(), cancelacion);

        return new PagosEnRevisionDto(
            pagos.Count,
            pagos.Select(pago => new PagoEnRevisionDto(
                MapperPagos.APago(pago, conSoporte.Contains(pago.Id)),
                pago.JugadorNombre,
                pago.UsuarioRolId is { } jugadorId
                    ? pendientesPorJugador[jugadorId].Select(MapperPagos.ACargo).ToList()
                    : [])).ToList());
    }

    /// <inheritdoc />
    public async Task<PagoDto> ConfirmarAsync(
        Guid pagoId, ConfirmarPagoDto? datos, UsuarioRol quienConfirma, CancellationToken cancelacion = default)
    {
        ValidadorPago.ValidarCorreccion(datos?.Valor);
        var ahora = _reloj.AhoraUtc;

        return await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var pago = await ObtenerEnRevisionBloqueandoAlJugadorAsync(pagoId, quienConfirma, cancelacion);
                if (!ReglaTransicionPago.PuedeConfirmar(pago.Estado))
                {
                    throw ErroresDePagos.PagoNoEstaEnRevision();
                }

                var informado = pago.Valor;
                var valor = datos?.Valor ?? informado;
                await _aplicador.ConfirmarAsync(
                    pago, valor, datos?.CargoIds is { Count: > 0 } ids ? ids.Distinct().ToList() : null, quienConfirma, ahora, cancelacion);
                if (valor != informado)
                {
                    pago.ValorInformado = informado;
                }

                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                return MapperPagos.APago(pago, tieneSoporte: true);
            },
            cancelacion);
    }

    /// <inheritdoc />
    public async Task<PagoDto> RechazarAsync(
        Guid pagoId, MotivoDto datos, UsuarioRol quienRechaza, CancellationToken cancelacion = default)
    {
        var motivo = ValidadorPago.ValidarMotivo(datos);
        var ahora = _reloj.AhoraUtc;

        return await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var pago = await ObtenerEnRevisionBloqueandoAlJugadorAsync(pagoId, quienRechaza, cancelacion);
                if (!ReglaTransicionPago.PuedeRechazar(pago.Estado))
                {
                    throw ErroresDePagos.PagoNoEstaEnRevision();
                }

                pago.Estado = EstadoPago.RECHAZADO;
                pago.RechazadoEn = ahora;
                pago.RechazadoPorUsuarioId = quienRechaza.UsuarioId;
                pago.RechazadoPorNombre = $"{quienRechaza.Nombres} {quienRechaza.Apellidos}";
                pago.MotivoRechazo = motivo;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                return MapperPagos.APago(pago, tieneSoporte: true);
            },
            cancelacion);
    }

    /// <summary>
    /// Lee el pago, bloquea a su jugador y lo vuelve a leer ya con el bloqueo, para decidir sobre su estado
    /// real: dos personas que confirman o rechazan a la vez lo hacen una detrás de otra.
    /// </summary>
    private async Task<Pago> ObtenerEnRevisionBloqueandoAlJugadorAsync(
        Guid pagoId, UsuarioRol quien, CancellationToken cancelacion)
    {
        var previo = await _pagos.ObtenerSinSeguirAsync(pagoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        if (previo.UsuarioRolId is not { } jugadorId
            || !await _pagos.BloquearJugadorAsync(quien.ClubId, jugadorId, cancelacion))
        {
            // Un pago sin jugador es histórico: ya no se revisa.
            throw ErroresDePagos.PagoNoEstaEnRevision();
        }

        return await _pagos.ObtenerAsync(pagoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
    }
}
