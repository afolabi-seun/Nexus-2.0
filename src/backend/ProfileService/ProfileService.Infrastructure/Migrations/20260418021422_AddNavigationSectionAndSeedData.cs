using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProfileService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationSectionAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "NavigationItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "NavigationItems",
                columns: new[] { "NavigationItemId", "DateCreated", "DateUpdated", "Icon", "IsEnabled", "Label", "MinPermissionLevel", "ParentId", "Path", "Section", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "LayoutDashboard", true, "Dashboard", 25, null, "/", "Work", 1 },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FolderKanban", true, "Projects", 25, null, "/projects", "Work", 2 },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BookOpen", true, "Stories", 25, null, "/stories", "Work", 3 },
                    { new Guid("a0000000-0000-0000-0000-000000000004"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Columns3", true, "Boards", 25, null, "/boards", "Work", 4 },
                    { new Guid("a0000000-0000-0000-0000-000000000005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Timer", true, "Sprints", 25, null, "/sprints", "Work", 5 },
                    { new Guid("a0000000-0000-0000-0000-000000000006"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Clock", true, "Time Tracking", 50, null, "/time-tracking", "Tracking", 1 },
                    { new Guid("a0000000-0000-0000-0000-000000000007"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TrendingUp", true, "Analytics", 25, null, "/analytics", "Tracking", 2 },
                    { new Guid("a0000000-0000-0000-0000-000000000008"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BarChart3", true, "Reports", 25, null, "/reports", "Tracking", 3 },
                    { new Guid("a0000000-0000-0000-0000-000000000009"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Users", true, "Members", 25, null, "/members", "Team", 1 },
                    { new Guid("a0000000-0000-0000-0000-000000000010"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Building2", true, "Departments", 25, null, "/departments", "Team", 2 },
                    { new Guid("a0000000-0000-0000-0000-000000000011"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mail", true, "Invites", 75, null, "/invites", "Team", 3 },
                    { new Guid("a0000000-0000-0000-0000-000000000012"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Settings", true, "Settings", 100, null, "/settings", "Organization", 1 },
                    { new Guid("a0000000-0000-0000-0000-000000000013"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CreditCard", true, "Billing", 100, null, "/billing", "Organization", 2 },
                    { new Guid("a0000000-0000-0000-0000-000000000014"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ClipboardList", true, "Audit Logs", 100, null, "/audit-logs", "Organization", 3 },
                    { new Guid("a0000000-0000-0000-0000-000000000015"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bell", true, "Notifications", 75, null, "/notifications", "Organization", 4 },
                    { new Guid("a0000000-0000-0000-0000-000000000041"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kanban", true, "Kanban", 25, new Guid("a0000000-0000-0000-0000-000000000004"), "/boards/kanban", "Work", 1 },
                    { new Guid("a0000000-0000-0000-0000-000000000042"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CalendarDays", true, "Sprint Board", 25, new Guid("a0000000-0000-0000-0000-000000000004"), "/boards/sprint", "Work", 2 },
                    { new Guid("a0000000-0000-0000-0000-000000000043"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Building2", true, "Dept Board", 25, new Guid("a0000000-0000-0000-0000-000000000004"), "/boards/department", "Work", 3 },
                    { new Guid("a0000000-0000-0000-0000-000000000044"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Archive", true, "Backlog", 25, new Guid("a0000000-0000-0000-0000-000000000004"), "/boards/backlog", "Work", 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                table: "NavigationItems",
                keyColumn: "NavigationItemId",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "Section",
                table: "NavigationItems");
        }
    }
}
