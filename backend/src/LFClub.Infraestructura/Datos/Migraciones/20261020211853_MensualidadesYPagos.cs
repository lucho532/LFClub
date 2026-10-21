using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFClub.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class MensualidadesYPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CobraDesde",
                table: "UsuariosRol",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorMensualidadPropio",
                table: "UsuariosRol",
                type: "numeric(14,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UltimoRecibo",
                table: "Clubes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorMensualidad",
                table: "Clubes",
                type: "numeric(14,2)",
                nullable: true);

            // Valfor F.C. cobra 65.000 al mes (constitución §16.7); los demás clubes quedan sin valor.
            migrationBuilder.Sql(
                "UPDATE \"Clubes\" SET \"ValorMensualidad\" = 65000 WHERE \"NombreNormalizado\" LIKE 'valfor%';");

            // Los jugadores que ya existen reciben su primer cargo en el mes de la migración, que se mide
            // en la hora de Colombia (UTC-5 fijo).
            migrationBuilder.Sql(
                "UPDATE \"UsuariosRol\" SET \"CobraDesde\" = " +
                "date_trunc('month', (now() AT TIME ZONE 'UTC') - interval '5 hours')::date " +
                "WHERE \"Rol\" = 'JUGADOR';");

            migrationBuilder.CreateTable(
                name: "Cargos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mes = table.Column<DateOnly>(type: "date", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id);
                    table.CheckConstraint("CK_Cargos_Mes", "\"Tipo\" <> 'MENSUALIDAD' OR \"Mes\" IS NOT NULL");
                    table.CheckConstraint("CK_Cargos_Valor", "\"Valor\" > 0");
                    table.ForeignKey(
                        name: "FK_Cargos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cargos_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cargos_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediosPagoClub",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Entidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Titular = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Numero = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Nota = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediosPagoClub", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediosPagoClub_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioRolId = table.Column<Guid>(type: "uuid", nullable: true),
                    JugadorNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JugadorTipoDocumento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    JugadorNumeroDocumento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Medio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Entidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Referencia = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ValorInformado = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Concepto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    NumeroRecibo = table.Column<int>(type: "integer", nullable: true),
                    SaldoTrasPago = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreadoPorNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConfirmadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmadoPorNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RechazadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RechazadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    RechazadoPorNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MotivoRechazo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AnuladoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AnuladoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnuladoPorNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.CheckConstraint("CK_Pagos_Entidad", "\"Medio\" <> 'TRANSFERENCIA' OR \"Entidad\" IS NOT NULL");
                    table.CheckConstraint("CK_Pagos_MotivoAnulacion", "\"Estado\" <> 'ANULADO' OR char_length(\"MotivoAnulacion\") > 0");
                    table.CheckConstraint("CK_Pagos_MotivoRechazo", "\"Estado\" <> 'RECHAZADO' OR char_length(\"MotivoRechazo\") > 0");
                    table.CheckConstraint("CK_Pagos_Recibo", "\"Estado\" NOT IN ('CONFIRMADO', 'ANULADO') OR \"NumeroRecibo\" IS NOT NULL");
                    table.CheckConstraint("CK_Pagos_Valor", "\"Valor\" > 0");
                    table.ForeignKey(
                        name: "FK_Pagos_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pagos_UsuariosRol_UsuarioRolId",
                        column: x => x.UsuarioRolId,
                        principalTable: "UsuariosRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pagos_Usuarios_AnuladoPorUsuarioId",
                        column: x => x.AnuladoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pagos_Usuarios_ConfirmadoPorUsuarioId",
                        column: x => x.ConfirmadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pagos_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pagos_Usuarios_RechazadoPorUsuarioId",
                        column: x => x.RechazadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AplicacionesPago",
                columns: table => new
                {
                    PagoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CargoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacionesPago", x => new { x.PagoId, x.CargoId });
                    table.CheckConstraint("CK_AplicacionesPago_Valor", "\"Valor\" > 0");
                    table.ForeignKey(
                        name: "FK_AplicacionesPago_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AplicacionesPago_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AplicacionesPago_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoportesPago",
                columns: table => new
                {
                    PagoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    TipoContenido = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GuardadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoportesPago", x => x.PagoId);
                    table.ForeignKey(
                        name: "FK_SoportesPago_Clubes_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoportesPago_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPago_CargoId",
                table: "AplicacionesPago",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPago_ClubId",
                table: "AplicacionesPago",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_CategoriaId",
                table: "Cargos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_ClubId_Mes",
                table: "Cargos",
                columns: new[] { "ClubId", "Mes" });

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_ClubId_UsuarioRolId",
                table: "Cargos",
                columns: new[] { "ClubId", "UsuarioRolId" });

            migrationBuilder.CreateIndex(
                name: "UX_Cargos_Mensualidad_Jugador_Mes",
                table: "Cargos",
                columns: new[] { "UsuarioRolId", "Mes" },
                unique: true,
                filter: "\"Tipo\" = 'MENSUALIDAD'");

            migrationBuilder.CreateIndex(
                name: "IX_MediosPagoClub_ClubId",
                table: "MediosPagoClub",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_AnuladoPorUsuarioId",
                table: "Pagos",
                column: "AnuladoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ClubId_Estado",
                table: "Pagos",
                columns: new[] { "ClubId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ConfirmadoPorUsuarioId",
                table: "Pagos",
                column: "ConfirmadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_CreadoPorUsuarioId",
                table: "Pagos",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_RechazadoPorUsuarioId",
                table: "Pagos",
                column: "RechazadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_UsuarioRolId",
                table: "Pagos",
                column: "UsuarioRolId");

            migrationBuilder.CreateIndex(
                name: "UX_Pagos_Club_Recibo",
                table: "Pagos",
                columns: new[] { "ClubId", "NumeroRecibo" },
                unique: true,
                filter: "\"NumeroRecibo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SoportesPago_ClubId",
                table: "SoportesPago",
                column: "ClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AplicacionesPago");

            migrationBuilder.DropTable(
                name: "MediosPagoClub");

            migrationBuilder.DropTable(
                name: "SoportesPago");

            migrationBuilder.DropTable(
                name: "Cargos");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropColumn(
                name: "CobraDesde",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "ValorMensualidadPropio",
                table: "UsuariosRol");

            migrationBuilder.DropColumn(
                name: "UltimoRecibo",
                table: "Clubes");

            migrationBuilder.DropColumn(
                name: "ValorMensualidad",
                table: "Clubes");
        }
    }
}
