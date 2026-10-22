using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="ConfiguracionPlataforma"/> (spec 017).
/// Su responsabilidad es fijar la tabla de una sola fila (<c>CHECK (Id = 1)</c>), el dinero como
/// <c>numeric(18,0)</c>, el rango de los días de gracia y el borrado a nulo del autor.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionConfiguracionPlataforma : IEntityTypeConfiguration<ConfiguracionPlataforma>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConfiguracionPlataforma> builder)
    {
        builder.ToTable("ConfiguracionPlataforma", tabla =>
        {
            tabla.HasCheckConstraint("CK_ConfiguracionPlataforma_UnaFila", "\"Id\" = 1");
            tabla.HasCheckConstraint("CK_ConfiguracionPlataforma_Dias", "\"DiasDeGracia\" BETWEEN 0 AND 30");
        });
        builder.HasKey(configuracion => configuracion.Id);
        builder.Property(configuracion => configuracion.Id).ValueGeneratedNever();

        builder.Property(configuracion => configuracion.PrecioJugador).HasColumnType("numeric(18,0)");
        builder.Property(configuracion => configuracion.PrecioEntrenador).HasColumnType("numeric(18,0)");
        builder.Property(configuracion => configuracion.PrecioDirectivo).HasColumnType("numeric(18,0)");
        builder.Property(configuracion => configuracion.PrecioPresidente).HasColumnType("numeric(18,0)");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(configuracion => configuracion.ActualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
