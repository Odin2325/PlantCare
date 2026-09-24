using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations;

public partial class AddMicrosoftCalendarSync : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExternalCalendarEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlantCareEntryId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ExternalEventId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExternalCalendarEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExternalCalendarEvents_ExternalCalendarConnections_ConnectionId",
                    column: x => x.ConnectionId,
                    principalTable: "ExternalCalendarConnections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCalendarEvents_ConnectionId_PlantCareEntryId",
            table: "ExternalCalendarEvents",
            columns: new[] { "ConnectionId", "PlantCareEntryId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ExternalCalendarEvents");
    }
}
