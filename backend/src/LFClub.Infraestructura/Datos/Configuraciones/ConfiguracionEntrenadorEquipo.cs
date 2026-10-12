using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;
/// <summary>
/// Representa la configuración de persistencia de <see cref="EntrenadorEquipo"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta y los borrados en cascada desde el
/// club, la asignación y el equipo.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionEntrenadorEquipo : IEntityTypeConfiguration<EntrenadorEquipo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EntrenadorEquipo> builder)
    {
        builder.ToTable("EntrenadoresEquipo");
        builder.HasKey(fila => new { fila.AsignacionEntrenadorCategoriaId, fila.EquipoId });

        builder.HasOne(fila => fila.Club)
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Asignacion)
            .WithMany(asignacion => asignacion.Equipos)
            .HasForeignKey(fila => fila.AsignacionEntrenadorCategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Equipo)
            .WithMany(equipo => equipo.Entrenadores)
            .HasForeignKey(fila => fila.EquipoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}