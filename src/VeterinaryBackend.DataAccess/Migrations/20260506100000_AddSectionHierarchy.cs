using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryBackend.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "sections",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sections_ParentId",
                table: "sections",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_sections_sections_ParentId",
                table: "sections",
                column: "ParentId",
                principalTable: "sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sections_sections_ParentId",
                table: "sections");

            migrationBuilder.DropIndex(
                name: "IX_sections_ParentId",
                table: "sections");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "sections");
        }
    }
}
