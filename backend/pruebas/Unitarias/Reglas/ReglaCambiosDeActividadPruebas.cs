using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaCambiosDeActividadPruebas
{
    private static readonly DateOnly Fecha = new(2026, 10, 17);
    private static readonly TimeOnly Inicio = new(9, 0);
    private static readonly TimeOnly Fin = new(10, 30);

    private static CambiosDeActividad Evaluar(
        DateOnly? fecha = null,
        TimeOnly? inicio = null,
        TimeOnly? fin = null,
        string? lugar = "Cancha Minitas",
        string? lugarAntes = "Cancha Minitas") =>
        ReglaCambiosDeActividad.Evaluar(
            Fecha, Inicio, Fin, lugarAntes,
            fecha ?? Fecha, inicio ?? Inicio, fin ?? Fin, lugar);

    [Fact]
    public void Si_no_cambia_nada_no_hay_cambios()
    {
        Assert.Equal(CambiosDeActividad.Ninguno, Evaluar());
    }

    [Fact]
    public void Cambiar_solo_la_fecha_es_un_cambio_de_fecha()
    {
        Assert.Equal(CambiosDeActividad.Fecha, Evaluar(fecha: Fecha.AddDays(1)));
    }

    [Fact]
    public void Cambiar_solo_la_hora_de_inicio_es_un_cambio_de_hora()
    {
        Assert.Equal(CambiosDeActividad.Hora, Evaluar(inicio: new TimeOnly(10, 0)));
    }

    [Fact]
    public void Cambiar_solo_la_hora_de_fin_es_un_cambio_de_hora()
    {
        Assert.Equal(CambiosDeActividad.Hora, Evaluar(fin: new TimeOnly(11, 0)));
    }

    [Fact]
    public void Cambiar_solo_el_lugar_es_un_cambio_de_lugar()
    {
        Assert.Equal(CambiosDeActividad.Lugar, Evaluar(lugar: "Estadio"));
    }

    [Fact]
    public void Varios_cambios_a_la_vez_se_combinan()
    {
        var cambios = Evaluar(fecha: Fecha.AddDays(2), inicio: new TimeOnly(8, 0), lugar: "Estadio");

        Assert.Equal(CambiosDeActividad.Fecha | CambiosDeActividad.Hora | CambiosDeActividad.Lugar, cambios);
    }

    [Fact]
    public void Un_partido_sin_hora_de_fin_compara_solo_el_inicio()
    {
        var sinCambio = ReglaCambiosDeActividad.Evaluar(
            Fecha, Inicio, null, "Cancha", Fecha, Inicio, null, "Cancha");
        var conCambio = ReglaCambiosDeActividad.Evaluar(
            Fecha, Inicio, null, "Cancha", Fecha, new TimeOnly(15, 0), null, "Cancha");

        Assert.Equal(CambiosDeActividad.Ninguno, sinCambio);
        Assert.Equal(CambiosDeActividad.Hora, conCambio);
    }

    [Fact]
    public void Pasar_el_lugar_de_nulo_a_un_valor_es_un_cambio()
    {
        Assert.Equal(CambiosDeActividad.Lugar, Evaluar(lugar: "Cancha", lugarAntes: null));
    }

    [Fact]
    public void Pasar_el_lugar_de_un_valor_a_nulo_es_un_cambio()
    {
        Assert.Equal(CambiosDeActividad.Lugar, Evaluar(lugar: null, lugarAntes: "Cancha"));
    }

    [Fact]
    public void Cambiar_solo_la_nota_o_el_rival_no_pasa_por_esta_regla_y_no_cambia_nada()
    {
        // La regla no recibe la nota, el rival, la condición ni el torneo: por diseño no los compara.
        Assert.Equal(CambiosDeActividad.Ninguno, Evaluar());
    }
}
