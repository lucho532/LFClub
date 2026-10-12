using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Club"/>.
/// Su responsabilidad es fijar la tabla, las longitudes y el índice único del nombre normalizado.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionClub : IEntityTypeConfiguration<Club>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.ToTable("Clubes");
        builder.HasKey(club => club.Id);
        builder.Property(club => club.Id).ValueGeneratedNever();

        builder.Property(club => club.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(club => club.NombreNormalizado).HasMaxLength(120).IsRequired();
        builder.Property(club => club.Sede).HasMaxLength(120);
        builder.Property(club => club.Direccion).HasMaxLength(200);
        builder.Property(club => club.CorreoContacto).HasMaxLength(254);
        builder.Property(club => club.TelefonoContacto).HasMaxLength(20);
        builder.Property(club => club.ColorPrincipal).HasMaxLength(7);
        builder.Property(club => club.ColorAcento).HasMaxLength(7);
        builder.Property(club => club.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(club => club.NombreNormalizado).IsUnique().HasDatabaseName(IndicesUnicos.NombreDeClub);
    }
}
