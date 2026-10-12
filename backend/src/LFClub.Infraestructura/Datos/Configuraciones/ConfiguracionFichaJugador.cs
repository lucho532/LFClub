using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="FichaJugador"/>.
/// Su responsabilidad es fijar la tabla, la clave primaria compartida con el jugador, las
/// longitudes, el borrado en cascada desde el jugador y desde el club (RF-037) y la referencia a
/// quien hizo el último cambio.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionFichaJugador : IEntityTypeConfiguration<FichaJugador>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FichaJugador> builder)
    {
        builder.ToTable("FichasJugador");
        builder.HasKey(ficha => ficha.UsuarioRolId);
        builder.Property(ficha => ficha.UsuarioRolId).ValueGeneratedNever();

        builder.Property(ficha => ficha.EmergenciaNombre).HasMaxLength(160);
        builder.Property(ficha => ficha.EmergenciaParentesco).HasMaxLength(40);
        builder.Property(ficha => ficha.EmergenciaCelular).HasMaxLength(20);
        builder.Property(ficha => ficha.EntidadSalud).HasMaxLength(120);
        builder.Property(ficha => ficha.LugarAtencion).HasMaxLength(200);
        builder.Property(ficha => ficha.GrupoSanguineo).HasConversion<string>().HasMaxLength(20);
        builder.Property(ficha => ficha.Alergias).HasMaxLength(1000);
        builder.Property(ficha => ficha.Enfermedades).HasMaxLength(1000);
        builder.Property(ficha => ficha.Medicamentos).HasMaxLength(1000);
        builder.Property(ficha => ficha.Observaciones).HasMaxLength(1000);
        builder.Property(ficha => ficha.UltimoCambioPorNombre).HasMaxLength(161).IsRequired();

        builder.HasOne(ficha => ficha.UsuarioRol)
            .WithOne()
            .HasForeignKey<FichaJugador>(ficha => ficha.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ficha => ficha.Club)
            .WithMany()
            .HasForeignKey(ficha => ficha.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quien cambió puede dejar la plataforma: la ficha conserva su nombre copiado (§13).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(ficha => ficha.UltimoCambioPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
