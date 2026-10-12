using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class CategoriasDelClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "UsuariosRol",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaId",
                table: "UsuariosRol",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetiradoEn",
                table: "UsuariosRol",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetiradoPorNombre",
                table: "UsuariosRol",
                type: "character varying(161)",
                maxLength: 161,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RetiradoPorUsuarioId",
                table: "UsuariosRol",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Usada = table.Column<bool>(type: "boolean", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categorias_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesEntrenadorCategoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesEntrenadorCategoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesEntrenadorCategoria_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionesEntrenadorCategoria_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionesEntrenadorCategoria_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NombreNormalizado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Usado = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipos_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntrenadoresEquipo",
                columns: table => new
                {
                    AsignacionEntrenadorCategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntrenadoresEquipo", x => new { x.AsignacionEntrenadorCategoriaId, x.EquipoId });
                    table.ForeignKey(
                        name: "FK_EntrenadoresEquipo_AsignacionesEntrenadorCategoria_Asignaci~",
                        column: x => x.AsignacionEntrenadorCategoriaId,
                        principalTable: "AsignacionesEntrenadorCategoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntrenadoresEquipo_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntrenadoresEquipo_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JugadoresEquipo",
                columns: table => new
                {
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JugadoresEquipo", x => new { x.UsuarioRolId, x.EquipoId });
                    table.ForeignKey(
                        name: "FK_JugadoresEquipo_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JugadoresEquipo_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JugadoresEquipo_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_CategoriaId",
                table: "UsuariosRol",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_ClubId_CategoriaId",
                table: "UsuariosRol",
                columns: new[] { "ClubId", "CategoriaId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_RetiradoPorUsuarioId",
                table: "UsuariosRol",
                column: "RetiradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEntrenadorCategoria_CategoriaId_UsuarioRolId",
                table: "AsignacionesEntrenadorCategoria",
                columns: new[] { "CategoriaId", "UsuarioRolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEntrenadorCategoria_ClubId",
                table: "AsignacionesEntrenadorCategoria",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEntrenadorCategoria_UsuarioRolId_Activa",
                table: "AsignacionesEntrenadorCategoria",
                columns: new[] { "UsuarioRolId", "Activa" });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_ClubId_Anio",
                table: "Categorias",
                columns: new[] { "ClubId", "Anio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntrenadoresEquipo_ClubId",
                table: "EntrenadoresEquipo",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EntrenadoresEquipo_EquipoId",
                table: "EntrenadoresEquipo",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_CategoriaId_NombreNormalizado",
                table: "Equipos",
                columns: new[] { "CategoriaId", "NombreNormalizado" },
                unique: true,
                filter: "\"Activo\"");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_ClubId",
                table: "Equipos",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresEquipo_ClubId",
                table: "JugadoresEquipo",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresEquipo_EquipoId",
                table: "JugadoresEquipo",
                column: "EquipoId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosRol_Categorias_CategoriaId",
                table: "UsuariosRol",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosRol_Usuarios_RetiradoPorUsuarioId",
                table: "UsuariosRol",
                column: "RetiradoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosRol_Categorias_CategoriaId",
                table: "UsuariosRol");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosRol_Usuarios_RetiradoPorUsuarioId",
                table: "UsuariosRol");

            migrationBuilder.DropTable(
                name: "EntrenadoresEquipo");

            migrationBuilder.DropTable(
                name: "JugadoresEquipo");

            migrationBuilder.DropTable(
                name: "AsignacionesEntrenadorCategoria");

            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosRol_CategoriaId",
                table: "UsuariosRol");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosRol_ClubId_CategoriaId",
                table: "UsuariosRol");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosRol_RetiradoPorUsuarioId",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "RetiradoEn",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "RetiradoPorNombre",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "RetiradoPorUsuarioId",
                table: "UsuariosRol");
        }
    }
}
