using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qc_authorization.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationUserPersonnelFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PersonnelId",
                table: "AspNetUsers",
                column: "PersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Personnel_PersonnelId",
                table: "AspNetUsers",
                column: "PersonnelId",
                principalTable: "Personnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Personnel_PersonnelId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PersonnelId",
                table: "AspNetUsers");
        }
    }
}
