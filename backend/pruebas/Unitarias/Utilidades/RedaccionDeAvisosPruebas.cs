using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Utilidades;

public class RedaccionDeAvisosPruebas
{
    // 2026-10-12 es lunes.
    private static readonly DateOnly Hoy = new(2026, 10, 12);
    private static readonly DateOnly Sabado = new(2026, 10, 17);

    [Fact]
    public void Programar_un_entrenamiento()
    {
        var texto = RedaccionDeAvisos.EntrenamientoProgramado(Sabado, new TimeOnly(9, 0), Hoy);

        Assert.Equal("Nuevo entrenamiento el sábado a las 9:00", texto);
    }

    [Theory]
    [InlineData(CambiosDeActividad.Hora, "Cambió la hora del entrenamiento: ahora el sábado a las 10:00")]
    [InlineData(CambiosDeActividad.Fecha, "Cambió la fecha del entrenamiento: ahora el sábado a las 10:00")]
    [InlineData(CambiosDeActividad.Lugar, "Cambió el lugar del entrenamiento: ahora en Cancha Minitas")]
    [InlineData(
        CambiosDeActividad.Fecha | CambiosDeActividad.Lugar,
        "Cambiaron la fecha y el lugar del entrenamiento: ahora el sábado a las 10:00 en Cancha Minitas")]
    [InlineData(
        CambiosDeActividad.Fecha | CambiosDeActividad.Hora | CambiosDeActividad.Lugar,
        "Cambiaron la fecha, la hora y el lugar del entrenamiento: ahora el sábado a las 10:00 en Cancha Minitas")]
    public void Cambiar_un_entrenamiento_dice_que_cambio_y_como_queda(CambiosDeActividad cambios, string esperado)
    {
        var texto = RedaccionDeAvisos.EntrenamientoCambiado(cambios, Sabado, new TimeOnly(10, 0), "Cancha Minitas", Hoy);

        Assert.Equal(esperado, texto);
    }

    [Fact]
    public void Cancelar_y_reactivar_un_entrenamiento()
    {
        Assert.Equal("Entrenamiento del sábado cancelado", RedaccionDeAvisos.EntrenamientoCancelado(Sabado, Hoy));
        Assert.Equal("El entrenamiento del sábado sigue en pie", RedaccionDeAvisos.EntrenamientoReactivado(Sabado, Hoy));
    }

    [Fact]
    public void La_asistencia_no_dice_quien_asistio_ni_quien_falto()
    {
        var texto = RedaccionDeAvisos.AsistenciaRegistrada(Sabado, Hoy);

        Assert.Equal("Se registró la asistencia del entrenamiento del sábado", texto);
    }

    [Fact]
    public void Cambiar_el_lugar_de_un_partido()
    {
        var texto = RedaccionDeAvisos.PartidoCambiado(
            CambiosDeActividad.Lugar, "Rival", Sabado, new TimeOnly(15, 0), "Cancha Minitas", Hoy);

        Assert.Equal("Cambió el lugar del partido contra Rival: ahora en Cancha Minitas", texto);
    }

    [Fact]
    public void Cancelar_y_reactivar_un_partido()
    {
        Assert.Equal("Partido contra Rival del sábado cancelado", RedaccionDeAvisos.PartidoCancelado("Rival", Sabado, Hoy));
        Assert.Equal("El partido contra Rival del sábado sigue en pie", RedaccionDeAvisos.PartidoReactivado("Rival", Sabado, Hoy));
    }

    [Fact]
    public void Convocar_a_un_jugador_o_a_varios()
    {
        var uno = RedaccionDeAvisos.JugadorConvocado(["Mateo"], "Rival", Sabado, new TimeOnly(15, 0), Hoy);
        var dos = RedaccionDeAvisos.JugadorConvocado(["Mateo", "Sofía"], "Rival", Sabado, new TimeOnly(15, 0), Hoy);
        var tres = RedaccionDeAvisos.JugadorConvocado(["Mateo", "Sofía", "Luis"], "Rival", Sabado, new TimeOnly(15, 0), Hoy);

        Assert.Equal("Mateo fue convocado al partido contra Rival el sábado a las 15:00", uno);
        Assert.Equal("Mateo y Sofía fueron convocados al partido contra Rival el sábado a las 15:00", dos);
        Assert.Equal("Mateo, Sofía y Luis fueron convocados al partido contra Rival el sábado a las 15:00", tres);
    }

