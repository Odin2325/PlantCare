using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlantCare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlantCareDbContext))]
[Migration("20260919150000_AddCalendarSharing")]
public partial class AddCalendarSharing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CalendarShares",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalendarShares", x => x.Id);
                table.ForeignKey("FK_CalendarShares_AspNetUsers_OwnerUserId", x => x.OwnerUserId, "AspNetUsers", "Id");
                table.ForeignKey("FK_CalendarShares_AspNetUsers_RecipientUserId", x => x.RecipientUserId, "AspNetUsers", "Id");
            });
        migrationBuilder.CreateIndex("IX_CalendarShares_OwnerUserId_RecipientUserId", "CalendarShares", new[] { "OwnerUserId", "RecipientUserId" }, unique: true, filter: "[RevokedAtUtc] IS NULL");
        migrationBuilder.CreateIndex("IX_CalendarShares_RecipientUserId_RevokedAtUtc", "CalendarShares", new[] { "RecipientUserId", "RevokedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "CalendarShares");
}
