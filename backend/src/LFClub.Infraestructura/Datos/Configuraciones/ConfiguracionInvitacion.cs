using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Invitacion"/>.
/// Su responsabilidad es fijar la tabla, las longitudes, el índice único del hash del token y el
/// borrado en cascada desde el club.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionInvitacion : IEntityTypeConfiguration<Invitacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invitacion> builder)
    {
        builder.ToTable("Invitaciones");
        builder.HasKey(invitacion => invitacion.Id);
        builder.Property(invitacion => invitacion.Id).ValueGeneratedNever();

        builder.Property(invitacion => invitacion.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(invitacion => invitacion.Correo).HasMaxLength(254).IsRequired();
        builder.Property(invitacion => invitacion.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(invitacion => invitacion.EstadoEnvio).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(invitacion => invitacion.Club)
            .WithMany()
            .HasForeignKey(invitacion => invitacion.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(invitacion => invitacion.TokenHash).IsUnique();
        builder.HasIndex(invitacion => new { invitacion.ClubId, invitacion.Correo });
    }
}
