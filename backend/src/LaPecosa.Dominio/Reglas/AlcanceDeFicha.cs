namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa lo que una persona puede hacer con la ficha de un jugador concreto: el resultado de
/// <see cref="ReglaAccesoAFicha"/>.
/// Su responsabilidad es llevar los cinco indicadores juntos, para que quien arma la respuesta y
/// quien cambia la ficha lean la misma decisión.
/// No decide nada ni conoce el rol de quien pregunta: solo transporta el resultado. Si
/// <see cref="Ve"/> es falso, los otros cuatro también lo son.
/// </summary>
/// <param name="Ve">Puede abrir la ficha.</param>
/// <param name="VeDatosClinicos">Recibe el grupo sanguíneo, las alergias, las enfermedades, los medicamentos y las observaciones.</param>
/// <param name="VeDocumentos">Recibe el estado de los documentos pedidos y puede abrir sus archivos.</param>
/// <param name="Cambia">Puede cambiar el contacto, la salud y el documento de identidad, y subir archivos.</param>
/// <param name="CorrigeIdentidad">
/// Puede corregir los nombres, los apellidos y la fecha de nacimiento: el PRESIDENTE y, desde la spec
/// 010, la cuenta del jugador sobre su propio jugador.
/// </param>
public sealed record AlcanceDeFicha(
    bool Ve,
    bool VeDatosClinicos,
    bool VeDocumentos,
    bool Cambia,
    bool CorrigeIdentidad)
{
    /// <summary>Quien no puede abrir la ficha: los cinco indicadores en falso.</summary>
    public static AlcanceDeFicha Ninguno { get; } = new(false, false, false, false, false);
}
