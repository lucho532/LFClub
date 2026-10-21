using LFClub.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LFClub.Infraestructura.Datos.Configuraciones;

/// <summary>
/// Representa la configuración de persistencia de <see cref="Pago"/> (spec 015).
/// Su responsabilidad es fijar la tabla, las longitudes, las enumeraciones como texto, el dinero como
/// <c>numeric(14,2)</c>, los <c>CHECK</c> baratos, el recibo único por club, los índices de consulta y
/// el borrado: en cascada con el club y a nulo con el jugador y con cada autor, porque el pago se
/// conserva cuando una cuenta se elimina (spec 014).
/// No contiene reglas de negocio.
/// </summary>
public class ConfiguracionPago : IEntityTypeConfiguration<Pago>
{
    /// <summary>Nombre del índice único del recibo por club.</summary>
    public const string IndiceReciboPorClub = "UX_Pagos_Club_Recibo";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos", tabla =>
        {
            tabla.HasCheckConstraint("CK_Pagos_Valor", "\"Valor\" > 0");
            tabla.HasCheckConstraint("CK_Pagos_Entidad", "\"Medio\" <> 'TRANSFERENCIA' OR \"Entidad\" IS NOT NULL");
            tabla.HasCheckConstraint(
                "CK_Pagos_Recibo", "\"Estado\" NOT IN ('CONFIRMADO', 'ANULADO') OR \"NumeroRecibo\" IS NOT NULL");
            tabla.HasCheckConstraint(
                "CK_Pagos_MotivoRechazo", "\"Estado\" <> 'RECHAZADO' OR char_length(\"MotivoRechazo\") > 0");
            tabla.HasCheckConstraint(
                "CK_Pagos_MotivoAnulacion", "\"Estado\" <> 'ANULADO' OR char_length(\"MotivoAnulacion\") > 0");
        });
        builder.HasKey(pago => pago.Id);
        builder.Property(pago => pago.Id).ValueGeneratedNever();

        builder.Property(pago => pago.JugadorNombre).HasMaxLength(200).IsRequired();
        builder.Property(pago => pago.JugadorTipoDocumento).HasMaxLength(30).IsRequired();
        builder.Property(pago => pago.JugadorNumeroDocumento).HasMaxLength(30).IsRequired();
        builder.Property(pago => pago.Medio).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(pago => pago.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(pago => pago.Entidad).HasMaxLength(40);
        builder.Property(pago => pago.Referencia).HasMaxLength(60);
        builder.Property(pago => pago.Concepto).HasMaxLength(300);
        builder.Property(pago => pago.Valor).HasColumnType("numeric(14,2)");
        builder.Property(pago => pago.ValorInformado).HasColumnType("numeric(14,2)");
        builder.Property(pago => pago.SaldoTrasPago).HasColumnType("numeric(14,2)");
        builder.Property(pago => pago.CreadoPorNombre).HasMaxLength(200).IsRequired();
        builder.Property(pago => pago.ConfirmadoPorNombre).HasMaxLength(200);
        builder.Property(pago => pago.RechazadoPorNombre).HasMaxLength(200);
        builder.Property(pago => pago.MotivoRechazo).HasMaxLength(300);
        builder.Property(pago => pago.AnuladoPorNombre).HasMaxLength(200);
        builder.Property(pago => pago.MotivoAnulacion).HasMaxLength(300);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(pago => pago.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // El jugador puede eliminar su cuenta: el pago se conserva con su nombre y documento copiados.
        builder.HasOne<UsuarioRol>()
            .WithMany()
            .HasForeignKey(pago => pago.UsuarioRolId)
            .OnDelete(DeleteBehavior.SetNull);

        // Quien registró, confirmó, rechazó o anuló puede dejar la plataforma: queda su nombre copiado (§13).
        builder.HasOne<Usuario>().WithMany().HasForeignKey(pago => pago.CreadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(pago => pago.ConfirmadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(pago => pago.RechazadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(pago => pago.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(pago => pago.Aplicaciones)
            .WithOne()
            .HasForeignKey(aplicacion => aplicacion.PagoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pago => new { pago.ClubId, pago.NumeroRecibo })
            .IsUnique()
            .HasFilter("\"NumeroRecibo\" IS NOT NULL")
            .HasDatabaseName(IndiceReciboPorClub);

        builder.HasIndex(pago => new { pago.ClubId, pago.Estado });
        builder.HasIndex(pago => pago.UsuarioRolId);
    }
}
