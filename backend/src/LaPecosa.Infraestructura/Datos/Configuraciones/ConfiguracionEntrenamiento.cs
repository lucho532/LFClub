using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Entrenamiento"/>.
/// Su responsabilidad es fijar la tabla, las longitudes, el estado como texto, la hora de fin
/// posterior a la de inicio, el índice del calendario, el borrado en cascada desde el club y la
/// referencia a quien lo programó.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionEntrenamiento : IEntityTypeConfiguration<Entrenamiento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Entrenamiento> builder)
    {
        builder.ToTable("Entrenamientos", tabla =>
            tabla.HasCheckConstraint("CK_Entrenamientos_HoraFin", "\"HoraFin\" > \"HoraInicio\""));
        builder.HasKey(entrenamiento => entrenamiento.Id);
        builder.Property(entrenamiento => entrenamiento.Id).ValueGeneratedNever();

        builder.Property(entrenamiento => entrenamiento.Lugar).HasMaxLength(Entrenamiento.LongitudMaximaDelLugar).IsRequired();
        builder.Property(entrenamiento => entrenamiento.Nota).HasMaxLength(Entrenamiento.LongitudMaximaDeLaNota);
        builder.Property(entrenamiento => entrenamiento.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(entrenamiento => entrenamiento.Club)
            .WithMany()
            .HasForeignKey(entrenamiento => entrenamiento.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quien lo programó puede dejar el club: el entrenamiento se queda.
        builder.HasOne<UsuarioRol>()
            .WithMany()
            .HasForeignKey(entrenamiento => entrenamiento.CreadoPorUsuarioRolId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(entrenamiento => new { entrenamiento.ClubId, entrenamiento.Fecha, entrenamiento.HoraInicio });
    }
}
