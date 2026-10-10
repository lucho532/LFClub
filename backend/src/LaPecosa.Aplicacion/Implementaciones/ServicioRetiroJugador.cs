using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que retira y reincorpora jugadores.
/// Su responsabilidad es decidir con <see cref="ReglaQuienRetira"/> si quien pide el retiro puede
/// hacerlo (el PRESIDENTE a cualquiera, la familia solo a su jugador; research §18 de la 006),
/// expresar el retiro con <c>Activo</c> (constitución §14.1), copiando en ese momento el nombre de
/// quien retira (§13), y sacar al jugador de su categoría y de sus equipos. Al reincorporar, niega
/// la reincorporación mientras el documento esté activo con otra cuenta en otro club (RF-041 de la
/// 006), vacía los datos del retiro (solo se guarda el estado actual, §18) y lo ubica con la misma
/// regla que a un ingreso recién aprobado. Todo ocurre con el club bloqueado, así que dos retiros
/// simultáneos retiran una sola vez; la reincorporación bloquea además, en orden, los clubes donde
/// está ese número, para que dos reincorporaciones a la vez no lo dejen activo en dos cuentas.
/// No accede al contexto de Entity Framework ni conoce HTTP. No impide el acceso del retirado: eso
/// lo hace la autorización en cada petición.
/// </summary>
public class ServicioRetiroJugador : IServicioRetiroJugador
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioRetiroEntreClubes _retiroEntreClubes;
    private readonly UbicadorDeJugadores _ubicador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRetiroJugador(
        IRepositorioClub club,
        IRepositorioJugadores jugadores,
        IRepositorioPertenencias pertenencias,
        IRepositorioRetiroEntreClubes retiroEntreClubes,
        UbicadorDeJugadores ubicador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _club = club;
        _jugadores = jugadores;
        _pertenencias = pertenencias;
        _retiroEntreClubes = retiroEntreClubes;
        _ubicador = ubicador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public Task RetirarAsync(Guid usuarioRolId, UsuarioRol quienRetira, CancellationToken cancelacion = default)
    {
        // Antes de leer nada: la respuesta no depende de si el jugador existe ni de su estado (§7.5).
        if (!ReglaQuienRetira.PuedeRetirar(quienRetira, usuarioRolId))
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        return _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var integrante = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();

                // Ya estaba retirado: no hay un segundo efecto ni cambia quién lo retiró (RF-047).
                if (EstaRetirado(integrante))
                {
                    return;
                }

                if (!integrante.EsJugadorDelClub)
                {
                    throw ErroresDeCategorias.NoEsJugador();
                }

                await _jugadores.SacarDeSusEquiposAsync(usuarioRolId, cancelacion);
                await _jugadores.RetirarAsync(
                    usuarioRolId,
                    _reloj.AhoraUtc,
                    quienRetira.UsuarioId,
                    $"{quienRetira.Nombres} {quienRetira.Apellidos}",
                    cancelacion);
            },
            cancelacion);
    }

    /// <inheritdoc />
    public Task<ReincorporacionDto> ReincorporarAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var integrante = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();

                // La primera lectura solo dice qué clubes bloquear; la decisión se toma releyendo con
                // todos bloqueados en orden (research §19 de la 006).
                var conEseNumero = await _pertenencias.ListarPorDocumentosAsync([integrante.NumeroDocumento], cancelacion);
                await _retiroEntreClubes.BloquearClubesAsync(
                    conEseNumero.Select(otro => otro.ClubId).Append(integrante.ClubId).ToList(), cancelacion);
                integrante = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                conEseNumero = await _pertenencias.ListarPorDocumentosAsync([integrante.NumeroDocumento], cancelacion);

                if (EstaRetirado(integrante)
                    && ReglaDocumentoCompartido.HayOtraCuentaActiva(conEseNumero, integrante.UsuarioId))
                {
                    throw ErroresDeCategorias.DocumentoActivoEnOtroClub();
                }

                if (!EstaRetirado(integrante) || !await _jugadores.ReincorporarAsync(usuarioRolId, cancelacion))
                {
                    throw ErroresDeCategorias.JugadorNoRetirado();
                }

                var categoria = await _ubicador.UbicarAUnoAsync(integrante, cancelacion);
                return new ReincorporacionDto(usuarioRolId, categoria?.Id, categoria?.Anio);
            },
            cancelacion);

    private static bool EstaRetirado(UsuarioRol integrante) => integrante.Rol == Rol.JUGADOR && !integrante.Activo;
}
