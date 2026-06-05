using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElevatorAds.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Cities = table.Column<string>(type: "jsonb", nullable: false),
                    BuildingTypes = table.Column<string>(type: "jsonb", nullable: false),
                    ScreenOrientations = table.Column<string>(type: "jsonb", nullable: false),
                    ScreenIds = table.Column<string>(type: "jsonb", nullable: false),
                    BuildingIds = table.Column<string>(type: "jsonb", nullable: false),
                    BaseCpm = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryPackages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Screens_BuildingId",
                table: "Screens",
                column: "BuildingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Screens_Buildings_BuildingId",
                table: "Screens",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Screens_Buildings_BuildingId",
                table: "Screens");

            migrationBuilder.DropTable(
                name: "InventoryPackages");

            migrationBuilder.DropIndex(
                name: "IX_Screens_BuildingId",
                table: "Screens");
        }
    }
}
