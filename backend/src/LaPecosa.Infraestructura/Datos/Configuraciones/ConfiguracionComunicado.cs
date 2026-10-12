using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Comunicado"/> (spec 011).
/// Su responsabilidad es fijar la tabla, las longitudes, el alcance y el rol como texto, el retiro
/// completo o vacío, el título y el texto no vacíos, los índices por club y el único por clave de
/// publicación, el borrado en cascada desde el club y las referencias a quien lo publicó y lo
/// retiró, que pasan a nulo si ese integrante se borra.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionComunicado : IEntityTypeConfiguration<Comunicado>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Comunicado> builder)
    {
        builder.ToTable("Comunicados", tabla =>
        {
            tabla.HasCheckConstraint(
                "CK_Comunicados_Retiro", "(\"RetiradoEn\" IS NULL) = (\"RetiradoPorNombre\" IS NULL)");
            tabla.HasCheckConstraint("CK_Comunicados_Titulo", "char_length(\"Titulo\") > 0");
            tabla.HasCheckConstraint("CK_Comunicados_Texto", "char_length(\"Texto\") > 0");
        });
        builder.HasKey(comunicado => comunicado.Id);
        builder.Property(comunicado => comunicado.Id).ValueGeneratedNever();

        builder.Property(comunicado => comunicado.Titulo).HasMaxLength(Comunicado.LongitudMaximaDelTitulo).IsRequired();
        builder.Property(comunicado => comunicado.Texto).HasMaxLength(Comunicado.LongitudMaximaDelTexto).IsRequired();
        builder.Property(comunicado => comunicado.Alcance).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(comunicado => comunicado.PublicadoPorRol).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(comunicado => comunicado.PublicadoPorNombre)
            .HasMaxLength(Comunicado.LongitudMaximaDelNombre).IsRequired();
        builder.Property(comunicado => comunicado.RetiradoPorNombre).HasMaxLength(Comunicado.LongitudMaximaDelNombre);

        builder.HasOne(comunicado => comunicado.Club)
            .WithMany()
            .HasForeignKey(comunicado => comunicado.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quien lo publicó o lo retiró puede dejar el club: el comunicado se queda con su nombre copiado.
        builder.HasOne<UsuarioRol>()
            .WithMany()
            .HasForeignKey(comunicado => comunicado.PublicadoPorUsuarioRolId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<UsuarioRol>()
            .WithMany()
            .HasForeignKey(comunicado => comunicado.RetiradoPorUsuarioRolId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(comunicado => new { comunicado.ClubId, comunicado.Id });
        builder.HasIndex(comunicado => new { comunicado.ClubId, comunicado.Clave }).IsUnique();
    }
}
