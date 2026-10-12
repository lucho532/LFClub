using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="ComunicadoDestino"/> (spec 011).
/// Su responsabilidad es fijar la tabla, los nombres copiados, el borrado en cascada desde el club
/// y el comunicado y las referencias a la categoría y al equipo, que pasan a nulo si se borran.
/// No contiene reglas de negocio ni comprueba la coherencia entre el alcance y las filas.
/// </summary>
public class ConfiguracionComunicadoDestino : IEntityTypeConfiguration<ComunicadoDestino>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ComunicadoDestino> builder)
    {
        builder.ToTable("ComunicadosDestino");
        builder.HasKey(destino => destino.Id);
        builder.Property(destino => destino.Id).ValueGeneratedNever();

        builder.Property(destino => destino.CategoriaNombre).HasMaxLength(Comunicado.LongitudMaximaDelNombre).IsRequired();
        builder.Property(destino => destino.EquipoNombre).HasMaxLength(Comunicado.LongitudMaximaDelNombre);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(destino => destino.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(destino => destino.Comunicado)
            .WithMany(comunicado => comunicado.Destinos)
            .HasForeignKey(destino => destino.ComunicadoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(destino => destino.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Equipo>()
            .WithMany()
            .HasForeignKey(destino => destino.EquipoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(destino => destino.ComunicadoId);
    }
}
