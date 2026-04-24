using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryTypeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultStoryType",
                table: "StoryTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoryType",
                table: "Stories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Feature");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultStoryType",
                table: "StoryTemplates");

            migrationBuilder.DropColumn(
                name: "StoryType",
                table: "Stories");
        }
    }
}
