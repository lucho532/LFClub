using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Usuario"/>.
/// Su responsabilidad es fijar la tabla, las longitudes, el índice único del correo y el índice
/// único parcial que impide una segunda cuenta DESARROLLADOR (RF-001).
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(usuario => usuario.Id);
        builder.Property(usuario => usuario.Id).ValueGeneratedNever();

        builder.Property(usuario => usuario.Correo).HasMaxLength(254).IsRequired();
        builder.Property(usuario => usuario.CorreoNormalizado).HasMaxLength(254).IsRequired();
        builder.Property(usuario => usuario.Celular).HasMaxLength(20);
        builder.Property(usuario => usuario.NombreResponsable).HasMaxLength(160);
        builder.Ignore(usuario => usuario.PuedeIniciarSesion);

        builder.HasIndex(usuario => usuario.CorreoNormalizado)
            .IsUnique()
            .HasDatabaseName(IndicesUnicos.CorreoDeUsuario);

        builder.HasIndex(usuario => usuario.EsDesarrollador)
            .IsUnique()
            .HasFilter("\"EsDesarrollador\" = TRUE")
            .HasDatabaseName(IndicesUnicos.UnicoDesarrollador);
    }
}
