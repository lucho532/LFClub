using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="AvisoDeActividad"/> (spec 012).
/// Su responsabilidad es fijar la tabla, la longitud del texto y que no esté vacío, el hecho y el
/// tipo de actividad como texto, los índices por club y por actividad y el borrado en cascada desde
/// el club. La actividad no tiene foránea (research §2).
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionAvisoDeActividad : IEntityTypeConfiguration<AvisoDeActividad>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AvisoDeActividad> builder)
    {
        builder.ToTable("AvisosDeActividad", tabla =>
            tabla.HasCheckConstraint("CK_AvisosDeActividad_Texto", "char_length(\"Texto\") > 0"));
        builder.HasKey(aviso => aviso.Id);
        builder.Property(aviso => aviso.Id).ValueGeneratedNever();

        builder.Property(aviso => aviso.Hecho).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(aviso => aviso.ActividadTipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(aviso => aviso.Texto).HasMaxLength(AvisoDeActividad.LongitudMaximaDelTexto).IsRequired();

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(aviso => aviso.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(aviso => new { aviso.ClubId, aviso.Id });
        builder.HasIndex(aviso => new { aviso.ClubId, aviso.ActividadTipo, aviso.ActividadId });
    }
}
