using LaPecosa.Infraestructura.Imagenes;

namespace LaPecosa.Pruebas.Unitarias.Utilidades;

// Los mismos casos y resultados que frontend/pruebas/plano.test.ts: pantalla e imagen eligen igual.
public class ContrasteDeColorPruebas
{
    [Fact]
    public void Con_colores_oscuros_el_texto_es_blanco_y_sin_sombra() =>
        Assert.Equal((ContrasteDeColor.TextoClaro, (string?)null), ContrasteDeColor.ColorDelTexto(("#1B3A6B", "#0F2240")));

    [Fact]
    public void Con_colores_claros_el_texto_es_casi_negro_y_sin_sombra() =>
        Assert.Equal((ContrasteDeColor.TextoOscuro, (string?)null), ContrasteDeColor.ColorDelTexto(("#F5F5F5", "#E0E0E0")));

    [Fact]
    public void Con_dos_amarillos_claros_el_texto_oscuro_se_lee_con_los_dos()
    {
        var (texto, _) = ContrasteDeColor.ColorDelTexto(("#FFF176", "#FFE082"));

        Assert.Equal(ContrasteDeColor.TextoOscuro, texto);
        Assert.True(ContrasteDeColor.Contraste(texto, "#FFF176") >= 4.5);
        Assert.True(ContrasteDeColor.Contraste(texto, "#FFE082") >= 4.5);
    }

    [Fact]
    public void Con_un_amarillo_y_un_mostaza_oscuro_el_texto_lleva_sombra_del_color_contrario() =>
        Assert.Equal(
            (ContrasteDeColor.TextoOscuro, (string?)ContrasteDeColor.TextoClaro),
            ContrasteDeColor.ColorDelTexto(("#FFE600", "#6B5B00")));

    [Fact]
    public void Sin_acento_el_degradado_va_del_principal_a_su_version_mas_oscura() =>
        Assert.Equal(("#1B3A6B", "#122646"), ContrasteDeColor.Degradado("#1b3a6b", null));

    [Fact]
    public void Sin_colores_usa_los_neutros() =>
        Assert.Equal(("#231F1E", "#5E5650"), ContrasteDeColor.Degradado(null, "#FFFFFF"));

    [Fact]
    public void El_contraste_de_blanco_y_negro_es_21() =>
        Assert.Equal(21, ContrasteDeColor.Contraste("#FFFFFF", "#000000"), 3);
}
