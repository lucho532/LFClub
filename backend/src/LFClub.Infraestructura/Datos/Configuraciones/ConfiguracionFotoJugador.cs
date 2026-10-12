using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="FotoJugador"/>.
/// Su responsabilidad es fijar la tabla, la clave primaria compartida con el jugador y el borrado
/// en cascada desde el jugador y desde el club (§7.4).
/// No contiene reglas de negocio ni añade índices: la tabla se lee siempre por su clave primaria.
/// </summary>
public class ConfiguracionFotoJugador : IEntityTypeConfiguration<FotoJugador>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FotoJugador> builder)
    {
        builder.ToTable("FotosJugador");
        builder.HasKey(foto => foto.UsuarioRolId);
        builder.Property(foto => foto.UsuarioRolId).ValueGeneratedNever();

        builder.Property(foto => foto.Contenido).IsRequired();
        builder.Property(foto => foto.TipoContenido).HasMaxLength(20).IsRequired();

        builder.HasOne(foto => foto.UsuarioRol)
            .WithOne()
            .HasForeignKey<FotoJugador>(foto => foto.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(foto => foto.Club)
            .WithMany()
            .HasForeignKey(foto => foto.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
