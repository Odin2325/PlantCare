using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations;

public partial class AddFlexibleCareSchedules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ScheduleMode",
            table: "CareSchedules",
            type: "int",
            nullable: false,
            defaultValue: 0);
        migrationBuilder.AddColumn<int>(
            name: "WeekDays",
            table: "CareSchedules",
            type: "int",
            nullable: false,
            defaultValue: 0);
        migrationBuilder.AddColumn<TimeOnly>(
            name: "PreferredTimeLocal",
            table: "CareSchedules",
            type: "time",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "TimeZoneId",
            table: "CareSchedules",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ScheduleMode", table: "CareSchedules");
        migrationBuilder.DropColumn(name: "WeekDays", table: "CareSchedules");
        migrationBuilder.DropColumn(name: "PreferredTimeLocal", table: "CareSchedules");
        migrationBuilder.DropColumn(name: "TimeZoneId", table: "CareSchedules");
    }
}
