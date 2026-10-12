using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPecosa.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AvisosDeActividad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvisosDeActividad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hecho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActividadTipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActividadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    GeneradoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosDeActividad", x => x.Id);
                    table.CheckConstraint("CK_AvisosDeActividad_Texto", "char_length(\"Texto\") > 0");
                    table.ForeignKey(
                        name: "FK_AvisosDeActividad_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvisosDeActividadDestinatario",
                columns: table => new
                {
                    AvisoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeidoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosDeActividadDestinatario", x => new { x.AvisoId, x.UsuarioRolId });
                    table.ForeignKey(
                        name: "FK_AvisosDeActividadDestinatario_AvisosDeActividad_AvisoId",
                        column: x => x.AvisoId,
                        principalTable: "AvisosDeActividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvisosDeActividadDestinatario_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvisosDeActividadDestinatario_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvisosDeActividadDestinatario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosDeActividad_ClubId_ActividadTipo_ActividadId",
                table: "AvisosDeActividad",
                columns: new[] { "ClubId", "ActividadTipo", "ActividadId" });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosDeActividad_ClubId_Id",
                table: "AvisosDeActividad",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosDeActividadDestinatario_ClubId",
                table: "AvisosDeActividadDestinatario",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosDeActividadDestinatario_SinLeer",
                table: "AvisosDeActividadDestinatario",
                columns: new[] { "UsuarioId", "ClubId" },
                filter: "\"LeidoEn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosDeActividadDestinatario_UsuarioId_ClubId_AvisoId",
                table: "AvisosDeActividadDestinatario",
                columns: new[] { "UsuarioId", "ClubId", "AvisoId" });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosDeActividadDestinatario_UsuarioRolId",
                table: "AvisosDeActividadDestinatario",
                column: "UsuarioRolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisosDeActividadDestinatario");

            migrationBuilder.DropTable(
                name: "AvisosDeActividad");
        }
    }
}
