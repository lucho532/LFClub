using LaPecosa.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaPecosa.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="CartaJugador"/> (spec 010).
/// Su responsabilidad es fijar la tabla, la clave compartida con el jugador, la posición como texto,
/// los nombres copiados de 161 caracteres, el borrado en cascada desde el jugador y el club, las
/// referencias a las cuentas que la cambiaron y las restricciones de comprobación: dorsal y
/// estadísticas de 1 a 99, las seis estadísticas todas nulas o todas con valor, y cada sello
/// completo o vacío.
/// No contiene reglas de negocio ni añade índices: la tabla se lee siempre por su clave primaria.
/// </summary>
public class ConfiguracionCartaJugador : IEntityTypeConfiguration<CartaJugador>
{
    private static readonly string[] Estadisticas = ["Ritmo", "Tiro", "Pase", "Regate", "Defensa", "Fisico"];

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CartaJugador> builder)
    {
        builder.ToTable("CartasJugador", tabla =>
        {
            tabla.HasCheckConstraint("CK_CartasJugador_Dorsal", "\"Dorsal\" IS NULL OR \"Dorsal\" BETWEEN 1 AND 99");
            tabla.HasCheckConstraint(
                "CK_CartasJugador_Estadisticas",
                "(" + string.Join(" AND ", Estadisticas.Select(nombre => $"\"{nombre}\" IS NULL")) + ") OR ("
                + string.Join(" AND ", Estadisticas.Select(nombre => $"\"{nombre}\" BETWEEN 1 AND 99")) + ")");
            tabla.HasCheckConstraint(
                "CK_CartasJugador_SelloEstadisticas",
                "(\"EstadisticasCambiadasEn\" IS NULL) = (\"EstadisticasCambiadasPorNombre\" IS NULL)");
            tabla.HasCheckConstraint(
                "CK_CartasJugador_SelloDorsal",
                "(\"DorsalCambiadoEn\" IS NULL) = (\"DorsalCambiadoPorNombre\" IS NULL)");
        });
        builder.HasKey(carta => carta.UsuarioRolId);
        builder.Property(carta => carta.UsuarioRolId).ValueGeneratedNever();
        builder.Property(carta => carta.Posicion).HasConversion<string>().HasMaxLength(20);
        builder.Property(carta => carta.EstadisticasCambiadasPorNombre).HasMaxLength(161);
        builder.Property(carta => carta.DorsalCambiadoPorNombre).HasMaxLength(161);
        builder.Ignore(carta => carta.TieneEstadisticas);

        builder.HasOne(carta => carta.UsuarioRol)
            .WithOne()
            .HasForeignKey<CartaJugador>(carta => carta.UsuarioRolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(carta => carta.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quien la cambió puede dejar la plataforma: se conserva su nombre copiado (§13).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(carta => carta.EstadisticasCambiadasPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(carta => carta.DorsalCambiadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
