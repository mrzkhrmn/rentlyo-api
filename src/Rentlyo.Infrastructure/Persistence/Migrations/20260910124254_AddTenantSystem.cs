using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rentlyo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxVehicles = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Code", "IsActive", "MaxUsers", "MaxVehicles", "Name" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333301"), "free", true, 2, 5, "Free" },
                    { new Guid("33333333-3333-3333-3333-333333333302"), "starter", true, 10, 25, "Starter" },
                    { new Guid("33333333-3333-3333-3333-333333333303"), "pro", true, 50, 100, "Pro" }
                });

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Tenants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Tenants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Tenants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Tenants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("33333333-3333-3333-3333-333333333301"));

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO TenantSettings (TenantId, Timezone, Currency, Locale)
                SELECT Id, N'Europe/Istanbul', N'TRY', N'tr-TR'
                FROM Tenants t
                WHERE NOT EXISTS (
                    SELECT 1 FROM TenantSettings s WHERE s.TenantId = t.Id
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PlanId",
                table: "Tenants",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_SubscriptionPlans_PlanId",
                table: "Tenants",
                column: "PlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_SubscriptionPlans_PlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_PlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Tenants");
        }
    }
}
