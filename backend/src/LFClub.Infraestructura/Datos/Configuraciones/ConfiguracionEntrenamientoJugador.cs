using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="EntrenamientoJugador"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta, los borrados en cascada desde el club,
/// el entrenamiento y el jugador, el origen guardado como texto, el equipo de origen que pasa a nulo
/// si el equipo se borra, el índice por jugador y la restricción de que un añadido no lleva equipo
/// de origen.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionEntrenamientoJugador : IEntityTypeConfiguration<EntrenamientoJugador>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EntrenamientoJugador> builder)
    {
        builder.ToTable(
            "EntrenamientosJugador",
            tabla => tabla.HasCheckConstraint(
                "CK_EntrenamientosJugador_OrigenDelAnadido",
                "\"Origen\" <> 'ANADIDO' OR \"EquipoDeOrigenId\" IS NULL"));
        builder.HasKey(fila => new { fila.EntrenamientoId, fila.UsuarioRolId });

        builder.Property(fila => fila.Origen).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(fila => fila.Club)
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Entrenamiento)
            .WithMany(entrenamiento => entrenamiento.Jugadores)
            .HasForeignKey(fila => fila.EntrenamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.UsuarioRol)
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.EquipoDeOrigen)
            .WithMany()
            .HasForeignKey(fila => fila.EquipoDeOrigenId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(fila => fila.UsuarioRolId);
    }
}
