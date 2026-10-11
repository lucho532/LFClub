using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class CalculoDeAsistenciaPruebas
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]
    [InlineData(3, 3, 100)]
    [InlineData(0, 4, 0)]
    [InlineData(1, 8, 13)]
    public void Redondea_al_entero_y_da_cero_sin_registros(int asistidos, int registrados, int esperado) =>
        Assert.Equal(esperado, CalculoDeAsistencia.Porcentaje(asistidos, registrados));
}
