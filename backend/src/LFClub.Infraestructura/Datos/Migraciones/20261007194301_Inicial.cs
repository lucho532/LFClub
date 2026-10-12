using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clubes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NombreNormalizado = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Sede = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CorreoContacto = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    TelefonoContacto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ColorPrincipal = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    ColorAcento = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    VersionEscudo = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstadoCambiadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstadoCambiadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    CorreoNormalizado = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ContrasenaHash = table.Column<string>(type: "text", nullable: true),
                    Celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EsDesarrollador = table.Column<bool>(type: "boolean", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "integer", nullable: false),
                    Bloqueada = table.Column<bool>(type: "boolean", nullable: false),
                    SelloSeguridad = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionFoto = table.Column<int>(type: "integer", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesRecuperacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VenceEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesRecuperacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesRecuperacion_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosRol",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstadoIngreso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombres = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosRol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosRol_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosRol_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clubes_NombreNormalizado",
                table: "Clubes",
                column: "NombreNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRecuperacion_TokenHash",
                table: "SolicitudesRecuperacion",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRecuperacion_UsuarioId",
                table: "SolicitudesRecuperacion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CorreoNormalizado",
                table: "Usuarios",
                column: "CorreoNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EsDesarrollador",
                table: "Usuarios",
                column: "EsDesarrollador",
                unique: true,
                filter: "\"EsDesarrollador\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_ClubId_NumeroDocumento",
                table: "UsuariosRol",
                columns: new[] { "ClubId", "NumeroDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_ClubId_UsuarioId",
                table: "UsuariosRol",
                columns: new[] { "ClubId", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_NumeroDocumento",
                table: "UsuariosRol",
                column: "NumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_UsuarioId",
                table: "UsuariosRol",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesRecuperacion");

            migrationBuilder.DropTable(
                name: "UsuariosRol");

            migrationBuilder.DropTable(
                name: "Clubes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
