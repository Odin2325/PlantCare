using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlantCare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlantCareDbContext))]
[Migration("20260919123000_AddNotifications")]
public partial class AddNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CareScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey("FK_Notifications_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.NoAction);
                table.ForeignKey("FK_Notifications_CareSchedules_CareScheduleId", x => x.CareScheduleId, "CareSchedules", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Notifications_CareScheduleId_DueAtUtc", "Notifications", new[] { "CareScheduleId", "DueAtUtc" }, unique: true);
        migrationBuilder.CreateIndex("IX_Notifications_UserId_ReadAtUtc_CreatedAtUtc", "Notifications", new[] { "UserId", "ReadAtUtc", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "Notifications");
}
