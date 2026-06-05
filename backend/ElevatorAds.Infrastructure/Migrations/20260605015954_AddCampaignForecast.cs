using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElevatorAds.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EligibleScreens = table.Column<int>(type: "integer", nullable: false),
                    EligibleBuildings = table.Column<int>(type: "integer", nullable: false),
                    EstimatedPlays = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedAudience = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AvailableCapacity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Warnings = table.Column<string>(type: "jsonb", nullable: false),
                    Conflicts = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignForecasts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignForecasts_BookingRequestId",
                table: "CampaignForecasts",
                column: "BookingRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignForecasts");
        }
    }
}
