using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPecosa.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class EntrenamientosYPartidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entrenamientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Lugar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPorUsuarioRolId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrenamientos", x => x.Id);
                    table.CheckConstraint("CK_Entrenamientos_HoraFin", "\"HoraFin\" > \"HoraInicio\"");
                    table.ForeignKey(
                        name: "FK_Entrenamientos_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entrenamientos_UsuariosRol_CreadoPorUsuarioRolId",
                        column: x => x.CreadoPorUsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Partidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rival = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Lugar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Condicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreTorneo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GolesClub = table.Column<int>(type: "integer", nullable: true),
                    GolesRival = table.Column<int>(type: "integer", nullable: true),
                    ResultadoCambiadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultadoCambiadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultadoCambiadoPorNombre = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPorUsuarioRolId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos", x => x.Id);
                    table.CheckConstraint("CK_Partidos_Marcador", "(\"Estado\" = 'JUGADO' AND \"GolesClub\" IS NOT NULL AND \"GolesRival\" IS NOT NULL AND \"GolesClub\" BETWEEN 0 AND 99 AND \"GolesRival\" BETWEEN 0 AND 99) OR (\"Estado\" <> 'JUGADO' AND \"GolesClub\" IS NULL AND \"GolesRival\" IS NULL)");
                    table.CheckConstraint("CK_Partidos_Torneo", "\"Tipo\" <> 'TORNEO' OR \"NombreTorneo\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Partidos_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Partidos_UsuariosRol_CreadoPorUsuarioRolId",
                        column: x => x.CreadoPorUsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Partidos_Usuarios_ResultadoCambiadoPorUsuarioId",
                        column: x => x.ResultadoCambiadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AsistenciasEntrenamiento",
                columns: table => new
                {
                    EntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Presente = table.Column<bool>(type: "boolean", nullable: false),
                    RegistradaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsistenciasEntrenamiento", x => new { x.EntrenamientoId, x.UsuarioRolId });
                    table.ForeignKey(
                        name: "FK_AsistenciasEntrenamiento_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AsistenciasEntrenamiento_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsistenciasEntrenamiento_Entrenamientos_EntrenamientoId",
                        column: x => x.EntrenamientoId,
                        principalTable: "Entrenamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsistenciasEntrenamiento_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EntrenamientosCategoria",
                columns: table => new
                {
                    EntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntrenamientosCategoria", x => new { x.EntrenamientoId, x.CategoriaId });
                    table.ForeignKey(
                        name: "FK_EntrenamientosCategoria_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EntrenamientosCategoria_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntrenamientosCategoria_Entrenamientos_EntrenamientoId",
                        column: x => x.EntrenamientoId,
                        principalTable: "Entrenamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasEntrenamiento",
                columns: table => new
                {
                    EntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Respuesta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RespondidaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasEntrenamiento", x => new { x.EntrenamientoId, x.UsuarioRolId });
                    table.ForeignKey(
                        name: "FK_RespuestasEntrenamiento_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespuestasEntrenamiento_Entrenamientos_EntrenamientoId",
                        column: x => x.EntrenamientoId,
                        principalTable: "Entrenamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespuestasEntrenamiento_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartidosCategoria",
                columns: table => new
                {
                    PartidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidosCategoria", x => new { x.PartidoId, x.CategoriaId });
                    table.ForeignKey(
                        name: "FK_PartidosCategoria_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PartidosCategoria_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartidosCategoria_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartidosJugador",
                columns: table => new
                {
                    PartidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Jugo = table.Column<bool>(type: "boolean", nullable: false),
                    Goles = table.Column<int>(type: "integer", nullable: false),
                    Respuesta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RespondidaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConvocadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidosJugador", x => new { x.PartidoId, x.UsuarioRolId });
                    table.CheckConstraint("CK_PartidosJugador_Goles", "\"Goles\" >= 0 AND (\"Jugo\" OR \"Goles\" = 0)");
                    table.ForeignKey(
                        name: "FK_PartidosJugador_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PartidosJugador_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartidosJugador_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartidosJugador_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsistenciasEntrenamiento_CategoriaId",
                table: "AsistenciasEntrenamiento",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_AsistenciasEntrenamiento_ClubId",
                table: "AsistenciasEntrenamiento",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_AsistenciasEntrenamiento_UsuarioRolId",
                table: "AsistenciasEntrenamiento",
                column: "UsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "IX_Entrenamientos_ClubId_Fecha_HoraInicio",
                table: "Entrenamientos",
                columns: new[] { "ClubId", "Fecha", "HoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Entrenamientos_CreadoPorUsuarioRolId",
                table: "Entrenamientos",
                column: "CreadoPorUsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "IX_EntrenamientosCategoria_CategoriaId",
                table: "EntrenamientosCategoria",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntrenamientosCategoria_ClubId",
                table: "EntrenamientosCategoria",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_ClubId_Fecha_Hora",
                table: "Partidos",
                columns: new[] { "ClubId", "Fecha", "Hora" });

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_CreadoPorUsuarioRolId",
                table: "Partidos",
                column: "CreadoPorUsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_ResultadoCambiadoPorUsuarioId",
                table: "Partidos",
                column: "ResultadoCambiadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidosCategoria_CategoriaId",
                table: "PartidosCategoria",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidosCategoria_ClubId",
                table: "PartidosCategoria",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidosJugador_CategoriaId",
                table: "PartidosJugador",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidosJugador_ClubId",
                table: "PartidosJugador",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidosJugador_UsuarioRolId",
                table: "PartidosJugador",
                column: "UsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasEntrenamiento_ClubId",
                table: "RespuestasEntrenamiento",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasEntrenamiento_UsuarioRolId",
                table: "RespuestasEntrenamiento",
                column: "UsuarioRolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsistenciasEntrenamiento");

            migrationBuilder.DropTable(
                name: "EntrenamientosCategoria");

            migrationBuilder.DropTable(
                name: "PartidosCategoria");

            migrationBuilder.DropTable(
                name: "PartidosJugador");

            migrationBuilder.DropTable(
                name: "RespuestasEntrenamiento");

            migrationBuilder.DropTable(
                name: "Partidos");

            migrationBuilder.DropTable(
                name: "Entrenamientos");
        }
    }
}
