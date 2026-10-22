using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="PagoPlataforma"/> (spec 017).
/// Su responsabilidad es fijar la tabla, el origen como texto, el dinero como <c>numeric(18,0)</c>, el
/// índice único de un pago por cobro y el borrado: en cascada con el cobro y a nulo con quien lo marcó.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionPagoPlataforma : IEntityTypeConfiguration<PagoPlataforma>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PagoPlataforma> builder)
    {
        builder.ToTable("PagosPlataforma");
        builder.HasKey(pago => pago.Id);
        builder.Property(pago => pago.Id).ValueGeneratedNever();

        builder.Property(pago => pago.Origen).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(pago => pago.Valor).HasColumnType("numeric(18,0)");
        builder.Property(pago => pago.Referencia).HasMaxLength(60);
        builder.Property(pago => pago.TransaccionId).HasMaxLength(80);
        builder.Property(pago => pago.MedioDePago).HasMaxLength(40);

        builder.HasOne<CobroPlataforma>()
            .WithMany()
            .HasForeignKey(pago => pago.CobroId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(pago => pago.RegistradoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(pago => pago.CobroId).IsUnique();
    }
}
