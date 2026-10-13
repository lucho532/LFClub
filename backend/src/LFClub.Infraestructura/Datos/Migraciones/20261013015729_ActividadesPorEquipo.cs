using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ActividadesPorEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntrenamientosEquipo",
                columns: table => new
                {
                    EntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntrenamientosEquipo", x => new { x.EntrenamientoId, x.EquipoId });
                    table.ForeignKey(
                        name: "FK_EntrenamientosEquipo_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntrenamientosEquipo_Entrenamientos_EntrenamientoId",
                        column: x => x.EntrenamientoId,
                        principalTable: "Entrenamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntrenamientosEquipo_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EntrenamientosJugador",
                columns: table => new
                {
                    EntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EquipoDeOrigenId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnadidoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntrenamientosJugador", x => new { x.EntrenamientoId, x.UsuarioRolId });
                    table.CheckConstraint("CK_EntrenamientosJugador_OrigenDelAnadido", "\"Origen\" <> 'ANADIDO' OR \"EquipoDeOrigenId\" IS NULL");
                    table.ForeignKey(
                        name: "FK_EntrenamientosJugador_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntrenamientosJugador_Entrenamientos_EntrenamientoId",
                        column: x => x.EntrenamientoId,
                        principalTable: "Entrenamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntrenamientosJugador_Equipos_EquipoDeOrigenId",
                        column: x => x.EquipoDeOrigenId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EntrenamientosJugador_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntrenamientosEquipo_ClubId",
                table: "EntrenamientosEquipo",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EntrenamientosEquipo_EquipoId",
                table: "EntrenamientosEquipo",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_EntrenamientosJugador_ClubId",
                table: "EntrenamientosJugador",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EntrenamientosJugador_EquipoDeOrigenId",
                table: "EntrenamientosJugador",
                column: "EquipoDeOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_EntrenamientosJugador_UsuarioRolId",
                table: "EntrenamientosJugador",
                column: "UsuarioRolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntrenamientosEquipo");

            migrationBuilder.DropTable(
                name: "EntrenamientosJugador");
        }
    }
}
