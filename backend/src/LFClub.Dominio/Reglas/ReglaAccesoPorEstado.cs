using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla que decide quién entra a un club según el estado del club, el estado de
/// ingreso de la persona y si el club la retiró (constitución §7.4, §12.1.1 y §14.1; RF-027 y
/// RF-028 de la 001, RF-016 de la 002 y RF-043 de la 003).
/// Su responsabilidad es responder si un integrante puede entrar. Mira primero el estado del club:
/// en uno activo entran todos; en uno suspendido, solo su PRESIDENTE; en uno dado de baja, nadie.
/// Después mira el estado de ingreso: quien está en espera no entra. Y por último el retiro: un
/// jugador retirado no entra. Por ese orden, una cuenta en espera o retirada de un club suspendido
/// o dado de baja recibe el motivo del club.
/// No comprueba que la persona pertenezca al club ni conoce HTTP.
/// </summary>
public static class ReglaAccesoPorEstado
{
    /// <summary>
    /// Evalúa el acceso de un integrante con ese rol, ese estado de ingreso y esa marca de activo
    /// (falso significa retirado del club) a un club en ese estado.
    /// </summary>
    public static ResultadoAcceso Evaluar(EstadoClub estado, Rol rol, EstadoIngreso estadoIngreso, bool activo)
    {
        var porEstadoDelClub = estado switch
        {
            EstadoClub.ACTIVO => ResultadoAcceso.Permitido,
            EstadoClub.SUSPENDIDO when rol == Rol.PRESIDENTE => ResultadoAcceso.Permitido,
            EstadoClub.SUSPENDIDO => ResultadoAcceso.ClubSuspendido,
            _ => ResultadoAcceso.ClubDadoDeBaja,
        };

        if (porEstadoDelClub != ResultadoAcceso.Permitido)
        {
            return porEstadoDelClub;
        }

        if (estadoIngreso == EstadoIngreso.EN_ESPERA)
        {
            return ResultadoAcceso.IngresoEnEspera;
        }

        return activo ? ResultadoAcceso.Permitido : ResultadoAcceso.IntegranteRetirado;
    }
}
