using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="FotoPerfil"/>.
/// Su responsabilidad es fijar la tabla, la clave primaria compartida con la cuenta y el borrado
/// en cascada desde ella.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionFotoPerfil : IEntityTypeConfiguration<FotoPerfil>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FotoPerfil> builder)
    {
        builder.ToTable("FotosPerfil");
        builder.HasKey(foto => foto.UsuarioId);
        builder.Property(foto => foto.UsuarioId).ValueGeneratedNever();

        builder.Property(foto => foto.Contenido).IsRequired();
        builder.Property(foto => foto.TipoContenido).HasMaxLength(30).IsRequired();

        builder.HasOne(foto => foto.Usuario)
            .WithOne()
            .HasForeignKey<FotoPerfil>(foto => foto.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
