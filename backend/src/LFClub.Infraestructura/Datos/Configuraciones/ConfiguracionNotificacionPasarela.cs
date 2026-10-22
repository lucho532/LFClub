using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="NotificacionPasarela"/> (spec 017).
/// Su responsabilidad es fijar la tabla, el resultado como texto, el índice único de transacción y
/// estado (solo cuando hay transacción), que hace idempotente la notificación, y el borrado en cascada
/// con el cobro asociado.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionNotificacionPasarela : IEntityTypeConfiguration<NotificacionPasarela>
{
    /// <summary>Nombre del índice único de transacción y estado.</summary>
    public const string IndiceTransaccionYEstado = "UX_NotificacionesPasarela_Transaccion_Estado";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NotificacionPasarela> builder)
    {
        builder.ToTable("NotificacionesPasarela");
        builder.HasKey(notificacion => notificacion.Id);
        builder.Property(notificacion => notificacion.Id).ValueGeneratedNever();

        builder.Property(notificacion => notificacion.Resultado).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(notificacion => notificacion.TransaccionId).HasMaxLength(80);
        builder.Property(notificacion => notificacion.EstadoPasarela).HasMaxLength(30);
        builder.Property(notificacion => notificacion.Referencia).HasMaxLength(60);
        builder.Property(notificacion => notificacion.Valor).HasColumnType("numeric(18,0)");

        builder.HasOne<CobroPlataforma>()
            .WithMany()
            .HasForeignKey(notificacion => notificacion.CobroId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(notificacion => new { notificacion.TransaccionId, notificacion.EstadoPasarela })
            .IsUnique()
            .HasFilter("\"TransaccionId\" IS NOT NULL")
            .HasDatabaseName(IndiceTransaccionYEstado);
        builder.HasIndex(notificacion => notificacion.RecibidaEn);
    }
}
