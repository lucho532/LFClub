using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla del número de documento que está en integrantes de cuentas distintas, en
/// clubes distintos (constitución §10 y §12.1.2; research §12, §15 y §19 de la 006).
/// Su responsabilidad es responder, a partir de los integrantes que tienen ese número en cualquier
/// club, cuatro preguntas: si se admite como hermano y con qué consecuencia, a quiénes retira
/// aprobarlo, qué cuenta abre un inicio de sesión con ese documento y si otra cuenta lo tiene
/// activo. En las cuatro, los integrantes de la misma cuenta no cuentan como "otra": son el mismo
/// niño inscrito en dos clubes.
/// No lee ni escribe datos, no conoce HTTP y no comprueba el propio club: el
/// <c>409 documento_repetido_en_club</c> lo decide el servicio antes de preguntarle.
/// </summary>
public static class ReglaDocumentoCompartido
{
    /// <summary>
    /// Si se admite el número como hermano de <paramref name="cuentaQueAgrega"/>: sin integrantes de
    /// otra cuenta, o solo jugadores retirados o en espera, <see cref="DocumentoEnOtraCuenta.SinBaja"/>;
    /// con alguno que no es JUGADOR, <see cref="DocumentoEnOtraCuenta.NoAdmitido"/>; con solo
    /// jugadores y alguno aprobado y activo, <see cref="DocumentoEnOtraCuenta.ConBaja"/>.
    /// </summary>
    public static DocumentoEnOtraCuenta Evaluar(IReadOnlyList<UsuarioRol> integrantesConEseNumero, Guid cuentaQueAgrega)
    {
        var deOtraCuenta = integrantesConEseNumero.Where(integrante => integrante.UsuarioId != cuentaQueAgrega).ToList();

        if (deOtraCuenta.Any(integrante => integrante.Rol != Rol.JUGADOR))
        {
            return DocumentoEnOtraCuenta.NoAdmitido;
        }

        return deOtraCuenta.Any(EstaActivo) ? DocumentoEnOtraCuenta.ConBaja : DocumentoEnOtraCuenta.SinBaja;
    }

    /// <summary>
    /// Los integrantes que quedan retirados al aprobar el número en <paramref name="cuentaQueQueda"/>:
    /// los jugadores aprobados y activos de otra cuenta, en cualquier club.
    /// </summary>
    public static IReadOnlyList<UsuarioRol> ARetirar(IReadOnlyList<UsuarioRol> integrantesConEseNumero, Guid cuentaQueQueda) =>
        integrantesConEseNumero
            .Where(integrante => integrante.UsuarioId != cuentaQueQueda && integrante.Rol == Rol.JUGADOR && EstaActivo(integrante))
            .ToList();

    /// <summary>
    /// La cuenta que abre un inicio de sesión con ese número: la del integrante aprobado y activo;
    /// si no hay, la del que está en espera; si tampoco, la del retirado. Entre dos del mismo nivel,
    /// la del más reciente (supuesto 8). Nulo si nadie tiene el número.
    /// </summary>
    public static Guid? CuentaQueAbre(IReadOnlyList<UsuarioRol> integrantesConEseNumero) =>
        integrantesConEseNumero
            .OrderBy(Nivel)
            .ThenByDescending(integrante => integrante.CreadoEn)
            .Select(integrante => (Guid?)integrante.UsuarioId)
            .FirstOrDefault();

    /// <summary>
    /// Si algún integrante de otra cuenta que no es <paramref name="cuenta"/> tiene el número
    /// aprobado y activo, con cualquier rol. En espera no cuenta (supuesto 12). Es lo que impide
    /// reincorporar a un retirado (RF-041).
    /// </summary>
    public static bool HayOtraCuentaActiva(IReadOnlyList<UsuarioRol> integrantesConEseNumero, Guid cuenta) =>
        integrantesConEseNumero.Any(integrante => integrante.UsuarioId != cuenta && EstaActivo(integrante));

    private static bool EstaActivo(UsuarioRol integrante) =>
        integrante.EstadoIngreso == EstadoIngreso.APROBADO && integrante.Activo;

    /// <summary>0 activo, 1 en espera, 2 retirado: el orden de <see cref="CuentaQueAbre"/>.</summary>
    private static int Nivel(UsuarioRol integrante) =>
        EstaActivo(integrante) ? 0 : integrante.EstadoIngreso == EstadoIngreso.EN_ESPERA ? 1 : 2;
}
