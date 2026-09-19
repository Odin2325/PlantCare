using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlantCare.Infrastructure.Persistence;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlantCareDbContext))]
[Migration("20260919110000_AddCareScheduleArchiving")]
public partial class AddCareScheduleArchiving : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsArchived",
            table: "CareSchedules",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsArchived",
            table: "CareSchedules");
    }
}
