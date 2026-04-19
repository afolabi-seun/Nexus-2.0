using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoryTemplates",
                columns: table => new
                {
                    StoryTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultTitle = table.Column<string>(type: "text", nullable: true),
                    DefaultDescription = table.Column<string>(type: "text", nullable: true),
                    DefaultAcceptanceCriteria = table.Column<string>(type: "text", nullable: true),
                    DefaultPriority = table.Column<string>(type: "text", nullable: false),
                    DefaultStoryPoints = table.Column<int>(type: "integer", nullable: true),
                    DefaultLabelsJson = table.Column<string>(type: "text", nullable: true),
                    DefaultTaskTypesJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryTemplates", x => x.StoryTemplateId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoryTemplates");
        }
    }
}
