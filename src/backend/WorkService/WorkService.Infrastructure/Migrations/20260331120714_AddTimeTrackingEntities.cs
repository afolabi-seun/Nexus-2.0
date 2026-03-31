using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeTrackingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostRates",
                columns: table => new
                {
                    CostRateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateType = table.Column<string>(type: "text", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleName = table.Column<string>(type: "text", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FlgStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "A"),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostRates", x => x.CostRateId);
                });

            migrationBuilder.CreateTable(
                name: "CostSnapshots",
                columns: table => new
                {
                    CostSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalBillableHours = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalNonBillableHours = table.Column<decimal>(type: "numeric", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSnapshots", x => x.CostSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "TimeApprovals",
                columns: table => new
                {
                    TimeApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeApprovals", x => x.TimeApprovalId);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    TimeEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBillable = table.Column<bool>(type: "boolean", nullable: false),
                    IsOvertime = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    FlgStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "A"),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntries", x => x.TimeEntryId);
                });

            migrationBuilder.CreateTable(
                name: "TimePolicies",
                columns: table => new
                {
                    TimePolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredHoursPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    OvertimeThresholdHoursPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    ApprovalRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalWorkflow = table.Column<string>(type: "text", nullable: false),
                    MaxDailyHours = table.Column<decimal>(type: "numeric", nullable: false),
                    FlgStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "A"),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimePolicies", x => x.TimePolicyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostRates_OrganizationId_RateType_MemberId_RoleName_Departm~",
                table: "CostRates",
                columns: new[] { "OrganizationId", "RateType", "MemberId", "RoleName", "DepartmentId" },
                unique: true,
                filter: "\"FlgStatus\" = 'A'");

            migrationBuilder.CreateIndex(
                name: "IX_CostSnapshots_ProjectId_PeriodStart_PeriodEnd",
                table: "CostSnapshots",
                columns: new[] { "ProjectId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeApprovals_TimeEntryId",
                table: "TimeApprovals",
                column: "TimeEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_OrganizationId_MemberId_Date",
                table: "TimeEntries",
                columns: new[] { "OrganizationId", "MemberId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_OrganizationId_Status",
                table: "TimeEntries",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_OrganizationId_StoryId",
                table: "TimeEntries",
                columns: new[] { "OrganizationId", "StoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimePolicies_OrganizationId",
                table: "TimePolicies",
                column: "OrganizationId",
                unique: true,
                filter: "\"FlgStatus\" = 'A'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostRates");

            migrationBuilder.DropTable(
                name: "CostSnapshots");

            migrationBuilder.DropTable(
                name: "TimeApprovals");

            migrationBuilder.DropTable(
                name: "TimeEntries");

            migrationBuilder.DropTable(
                name: "TimePolicies");
        }
    }
}
