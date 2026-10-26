using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="ConfiguracionPlataforma"/> (spec 017).
/// Su responsabilidad es fijar la tabla de una sola fila (<c>CHECK (Id = 1)</c>), los rangos de la tarifa como
/// <c>jsonb</c>, el rango de los días de gracia y el borrado a nulo del autor.
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

        builder.Property(configuracion => configuracion.Rangos)
            .HasColumnType("jsonb")
            .HasConversion(
                rangos => JsonSerializer.Serialize(rangos, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<RangoTarifa>>(json, (JsonSerializerOptions?)null) ?? new List<RangoTarifa>(),
                new ValueComparer<List<RangoTarifa>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    rangos => rangos.Aggregate(0, (hash, rango) => HashCode.Combine(hash, rango)),
                    rangos => rangos.ToList()))
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(configuracion => configuracion.ActualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
