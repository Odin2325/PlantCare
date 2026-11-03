using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivatePhotoJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoJournalQuota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoJournalQuota", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantPhotoEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserPlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PhotographedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantPhotoEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantPhotoEntries_UserPlants_UserPlantId",
                        column: x => x.UserPlantId,
                        principalTable: "UserPlants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantPhotoMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Image = table.Column<byte[]>(type: "varbinary(max)", maxLength: 524288, nullable: false),
                    Thumbnail = table.Column<byte[]>(type: "varbinary(max)", maxLength: 65536, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    ImageSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ThumbnailSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantPhotoMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantPhotoMedia_PlantPhotoEntries_Id",
                        column: x => x.Id,
                        principalTable: "PlantPhotoEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PhotoJournalQuota",
                columns: new[] { "Id", "Revision" },
                values: new object[] { 1, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_PlantPhotoEntries_UserPlantId_PhotographedOn_Id",
                table: "PlantPhotoEntries",
                columns: new[] { "UserPlantId", "PhotographedOn", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PlantPhotoEntries_UserPlantId_UploadRequestId",
                table: "PlantPhotoEntries",
                columns: new[] { "UserPlantId", "UploadRequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoJournalQuota");

            migrationBuilder.DropTable(
                name: "PlantPhotoMedia");

            migrationBuilder.DropTable(
                name: "PlantPhotoEntries");
        }
    }
}
