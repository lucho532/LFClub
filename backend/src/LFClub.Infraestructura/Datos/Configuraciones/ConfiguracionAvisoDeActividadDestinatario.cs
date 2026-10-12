using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="AvisoDeActividadDestinatario"/> (spec
/// 012).
/// Su responsabilidad es fijar la tabla, la clave compuesta aviso y jugador, el borrado en cascada
/// desde el club, el aviso, el jugador y la cuenta, el índice de la lista de la cuenta y el índice
/// parcial de los sin leer, que es el del contador.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionAvisoDeActividadDestinatario : IEntityTypeConfiguration<AvisoDeActividadDestinatario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AvisoDeActividadDestinatario> builder)
    {
        builder.ToTable("AvisosDeActividadDestinatario");
        builder.HasKey(fila => new { fila.AvisoId, fila.UsuarioRolId });

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Aviso)
            .WithMany(aviso => aviso.Destinatarios)
            .HasForeignKey(fila => fila.AvisoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.UsuarioRol)
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fila => new { fila.UsuarioId, fila.ClubId, fila.AvisoId });
        builder.HasIndex(fila => new { fila.UsuarioId, fila.ClubId })
            .HasFilter("\"LeidoEn\" IS NULL")
            .HasDatabaseName("IX_AvisosDeActividadDestinatario_SinLeer");
    }
}
