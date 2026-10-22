using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="IntentoPagoPlataforma"/> (spec 017).
/// Su responsabilidad es fijar la tabla, la referencia única que viaja a la pasarela, el dinero como
/// <c>numeric(18,0)</c> y el borrado: en cascada con el cobro y a nulo con quien lo inició.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionIntentoPagoPlataforma : IEntityTypeConfiguration<IntentoPagoPlataforma>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IntentoPagoPlataforma> builder)
    {
        builder.ToTable("IntentosPagoPlataforma");
        builder.HasKey(intento => intento.Id);
        builder.Property(intento => intento.Id).ValueGeneratedNever();

        builder.Property(intento => intento.Referencia).HasMaxLength(60).IsRequired();
        builder.Property(intento => intento.Valor).HasColumnType("numeric(18,0)");

        builder.HasOne<CobroPlataforma>()
            .WithMany()
            .HasForeignKey(intento => intento.CobroId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(intento => intento.CreadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(intento => intento.Referencia).IsUnique();
        builder.HasIndex(intento => new { intento.CobroId, intento.CreadoEn });
    }
}
