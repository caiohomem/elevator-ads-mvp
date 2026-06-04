using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElevatorAds.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Step 1: Create the Organizations table with a seeded "Default Organization" so
        // existing rows can be backfilled without violating the new foreign key.
        migrationBuilder.CreateTable(
            name: "Organizations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Organizations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_Slug",
            table: "Organizations",
            column: "Slug",
            unique: true);

        // Seed a deterministic default organization. Existing rows will be linked to it
        // before the new NOT NULL FK columns are added.
        var defaultOrgId = new Guid("11111111-1111-1111-1111-111111111111");
        migrationBuilder.InsertData(
            table: "Organizations",
            columns: new[] { "Id", "Name", "Slug", "Status", "CreatedAt", "UpdatedAt" },
            values: new object[] { defaultOrgId, "Default Organization", "default", "active", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        // Step 2: Add the OrganizationId columns as nullable so we can backfill safely.
        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Advertisers",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Buildings",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Campaigns",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Creatives",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "DailyPlaylists",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "ProofOfPlayEvents",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Screens",
            type: "uuid",
            nullable: true);

        // Step 3: Backfill any existing rows to the default organization so the FK
        // constraint is satisfied for all rows.
        migrationBuilder.Sql($"UPDATE \"Advertisers\" SET \"OrganizationId\" = '{defaultOrgId}' WHERE \"OrganizationId\" IS NULL;");
        migrationBuilder.Sql($"UPDATE \"Buildings\" SET \"OrganizationId\" = '{defaultOrgId}' WHERE \"OrganizationId\" IS NULL;");
        migrationBuilder.Sql($"UPDATE \"Campaigns\" SET \"OrganizationId\" = '{defaultOrgId}' WHERE \"OrganizationId\" IS NULL;");
        migrationBuilder.Sql($"UPDATE \"Creatives\" SET \"OrganizationId\" = '{defaultOrgId}' WHERE \"OrganizationId\" IS NULL;");
        migrationBuilder.Sql($"UPDATE \"DailyPlaylists\" SET \"OrganizationId\" = '{defaultOrgId}' WHERE \"OrganizationId\" IS NULL;");
        migrationBuilder.Sql($"UPDATE \"ProofOfPlayEvents\" SET \"OrganizationId\" = '{defaultOrgId}' WHERE \"OrganizationId\" IS NULL;");
        migrationBuilder.Sql($"UPDATE \"Screens\" SET \"OrganizationId\" = '{defaultOrgId}' WHERE \"OrganizationId\" IS NULL;");

        // Step 4: Make the columns non-nullable and add FK constraints + indexes.
        migrationBuilder.AlterColumn<Guid>(
            name: "OrganizationId",
            table: "Advertisers",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "OrganizationId",
            table: "Buildings",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "OrganizationId",
            table: "Campaigns",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "OrganizationId",
            table: "Creatives",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "OrganizationId",
            table: "DailyPlaylists",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "OrganizationId",
            table: "ProofOfPlayEvents",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "OrganizationId",
            table: "Screens",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Advertisers_OrganizationId",
            table: "Advertisers",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Buildings_OrganizationId",
            table: "Buildings",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Campaigns_OrganizationId",
            table: "Campaigns",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Creatives_OrganizationId",
            table: "Creatives",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyPlaylists_OrganizationId",
            table: "DailyPlaylists",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_ProofOfPlayEvents_OrganizationId",
            table: "ProofOfPlayEvents",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Screens_OrganizationId",
            table: "Screens",
            column: "OrganizationId");

        migrationBuilder.AddForeignKey(
            name: "FK_Advertisers_Organizations_OrganizationId",
            table: "Advertisers",
            column: "OrganizationId",
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Buildings_Organizations_OrganizationId",
            table: "Buildings",
            column: "OrganizationId",
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Campaigns_Organizations_OrganizationId",
            table: "Campaigns",
            column: "OrganizationId",
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Creatives_Organizations_OrganizationId",
            table: "Creatives",
            column: "OrganizationId",
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DailyPlaylists_Organizations_OrganizationId",
            table: "DailyPlaylists",
            column: "OrganizationId",
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ProofOfPlayEvents_Organizations_OrganizationId",
            table: "ProofOfPlayEvents",
            column: "OrganizationId",
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Screens_Organizations_OrganizationId",
            table: "Screens",
            column: "OrganizationId",
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Advertisers_Organizations_OrganizationId",
            table: "Advertisers");

        migrationBuilder.DropForeignKey(
            name: "FK_Buildings_Organizations_OrganizationId",
            table: "Buildings");

        migrationBuilder.DropForeignKey(
            name: "FK_Campaigns_Organizations_OrganizationId",
            table: "Campaigns");

        migrationBuilder.DropForeignKey(
            name: "FK_Creatives_Organizations_OrganizationId",
            table: "Creatisers");

        migrationBuilder.DropForeignKey(
            name: "FK_DailyPlaylists_Organizations_OrganizationId",
            table: "DailyPlaylists");

        migrationBuilder.DropForeignKey(
            name: "FK_ProofOfPlayEvents_Organizations_OrganizationId",
            table: "ProofOfPlayEvents");

        migrationBuilder.DropForeignKey(
            name: "FK_Screens_Organizations_OrganizationId",
            table: "Screens");

        migrationBuilder.DropIndex(
            name: "IX_Advertisers_OrganizationId",
            table: "Advertisers");

        migrationBuilder.DropIndex(
            name: "IX_Buildings_OrganizationId",
            table: "Buildings");

        migrationBuilder.DropIndex(
            name: "IX_Campaigns_OrganizationId",
            table: "Campaigns");

        migrationBuilder.DropIndex(
            name: "IX_Creatives_OrganizationId",
            table: "Creatives");

        migrationBuilder.DropIndex(
            name: "IX_DailyPlaylists_OrganizationId",
            table: "DailyPlaylists");

        migrationBuilder.DropIndex(
            name: "IX_ProofOfPlayEvents_OrganizationId",
            table: "ProofOfPlayEvents");

        migrationBuilder.DropIndex(
            name: "IX_Screens_OrganizationId",
            table: "Screens");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Advertisers");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Buildings");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Creatives");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "DailyPlaylists");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "ProofOfPlayEvents");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Screens");

        migrationBuilder.DropTable(
            name: "Organizations");
    }
    }
}
