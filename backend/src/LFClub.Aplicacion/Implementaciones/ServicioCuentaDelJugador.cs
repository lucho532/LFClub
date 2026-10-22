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
/// Representa el servicio que arma el estado de cuenta de un jugador.
/// Su responsabilidad es leer al jugador del club de la petición, decidir con
/// <see cref="ReglaAccesoACuenta"/> si quien pregunta puede verlo (y responder 404 si no, para no
/// revelar que existe), leer sus cargos con lo aplicado por pagos confirmados y sus pagos de cualquier
/// estado, y calcular su saldo y su estado con <see cref="ReglaEstadoDeCuenta"/>. Dice además si quien
/// pregunta puede registrar un pago (solo el PRESIDENTE) o informar una transferencia (solo la cuenta
/// del propio jugador, mientras no esté retirado). Incluye los medios de pago activos del club.
/// No cambia datos, no restringe ningún acceso por deber (§16.8), no accede al contexto de Entity
/// Framework ni conoce HTTP.
/// </summary>
public class ServicioCuentaDelJugador : IServicioCuentaDelJugador
{
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioCargos _cargos;
    private readonly IRepositorioPagos _pagos;
    private readonly IRepositorioMediosPago _medios;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCuentaDelJugador(
        IRepositorioJugadores jugadores,
        IRepositorioCargos cargos,
        IRepositorioPagos pagos,
        IRepositorioMediosPago medios)
    {
        _jugadores = jugadores;
        _cargos = cargos;
        _pagos = pagos;
        _medios = medios;
    }

    /// <inheritdoc />
    public async Task<CuentaJugadorDto> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var jugador = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion);
        var esElMismo = jugador is not null && jugador.Id == quienPregunta.Id;
        if (jugador is not { Rol: Rol.JUGADOR, EstadoIngreso: EstadoIngreso.APROBADO }
            || !ReglaAccesoACuenta.PuedeVer(quienPregunta.Rol, esElMismo, jugador.Activo))
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var cargos = await _cargos.ListarConAplicadoAsync(jugador.Id, cancelacion);
        var pagos = await _pagos.ListarDelJugadorAsync(jugador.Id, cancelacion);
        var conSoporte = await _pagos.IdsConSoporteAsync(pagos.Select(pago => pago.Id).ToList(), cancelacion);
        var medios = await _medios.ListarAsync(soloActivos: true, cancelacion);
        var cuenta = MapperPagos.ACuenta(cargos);

        return new CuentaJugadorDto(
            jugador.Id,
            $"{jugador.Nombres} {jugador.Apellidos}",
            cuenta.Estado,
            cuenta.Saldo,
            cargos.Select(MapperPagos.ACargo).ToList(),
            pagos.Select(pago => MapperPagos.APago(pago, conSoporte.Contains(pago.Id))).ToList(),
            medios.Select(ServicioMediosPago.ADto).ToList(),
            ReglaAccesoACuenta.PuedeOperar(quienPregunta.Rol),
            jugador.Activo && ReglaAccesoACuenta.PuedeInformar(quienPregunta.Rol, esElMismo));
    }
}
