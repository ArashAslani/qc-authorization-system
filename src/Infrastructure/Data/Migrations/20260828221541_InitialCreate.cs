using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qc_authorization.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationAuditEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationAuditEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personnel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NationalId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PersonalCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Position",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Position", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Position_Position_ParentPositionId",
                        column: x => x.ParentPositionId,
                        principalTable: "Position",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PositionAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonnelId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionAssignment_Personnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "Personnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PositionAssignment_Position_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Position",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResourceCatalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    ActionCatalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Resource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_ActionCatalog_ActionCatalogId",
                        column: x => x.ActionCatalogId,
                        principalTable: "ActionCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permission_ResourceCatalog_ResourceCatalogId",
                        column: x => x.ResourceCatalogId,
                        principalTable: "ResourceCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleGroupMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroupMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleGroupMember_RoleGroup_RoleGroupId",
                        column: x => x.RoleGroupId,
                        principalTable: "RoleGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleGroupMember_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Delegation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DelegatorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DelegateUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScopeKind = table.Column<int>(type: "INTEGER", nullable: false),
                    ScopeIdentifier = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ValidFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Delegable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Delegation_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubjectType = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Resource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ScopeKind = table.Column<int>(type: "INTEGER", nullable: false),
                    ScopeIdentifier = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Effect = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceType = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grant_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrantConstraint",
                columns: table => new
                {
                    GrantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    TimeFrom = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    TimeTo = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    ScopeKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ScopeValue = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrantConstraint", x => new { x.GrantId, x.Id });
                    table.ForeignKey(
                        name: "FK_GrantConstraint_Grant_GrantId",
                        column: x => x.GrantId,
                        principalTable: "Grant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionCatalog_Code",
                table: "ActionCatalog",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditEntry_Created",
                table: "AuthorizationAuditEntry",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditEntry_EventType",
                table: "AuthorizationAuditEntry",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_DelegateUserId_PermissionId",
                table: "Delegation",
                columns: new[] { "DelegateUserId", "PermissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_DelegatorUserId",
                table: "Delegation",
                column: "DelegatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_PermissionId",
                table: "Delegation",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Grant_PermissionId",
                table: "Grant",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Grant_SourceType_SourceId",
                table: "Grant",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Grant_SubjectType_SubjectId_PermissionId",
                table: "Grant",
                columns: new[] { "SubjectType", "SubjectId", "PermissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Permission_ActionCatalogId",
                table: "Permission",
                column: "ActionCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Code",
                table: "Permission",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permission_ResourceCatalogId",
                table: "Permission",
                column: "ResourceCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_NationalId",
                table: "Personnel",
                column: "NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_PersonalCode",
                table: "Personnel",
                column: "PersonalCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_SystemUserId",
                table: "Personnel",
                column: "SystemUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Position_CompanyId",
                table: "Position",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Position_CompanyId_Code",
                table: "Position",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Position_ParentPositionId",
                table: "Position",
                column: "ParentPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignment_PersonnelId_PositionId_ValidFrom",
                table: "PositionAssignment",
                columns: new[] { "PersonnelId", "PositionId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignment_PositionId",
                table: "PositionAssignment",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCatalog_Code",
                table: "ResourceCatalog",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_Code",
                table: "Role",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroup_Code",
                table: "RoleGroup",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroupMember_RoleGroupId_RoleId",
                table: "RoleGroupMember",
                columns: new[] { "RoleGroupId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroupMember_RoleId",
                table: "RoleGroupMember",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RoleId_PermissionId",
                table: "RolePermission",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizationAuditEntry");

            migrationBuilder.DropTable(
                name: "Delegation");

            migrationBuilder.DropTable(
                name: "GrantConstraint");

            migrationBuilder.DropTable(
                name: "PositionAssignment");

            migrationBuilder.DropTable(
                name: "RoleGroupMember");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "Grant");

            migrationBuilder.DropTable(
                name: "Personnel");

            migrationBuilder.DropTable(
                name: "Position");

            migrationBuilder.DropTable(
                name: "RoleGroup");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "ActionCatalog");

            migrationBuilder.DropTable(
                name: "ResourceCatalog");
        }
    }
}
