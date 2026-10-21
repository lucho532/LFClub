using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="MedioPagoClub"/> (spec 015).
/// Su responsabilidad es fijar la tabla, las longitudes, el índice por club y el borrado en cascada
/// desde el club.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionMedioPagoClub : IEntityTypeConfiguration<MedioPagoClub>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MedioPagoClub> builder)
    {
        builder.ToTable("MediosPagoClub");
        builder.HasKey(medio => medio.Id);
        builder.Property(medio => medio.Id).ValueGeneratedNever();

        builder.Property(medio => medio.Entidad).HasMaxLength(40).IsRequired();
        builder.Property(medio => medio.Titular).HasMaxLength(100).IsRequired();
        builder.Property(medio => medio.Numero).HasMaxLength(60).IsRequired();
        builder.Property(medio => medio.Nota).HasMaxLength(200);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(medio => medio.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(medio => medio.ClubId);
    }
}
