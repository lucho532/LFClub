using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="EntrenamientoEquipo"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta, el borrado en cascada desde el club y
/// el entrenamiento, que un equipo con entrenamientos no se pueda borrar y el índice por equipo.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionEntrenamientoEquipo : IEntityTypeConfiguration<EntrenamientoEquipo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EntrenamientoEquipo> builder)
    {
        builder.ToTable("EntrenamientosEquipo");
        builder.HasKey(fila => new { fila.EntrenamientoId, fila.EquipoId });

        builder.HasOne(fila => fila.Club)
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Entrenamiento)
            .WithMany(entrenamiento => entrenamiento.Equipos)
            .HasForeignKey(fila => fila.EntrenamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Como Restrict, pero comprobado al final de la sentencia: borrar el club en cascada no falla.
        builder.HasOne(fila => fila.Equipo)
            .WithMany()
            .HasForeignKey(fila => fila.EquipoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(fila => fila.EquipoId);
    }
}
