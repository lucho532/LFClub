using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que arma el aviso de mora del inicio de la familia.
/// Su responsabilidad es leer al jugador del club de la petición, decidir con
/// <see cref="ReglaAccesoACuenta"/> si quien pregunta puede verlo (solo la cuenta del propio jugador,
/// y 404 si no, para no revelar que existe), leer sus cargos con lo aplicado por pagos confirmados y
/// calcular el estado con <see cref="ReglaMora"/> y el reloj. Cuenta además sus pagos en revisión.
/// No cambia datos, no restringe ningún acceso por deber (§16.7), no accede al contexto de Entity
/// Framework ni conoce HTTP.
/// </summary>
public class ServicioAvisoDeMora : IServicioAvisoDeMora
{
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioCargos _cargos;
    private readonly IRepositorioPagos _pagos;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAvisoDeMora(
        IRepositorioJugadores jugadores, IRepositorioCargos cargos, IRepositorioPagos pagos, IReloj reloj)
    {
        _jugadores = jugadores;
        _cargos = cargos;
        _pagos = pagos;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<AvisoDeMoraDto> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var jugador = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion);
        var esElMismo = jugador is not null && jugador.Id == quienPregunta.Id;
        if (quienPregunta.Rol != Rol.JUGADOR
            || jugador is not { Rol: Rol.JUGADOR, EstadoIngreso: EstadoIngreso.APROBADO }
            || !ReglaAccesoACuenta.PuedeVer(quienPregunta.Rol, esElMismo, jugador.Activo))
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var cargos = await _cargos.ListarConAplicadoAsync(jugador.Id, cancelacion);
        var pagos = await _pagos.ListarDelJugadorAsync(jugador.Id, cancelacion);
        var mora = ReglaMora.Calcular(
            cargos.Select(cargo => new ReglaMora.CargoParaMora(cargo.Tipo, cargo.Mes, cargo.Valor, cargo.Aplicado)),
            _reloj.AhoraUtc);

        return new AvisoDeMoraDto(
            mora.Estado,
            mora.ValorVencido,
            mora.MesesVencidos,
            mora.ValorEnPlazo,
            mora.VenceEl,
            pagos.Count(pago => pago.Estado == EstadoPago.EN_REVISION));
    }
}
