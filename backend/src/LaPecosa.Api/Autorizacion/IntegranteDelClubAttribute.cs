using LaPecosa.Api.Errores;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LaPecosa.Api.Autorizacion;

/// <summary>
/// Representa la autorización de los endpoints <c>/api/clubes/{clubId}/**</c> (research §3).
/// Su responsabilidad es comprobar en cada petición, contra la base de datos, que la cuenta
/// pertenece al club de la ruta, decidir cuál de sus integrantes en él hace la petición y
/// comprobar que el estado del club le permite entrar y que su ingreso ya fue aprobado; si pasa,
/// fija el club en <see cref="IContextoClub"/> y deja ese integrante disponible para el
/// controlador. Puede exigir además uno de varios roles.
/// Una cuenta puede tener varios integrantes en el club (hermanos): cuál hace la petición lo
/// decide <see cref="ReglaJugadorDeLaSesion"/> con la cabecera <see cref="CabeceraJugadorElegido"/>
/// y con la limitación de la sesión iniciada con un documento. Es el único lugar que lee esa
/// cabecera: los controladores y los servicios reciben el integrante ya resuelto, así que todo lo
/// que hacen queda atado al jugador elegido. La cabecera no concede nada: solo vale el
/// identificador de un integrante de la cuenta en este club, y cualquier otro, también uno mal
/// formado, responde el mismo 404. Si la cuenta tiene varios y no dice cuál, responde
/// <c>409 jugador_sin_elegir</c>.
/// No confía en el token para el club ni el rol, y no revela que un club existe a quien no
/// pertenece a él: responde el mismo 404 que para un club inexistente, también al DESARROLLADOR.
/// No abre ningún endpoint a quien está en la sala de espera ni a un jugador retirado del club: se
/// les niega por defecto, antes de mirar los roles, así que cualquier endpoint de club futuro queda
/// cubierto (research §4 de la 002 y §10 de la 003). Como se consulta la base de datos en cada
/// petición, el retiro se aplica también a una sesión ya abierta (RF-043).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IntegranteDelClubAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// Cabecera con la que una cuenta con varios jugadores en el club dice con cuál continúa.
    /// </summary>
    public const string CabeceraJugadorElegido = "X-Jugador-Elegido";

    private const string ClaveIntegrante = "LaPecosa.IntegranteDeLaPeticion";

    private readonly Rol[] _rolesExigidos;

    /// <summary>
    /// Exige ser integrante aprobado del club con uno de los roles indicados; sin roles, con
    /// cualquiera.
    /// </summary>
    public IntegranteDelClubAttribute(params Rol[] rolesExigidos)
    {
        _rolesExigidos = rolesExigidos;
    }

    /// <summary>Integrante de la cuenta que hace la petición en el club, ya comprobado.</summary>
    public static UsuarioRol IntegranteDe(HttpContext contexto) =>
        contexto.Items[ClaveIntegrante] as UsuarioRol
        ?? throw new InvalidOperationException("El endpoint no está protegido con [IntegranteDelClub].");

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var usuario = ValidadorSesion.UsuarioDe(http);
        if (usuario is null)
        {
            context.Result = Problema.SinSesion().ComoResultado();
            return;
        }

        if (!Guid.TryParse(http.Request.RouteValues["clubId"]?.ToString(), out var clubId))
        {
            context.Result = Problema.NoEncontrado().ComoResultado();
            return;
        }

        // Ausente o vacía es "sin elegido"; presente y mal formada, un jugador que no existe.
        var cabecera = http.Request.Headers[CabeceraJugadorElegido].ToString();
        Guid? jugadorElegido = null;
        if (!string.IsNullOrWhiteSpace(cabecera))
        {
            if (!Guid.TryParse(cabecera, out var elegido))
            {
                context.Result = Problema.NoEncontrado().ComoResultado();
                return;
            }

            jugadorElegido = elegido;
        }

        var integrantes = await http.RequestServices.GetRequiredService<IRepositorioPertenencias>()
            .ListarDeLaCuentaEnClubAsync(usuario.Id, clubId, http.RequestAborted);
        var deLaPeticion = ReglaJugadorDeLaSesion.Resolver(
            integrantes, jugadorElegido, ValidadorSesion.LimitacionDe(http));
        if (deLaPeticion.SinElegir)
        {
            context.Result = new Problema(
                409, "jugador_sin_elegir", "Elige con cuál jugador quieres continuar.").ComoResultado();
            return;
        }

        var integrante = deLaPeticion.Integrante;
        if (integrante?.Club is null)
        {
            context.Result = Problema.NoEncontrado().ComoResultado();
            return;
        }

        var acceso = ReglaAccesoPorEstado.Evaluar(
            integrante.Club.Estado, integrante.Rol, integrante.EstadoIngreso, integrante.Activo);
        var problema = acceso switch
        {
            ResultadoAcceso.ClubSuspendido => new Problema(
                403, "club_suspendido", "Hay una incidencia temporal. Comunícate con el presidente del club."),
            ResultadoAcceso.ClubDadoDeBaja => new Problema(403, "club_dado_de_baja", "Este club no está disponible."),
            ResultadoAcceso.IngresoEnEspera => new Problema(
                403, "ingreso_en_espera", "Tu ingreso está pendiente de aprobación."),
            ResultadoAcceso.IntegranteRetirado => new Problema(
                403, "integrante_retirado", "Ya no estás en este club."),
            _ when _rolesExigidos.Length > 0 && !_rolesExigidos.Contains(integrante.Rol) => new Problema(
                403, "rol_no_autorizado", "Tu rol en este club no permite hacer esto."),
            _ => null,
        };

        if (problema is not null)
        {
            context.Result = problema.ComoResultado();
            return;
        }

        http.RequestServices.GetRequiredService<IContextoClub>().Fijar(clubId);
        http.Items[ClaveIntegrante] = integrante;
    }
}
