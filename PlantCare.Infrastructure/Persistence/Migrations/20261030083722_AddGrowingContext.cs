using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrowingContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CareSeason",
                table: "UserPlants",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrowingEnvironment",
                table: "UserPlants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareSeason",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "GrowingEnvironment",
                table: "UserPlants");
        }
    }
}
