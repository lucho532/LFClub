using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;
/// <summary>
/// Representa la configuración de persistencia de <see cref="Equipo"/>.
/// Su responsabilidad es fijar la tabla, las longitudes, el nombre único entre los equipos activos
/// de una categoría (RF-023) y los borrados en cascada desde el club y desde la categoría.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionEquipo : IEntityTypeConfiguration<Equipo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Equipo> builder)
    {
        builder.ToTable("Equipos");
        builder.HasKey(equipo => equipo.Id);
        builder.Property(equipo => equipo.Id).ValueGeneratedNever();

        builder.Property(equipo => equipo.Nombre).HasMaxLength(Equipo.LongitudMaximaDelNombre).IsRequired();
        builder.Property(equipo => equipo.NombreNormalizado)
            .HasMaxLength(Equipo.LongitudMaximaDelNombre).IsRequired();

        builder.HasOne(equipo => equipo.Club)
            .WithMany()
            .HasForeignKey(equipo => equipo.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(equipo => equipo.Categoria)
            .WithMany(categoria => categoria.Equipos)
            .HasForeignKey(equipo => equipo.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Parcial: el nombre de un equipo desactivado queda libre (research §8).
        builder.HasIndex(equipo => new { equipo.CategoriaId, equipo.NombreNormalizado })
            .IsUnique()
            .HasFilter("\"Activo\"")
            .HasDatabaseName(IndicesUnicos.EquipoEnCategoria);
    }
}