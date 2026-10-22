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
/// Representa el servicio que aprueba un ingreso en espera.
/// Su responsabilidad es negar la aprobación que indique un rol distinto de JUGADOR y aprobar con
/// una única sentencia condicionada a que el ingreso siga en espera: si no cambia ninguna fila, el
/// ingreso ya estaba aprobado (409) o fue rechazado (404), y nunca hay un segundo efecto. La
/// persona queda siempre como JUGADOR (RF-016) y, en la misma transacción, en la categoría activa
/// de su año de nacimiento; si el club no la tiene, el ingreso se aprueba igual y queda sin
/// categoría. Si el documento del hermano lo tiene otra cuenta, aplica
/// <see cref="ReglaDocumentoCompartido"/> con lo que haya al aprobar: no aprueba si esa cuenta lo
/// tiene en un rol que no es JUGADOR (supuesto 7 de la 006) y, en la misma transacción, retira al
/// jugador activo de esa otra cuenta en su club (RF-036). Bloquea el club propio y los de los
/// jugadores que retira en una sola sentencia ordenada (research §13 de la 006).
/// No deja elegir rol ni comprueba quién aprueba: solo llega aquí el PRESIDENTE, que nunca está en
/// espera. Fija el mes desde el que se le cobra: el siguiente al de la aprobación (spec 015, RF-008).
/// No asigna equipo, no genera cobros ni envía correos, y no avisa al otro club. No accede
/// al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioAprobacionIngreso : IServicioAprobacionIngreso
{
    private readonly IRepositorioIngresos _ingresos;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioRetiroEntreClubes _retiroEntreClubes;
    private readonly UbicadorDeJugadores _ubicador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAprobacionIngreso(
        IRepositorioIngresos ingresos,
        IRepositorioPertenencias pertenencias,
        IRepositorioRetiroEntreClubes retiroEntreClubes,
        UbicadorDeJugadores ubicador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _ingresos = ingresos;
        _pertenencias = pertenencias;
        _retiroEntreClubes = retiroEntreClubes;
        _ubicador = ubicador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<IngresoAprobadoDto> AprobarAsync(
        Guid usuarioRolId, AprobarIngresoDto? datos, UsuarioRol quienAprueba, CancellationToken cancelacion = default)
    {
        // Sin cuerpo, sin rol o con JUGADOR se aprueba; cualquier otro rol se niega (RF-016).
        if (datos?.Rol is { } rolIndicado && rolIndicado != Rol.JUGADOR)
        {
            throw ErroresDeIngreso.RolNoAsignable();
        }

        return await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                // Se lee antes de bloquear para saber qué clubes bloquear (research §13 de la 006).
                var ingreso = await _ingresos.ObtenerAsync(usuarioRolId, cancelacion)
                    ?? throw ErroresDeIngreso.NoEncontrado();
                var conEseNumero = await _pertenencias.ListarPorDocumentosAsync([ingreso.NumeroDocumento], cancelacion);

                if (ingreso.EstadoIngreso == EstadoIngreso.EN_ESPERA
                    && ReglaDocumentoCompartido.Evaluar(conEseNumero, ingreso.UsuarioId) == DocumentoEnOtraCuenta.NoAdmitido)
                {
                    throw ErroresDeFicha.DocumentoEnOtraCuenta();
                }

                // Con el club bloqueado, aprobar y crear la categoría del año a la vez deja siempre
                // al jugador dentro de ella (research §4 de la 003). Los clubes donde se retira a
                // alguien se bloquean en la misma sentencia, en orden.
                var clubes = ReglaDocumentoCompartido.ARetirar(conEseNumero, ingreso.UsuarioId)
                    .Select(integrante => integrante.ClubId)
                    .Append(ingreso.ClubId)
                    .ToList();
                await _retiroEntreClubes.BloquearClubesAsync(clubes, cancelacion);

                // El nombre de quien aprueba se copia tal como es ahora (§13).
                var ahora = _reloj.AhoraUtc;
                var aprobado = await _ingresos.AprobarAsync(
                    usuarioRolId,
                    ahora,
                    quienAprueba.UsuarioId,
                    $"{quienAprueba.Nombres} {quienAprueba.Apellidos}",
                    ReglaMesDeCobro.PrimerMesDeCobro(ahora),
                    cancelacion);

                var integrante = await _ingresos.ObtenerAsync(usuarioRolId, cancelacion)
                    ?? throw ErroresDeIngreso.NoEncontrado();
                if (!aprobado)
                {
                    throw ErroresDeIngreso.YaAprobado();
                }

                await _ubicador.UbicarAUnoAsync(integrante, cancelacion);

                // Siempre, con lo que haya al aprobar: sin documento compartido no cambia nada (RF-036).
                await _retiroEntreClubes.RetirarDeOtrasCuentasAsync(
                    integrante.NumeroDocumento, integrante.UsuarioId, ahora, cancelacion);

                // Quien aprueba es el PRESIDENTE de esta petición: su ficha básica, sin consultar nada.
                return MapperIngresos.AIngresoAprobado(
                    integrante, ResolutorDeEnlaces.DeIntegrante(quienAprueba, ReglaEnlaceDeFicha.ParaAutor));
            },
            cancelacion);
    }
}
