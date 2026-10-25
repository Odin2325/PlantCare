using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrowingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpectedHarvestOn",
                table: "UserPlants",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedPlantingOutOn",
                table: "UserPlants",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedSowingOn",
                table: "UserPlants",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedHarvestOn",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "PlannedPlantingOutOn",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "PlannedSowingOn",
                table: "UserPlants");
        }
    }
}
