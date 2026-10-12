using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class IngresoAlClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AprobadoEn",
                table: "UsuariosRol",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AprobadoPorNombre",
                table: "UsuariosRol",
                type: "character varying(161)",
                maxLength: 161,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AprobadoPorUsuarioId",
                table: "UsuariosRol",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolDeIngreso",
                table: "UsuariosRol",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreResponsable",
                table: "Usuarios",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_AprobadoPorUsuarioId",
                table: "UsuariosRol",
                column: "AprobadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_ClubId_EstadoIngreso",
                table: "UsuariosRol",
                columns: new[] { "ClubId", "EstadoIngreso" });

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosRol_Usuarios_AprobadoPorUsuarioId",
                table: "UsuariosRol",
                column: "AprobadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosRol_Usuarios_AprobadoPorUsuarioId",
                table: "UsuariosRol");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosRol_AprobadoPorUsuarioId",
                table: "UsuariosRol");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosRol_ClubId_EstadoIngreso",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "AprobadoEn",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "AprobadoPorNombre",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "AprobadoPorUsuarioId",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "RolDeIngreso",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "NombreResponsable",
                table: "Usuarios");
        }
    }
}
