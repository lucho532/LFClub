using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Unitarias.Validadores;

/// <summary>
/// El responsable solo se pide con una invitación de JUGADOR: obligatorio para un menor de 18 años
/// y opcional para un adulto. Con cualquier otro rol ni se exige ni se valida (RF-013).
/// </summary>
public class ValidadorRegistroPruebas
{
    private static readonly DateOnly Hoy = new(2026, 10, 7);
    private static readonly DateOnly Menor = new(2016, 5, 20);
    private static readonly DateOnly Adulto = new(1988, 3, 15);

    private static RegistrarConInvitacionDto Datos(DateOnly? fechaNacimiento, string? nombreResponsable) => new(
        "token", "Ana", "Pérez", TipoDocumento.TARJETA_IDENTIDAD, "1002003004", fechaNacimiento, "3001234567",
        nombreResponsable, "mi-contrasena-propia");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_jugador_menor_sin_responsable_se_rechaza_en_el_campo_del_responsable(string? responsable)
    {
        var error = Assert.Throws<ExcepcionDeAplicacion>(
            () => ValidadorRegistro.Validar(Datos(Menor, responsable), Rol.JUGADOR, Hoy));

        Assert.Equal("datos_invalidos", error.Codigo);
        Assert.Equal(400, error.EstadoHttp);
        Assert.Equal("nombreResponsable", Assert.Single(error.Errores!.Keys));
    }

    [Fact]
    public void Un_jugador_menor_con_responsable_es_valido() =>
        ValidadorRegistro.Validar(Datos(Menor, "Marta Gómez"), Rol.JUGADOR, Hoy);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Marta Gómez")]
    public void Un_jugador_adulto_es_valido_con_o_sin_responsable(string? responsable) =>
        ValidadorRegistro.Validar(Datos(Adulto, responsable), Rol.JUGADOR, Hoy);

    [Fact]
    public void Un_jugador_que_cumple_18_hoy_ya_no_necesita_responsable_y_quien_los_cumple_manana_si()
    {
        ValidadorRegistro.Validar(Datos(new DateOnly(2008, 10, 7), null), Rol.JUGADOR, Hoy);

        var error = Assert.Throws<ExcepcionDeAplicacion>(
            () => ValidadorRegistro.Validar(Datos(new DateOnly(2008, 10, 8), null), Rol.JUGADOR, Hoy));
        Assert.True(error.Errores!.ContainsKey("nombreResponsable"));
    }

    [Theory]
    [InlineData(160, true)]
    [InlineData(161, false)]
    public void El_responsable_de_un_jugador_admite_160_caracteres_y_no_161(int longitud, bool valido)
    {
        var datos = Datos(Adulto, new string('a', longitud));

        if (valido)
        {
            ValidadorRegistro.Validar(datos, Rol.JUGADOR, Hoy);
            return;
        }

        var error = Assert.Throws<ExcepcionDeAplicacion>(() => ValidadorRegistro.Validar(datos, Rol.JUGADOR, Hoy));
        Assert.Equal("nombreResponsable", Assert.Single(error.Errores!.Keys));
    }

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.PRESIDENTE)]
    public void Fuera_del_jugador_un_menor_sin_responsable_es_valido_y_uno_de_161_caracteres_no_da_error(Rol rol)
    {
        ValidadorRegistro.Validar(Datos(Menor, null), rol, Hoy);
        ValidadorRegistro.Validar(Datos(Menor, new string('a', 161)), rol, Hoy);
        ValidadorRegistro.Validar(Datos(Adulto, new string('a', 161)), rol, Hoy);
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.ENTRENADOR)]
    public void Sin_fecha_de_nacimiento_el_error_es_de_la_fecha_y_no_del_responsable(Rol rol)
    {
        var error = Assert.Throws<ExcepcionDeAplicacion>(() => ValidadorRegistro.Validar(Datos(null, null), rol, Hoy));

        Assert.Equal("fechaNacimiento", Assert.Single(error.Errores!.Keys));
    }
}
