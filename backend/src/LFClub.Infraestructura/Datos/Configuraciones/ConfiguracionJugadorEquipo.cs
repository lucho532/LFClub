using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;
/// <summary>
/// Representa la configuración de persistencia de <see cref="JugadorEquipo"/>.
/// Su responsabilidad es fijar la tabla, la clave compuesta y los borrados en cascada desde el
/// club, el jugador y el equipo.
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionJugadorEquipo : IEntityTypeConfiguration<JugadorEquipo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<JugadorEquipo> builder)
    {
        builder.ToTable("JugadoresEquipo");
        builder.HasKey(fila => new { fila.UsuarioRolId, fila.EquipoId });

        builder.HasOne(fila => fila.Club)
            .WithMany()
            .HasForeignKey(fila => fila.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.UsuarioRol)
            .WithMany(integrante => integrante.Equipos)
            .HasForeignKey(fila => fila.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fila => fila.Equipo)
            .WithMany(equipo => equipo.Jugadores)
            .HasForeignKey(fila => fila.EquipoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}