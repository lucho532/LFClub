using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Pruebas.Unitarias.Reglas;

public class ReglaDocumentoCompartidoPruebas
{
    private static readonly Guid Propia = Guid.NewGuid();
    private static readonly Guid Otra = Guid.NewGuid();
    private static readonly DateTime Antes = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static UsuarioRol Integrante(
        Guid cuenta,
        Rol rol = Rol.JUGADOR,
        EstadoIngreso estado = EstadoIngreso.APROBADO,
        bool activo = true,
        DateTime? creadoEn = null) => new()
    {
        UsuarioId = cuenta,
        ClubId = Guid.NewGuid(),
        Rol = rol,
        EstadoIngreso = estado,
        Activo = activo,
        CreadoEn = creadoEn ?? Antes,
    };

    // Research §12, fila por fila.
    [Fact]
    public void Sin_integrantes_de_otra_cuenta_se_admite_sin_baja()
    {
        Assert.Equal(DocumentoEnOtraCuenta.SinBaja, ReglaDocumentoCompartido.Evaluar([], Propia));
        Assert.Equal(DocumentoEnOtraCuenta.SinBaja, ReglaDocumentoCompartido.Evaluar([Integrante(Propia)], Propia));
    }

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    public void Otro_rol_de_otra_cuenta_no_se_admite_aunque_tambien_haya_un_jugador_activo(Rol rol)
    {
        Assert.Equal(DocumentoEnOtraCuenta.NoAdmitido, ReglaDocumentoCompartido.Evaluar([Integrante(Otra, rol)], Propia));
        Assert.Equal(
            DocumentoEnOtraCuenta.NoAdmitido,
            ReglaDocumentoCompartido.Evaluar([Integrante(Otra), Integrante(Guid.NewGuid(), rol, activo: true)], Propia));
    }

    [Fact]
    public void Un_jugador_activo_de_otra_cuenta_se_admite_con_baja() =>
        Assert.Equal(DocumentoEnOtraCuenta.ConBaja, ReglaDocumentoCompartido.Evaluar([Integrante(Otra)], Propia));

    [Fact]
    public void Solo_jugadores_retirados_o_en_espera_de_otra_cuenta_se_admite_sin_baja() =>
        Assert.Equal(
            DocumentoEnOtraCuenta.SinBaja,
            ReglaDocumentoCompartido.Evaluar(
                [Integrante(Otra, activo: false), Integrante(Otra, estado: EstadoIngreso.EN_ESPERA)], Propia));

    // La misma cuenta en otro club no cuenta, ni siquiera como otro rol.
    [Fact]
    public void La_misma_cuenta_se_ignora()
    {
        Assert.Equal(
            DocumentoEnOtraCuenta.SinBaja, ReglaDocumentoCompartido.Evaluar([Integrante(Propia, Rol.ENTRENADOR)], Propia));
        Assert.Empty(ReglaDocumentoCompartido.ARetirar([Integrante(Propia)], Propia));
        Assert.False(ReglaDocumentoCompartido.HayOtraCuentaActiva([Integrante(Propia)], Propia));
    }

    [Fact]
    public void Retira_a_los_jugadores_activos_de_otra_cuenta_en_todos_sus_clubes_y_a_nadie_mas()
    {
        var activoEnUnClub = Integrante(Otra);
        var activoEnOtroClub = Integrante(Otra);
        var deUnaTercera = Integrante(Guid.NewGuid());
        var retirado = Integrante(Otra, activo: false);
        var enEspera = Integrante(Otra, estado: EstadoIngreso.EN_ESPERA);
        var propio = Integrante(Propia);

        var aRetirar = ReglaDocumentoCompartido.ARetirar(
            [activoEnUnClub, activoEnOtroClub, deUnaTercera, retirado, enEspera, propio], Propia);

        Assert.Equal([activoEnUnClub, activoEnOtroClub, deUnaTercera], aRetirar);
    }

    // Research §15: activo, luego en espera, luego retirado; entre iguales, el más reciente.
    [Fact]
    public void Abre_la_cuenta_del_activo_antes_que_la_del_que_espera_y_esta_antes_que_la_del_retirado()
    {
        var activa = Guid.NewGuid();
        var esperando = Guid.NewGuid();
        var retirada = Guid.NewGuid();
        var lista = new[]
        {
            Integrante(retirada, activo: false, creadoEn: Antes.AddDays(3)),
            Integrante(esperando, estado: EstadoIngreso.EN_ESPERA, creadoEn: Antes.AddDays(2)),
            Integrante(activa, creadoEn: Antes),
        };

        Assert.Equal(activa, ReglaDocumentoCompartido.CuentaQueAbre(lista));
        Assert.Equal(esperando, ReglaDocumentoCompartido.CuentaQueAbre(lista[..2]));
        Assert.Equal(retirada, ReglaDocumentoCompartido.CuentaQueAbre(lista[..1]));
    }

    [Theory]
    [InlineData(EstadoIngreso.APROBADO, true)]
    [InlineData(EstadoIngreso.EN_ESPERA, true)]
    [InlineData(EstadoIngreso.APROBADO, false)]
    public void Entre_dos_del_mismo_nivel_abre_la_del_mas_reciente(EstadoIngreso estado, bool activo)
    {
        var antigua = Guid.NewGuid();
        var reciente = Guid.NewGuid();

        Assert.Equal(
            reciente,
            ReglaDocumentoCompartido.CuentaQueAbre(
            [
                Integrante(antigua, estado: estado, activo: activo, creadoEn: Antes),
                Integrante(reciente, estado: estado, activo: activo, creadoEn: Antes.AddHours(1)),
            ]));
    }

    [Fact]
    public void Con_una_sola_cuenta_abre_esa_y_sin_nadie_ninguna()
    {
        Assert.Equal(Propia, ReglaDocumentoCompartido.CuentaQueAbre([Integrante(Propia), Integrante(Propia, activo: false)]));
        Assert.Null(ReglaDocumentoCompartido.CuentaQueAbre([]));
    }

    // Research §19: cualquier rol activo de otra cuenta cuenta; en espera y retirado, no.
    [Theory]
    [InlineData(Rol.JUGADOR, EstadoIngreso.APROBADO, true, true)]
    [InlineData(Rol.DIRECTIVO, EstadoIngreso.APROBADO, true, true)]
    [InlineData(Rol.JUGADOR, EstadoIngreso.EN_ESPERA, true, false)]
    [InlineData(Rol.JUGADOR, EstadoIngreso.APROBADO, false, false)]
    public void Hay_otra_cuenta_activa_solo_con_un_integrante_aprobado_y_activo_de_otra_cuenta(
        Rol rol, EstadoIngreso estado, bool activo, bool esperado) =>
        Assert.Equal(
            esperado,
            ReglaDocumentoCompartido.HayOtraCuentaActiva([Integrante(Otra, rol, estado, activo)], Propia));
}
