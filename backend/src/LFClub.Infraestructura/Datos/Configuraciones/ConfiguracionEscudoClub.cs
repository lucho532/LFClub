using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="EscudoClub"/>.
/// Su responsabilidad es fijar la tabla, la clave primaria compartida con el club y el borrado en
/// cascada desde él.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionEscudoClub : IEntityTypeConfiguration<EscudoClub>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EscudoClub> builder)
    {
        builder.ToTable("EscudosClub");
        builder.HasKey(escudo => escudo.ClubId);
        builder.Property(escudo => escudo.ClubId).ValueGeneratedNever();

        builder.Property(escudo => escudo.Contenido).IsRequired();
        builder.Property(escudo => escudo.TipoContenido).HasMaxLength(30).IsRequired();

        builder.HasOne(escudo => escudo.Club)
            .WithOne()
            .HasForeignKey<EscudoClub>(escudo => escudo.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
