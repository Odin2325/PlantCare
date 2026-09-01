using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantSpecies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScientificName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SunlightRequirement = table.Column<int>(type: "int", nullable: false),
                    SunlightInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DefaultWateringIntervalDays = table.Column<int>(type: "int", nullable: false),
                    WateringInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DefaultFertilizingIntervalDays = table.Column<int>(type: "int", nullable: true),
                    FertilizingInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SoilInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HumidityInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MinimumTemperatureCelsius = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MaximumTemperatureCelsius = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsToxicToPets = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSpecies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantSpecies_CommonName",
                table: "PlantSpecies",
                column: "CommonName");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSpecies_ScientificName",
                table: "PlantSpecies",
                column: "ScientificName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantSpecies");
        }
    }
}