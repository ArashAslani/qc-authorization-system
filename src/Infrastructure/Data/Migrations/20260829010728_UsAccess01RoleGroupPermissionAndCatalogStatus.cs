using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qc_authorization.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UsAccess01RoleGroupPermissionAndCatalogStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "RoleGroup",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Role",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoleGroupPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroupPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleGroupPermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleGroupPermission_RoleGroup_RoleGroupId",
                        column: x => x.RoleGroupId,
                        principalTable: "RoleGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroupPermission_PermissionId",
                table: "RoleGroupPermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroupPermission_RoleGroupId_PermissionId",
                table: "RoleGroupPermission",
                columns: new[] { "RoleGroupId", "PermissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleGroupPermission");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RoleGroup");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Role");
        }
    }
}
