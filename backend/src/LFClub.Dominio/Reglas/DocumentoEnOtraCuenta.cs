namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa lo que decide <see cref="ReglaDocumentoCompartido"/> sobre el documento de un hermano
/// que también tienen integrantes de otra cuenta (research §12 de la 006).
/// Su responsabilidad es distinguir los tres casos: se admite sin consecuencias, se admite y
/// aprobarlo retira a un jugador en otro club, o no se admite.
/// No lleva el club ni la cuenta de esos integrantes: nada de ellos sale hacia quien agrega o
/// aprueba (RF-038).
/// </summary>
public enum DocumentoEnOtraCuenta
{
    /// <summary>
    /// Ninguna otra cuenta lo tiene, o solo como jugador retirado o en espera: se admite y aprobarlo
    /// no retira a nadie.
    /// </summary>
    SinBaja,

    /// <summary>
    /// Otra cuenta lo tiene solo en jugadores y alguno está aprobado y activo: se admite, y al
    /// aprobarlo ese jugador queda retirado de su club (RF-034).
    /// </summary>
    ConBaja,

    /// <summary>
    /// Otra cuenta lo tiene en un PRESIDENTE, un DIRECTIVO o un ENTRENADOR, en cualquier club: no se
    /// admite (RF-039).
    /// </summary>
    NoAdmitido,
}
