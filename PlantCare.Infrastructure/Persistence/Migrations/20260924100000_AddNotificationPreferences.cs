using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations;

public partial class AddNotificationPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationPreferences",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                PushEnabled = table.Column<bool>(type: "bit", nullable: false),
                WateringEnabled = table.Column<bool>(type: "bit", nullable: false),
                FertilizingEnabled = table.Column<bool>(type: "bit", nullable: false),
                MistingEnabled = table.Column<bool>(type: "bit", nullable: false),
                PruningEnabled = table.Column<bool>(type: "bit", nullable: false),
                RepottingEnabled = table.Column<bool>(type: "bit", nullable: false),
                ReminderLeadTimeHours = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationPreferences", x => x.UserId);
                table.ForeignKey(
                    name: "FK_NotificationPreferences_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationPreferences");
    }
}
