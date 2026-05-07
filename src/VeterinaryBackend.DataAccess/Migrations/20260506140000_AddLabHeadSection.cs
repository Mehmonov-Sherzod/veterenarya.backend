using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryBackend.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLabHeadSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "lab_heads",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lab_heads_SectionId",
                table: "lab_heads",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_lab_heads_sections_SectionId",
                table: "lab_heads",
                column: "SectionId",
                principalTable: "sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lab_heads_sections_SectionId",
                table: "lab_heads");

            migrationBuilder.DropIndex(
                name: "IX_lab_heads_SectionId",
                table: "lab_heads");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "lab_heads");
        }
    }
}
