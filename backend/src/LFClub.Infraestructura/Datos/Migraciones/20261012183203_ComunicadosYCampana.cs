using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ComunicadosYCampana : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comunicados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Alcance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Clave = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicadoPorUsuarioRolId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublicadoPorNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: false),
                    PublicadoPorRol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RetiradoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetiradoPorUsuarioRolId = table.Column<Guid>(type: "uuid", nullable: true),
                    RetiradoPorNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunicados", x => x.Id);
                    table.CheckConstraint("CK_Comunicados_Retiro", "(\"RetiradoEn\" IS NULL) = (\"RetiradoPorNombre\" IS NULL)");
                    table.CheckConstraint("CK_Comunicados_Texto", "char_length(\"Texto\") > 0");
                    table.CheckConstraint("CK_Comunicados_Titulo", "char_length(\"Titulo\") > 0");
                    table.ForeignKey(
                        name: "FK_Comunicados_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comunicados_UsuariosRol_PublicadoPorUsuarioRolId",
                        column: x => x.PublicadoPorUsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Comunicados_UsuariosRol_RetiradoPorUsuarioRolId",
                        column: x => x.RetiradoPorUsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComunicadosDestinatario",
                columns: table => new
                {
                    ComunicadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeidoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicadosDestinatario", x => new { x.ComunicadoId, x.UsuarioRolId });
                    table.ForeignKey(
                        name: "FK_ComunicadosDestinatario_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunicadosDestinatario_Comunicados_ComunicadoId",
                        column: x => x.ComunicadoId,
                        principalTable: "Comunicados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunicadosDestinatario_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunicadosDestinatario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunicadosDestino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComunicadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoriaNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: false),
                    EquipoId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipoNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicadosDestino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicadosDestino_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComunicadosDestino_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunicadosDestino_Comunicados_ComunicadoId",
                        column: x => x.ComunicadoId,
                        principalTable: "Comunicados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunicadosDestino_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comunicados_ClubId_Clave",
                table: "Comunicados",
                columns: new[] { "ClubId", "Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comunicados_ClubId_Id",
                table: "Comunicados",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Comunicados_PublicadoPorUsuarioRolId",
                table: "Comunicados",
                column: "PublicadoPorUsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "IX_Comunicados_RetiradoPorUsuarioRolId",
                table: "Comunicados",
                column: "RetiradoPorUsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestinatario_ClubId",
                table: "ComunicadosDestinatario",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestinatario_SinLeer",
                table: "ComunicadosDestinatario",
                columns: new[] { "UsuarioId", "ClubId" },
                filter: "\"LeidoEn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestinatario_UsuarioId_ClubId_ComunicadoId",
                table: "ComunicadosDestinatario",
                columns: new[] { "UsuarioId", "ClubId", "ComunicadoId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestinatario_UsuarioRolId",
                table: "ComunicadosDestinatario",
                column: "UsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestino_CategoriaId",
                table: "ComunicadosDestino",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestino_ClubId",
                table: "ComunicadosDestino",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestino_ComunicadoId",
                table: "ComunicadosDestino",
                column: "ComunicadoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicadosDestino_EquipoId",
                table: "ComunicadosDestino",
                column: "EquipoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComunicadosDestinatario");

            migrationBuilder.DropTable(
                name: "ComunicadosDestino");

            migrationBuilder.DropTable(
                name: "Comunicados");
        }
    }
}
