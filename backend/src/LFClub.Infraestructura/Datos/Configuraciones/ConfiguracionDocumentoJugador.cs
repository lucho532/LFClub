using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="DocumentoJugador"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta de jugador y documento pedido, que
/// garantiza un solo archivo por documento (RF-029), y el borrado en cascada desde el jugador y
/// desde el club (RF-037).
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionDocumentoJugador : IEntityTypeConfiguration<DocumentoJugador>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DocumentoJugador> builder)
    {
        builder.ToTable("DocumentosJugador");
        builder.HasKey(archivo => new { archivo.UsuarioRolId, archivo.Documento });

        builder.Property(archivo => archivo.Documento).HasConversion<string>().HasMaxLength(30);
        builder.Property(archivo => archivo.Contenido).IsRequired();
        builder.Property(archivo => archivo.TipoContenido).HasMaxLength(30).IsRequired();

        builder.HasOne(archivo => archivo.UsuarioRol)
            .WithMany()
            .HasForeignKey(archivo => archivo.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(archivo => archivo.Club)
            .WithMany()
            .HasForeignKey(archivo => archivo.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
