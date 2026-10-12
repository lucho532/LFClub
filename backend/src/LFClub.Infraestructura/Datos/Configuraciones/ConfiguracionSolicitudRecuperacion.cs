using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="SolicitudRecuperacion"/>.
/// Su responsabilidad es fijar la tabla, el índice único del hash del token y el borrado en
/// cascada desde la cuenta.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionSolicitudRecuperacion : IEntityTypeConfiguration<SolicitudRecuperacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SolicitudRecuperacion> builder)
    {
        builder.ToTable("SolicitudesRecuperacion");
        builder.HasKey(solicitud => solicitud.Id);
        builder.Property(solicitud => solicitud.Id).ValueGeneratedNever();

        builder.Property(solicitud => solicitud.TokenHash).HasMaxLength(64).IsRequired();

        builder.HasOne(solicitud => solicitud.Usuario)
            .WithMany()
            .HasForeignKey(solicitud => solicitud.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(solicitud => solicitud.TokenHash).IsUnique();
    }
}
