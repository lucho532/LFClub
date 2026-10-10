using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPecosa.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class HermanosDeLaCuenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgregadoDesdeUsuarioRolId",
                table: "UsuariosRol",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRol_AgregadoDesdeUsuarioRolId",
                table: "UsuariosRol",
                column: "AgregadoDesdeUsuarioRolId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosRol_UsuariosRol_AgregadoDesdeUsuarioRolId",
                table: "UsuariosRol",
                column: "AgregadoDesdeUsuarioRolId",
                principalTable: "UsuariosRol",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosRol_UsuariosRol_AgregadoDesdeUsuarioRolId",
                table: "UsuariosRol");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosRol_AgregadoDesdeUsuarioRolId",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "AgregadoDesdeUsuarioRolId",
                table: "UsuariosRol");
        }
    }
}
