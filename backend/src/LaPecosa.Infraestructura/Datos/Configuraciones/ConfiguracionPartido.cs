using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Partido"/>.
/// Su responsabilidad es fijar la tabla, las longitudes, las enumeraciones como texto, las
/// restricciones del marcador y del torneo, el índice del calendario, el borrado en cascada desde
/// el club y las referencias a quien lo creó y a quien cambió su resultado.
/// No contiene reglas de negocio: las restricciones solo impiden que la base guarde un partido
/// imposible.
/// </summary>
public class ConfiguracionPartido : IEntityTypeConfiguration<Partido>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Partido> builder)
    {
        builder.ToTable("Partidos", tabla =>
        {
            // El marcador existe entero, y solo en un partido jugado (RF-019).
            tabla.HasCheckConstraint(
                "CK_Partidos_Marcador",
                "(\"Estado\" = 'JUGADO' AND \"GolesClub\" IS NOT NULL AND \"GolesRival\" IS NOT NULL"
                + " AND \"GolesClub\" BETWEEN 0 AND 99 AND \"GolesRival\" BETWEEN 0 AND 99)"
                + " OR (\"Estado\" <> 'JUGADO' AND \"GolesClub\" IS NULL AND \"GolesRival\" IS NULL)");
            tabla.HasCheckConstraint(
                "CK_Partidos_Torneo", "\"Tipo\" <> 'TORNEO' OR \"NombreTorneo\" IS NOT NULL");
        });
        builder.HasKey(partido => partido.Id);
        builder.Property(partido => partido.Id).ValueGeneratedNever();

        builder.Property(partido => partido.Rival).HasMaxLength(Partido.LongitudMaximaDeTexto).IsRequired();
        builder.Property(partido => partido.Lugar).HasMaxLength(Partido.LongitudMaximaDeTexto).IsRequired();
        builder.Property(partido => partido.NombreTorneo).HasMaxLength(Partido.LongitudMaximaDeTexto);
        builder.Property(partido => partido.Condicion).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(partido => partido.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(partido => partido.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(partido => partido.ResultadoCambiadoPorNombre).HasMaxLength(161);

        builder.HasOne(partido => partido.Club)
            .WithMany()
            .HasForeignKey(partido => partido.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quien lo creó puede dejar el club: el partido se queda.
        builder.HasOne<UsuarioRol>()
            .WithMany()
            .HasForeignKey(partido => partido.CreadoPorUsuarioRolId)
            .OnDelete(DeleteBehavior.SetNull);

        // Quien cambió el resultado puede dejar la plataforma: se conserva su nombre copiado (§13).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(partido => partido.ResultadoCambiadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(partido => new { partido.ClubId, partido.Fecha, partido.Hora });
    }
}
