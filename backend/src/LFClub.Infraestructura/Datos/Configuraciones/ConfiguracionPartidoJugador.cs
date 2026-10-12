using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="PartidoJugador"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta, la respuesta como texto, que los goles
/// no sean negativos ni existan sin haber jugado, el borrado en cascada desde el club y el partido,
/// que ni el jugador ni la categoría convocados se puedan borrar, y el índice por jugador de las
/// estadísticas; y, desde la spec 010, que la indicación de cada fila (origen, fecha y nombre) esté
/// completa o vacía, con la cuenta que la hizo a nulo si se elimina.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionPartidoJugador : IEntityTypeConfiguration<PartidoJugador>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PartidoJugador> builder)
    {
        builder.ToTable("PartidosJugador", tabla =>
        {
            tabla.HasCheckConstraint("CK_PartidosJugador_Goles", "\"Goles\" >= 0 AND (\"Jugo\" OR \"Goles\" = 0)");

            // La indicación existe entera o no existe (spec 010).
            tabla.HasCheckConstraint(
                "CK_PartidosJugador_Indicacion",
                "(\"IndicadoPor\" IS NULL) = (\"IndicadoEn\" IS NULL)"
                + " AND (\"IndicadoPor\" IS NULL) = (\"IndicadoPorNombre\" IS NULL)");
        });
        builder.HasKey(fila => new { fila.PartidoId, fila.UsuarioRolId });
        builder.Property(fila => fila.Respuesta).HasConversion<string>().HasMaxLength(20);
        builder.Property(fila => fila.IndicadoPor).HasConversion<string>().HasMaxLength(20);
        builder.Property(fila => fila.IndicadoPorNombre).HasMaxLength(161);

        // Quien indicó puede dejar la plataforma: se conserva su nombre copiado (§13).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(fila => fila.IndicadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Partido)
            .WithMany(partido => partido.Jugadores)
            .HasForeignKey(fila => fila.PartidoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Como Restrict, pero comprobado al final de la sentencia: borrar el club en cascada no falla.
        builder.HasOne(fila => fila.UsuarioRol)
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioRolId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(fila => fila.Categoria)
            .WithMany()
            .HasForeignKey(fila => fila.CategoriaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(fila => fila.UsuarioRolId);
    }
}