    [Fact]
    public void El_marcador_sigue_el_orden_local_o_visitante()
    {
        var local = RedaccionDeAvisos.ResultadoPublicado("Rival", CondicionPartido.LOCAL, "La Pecosa", 3, 1);
        var visitante = RedaccionDeAvisos.ResultadoPublicado("Rival", CondicionPartido.VISITANTE, "La Pecosa", 3, 1);

        Assert.Equal("Resultado del partido contra Rival: La Pecosa 3 - 1 Rival", local);
        Assert.Equal("Resultado del partido contra Rival: Rival 1 - 3 La Pecosa", visitante);
    }

    [Fact]
    public void La_fecha_se_dice_hoy_o_manana_cuando_corresponde()
    {
        Assert.Equal("Nuevo entrenamiento hoy a las 9:00", RedaccionDeAvisos.EntrenamientoProgramado(Hoy, new TimeOnly(9, 0), Hoy));
        Assert.Equal("Nuevo entrenamiento mañana a las 9:00", RedaccionDeAvisos.EntrenamientoProgramado(Hoy.AddDays(1), new TimeOnly(9, 0), Hoy));
        Assert.Equal("Entrenamiento de hoy cancelado", RedaccionDeAvisos.EntrenamientoCancelado(Hoy, Hoy));
        Assert.Equal("Entrenamiento de mañana cancelado", RedaccionDeAvisos.EntrenamientoCancelado(Hoy.AddDays(1), Hoy));
    }

    [Fact]
    public void Dentro_de_seis_dias_se_dice_el_dia_de_la_semana_y_despues_la_fecha()
    {
        var enSeisDias = Hoy.AddDays(6);
        var enSieteDias = Hoy.AddDays(7);

        Assert.Equal("Nuevo entrenamiento el domingo a las 9:00", RedaccionDeAvisos.EntrenamientoProgramado(enSeisDias, new TimeOnly(9, 0), Hoy));
        Assert.Equal("Nuevo entrenamiento el 19 de octubre a las 9:00", RedaccionDeAvisos.EntrenamientoProgramado(enSieteDias, new TimeOnly(9, 0), Hoy));
        Assert.Equal("Entrenamiento del 19 de octubre cancelado", RedaccionDeAvisos.EntrenamientoCancelado(enSieteDias, Hoy));
    }

    [Fact]
    public void Una_fecha_pasada_se_dice_con_dia_y_mes()
    {
        var texto = RedaccionDeAvisos.PartidoCancelado("Rival", new DateOnly(2026, 10, 5), Hoy);

        Assert.Equal("Partido contra Rival del 5 de octubre cancelado", texto);
    }

    [Fact]
    public void La_hora_va_en_24_horas_y_sin_cero_a_la_izquierda()
    {
        Assert.Contains("a las 9:05", RedaccionDeAvisos.EntrenamientoProgramado(Sabado, new TimeOnly(9, 5), Hoy));
        Assert.Contains("a las 15:00", RedaccionDeAvisos.EntrenamientoProgramado(Sabado, new TimeOnly(15, 0), Hoy));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Por definir")]
    public void Sin_rival_se_habla_del_partido_a_secas(string? rival)
    {
        Assert.Equal("Partido del sábado cancelado", RedaccionDeAvisos.PartidoCancelado(rival, Sabado, Hoy));
        Assert.Equal("El partido del sábado sigue en pie", RedaccionDeAvisos.PartidoReactivado(rival, Sabado, Hoy));
        Assert.Equal(
            "Mateo fue convocado al partido el sábado a las 15:00",
            RedaccionDeAvisos.JugadorConvocado(["Mateo"], rival, Sabado, new TimeOnly(15, 0), Hoy));
    }

    [Fact]
    public void El_texto_nunca_pasa_de_300_caracteres()
    {
        var largo = new string('x', 120);
        var nombres = Enumerable.Range(0, 20).Select(numero => $"Jugador{numero}" + new string('y', 30)).ToList();

        var cambiado = RedaccionDeAvisos.PartidoCambiado(
            CambiosDeActividad.Fecha | CambiosDeActividad.Lugar, largo, Sabado, new TimeOnly(9, 0), largo, Hoy);
        var convocados = RedaccionDeAvisos.JugadorConvocado(nombres, largo, Sabado, new TimeOnly(9, 0), Hoy);

        Assert.True(cambiado.Length <= 300);
        Assert.Equal(300, convocados.Length);
        Assert.EndsWith("…", convocados);
    }
}
