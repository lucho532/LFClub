using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="PartidoCategoria"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta, el borrado en cascada desde el club y
/// el partido, y que una categoría con partidos no se pueda borrar.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionPartidoCategoria : IEntityTypeConfiguration<PartidoCategoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PartidoCategoria> builder)
    {
        builder.ToTable("PartidosCategoria");
        builder.HasKey(fila => new { fila.PartidoId, fila.CategoriaId });

        builder.HasOne(fila => fila.Club)
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Partido)
            .WithMany(partido => partido.Categorias)
            .HasForeignKey(fila => fila.PartidoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Como Restrict, pero comprobado al final de la sentencia: borrar el club en cascada no falla.
        builder.HasOne(fila => fila.Categoria)
            .WithMany()
            .HasForeignKey(fila => fila.CategoriaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(fila => fila.CategoriaId);
    }
}
