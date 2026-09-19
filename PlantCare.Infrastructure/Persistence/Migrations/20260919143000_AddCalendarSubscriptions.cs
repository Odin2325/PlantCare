using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlantCare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlantCareDbContext))]
[Migration("20260919143000_AddCalendarSubscriptions")]
public partial class AddCalendarSubscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CalendarSubscriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalendarSubscriptions", x => x.Id);
                table.ForeignKey("FK_CalendarSubscriptions_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_CalendarSubscriptions_TokenHash", "CalendarSubscriptions", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_CalendarSubscriptions_UserId", "CalendarSubscriptions", "UserId", unique: true, filter: "[RevokedAtUtc] IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "CalendarSubscriptions");
}
