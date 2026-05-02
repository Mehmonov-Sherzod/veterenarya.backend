using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryBackend.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePosition",
                table: "contents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "top");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePosition",
                table: "contents");
        }
    }
}
