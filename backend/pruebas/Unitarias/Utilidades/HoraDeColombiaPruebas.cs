using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Utilidades;

namespace LaPecosa.Pruebas.Unitarias.Utilidades;

public class HoraDeColombiaPruebas
{
    [Fact]
    public void Las_cuatro_y_cincuenta_y_nueve_utc_son_las_once_y_cincuenta_y_nueve_del_dia_anterior()
    {
        var (hoy, hora) = HoraDeColombia.De(new DateTime(2026, 10, 13, 4, 59, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2026, 10, 12), hoy);
        Assert.Equal(new TimeOnly(23, 59), hora);
    }

    [Fact]
    public void Las_cinco_utc_son_la_medianoche_del_mismo_dia()
    {
        var (hoy, hora) = HoraDeColombia.De(new DateTime(2026, 10, 13, 5, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2026, 10, 13), hoy);
        Assert.Equal(new TimeOnly(0, 0), hora);
    }

    [Fact]
    public void El_mediodia_utc_son_las_siete_de_la_manana()
    {
        var (hoy, hora) = HoraDeColombia.De(new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2026, 10, 13), hoy);
        Assert.Equal(new TimeOnly(7, 0), hora);
    }

    [Fact]
    public void El_primero_de_enero_a_las_tres_utc_sigue_siendo_el_ano_anterior()
    {
        var (hoy, hora) = HoraDeColombia.De(new DateTime(2027, 1, 1, 3, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2026, 12, 31), hoy);
        Assert.Equal(new TimeOnly(22, 30), hora);
    }

    [Fact]
    public void Lee_el_instante_del_reloj()
    {
        var (hoy, hora) = HoraDeColombia.Ahora(new RelojFijo(new DateTime(2026, 10, 13, 21, 15, 0, DateTimeKind.Utc)));

        Assert.Equal(new DateOnly(2026, 10, 13), hoy);
        Assert.Equal(new TimeOnly(16, 15), hora);
    }

    private sealed class RelojFijo(DateTime ahoraUtc) : IReloj
    {
        public DateTime AhoraUtc => ahoraUtc;
    }
}
