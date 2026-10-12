using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;
/// <summary>
/// Representa la configuración de persistencia de <see cref="AsignacionEntrenadorCategoria"/>.
/// Su responsabilidad es fijar la tabla, una sola fila por pareja categoría e integrante (RF-020),
/// el índice de "las categorías de este entrenador" y los borrados en cascada.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionAsignacionEntrenadorCategoria : IEntityTypeConfiguration<AsignacionEntrenadorCategoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AsignacionEntrenadorCategoria> builder)
    {
        builder.ToTable("AsignacionesEntrenadorCategoria");
        builder.HasKey(asignacion => asignacion.Id);
        builder.Property(asignacion => asignacion.Id).ValueGeneratedNever();

        builder.HasOne(asignacion => asignacion.Club)
            .WithMany()
            .HasForeignKey(asignacion => asignacion.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(asignacion => asignacion.Categoria)
            .WithMany(categoria => categoria.Asignaciones)
            .HasForeignKey(asignacion => asignacion.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(asignacion => asignacion.UsuarioRol)
            .WithMany()
            .HasForeignKey(asignacion => asignacion.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(asignacion => new { asignacion.CategoriaId, asignacion.UsuarioRolId }).IsUnique();
        builder.HasIndex(asignacion => new { asignacion.UsuarioRolId, asignacion.Activa });
    }
}