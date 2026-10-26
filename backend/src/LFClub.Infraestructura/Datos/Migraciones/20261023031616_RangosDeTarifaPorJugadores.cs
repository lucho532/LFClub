using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RangosDeTarifaPorJugadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioDirectivo",
                table: "ConfiguracionPlataforma");

            migrationBuilder.DropColumn(
                name: "PrecioEntrenador",
                table: "ConfiguracionPlataforma");

            migrationBuilder.DropColumn(
                name: "PrecioJugador",
                table: "ConfiguracionPlataforma");

            migrationBuilder.DropColumn(
                name: "PrecioPresidente",
                table: "ConfiguracionPlataforma");

            migrationBuilder.AddColumn<string>(
                name: "Rangos",
                table: "ConfiguracionPlataforma",
                type: "jsonb",
                nullable: false,
                defaultValue: "[{\"Desde\":1,\"Hasta\":20,\"Precio\":25000},{\"Desde\":21,\"Hasta\":40,\"Precio\":45000},{\"Desde\":41,\"Hasta\":80,\"Precio\":75000},{\"Desde\":81,\"Hasta\":null,\"Precio\":110000}]");

            migrationBuilder.AddColumn<int>(
                name: "RangoDesde",
                table: "CobrosPlataforma",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RangoHasta",
                table: "CobrosPlataforma",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rangos",
                table: "ConfiguracionPlataforma");

            migrationBuilder.DropColumn(
                name: "RangoDesde",
                table: "CobrosPlataforma");

            migrationBuilder.DropColumn(
                name: "RangoHasta",
                table: "CobrosPlataforma");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioDirectivo",
                table: "ConfiguracionPlataforma",
                type: "numeric(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioEntrenador",
                table: "ConfiguracionPlataforma",
                type: "numeric(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioJugador",
                table: "ConfiguracionPlataforma",
                type: "numeric(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioPresidente",
                table: "ConfiguracionPlataforma",
                type: "numeric(18,0)",
                nullable: true);
        }
    }
}
