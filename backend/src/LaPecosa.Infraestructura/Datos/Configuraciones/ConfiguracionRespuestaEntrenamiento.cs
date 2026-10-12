using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="RespuestaEntrenamiento"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta, la respuesta como texto y el borrado en
/// cascada desde el club, el entrenamiento y el jugador.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionRespuestaEntrenamiento : IEntityTypeConfiguration<RespuestaEntrenamiento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RespuestaEntrenamiento> builder)
    {
        builder.ToTable("RespuestasEntrenamiento");
        builder.HasKey(fila => new { fila.EntrenamientoId, fila.UsuarioRolId });
        builder.Property(fila => fila.Respuesta).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Entrenamiento)
            .WithMany()
            .HasForeignKey(fila => fila.EntrenamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.UsuarioRol)
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
