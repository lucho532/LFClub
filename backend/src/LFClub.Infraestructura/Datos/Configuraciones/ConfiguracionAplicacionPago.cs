using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="AplicacionPago"/> (spec 015).
/// Su responsabilidad es fijar la tabla, la clave compuesta de pago y cargo, el dinero como
/// <c>numeric(14,2)</c> y el borrado en cascada desde el pago, el cargo y el club.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionAplicacionPago : IEntityTypeConfiguration<AplicacionPago>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AplicacionPago> builder)
    {
        builder.ToTable("AplicacionesPago", tabla =>
            tabla.HasCheckConstraint("CK_AplicacionesPago_Valor", "\"Valor\" > 0"));
        builder.HasKey(aplicacion => new { aplicacion.PagoId, aplicacion.CargoId });
        builder.Property(aplicacion => aplicacion.Valor).HasColumnType("numeric(14,2)");

        builder.HasOne<Cargo>()
            .WithMany()
            .HasForeignKey(aplicacion => aplicacion.CargoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(aplicacion => aplicacion.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(aplicacion => aplicacion.CargoId);
    }
}
