using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VeterinaryBackend.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "contents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TitleUz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitleRu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contents_SectionId",
                table: "contents",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_sections_IsActive",
                table: "sections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_sections_Slug",
                table: "sections",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sections_SortOrder",
                table: "sections",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_contents_sections_SectionId",
                table: "contents",
                column: "SectionId",
                principalTable: "sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contents_sections_SectionId",
                table: "contents");

            migrationBuilder.DropTable(
                name: "sections");

            migrationBuilder.DropIndex(
                name: "IX_contents_SectionId",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "contents");
        }
    }
}
