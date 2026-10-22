using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="SoportePago"/> (spec 015).
/// Su responsabilidad es fijar la tabla, la clave del pago (uno por pago), la longitud del tipo de
/// contenido y el borrado en cascada desde el pago y el club.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionSoportePago : IEntityTypeConfiguration<SoportePago>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SoportePago> builder)
    {
        builder.ToTable("SoportesPago");
        builder.HasKey(soporte => soporte.PagoId);
        builder.Property(soporte => soporte.PagoId).ValueGeneratedNever();

        builder.Property(soporte => soporte.Contenido).IsRequired();
        builder.Property(soporte => soporte.TipoContenido).HasMaxLength(20).IsRequired();

        builder.HasOne<Pago>()
            .WithOne()
            .HasForeignKey<SoportePago>(soporte => soporte.PagoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(soporte => soporte.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
