using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TddOrganizationalUnitAndScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Personnel_PersonalCode",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "ScopeIdentifier",
                table: "Grant");

            migrationBuilder.DropColumn(
                name: "ScopeKind",
                table: "Grant");

            migrationBuilder.DropColumn(
                name: "ScopeIdentifier",
                table: "Delegation");

            migrationBuilder.DropColumn(
                name: "ScopeKind",
                table: "Delegation");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "Position",
                newName: "CompanyUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Position_CompanyId_Code",
                table: "Position",
                newName: "IX_Position_CompanyUnitId_Code");

            migrationBuilder.RenameIndex(
                name: "IX_Position_CompanyId",
                table: "Position",
                newName: "IX_Position_CompanyUnitId");

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Personnel",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemUser",
                table: "Personnel",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PersonnelCode",
                table: "Personnel",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("UPDATE Personnel SET PersonnelCode = PersonalCode;");

            migrationBuilder.DropColumn(
                name: "PersonalCode",
                table: "Personnel");

            migrationBuilder.AddColumn<string>(
                name: "PluginCode",
                table: "Permission",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "CORE");

            migrationBuilder.AddColumn<Guid>(
                name: "ScopeUnitId",
                table: "Grant",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScopeUnitId",
                table: "Delegation",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessDecisionLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivePositionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PermissionCode = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ScopeUnitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CandidateGrantsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessDecisionLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleScopeConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResourceCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaxScopeUnitType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleScopeConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationalUnit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UnitType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationalUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationalUnit_OrganizationalUnit_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrganizationalUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_PersonnelCode",
                table: "Personnel",
                column: "PersonnelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grant_ScopeUnitId",
                table: "Grant",
                column: "ScopeUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_ScopeUnitId",
                table: "Delegation",
                column: "ScopeUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDecisionLog_CreatedAt",
                table: "AccessDecisionLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDecisionLog_RequestedByUserId",
                table: "AccessDecisionLog",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleScopeConfig_ResourceCode",
                table: "ModuleScopeConfig",
                column: "ResourceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnit_ParentId",
                table: "OrganizationalUnit",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnit_UnitType",
                table: "OrganizationalUnit",
                column: "UnitType");

            migrationBuilder.Sql("""
                INSERT INTO OrganizationalUnit (Id, ParentId, UnitType, Name, Created, LastModified)
                SELECT DISTINCT p.CompanyUnitId, NULL, 'Company', 'Company', '2026-01-01 00:00:00', '2026-01-01 00:00:00'
                FROM Position p
                WHERE p.CompanyUnitId IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM OrganizationalUnit u WHERE u.Id = p.CompanyUnitId);
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Position_OrganizationalUnit_CompanyUnitId",
                table: "Position",
                column: "CompanyUnitId",
                principalTable: "OrganizationalUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Position_OrganizationalUnit_CompanyUnitId",
                table: "Position");

            migrationBuilder.DropTable(
                name: "AccessDecisionLog");

            migrationBuilder.DropTable(
                name: "ModuleScopeConfig");

            migrationBuilder.DropTable(
                name: "OrganizationalUnit");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_PersonnelCode",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Grant_ScopeUnitId",
                table: "Grant");

            migrationBuilder.DropIndex(
                name: "IX_Delegation_ScopeUnitId",
                table: "Delegation");

            migrationBuilder.DropColumn(
                name: "IsSystemUser",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "PersonnelCode",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "PluginCode",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "ScopeUnitId",
                table: "Grant");

            migrationBuilder.DropColumn(
                name: "ScopeUnitId",
                table: "Delegation");

            migrationBuilder.RenameColumn(
                name: "CompanyUnitId",
                table: "Position",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_Position_CompanyUnitId_Code",
                table: "Position",
                newName: "IX_Position_CompanyId_Code");

            migrationBuilder.RenameIndex(
                name: "IX_Position_CompanyUnitId",
                table: "Position",
                newName: "IX_Position_CompanyId");

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Personnel",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalCode",
                table: "Personnel",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScopeIdentifier",
                table: "Grant",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScopeKind",
                table: "Grant",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScopeIdentifier",
                table: "Delegation",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScopeKind",
                table: "Delegation",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_PersonalCode",
                table: "Personnel",
                column: "PersonalCode",
                unique: true);
        }
    }
}
