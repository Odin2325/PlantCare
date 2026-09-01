using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCareScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserPlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    LastCompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareSchedules_UserPlants_UserPlantId",
                        column: x => x.UserPlantId,
                        principalTable: "UserPlants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
    """
    INSERT INTO CareSchedules
    (
        Id,
        UserPlantId,
        ActionType,
        IntervalDays,
        LastCompletedAtUtc,
        NextDueAtUtc,
        IsEnabled
    )
    SELECT
        NEWID(),
        up.Id,
        1,
        ps.DefaultWateringIntervalDays,
        NULL,
        NULL,
        CASE
            WHEN up.IsActive = 1 THEN 1
            ELSE 0
        END
    FROM UserPlants up
    INNER JOIN PlantSpecies ps
        ON ps.Id = up.PlantSpeciesId;
    """);

            migrationBuilder.Sql(
                """
    INSERT INTO CareSchedules
    (
        Id,
        UserPlantId,
        ActionType,
        IntervalDays,
        LastCompletedAtUtc,
        NextDueAtUtc,
        IsEnabled
    )
    SELECT
        NEWID(),
        up.Id,
        2,
        ps.DefaultFertilizingIntervalDays,
        NULL,
        NULL,
        CASE
            WHEN up.IsActive = 1 THEN 1
            ELSE 0
        END
    FROM UserPlants up
    INNER JOIN PlantSpecies ps
        ON ps.Id = up.PlantSpeciesId
    WHERE ps.DefaultFertilizingIntervalDays IS NOT NULL;
    """);

            migrationBuilder.CreateTable(
                name: "CareEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareEvents_CareSchedules_CareScheduleId",
                        column: x => x.CareScheduleId,
                        principalTable: "CareSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareEvents_CareScheduleId",
                table: "CareEvents",
                column: "CareScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CareSchedules_IsEnabled_NextDueAtUtc",
                table: "CareSchedules",
                columns: new[] { "IsEnabled", "NextDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CareSchedules_UserPlantId_ActionType",
                table: "CareSchedules",
                columns: new[] { "UserPlantId", "ActionType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareEvents");

            migrationBuilder.DropTable(
                name: "CareSchedules");
        }
    }
}
