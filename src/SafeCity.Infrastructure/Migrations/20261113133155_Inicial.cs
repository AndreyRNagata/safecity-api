using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeCity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Zonas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    RaioMetros = table.Column<int>(type: "INTEGER", nullable: false),
                    RiscoPorHora = table.Column<string>(type: "TEXT", nullable: false),
                    OcorrenciasUltimos30Dias = table.Column<int>(type: "INTEGER", nullable: false),
                    Dicas = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zonas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ocorrencias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    ZonaId = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Zonas_ZonaId",
                        column: x => x.ZonaId,
                        principalTable: "Zonas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_Latitude_Longitude",
                table: "Ocorrencias",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_OcorridoEm",
                table: "Ocorrencias",
                column: "OcorridoEm");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_ZonaId",
                table: "Ocorrencias",
                column: "ZonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Zonas_Bairro",
                table: "Zonas",
                column: "Bairro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ocorrencias");

            migrationBuilder.DropTable(
                name: "Zonas");
        }
    }
}
