using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Cargo"/> (spec 015).
/// Su responsabilidad es fijar la tabla, el tipo como texto, el dinero como <c>numeric(14,2)</c>, el
/// índice único de una mensualidad por jugador y mes (CE-001), los índices de consulta y el borrado:
/// en cascada con el club y el jugador, a nulo con la categoría.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionCargo : IEntityTypeConfiguration<Cargo>
{
    /// <summary>Nombre del índice único de una mensualidad por jugador y mes.</summary>
    public const string IndiceMensualidadPorMes = "UX_Cargos_Mensualidad_Jugador_Mes";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("Cargos", tabla =>
        {
            tabla.HasCheckConstraint("CK_Cargos_Valor", "\"Valor\" > 0");
            tabla.HasCheckConstraint("CK_Cargos_Mes", "\"Tipo\" <> 'MENSUALIDAD' OR \"Mes\" IS NOT NULL");
        });
        builder.HasKey(cargo => cargo.Id);
        builder.Property(cargo => cargo.Id).ValueGeneratedNever();

        builder.Property(cargo => cargo.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(cargo => cargo.Valor).HasColumnType("numeric(14,2)");

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(cargo => cargo.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<UsuarioRol>()
            .WithMany()
            .HasForeignKey(cargo => cargo.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        // Borrar una categoría no borra el cargo: solo pierde la categoría (§13).
        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(cargo => cargo.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(cargo => new { cargo.UsuarioRolId, cargo.Mes })
            .IsUnique()
            .HasFilter("\"Tipo\" = 'MENSUALIDAD'")
            .HasDatabaseName(IndiceMensualidadPorMes);

        builder.HasIndex(cargo => new { cargo.ClubId, cargo.UsuarioRolId });
        builder.HasIndex(cargo => new { cargo.ClubId, cargo.Mes });
    }
}
