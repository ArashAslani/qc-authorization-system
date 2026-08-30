using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewFindingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentRoleId",
                table: "Role",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "Delegation",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TraceId",
                table: "AccessDecisionLog",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Role_ParentRoleId",
                table: "Role",
                column: "ParentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDecisionLog_TraceId",
                table: "AccessDecisionLog",
                column: "TraceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Role_ParentRoleId",
                table: "Role",
                column: "ParentRoleId",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Role_Role_ParentRoleId",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_Role_ParentRoleId",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_AccessDecisionLog_TraceId",
                table: "AccessDecisionLog");

            migrationBuilder.DropColumn(
                name: "ParentRoleId",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "Delegation");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "AccessDecisionLog");
        }
    }
}
