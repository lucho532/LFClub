using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="EntrenamientoCategoria"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta, el borrado en cascada desde el club y
/// el entrenamiento, y que una categoría con entrenamientos no se pueda borrar.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionEntrenamientoCategoria : IEntityTypeConfiguration<EntrenamientoCategoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EntrenamientoCategoria> builder)
    {
        builder.ToTable("EntrenamientosCategoria");
        builder.HasKey(fila => new { fila.EntrenamientoId, fila.CategoriaId });

        builder.HasOne(fila => fila.Club)
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Entrenamiento)
            .WithMany(entrenamiento => entrenamiento.Categorias)
            .HasForeignKey(fila => fila.EntrenamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Como Restrict, pero comprobado al final de la sentencia: borrar el club en cascada no falla.
        builder.HasOne(fila => fila.Categoria)
            .WithMany()
            .HasForeignKey(fila => fila.CategoriaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(fila => fila.CategoriaId);
    }
}
