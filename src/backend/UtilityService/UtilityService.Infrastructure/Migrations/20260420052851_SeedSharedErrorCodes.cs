using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UtilityService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSharedErrorCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ErrorCodeEntries",
                columns: new[] { "ErrorCodeEntryId", "Code", "DateCreated", "DateUpdated", "Description", "HttpStatusCode", "ResponseCode", "ServiceName", "Value" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000009001"), "UNIQUE_CONSTRAINT_VIOLATION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A record with this value already exists (database constraint).", 409, "06", "All", 9001 },
                    { new Guid("b0000000-0000-0000-0000-000000009002"), "FOREIGN_KEY_VIOLATION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Referenced record does not exist or cannot be removed.", 409, "06", "All", 9002 },
                    { new Guid("b0000000-0000-0000-0000-000000009003"), "TOKEN_EXPIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "JWT token has expired. Refresh your session.", 401, "03", "All", 9003 },
                    { new Guid("b0000000-0000-0000-0000-000000009004"), "INVALID_TOKEN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Invalid or malformed authentication token.", 401, "03", "All", 9004 },
                    { new Guid("b0000000-0000-0000-0000-000000009999"), "INTERNAL_ERROR", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "An unexpected internal error occurred.", 500, "98", "All", 9999 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000009001"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000009002"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000009003"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000009004"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000009999"));
        }
    }
}
