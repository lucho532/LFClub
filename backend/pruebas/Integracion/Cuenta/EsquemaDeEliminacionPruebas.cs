using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Guardián del esquema de la eliminación de cuentas (spec 014, RF-007): toda clave foránea hacia
/// una cuenta o un integrante debe decidir qué pasa cuando se eliminan.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EsquemaDeEliminacionPruebas
{
    /// <summary>
    /// Las únicas claves que no caen solas, con el motivo: el repositorio de eliminación las borra a
    /// mano y en este orden.
    /// </summary>
    private static readonly HashSet<string> BorradasAMano =
    [
        "PartidoJugador.UsuarioRolId",
        "AsistenciaEntrenamiento.UsuarioRolId",
        "UsuarioRol.UsuarioId",
    ];

    private readonly FabricaApi _fabrica;

    public EsquemaDeEliminacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Toda_clave_hacia_la_cuenta_o_el_integrante_cae_en_cascada_o_a_nulo_o_se_borra_a_mano()
    {
        var incumplen = await _fabrica.ConContextoAsync(contexto => Task.FromResult(
            contexto.Model.GetEntityTypes()
                .SelectMany(entidad => entidad.GetForeignKeys())
                .Where(clave => clave.PrincipalEntityType.ClrType == typeof(Usuario)
                    || clave.PrincipalEntityType.ClrType == typeof(UsuarioRol))
                .Where(clave => clave.DeleteBehavior is not (DeleteBehavior.Cascade or DeleteBehavior.SetNull))
                .Select(clave => $"{clave.DeclaringEntityType.ClrType.Name}."
                    + $"{string.Join(",", clave.Properties.Select(propiedad => propiedad.Name))}")
                .Where(nombre => !BorradasAMano.Contains(nombre))
                .ToList()));

        Assert.True(
            incumplen.Count == 0,
            "Una clave foránea hacia la cuenta o el integrante no cae en cascada ni a nulo: "
            + string.Join("; ", incumplen)
            + ". Eliminar una cuenta se rompería. Si es una tabla de pagos (§16.3, RF-007), debe copiar "
            + "nombre y documento y usar SetNull; si no, decide qué pasa y bórrala en "
            + "RepositorioEliminacionCuenta.");
    }

    [Fact]
    public async Task Las_claves_borradas_a_mano_siguen_existiendo()
    {
        var existentes = await _fabrica.ConContextoAsync(contexto => Task.FromResult(
            contexto.Model.GetEntityTypes()
                .SelectMany(entidad => entidad.GetForeignKeys())
                .Select(clave => $"{clave.DeclaringEntityType.ClrType.Name}."
                    + $"{string.Join(",", clave.Properties.Select(propiedad => propiedad.Name))}")
                .ToHashSet()));

        Assert.All(BorradasAMano, nombre => Assert.Contains(nombre, existentes));
    }
}
