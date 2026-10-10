using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="UsuarioRol"/>.
/// Su responsabilidad es fijar la tabla, las longitudes, el documento único por club (RF-017), el
/// borrado en cascada desde el club, la referencia a quien aprobó el ingreso, el índice de la
/// sala de espera y, del jugador, su categoría actual, los datos de su retiro y el jugador desde
/// cuya ficha se agregó.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionUsuarioRol : IEntityTypeConfiguration<UsuarioRol>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.ToTable("UsuariosRol");
        builder.HasKey(integrante => integrante.Id);
        builder.Property(integrante => integrante.Id).ValueGeneratedNever();

        builder.Property(integrante => integrante.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(integrante => integrante.EstadoIngreso).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(integrante => integrante.Nombres).HasMaxLength(80).IsRequired();
        builder.Property(integrante => integrante.Apellidos).HasMaxLength(80).IsRequired();
        builder.Property(integrante => integrante.TipoDocumento).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(integrante => integrante.NumeroDocumento).HasMaxLength(20).IsRequired();
        builder.Property(integrante => integrante.AprobadoPorNombre).HasMaxLength(161);
        builder.Property(integrante => integrante.RolDeIngreso).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(integrante => integrante.Club)
            .WithMany()
            .HasForeignKey(integrante => integrante.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(integrante => integrante.Usuario)
            .WithMany()
            .HasForeignKey(integrante => integrante.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quien aprobó puede dejar la plataforma: la aprobación conserva su nombre copiado (§13).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(integrante => integrante.AprobadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(integrante => integrante.Activo).HasDefaultValue(true);
        builder.Property(integrante => integrante.RetiradoPorNombre).HasMaxLength(161);

        // Borrar una categoría no borra a nadie: solo se borra la que nunca se usó (RF-004a).
        builder.HasOne(integrante => integrante.Categoria)
            .WithMany()
            .HasForeignKey(integrante => integrante.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        // Quien retiró puede dejar la plataforma: el retiro conserva su nombre copiado (§13).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(integrante => integrante.RetiradoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // El jugador de origen puede dejar el club: su hermano se queda, sin decir de quién lo es.
        builder.HasOne(integrante => integrante.AgregadoDesde)
            .WithMany()
            .HasForeignKey(integrante => integrante.AgregadoDesdeUsuarioRolId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(integrante => new { integrante.ClubId, integrante.CategoriaId });
        builder.HasIndex(integrante => new { integrante.ClubId, integrante.EstadoIngreso });

        builder.HasIndex(integrante => new { integrante.ClubId, integrante.NumeroDocumento })
            .IsUnique()
            .HasDatabaseName(IndicesUnicos.DocumentoEnClub);

        builder.HasIndex(integrante => new { integrante.ClubId, integrante.UsuarioId });
        builder.HasIndex(integrante => integrante.NumeroDocumento);
    }
}
