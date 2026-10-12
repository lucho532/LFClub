using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class FichaDelJugador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentosJugador",
                columns: table => new
                {
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    TipoContenido = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TamanoBytes = table.Column<int>(type: "integer", nullable: false),
                    SubidoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosJugador", x => new { x.UsuarioRolId, x.Documento });
                    table.ForeignKey(
                        name: "FK_DocumentosJugador_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentosJugador_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FichasJugador",
                columns: table => new
                {
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmergenciaNombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    EmergenciaParentesco = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EmergenciaCelular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EntidadSalud = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LugarAtencion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GrupoSanguineo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Alergias = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Enfermedades = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Medicamentos = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UltimoCambioEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoCambioPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    UltimoCambioPorNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasJugador", x => x.UsuarioRolId);
                    table.ForeignKey(
                        name: "FK_FichasJugador_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FichasJugador_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FichasJugador_Usuarios_UltimoCambioPorUsuarioId",
                        column: x => x.UltimoCambioPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosJugador_ClubId",
                table: "DocumentosJugador",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_FichasJugador_ClubId",
                table: "FichasJugador",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_FichasJugador_UltimoCambioPorUsuarioId",
                table: "FichasJugador",
                column: "UltimoCambioPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosJugador");

            migrationBuilder.DropTable(
                name: "FichasJugador");
        }
    }
}
