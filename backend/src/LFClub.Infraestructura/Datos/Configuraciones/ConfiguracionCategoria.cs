using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;
/// <summary>
/// Representa la configuración de persistencia de <see cref="Categoria"/>.
/// Su responsabilidad es fijar la tabla, la categoría única por club y año (RF-003) y el borrado
/// en cascada desde el club.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionCategoria : IEntityTypeConfiguration<Categoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");
        builder.HasKey(categoria => categoria.Id);
        builder.Property(categoria => categoria.Id).ValueGeneratedNever();

        builder.HasOne(categoria => categoria.Club)
            .WithMany()
            .HasForeignKey(categoria => categoria.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(categoria => new { categoria.ClubId, categoria.Anio })
            .IsUnique()
            .HasDatabaseName(IndicesUnicos.CategoriaEnClub);
    }
}