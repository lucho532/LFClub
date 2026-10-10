using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la foto propia de un jugador.
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAFicha"/> antes de leer o escribir
/// nada, sin regla propia: ver la foto exige poder ver la ficha, y cargarla o quitarla, poder
/// cambiarla. Al cargar, niega la foto a un jugador retirado (§14.2) y valida la imagen por su
/// contenido antes de abrir la transacción; con el club bloqueado, vuelve a comprobar que el
/// jugador sigue en el club, guarda o borra la foto y sella el último cambio de la ficha. Una
/// imagen rechazada no toca la que había.
/// No confía en la extensión ni en el tipo declarado, no toca la foto de perfil de la cuenta, no
/// accede al contexto de Entity Framework y no conoce HTTP.
/// </summary>
public class ServicioFotoJugador : IServicioFotoJugador
{
    private readonly AccesoAFicha _acceso;
    private readonly IRepositorioClub _club;
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioFotosJugador _fotos;
    private readonly IRepositorioFichas _fichas;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaFicha _consulta;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioFotoJugador(
        AccesoAFicha acceso,
        IRepositorioClub club,
        IRepositorioJugadores jugadores,
        IRepositorioFotosJugador fotos,
        IRepositorioFichas fichas,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaFicha consulta,
        IReloj reloj)
    {
        _acceso = acceso;
        _club = club;
        _jugadores = jugadores;
        _fotos = fotos;
        _fichas = fichas;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<(byte[] Contenido, string TipoContenido)> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        await _acceso.ResolverAsync(usuarioRolId, quienPregunta, cancelacion);
        var foto = await _fotos.ObtenerAsync(usuarioRolId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return (foto.Contenido, foto.TipoContenido);
    }

    /// <inheritdoc />
    public async Task<FichaJugadorDto> GuardarAsync(
        Guid usuarioRolId, byte[]? contenido, UsuarioRol quienCarga, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienCarga, cancelacion);
        AccesoAFicha.ExigirCambio(alcance);
        if (!jugador.Activo)
        {
            throw ErroresDeFicha.JugadorRetirado();
        }

        var (tipoContenido, motivo) = ValidadorImagen.Validar(contenido ?? []);
        if (tipoContenido is null)
        {
            throw motivo == MotivoRechazoImagen.DemasiadoGrande
                ? ErroresDeFicha.FotoDemasiadoGrande()
                : ErroresDeFicha.FotoNoEsImagen();
        }

        // La imagen ya está en memoria: el bloqueo del club no espera a la red.
        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);

                // Un retiro simultáneo borra la foto con el club bloqueado: no puede quedar guardada después.
                var actual = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion);
                if (actual is not { EsJugadorDelClub: true })
                {
                    throw ErroresDeFicha.JugadorRetirado();
                }

                var ahora = _reloj.AhoraUtc;
                await _fotos.GuardarAsync(jugador.Id, jugador.ClubId, contenido!, tipoContenido, ahora, cancelacion);
                await _fichas.SellarCambioAsync(jugador, quienCarga, ahora, cancelacion);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(usuarioRolId, quienCarga, cancelacion);
    }

    /// <inheritdoc />
    public async Task<FichaJugadorDto> QuitarAsync(
        Guid usuarioRolId, UsuarioRol quienQuita, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienQuita, cancelacion);
        AccesoAFicha.ExigirCambio(alcance);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                await _fotos.BorrarAsync(jugador.Id, cancelacion);
                await _fichas.SellarCambioAsync(jugador, quienQuita, _reloj.AhoraUtc, cancelacion);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(usuarioRolId, quienQuita, cancelacion);
    }
}
