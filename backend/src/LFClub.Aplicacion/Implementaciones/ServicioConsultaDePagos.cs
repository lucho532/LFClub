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
/// Representa el servicio que consulta un pago.
/// Su responsabilidad es leer el pago del club de la petición y dejarlo ver solo a quien
/// <see cref="ReglaAccesoACuenta"/> lo permite sobre el jugador del pago: el PRESIDENTE y el DIRECTIVO
/// siempre, la cuenta de un jugador solo si es la del pago. Un pago cuyo jugador ya no existe lo ven
/// solo el PRESIDENTE y el DIRECTIVO. Para cualquier otro responde 404. Dibuja a demanda el resguardo de
/// un pago confirmado o anulado con <see cref="IGeneradorImagenResguardo"/>, con los colores y el escudo
/// del club, sin guardarlo.
/// No cambia datos, no accede al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioConsultaDePagos : IServicioConsultaDePagos
{
    private readonly IRepositorioPagos _pagos;
    private readonly IRepositorioClub _club;
    private readonly IRepositorioEscudos _escudos;
    private readonly IGeneradorImagenResguardo _generador;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaDePagos(
        IRepositorioPagos pagos, IRepositorioClub club, IRepositorioEscudos escudos, IGeneradorImagenResguardo generador)
    {
        _pagos = pagos;
        _club = club;
        _escudos = escudos;
        _generador = generador;
    }

    /// <inheritdoc />
    public async Task<PagoDto> ObtenerAsync(Guid pagoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var pago = await _pagos.ObtenerSinSeguirAsync(pagoId, cancelacion);
        if (pago is null || !PuedeVerlo(pago, quienPregunta))
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var conSoporte = await _pagos.IdsConSoporteAsync([pago.Id], cancelacion);
        return MapperPagos.APago(pago, conSoporte.Contains(pago.Id));
    }

    /// <inheritdoc />
    public async Task<(byte[] Contenido, string TipoContenido)> AbrirSoporteAsync(
        Guid pagoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var pago = await _pagos.ObtenerSinSeguirAsync(pagoId, cancelacion);
        if (pago is null || !PuedeVerlo(pago, quienPregunta))
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var soporte = await _pagos.ObtenerSoporteAsync(pagoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return (soporte.Contenido, soporte.TipoContenido);
    }

    /// <inheritdoc />
    public async Task<byte[]> AbrirResguardoAsync(Guid pagoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var pago = await _pagos.ObtenerSinSeguirAsync(pagoId, cancelacion);
        if (pago is not { UsuarioRolId: not null, NumeroRecibo: not null, SaldoTrasPago: not null }
            || !ReglaTransicionPago.TieneRecibo(pago.Estado)
            || !PuedeVerlo(pago, quienPregunta))
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var club = await _club.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var escudo = await _escudos.ObtenerAsync(cancelacion);
        return _generador.Generar(new DatosImagenResguardo(
            club.Nombre,
            escudo?.Contenido,
            club.ColorPrincipal,
            club.ColorAcento,
            pago.NumeroRecibo.Value,
            pago.JugadorNombre,
            pago.Valor,
            pago.Medio,
            pago.Entidad,
            pago.Fecha,
            pago.Concepto,
            pago.ConfirmadoPorNombre ?? string.Empty,
            pago.SaldoTrasPago.Value,
            pago.Estado == EstadoPago.ANULADO));
    }

    private static bool PuedeVerlo(Pago pago, UsuarioRol quien) =>
        ReglaAccesoACuenta.PuedeVer(quien.Rol, pago.UsuarioRolId == quien.Id, jugadorActivo: true);
}
