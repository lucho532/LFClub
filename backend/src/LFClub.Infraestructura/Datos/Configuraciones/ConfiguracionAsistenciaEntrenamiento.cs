using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="AsistenciaEntrenamiento"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta, el borrado en cascada desde el club y
/// el entrenamiento, que ni el jugador ni la categoría con asistencia se puedan borrar, y el
/// índice por jugador de las estadísticas.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionAsistenciaEntrenamiento : IEntityTypeConfiguration<AsistenciaEntrenamiento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AsistenciaEntrenamiento> builder)
    {
        builder.ToTable("AsistenciasEntrenamiento");
        builder.HasKey(fila => new { fila.EntrenamientoId, fila.UsuarioRolId });

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Entrenamiento)
            .WithMany()
            .HasForeignKey(fila => fila.EntrenamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Como Restrict, pero comprobado al final de la sentencia: borrar el club en cascada no falla.
        builder.HasOne(fila => fila.UsuarioRol)
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioRolId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(fila => fila.Categoria)
            .WithMany()
            .HasForeignKey(fila => fila.CategoriaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(fila => fila.UsuarioRolId);
    }
}
