using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class PagoDeLaPlataforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "MesInicioCobro",
                table: "Clubes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoSuspension",
                table: "Clubes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CobrosPlataforma",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Jugadores = table.Column<int>(type: "integer", nullable: false),
                    Entrenadores = table.Column<int>(type: "integer", nullable: false),
                    Directivos = table.Column<int>(type: "integer", nullable: false),
                    Presidentes = table.Column<int>(type: "integer", nullable: false),
                    PrecioJugador = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    PrecioEntrenador = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    PrecioDirectivo = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    PrecioPresidente = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    GeneradoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiasDeGracia = table.Column<int>(type: "integer", nullable: false),
                    SuspendeDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    SuspensionOmitida = table.Column<bool>(type: "boolean", nullable: false),
                    AnuladoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnuladoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CobrosPlataforma", x => x.Id);
                    table.CheckConstraint("CK_CobrosPlataforma_Total", "\"Total\" > 0");
                    table.ForeignKey(
                        name: "FK_CobrosPlataforma_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CobrosPlataforma_Usuarios_AnuladoPorUsuarioId",
                        column: x => x.AnuladoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionPlataforma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PrecioJugador = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    PrecioEntrenador = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    PrecioDirectivo = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    PrecioPresidente = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    DiasDeGracia = table.Column<int>(type: "integer", nullable: false),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionPlataforma", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionPlataforma_Dias", "\"DiasDeGracia\" BETWEEN 0 AND 30");
                    table.CheckConstraint("CK_ConfiguracionPlataforma_UnaFila", "\"Id\" = 1");
                    table.ForeignKey(
                        name: "FK_ConfiguracionPlataforma_Usuarios_ActualizadoPorUsuarioId",
                        column: x => x.ActualizadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IntentosPagoPlataforma",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CobroId = table.Column<Guid>(type: "uuid", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntentosPagoPlataforma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntentosPagoPlataforma_CobrosPlataforma_CobroId",
                        column: x => x.CobroId,
                        principalTable: "CobrosPlataforma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntentosPagoPlataforma_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificacionesPasarela",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecibidaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Resultado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransaccionId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    EstadoPasarela = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Referencia = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    CobroId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesPasarela", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacionesPasarela_CobrosPlataforma_CobroId",
                        column: x => x.CobroId,
                        principalTable: "CobrosPlataforma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagosPlataforma",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CobroId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    PagadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    TransaccionId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    MedioDePago = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RegistradoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosPlataforma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosPlataforma_CobrosPlataforma_CobroId",
                        column: x => x.CobroId,
                        principalTable: "CobrosPlataforma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagosPlataforma_Usuarios_RegistradoPorUsuarioId",
                        column: x => x.RegistradoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CobrosPlataforma_AnuladoPorUsuarioId",
                table: "CobrosPlataforma",
                column: "AnuladoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_CobrosPlataforma_Club_Periodo",
                table: "CobrosPlataforma",
                columns: new[] { "ClubId", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionPlataforma_ActualizadoPorUsuarioId",
                table: "ConfiguracionPlataforma",
                column: "ActualizadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_IntentosPagoPlataforma_CobroId_CreadoEn",
                table: "IntentosPagoPlataforma",
                columns: new[] { "CobroId", "CreadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_IntentosPagoPlataforma_CreadoPorUsuarioId",
                table: "IntentosPagoPlataforma",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_IntentosPagoPlataforma_Referencia",
                table: "IntentosPagoPlataforma",
                column: "Referencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesPasarela_CobroId",
                table: "NotificacionesPasarela",
                column: "CobroId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesPasarela_RecibidaEn",
                table: "NotificacionesPasarela",
                column: "RecibidaEn");

            migrationBuilder.CreateIndex(
                name: "UX_NotificacionesPasarela_Transaccion_Estado",
                table: "NotificacionesPasarela",
                columns: new[] { "TransaccionId", "EstadoPasarela" },
                unique: true,
                filter: "\"TransaccionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PagosPlataforma_CobroId",
                table: "PagosPlataforma",
                column: "CobroId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosPlataforma_RegistradoPorUsuarioId",
                table: "PagosPlataforma",
                column: "RegistradoPorUsuarioId");

            // Hasta hoy toda suspensión fue manual (spec 017, research §5). MesInicioCobro queda nulo.
            migrationBuilder.Sql(
                "UPDATE \"Clubes\" SET \"MotivoSuspension\" = 'MANUAL' WHERE \"Estado\" = 'SUSPENDIDO';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionPlataforma");

            migrationBuilder.DropTable(
                name: "IntentosPagoPlataforma");

            migrationBuilder.DropTable(
                name: "NotificacionesPasarela");

            migrationBuilder.DropTable(
                name: "PagosPlataforma");

            migrationBuilder.DropTable(
                name: "CobrosPlataforma");

            migrationBuilder.DropColumn(
                name: "MesInicioCobro",
                table: "Clubes");

            migrationBuilder.DropColumn(
                name: "MotivoSuspension",
                table: "Clubes");
        }
    }
}
