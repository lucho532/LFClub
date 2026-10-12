using LaPecosa.Aplicacion.Utilidades;

namespace LaPecosa.Pruebas.Unitarias.Utilidades;

/// <remarks>
/// Hallazgo (2026-10-12): en .NET 10 <see cref="Guid.CompareTo(Guid)"/> coincide con el orden de
/// bytes de red en 20.000 parejas aleatorias, así que ya no hay parejas que lo distingan. El
/// comparador se conserva porque fija el contrato (el orden de PostgreSQL) sin depender de ese
/// detalle del runtime, y estas pruebas lo comparan con el texto hexadecimal, no con CompareTo.
/// </remarks>
public class OrdenDeIdentificadoresPruebas
{
    [Fact]
    public void Compara_byte_a_byte_en_orden_de_red()
    {
        var menor = new Guid("00000000-0000-0000-0000-000000000001");
        var mayor = new Guid("00000000-0000-0000-0000-000000000002");

        Assert.True(OrdenDeIdentificadores.Comparar(menor, mayor) < 0);
        Assert.True(OrdenDeIdentificadores.Comparar(mayor, menor) > 0);
        Assert.Equal(0, OrdenDeIdentificadores.Comparar(menor, menor));
    }

    [Fact]
    public void Compara_cada_byte_sin_signo()
    {
        var conBitAlto = new Guid("80000000-0000-0000-0000-000000000000");
        var sinBitAlto = new Guid("7fffffff-ffff-ffff-ffff-ffffffffffff");

        Assert.True(OrdenDeIdentificadores.Comparar(conBitAlto, sinBitAlto) > 0);
    }

    [Fact]
    public void Coincide_con_el_orden_del_texto_hexadecimal_que_aplica_PostgreSQL()
    {
        var aleatorio = new Random(12);
        var identificadores = Enumerable.Range(0, 200)
            .Select(_ =>
            {
                var bytes = new byte[16];
                aleatorio.NextBytes(bytes);
                return new Guid(bytes);
            })
            .ToList();

        var porBytes = identificadores.Order(Comparer<Guid>.Create(OrdenDeIdentificadores.Comparar)).ToList();
        var porTexto = identificadores.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToList();

        Assert.Equal(porTexto, porBytes);
    }

    [Fact]
    public void El_orden_descendente_pone_primero_al_uuid_v7_creado_despues()
    {
        var anterior = Guid.CreateVersion7(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero));
        var posterior = Guid.CreateVersion7(new DateTimeOffset(2026, 10, 12, 10, 0, 1, TimeSpan.Zero));

        var ordenados = new[] { anterior, posterior }
            .Order(Comparer<Guid>.Create(OrdenDeIdentificadores.CompararDescendente))
            .ToList();

        Assert.Equal([posterior, anterior], ordenados);
    }
}
