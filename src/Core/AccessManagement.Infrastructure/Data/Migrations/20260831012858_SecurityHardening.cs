using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Delegable",
                table: "Delegation",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentDelegationId",
                table: "Delegation",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveCompanyId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RevokedAccessTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Jti = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedAccessTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_IsSystemUser",
                table: "Personnel",
                column: "IsSystemUser",
                unique: true,
                filter: "\"IsSystemUser\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_ParentDelegationId",
                table: "Delegation",
                column: "ParentDelegationId");

            migrationBuilder.CreateIndex(
                name: "IX_RevokedAccessTokens_ExpiresAtUtc",
                table: "RevokedAccessTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RevokedAccessTokens_Jti",
                table: "RevokedAccessTokens",
                column: "Jti",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevokedAccessTokens");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_IsSystemUser",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Delegation_ParentDelegationId",
                table: "Delegation");

            migrationBuilder.DropColumn(
                name: "ParentDelegationId",
                table: "Delegation");

            migrationBuilder.DropColumn(
                name: "ActiveCompanyId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<bool>(
                name: "Delegable",
                table: "Delegation",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);
        }
    }
}
