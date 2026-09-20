using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlantCare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlantCareDbContext))]
[Migration("20260920173000_AddPushNotifications")]
public partial class AddPushNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(name: "PushSentAtUtc", table: "Notifications", type: "datetimeoffset", nullable: true);
        migrationBuilder.CreateTable(name: "PushSubscriptions", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Endpoint = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false), P256dh = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
            Auth = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false), CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
            table.ForeignKey("FK_PushSubscriptions_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_PushSubscriptions_Endpoint", "PushSubscriptions", "Endpoint", unique: true);
        migrationBuilder.CreateIndex("IX_PushSubscriptions_UserId", "PushSubscriptions", "UserId");
        migrationBuilder.CreateIndex("IX_Notifications_PushSentAtUtc_CreatedAtUtc", "Notifications", new[] { "PushSentAtUtc", "CreatedAtUtc" });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PushSubscriptions");
        migrationBuilder.DropIndex(name: "IX_Notifications_PushSentAtUtc_CreatedAtUtc", table: "Notifications");
        migrationBuilder.DropColumn(name: "PushSentAtUtc", table: "Notifications");
    }
}
