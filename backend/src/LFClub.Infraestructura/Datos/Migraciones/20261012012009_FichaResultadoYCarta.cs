using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class FichaResultadoYCarta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IndicadoEn",
                table: "PartidosJugador",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndicadoPor",
                table: "PartidosJugador",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndicadoPorNombre",
                table: "PartidosJugador",
                type: "character varying(161)",
                maxLength: 161,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IndicadoPorUsuarioId",
                table: "PartidosJugador",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigenResultado",
                table: "Partidos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicadoEn",
                table: "Partidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PublicadoPorFamilia",
                table: "Partidos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PublicadoPorNombre",
                table: "Partidos",
                type: "character varying(161)",
                maxLength: 161,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicadoPorUsuarioId",
                table: "Partidos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CartasJugador",
                columns: table => new
                {
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dorsal = table.Column<int>(type: "integer", nullable: true),
                    Posicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Ritmo = table.Column<int>(type: "integer", nullable: true),
                    Tiro = table.Column<int>(type: "integer", nullable: true),
                    Pase = table.Column<int>(type: "integer", nullable: true),
                    Regate = table.Column<int>(type: "integer", nullable: true),
                    Defensa = table.Column<int>(type: "integer", nullable: true),
                    Fisico = table.Column<int>(type: "integer", nullable: true),
                    EstadisticasCambiadasEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstadisticasCambiadasPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstadisticasCambiadasPorNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: true),
                    DorsalCambiadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DorsalCambiadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    DorsalCambiadoPorNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartasJugador", x => x.UsuarioRolId);
                    table.CheckConstraint("CK_CartasJugador_Dorsal", "\"Dorsal\" IS NULL OR \"Dorsal\" BETWEEN 1 AND 99");
                    table.CheckConstraint("CK_CartasJugador_Estadisticas", "(\"Ritmo\" IS NULL AND \"Tiro\" IS NULL AND \"Pase\" IS NULL AND \"Regate\" IS NULL AND \"Defensa\" IS NULL AND \"Fisico\" IS NULL) OR (\"Ritmo\" BETWEEN 1 AND 99 AND \"Tiro\" BETWEEN 1 AND 99 AND \"Pase\" BETWEEN 1 AND 99 AND \"Regate\" BETWEEN 1 AND 99 AND \"Defensa\" BETWEEN 1 AND 99 AND \"Fisico\" BETWEEN 1 AND 99)");
                    table.CheckConstraint("CK_CartasJugador_SelloDorsal", "(\"DorsalCambiadoEn\" IS NULL) = (\"DorsalCambiadoPorNombre\" IS NULL)");
                    table.CheckConstraint("CK_CartasJugador_SelloEstadisticas", "(\"EstadisticasCambiadasEn\" IS NULL) = (\"EstadisticasCambiadasPorNombre\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_CartasJugador_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartasJugador_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartasJugador_Usuarios_DorsalCambiadoPorUsuarioId",
                        column: x => x.DorsalCambiadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CartasJugador_Usuarios_EstadisticasCambiadasPorUsuarioId",
                        column: x => x.EstadisticasCambiadasPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartidosJugador_IndicadoPorUsuarioId",
                table: "PartidosJugador",
                column: "IndicadoPorUsuarioId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PartidosJugador_Indicacion",
                table: "PartidosJugador",
                sql: "(\"IndicadoPor\" IS NULL) = (\"IndicadoEn\" IS NULL) AND (\"IndicadoPor\" IS NULL) = (\"IndicadoPorNombre\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_PublicadoPorUsuarioId",
                table: "Partidos",
                column: "PublicadoPorUsuarioId");

            // Los partidos jugados antes de la 010 los registró quien gestiona: su último cambio pasa
            // a ser también la primera publicación, con origen GESTION.
            migrationBuilder.Sql(
                "UPDATE \"Partidos\" SET \"OrigenResultado\" = 'GESTION', "
                + "\"PublicadoEn\" = COALESCE(\"ResultadoCambiadoEn\", \"CreadoEn\"), "
                + "\"PublicadoPorUsuarioId\" = \"ResultadoCambiadoPorUsuarioId\", "
                + "\"PublicadoPorNombre\" = COALESCE(\"ResultadoCambiadoPorNombre\", '') "
                + "WHERE \"Estado\" = 'JUGADO';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Partidos_Publicacion",
                table: "Partidos",
                sql: "(\"Estado\" = 'JUGADO') = (\"OrigenResultado\" IS NOT NULL) AND (\"Estado\" = 'JUGADO') = (\"PublicadoEn\" IS NOT NULL) AND (\"Estado\" = 'JUGADO') = (\"PublicadoPorNombre\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CartasJugador_ClubId",
                table: "CartasJugador",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CartasJugador_DorsalCambiadoPorUsuarioId",
                table: "CartasJugador",
                column: "DorsalCambiadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CartasJugador_EstadisticasCambiadasPorUsuarioId",
                table: "CartasJugador",
                column: "EstadisticasCambiadasPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Partidos_Usuarios_PublicadoPorUsuarioId",
                table: "Partidos",
                column: "PublicadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PartidosJugador_Usuarios_IndicadoPorUsuarioId",
                table: "PartidosJugador",
                column: "IndicadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partidos_Usuarios_PublicadoPorUsuarioId",
                table: "Partidos");

            migrationBuilder.DropForeignKey(
                name: "FK_PartidosJugador_Usuarios_IndicadoPorUsuarioId",
                table: "PartidosJugador");

            migrationBuilder.DropTable(
                name: "CartasJugador");

            migrationBuilder.DropIndex(
                name: "IX_PartidosJugador_IndicadoPorUsuarioId",
                table: "PartidosJugador");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PartidosJugador_Indicacion",
                table: "PartidosJugador");

            migrationBuilder.DropIndex(
                name: "IX_Partidos_PublicadoPorUsuarioId",
                table: "Partidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Partidos_Publicacion",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "IndicadoEn",
                table: "PartidosJugador");

            migrationBuilder.DropColumn(
                name: "IndicadoPor",
                table: "PartidosJugador");

            migrationBuilder.DropColumn(
                name: "IndicadoPorNombre",
                table: "PartidosJugador");

            migrationBuilder.DropColumn(
                name: "IndicadoPorUsuarioId",
                table: "PartidosJugador");

            migrationBuilder.DropColumn(
                name: "OrigenResultado",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "PublicadoEn",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "PublicadoPorFamilia",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "PublicadoPorNombre",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "PublicadoPorUsuarioId",
                table: "Partidos");
        }
    }
}
