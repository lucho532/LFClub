using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que registra un pago recibido directamente por el PRESIDENTE.
/// Su responsabilidad es validar los datos, y en una sola transacción: bloquear al jugador, copiar su
/// nombre y su documento al pago (se conserva aunque su cuenta se elimine), confirmarlo con
/// <see cref="AplicadorDePagos"/> (reparto, recibo, concepto y saldo) y guardarlo. Dos registros a la vez
/// sobre el mismo jugador se ejecutan uno detrás de otro.
/// No comprueba el rol, no envía avisos y no accede al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioRegistroDePagos : IServicioRegistroDePagos
{
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioPagos _pagos;
    private readonly AplicadorDePagos _aplicador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRegistroDePagos(
        IRepositorioJugadores jugadores,
        IRepositorioPagos pagos,
        AplicadorDePagos aplicador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _jugadores = jugadores;
        _pagos = pagos;
        _aplicador = aplicador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PagoDto> RegistrarAsync(
        Guid usuarioRolId, RegistrarPagoDto datos, UsuarioRol quienRegistra, CancellationToken cancelacion = default)
    {
        var ahora = _reloj.AhoraUtc;
        var pagoValidado = ValidadorPago.ValidarRegistro(datos, HoraDeColombia.De(ahora).Hoy);

        return await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                if (!await _pagos.BloquearJugadorAsync(quienRegistra.ClubId, usuarioRolId, cancelacion))
                {
                    throw ExcepcionDeAplicacion.NoEncontrado();
                }

                var jugador = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion);
                if (jugador is not { Rol: Rol.JUGADOR, EstadoIngreso: EstadoIngreso.APROBADO })
                {
                    throw ExcepcionDeAplicacion.NoEncontrado();
                }

                var pago = new Pago
                {
                    ClubId = jugador.ClubId,
                    UsuarioRolId = jugador.Id,
                    JugadorNombre = $"{jugador.Nombres} {jugador.Apellidos}",
                    JugadorTipoDocumento = jugador.TipoDocumento.ToString(),
                    JugadorNumeroDocumento = jugador.NumeroDocumento,
                    Medio = pagoValidado.Medio,
                    Entidad = pagoValidado.Entidad,
                    Referencia = pagoValidado.Referencia,
                    Fecha = pagoValidado.Fecha,
                    CreadoEn = ahora,
                    CreadoPorUsuarioId = quienRegistra.UsuarioId,
                    CreadoPorNombre = $"{quienRegistra.Nombres} {quienRegistra.Apellidos}",
                };

                await _aplicador.ConfirmarAsync(pago, pagoValidado.Valor, pagoValidado.CargoIds, quienRegistra, ahora, cancelacion);
                _pagos.Agregar(pago);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                return MapperPagos.APago(pago, tieneSoporte: false);
            },
            cancelacion);
    }
}
