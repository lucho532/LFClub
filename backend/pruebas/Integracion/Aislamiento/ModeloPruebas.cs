using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Aislamiento;

/// <summary>
/// Red de seguridad del aislamiento entre clubes (constitución §7.1, research §2): vigila el
/// modelo para que ninguna funcionalidad futura añada una tabla sin aislar.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ModeloPruebas
{
    // El cobro del uso de la plataforma (spec 017, research §6) no es de un club en la petición: lo leen el
    // trabajo en segundo plano y la notificación de la pasarela, que no tienen club. Sus repositorios filtran
    // por club de forma explícita y lo prueba AislamientoDePagoPlataformaPruebas.
    private static readonly string[] EntidadesDeLaPlataforma =
    [
        "Club", "Usuario", "FotoPerfil", "SolicitudRecuperacion",
        "ConfiguracionPlataforma", "CobroPlataforma", "IntentoPagoPlataforma", "PagoPlataforma", "NotificacionPasarela",
    ];

    private readonly FabricaApi _fabrica;

    public ModeloPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Toda_entidad_pertenece_a_un_club_o_esta_en_la_lista_de_la_plataforma()
    {
        var sinAislar = await _fabrica.ConContextoAsync(contexto => Task.FromResult(
            contexto.Model.GetEntityTypes()
                .Where(tipo => !typeof(IPerteneceAClub).IsAssignableFrom(tipo.ClrType))
                .Select(tipo => tipo.ClrType.Name)
                .Except(EntidadesDeLaPlataforma)
                .ToList()));

        Assert.Empty(sinAislar);
    }

    [Fact]
    public async Task Toda_entidad_de_club_tiene_filtro_y_clave_foranea_a_Club_con_cascada()
    {
        var incumplen = await _fabrica.ConContextoAsync(contexto => Task.FromResult(
            contexto.Model.GetEntityTypes()
                .Where(tipo => typeof(IPerteneceAClub).IsAssignableFrom(tipo.ClrType))
                .Where(tipo =>
                    tipo.GetDeclaredQueryFilters().Count == 0
                    || !tipo.GetForeignKeys().Any(clave =>
                        clave.PrincipalEntityType.ClrType == typeof(Club)
                        && clave.DeleteBehavior == DeleteBehavior.Cascade
                        && clave.Properties.Single().Name == nameof(IPerteneceAClub.ClubId)))
                .Select(tipo => tipo.ClrType.Name)
                .ToList()));

        Assert.Empty(incumplen);
    }

    [Fact]
    public void IgnoreQueryFilters_solo_aparece_en_los_repositorios_de_la_plataforma()
    {
        var raizCodigo = Path.Combine(RaizDelBackend(), "src");
        var permitida = Path.Combine("Repositorios", "Plataforma") + Path.DirectorySeparatorChar;

        var fuera = Directory.EnumerateFiles(raizCodigo, "*.cs", SearchOption.AllDirectories)
            .Where(archivo => !archivo.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !archivo.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(archivo => !archivo.Contains(permitida))
            .Where(archivo => File.ReadAllText(archivo).Contains("IgnoreQueryFilters("))
            .Select(archivo => Path.GetRelativePath(raizCodigo, archivo))
            .ToList();

        Assert.Empty(fuera);
    }

    [Fact]
    public async Task Sin_club_en_el_contexto_una_consulta_de_integrantes_no_devuelve_filas()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);

        var filas = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.CountAsync());
        var reales = await _fabrica.ConContextoAsync(contexto => contexto.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"UsuariosRol\"").SingleAsync());

        Assert.Equal(0, filas);
        Assert.True(reales > 0);
    }

    private static string RaizDelBackend()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "LFClub.sln")))
        {
            carpeta = carpeta.Parent;
        }

        return carpeta?.FullName ?? throw new InvalidOperationException("No se encontró LFClub.sln.");
    }
}
