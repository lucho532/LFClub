using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPecosa.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class FotosDeJugadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotosJugador",
                columns: table => new
                {
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    TipoContenido = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GuardadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosJugador", x => x.UsuarioRolId);
                    table.ForeignKey(
                        name: "FK_FotosJugador_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FotosJugador_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosJugador_ClubId",
                table: "FotosJugador",
                column: "ClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosJugador");
        }
    }
}
