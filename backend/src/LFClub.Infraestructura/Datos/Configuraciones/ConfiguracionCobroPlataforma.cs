using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="CobroPlataforma"/> (spec 017).
/// Su responsabilidad es fijar la tabla, el estado como texto, el dinero como <c>numeric(18,0)</c>, el
/// índice único de un cobro por club y periodo (idempotencia de la generación) y el borrado: en
/// cascada con el club y a nulo con quien anuló.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionCobroPlataforma : IEntityTypeConfiguration<CobroPlataforma>
{
    /// <summary>Nombre del índice único de un cobro por club y periodo.</summary>
    public const string IndiceCobroPorPeriodo = "UX_CobrosPlataforma_Club_Periodo";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CobroPlataforma> builder)
    {
        builder.ToTable("CobrosPlataforma", tabla =>
        {
            tabla.HasCheckConstraint("CK_CobrosPlataforma_Total", "\"Total\" > 0");
        });
        builder.HasKey(cobro => cobro.Id);
        builder.Property(cobro => cobro.Id).ValueGeneratedNever();

        builder.Property(cobro => cobro.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(cobro => cobro.PrecioJugador).HasColumnType("numeric(18,0)");
        builder.Property(cobro => cobro.PrecioEntrenador).HasColumnType("numeric(18,0)");
        builder.Property(cobro => cobro.PrecioDirectivo).HasColumnType("numeric(18,0)");
        builder.Property(cobro => cobro.PrecioPresidente).HasColumnType("numeric(18,0)");
        builder.Property(cobro => cobro.Total).HasColumnType("numeric(18,0)");
        builder.Property(cobro => cobro.MotivoAnulacion).HasMaxLength(300);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(cobro => cobro.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(cobro => cobro.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(cobro => new { cobro.ClubId, cobro.Periodo })
            .IsUnique()
            .HasDatabaseName(IndiceCobroPorPeriodo);
    }
}
