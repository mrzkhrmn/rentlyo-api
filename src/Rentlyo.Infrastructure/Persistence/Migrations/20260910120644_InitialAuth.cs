using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rentlyo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), "vehicles.read", "Read vehicles" },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "vehicles.write", "Write vehicles" },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "reservations.read", "Read reservations" },
                    { new Guid("22222222-2222-2222-2222-222222222204"), "reservations.write", "Write reservations" },
                    { new Guid("22222222-2222-2222-2222-222222222205"), "customers.read", "Read customers" },
                    { new Guid("22222222-2222-2222-2222-222222222206"), "customers.write", "Write customers" },
                    { new Guid("22222222-2222-2222-2222-222222222207"), "settings.manage", "Manage settings" },
                    { new Guid("22222222-2222-2222-2222-222222222208"), "employees.manage", "Manage employees" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "Owner" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "Admin" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "Manager" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "Employee" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222202"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222203"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222204"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222205"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222206"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222207"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222208"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-222222222201"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222202"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222203"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222204"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222205"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222206"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222207"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222208"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-222222222201"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-222222222202"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-222222222203"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-222222222204"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-222222222205"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-222222222206"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-222222222201"), new Guid("11111111-1111-1111-1111-111111111104") },
                    { new Guid("22222222-2222-2222-2222-222222222203"), new Guid("11111111-1111-1111-1111-111111111104") },
                    { new Guid("22222222-2222-2222-2222-222222222204"), new Guid("11111111-1111-1111-1111-111111111104") },
                    { new Guid("22222222-2222-2222-2222-222222222205"), new Guid("11111111-1111-1111-1111-111111111104") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
