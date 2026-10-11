using LaPecosa.Aplicacion.DTOs;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso de los datos propios de "Mi perfil" (spec 008, historia 4;
/// constitución §12.6 y §7.1 desde la 4.5.1).
/// Su responsabilidad es que un PRESIDENTE, DIRECTIVO o ENTRENADOR vea sus datos personales y
/// cambie nombres, apellidos, fecha de nacimiento y celular, con las reglas del registro y en todos
/// sus clubes a la vez.
/// Trabaja solo sobre la cuenta con sesión. No cambia documento ni correo, no toca los integrantes
/// JUGADOR de la cuenta ni los nombres ya copiados en registros históricos (§13), y no devuelve
/// datos de ningún club.
/// </summary>
public interface IServicioDatosPropios
{
    /// <summary>
    /// Los datos de la cuenta, tomados de su integrante PRESIDENTE, DIRECTIVO o ENTRENADOR más
    /// antiguo. Una cuenta sin ninguno de esos roles recibe 404 <c>no_encontrado</c> (RF-017).
    /// </summary>
    Task<DatosPropiosDto> ObtenerAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// Cambia los cuatro datos y devuelve los datos ya guardados. Si algo no es válido responde 400
    /// <c>datos_invalidos</c> y no cambia nada; una cuenta sin esos roles recibe 404.
    /// </summary>
    Task<DatosPropiosDto> CambiarAsync(
        Guid usuarioId, CambiarDatosPropiosDto datos, CancellationToken cancelacion = default);
}
