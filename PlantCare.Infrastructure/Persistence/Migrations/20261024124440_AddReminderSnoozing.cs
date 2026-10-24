using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderSnoozing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_CareScheduleId_DueAtUtc",
                table: "Notifications");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NotBeforeUtc",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderSequence",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupersededAtUtc",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CareScheduleId_DueAtUtc_ReminderSequence",
                table: "Notifications",
                columns: new[] { "CareScheduleId", "DueAtUtc", "ReminderSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Snoozed reminders require a snooze-aware runtime. Use forward recovery; dropping state can resend delayed care early.");
        }
    }
}
