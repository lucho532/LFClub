using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="ComunicadoDestinatario"/> (spec 011).
/// Su responsabilidad es fijar la tabla, la clave compuesta comunicado y jugador, el borrado en
/// cascada desde el club, el comunicado, el jugador y la cuenta, el índice de la lista de la cuenta
/// y el índice parcial de los sin leer, que es el del contador.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionComunicadoDestinatario : IEntityTypeConfiguration<ComunicadoDestinatario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ComunicadoDestinatario> builder)
    {
        builder.ToTable("ComunicadosDestinatario");
        builder.HasKey(fila => new { fila.ComunicadoId, fila.UsuarioRolId });

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Comunicado)
            .WithMany(comunicado => comunicado.Destinatarios)
            .HasForeignKey(fila => fila.ComunicadoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.UsuarioRol)
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(fila => fila.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fila => new { fila.UsuarioId, fila.ClubId, fila.ComunicadoId });
        builder.HasIndex(fila => new { fila.UsuarioId, fila.ClubId })
            .HasFilter("\"LeidoEn\" IS NULL")
            .HasDatabaseName("IX_ComunicadosDestinatario_SinLeer");
    }
}
