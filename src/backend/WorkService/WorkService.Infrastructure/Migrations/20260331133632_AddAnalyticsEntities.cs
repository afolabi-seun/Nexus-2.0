using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectHealthSnapshots",
                columns: table => new
                {
                    ProjectHealthSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    VelocityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    BugRateScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OverdueScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Trend = table.Column<string>(type: "text", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectHealthSnapshots", x => x.ProjectHealthSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "ResourceAllocationSnapshots",
                columns: table => new
                {
                    ResourceAllocationSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalLoggedHours = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedHours = table.Column<decimal>(type: "numeric", nullable: false),
                    UtilizationPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    BillableHours = table.Column<decimal>(type: "numeric", nullable: false),
                    NonBillableHours = table.Column<decimal>(type: "numeric", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "numeric", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAllocationSnapshots", x => x.ResourceAllocationSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "RiskRegisters",
                columns: table => new
                {
                    RiskRegisterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<string>(type: "text", nullable: false, defaultValue: "Medium"),
                    Likelihood = table.Column<string>(type: "text", nullable: false, defaultValue: "Medium"),
                    MitigationStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "Open"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FlgStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "A"),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskRegisters", x => x.RiskRegisterId);
                });

            migrationBuilder.CreateTable(
                name: "VelocitySnapshots",
                columns: table => new
                {
                    VelocitySnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintName = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CommittedPoints = table.Column<int>(type: "integer", nullable: false),
                    CompletedPoints = table.Column<int>(type: "integer", nullable: false),
                    TotalLoggedHours = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageHoursPerPoint = table.Column<decimal>(type: "numeric", nullable: true),
                    CompletedStoryCount = table.Column<int>(type: "integer", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VelocitySnapshots", x => x.VelocitySnapshotId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectHealthSnapshots_ProjectId_SnapshotDate",
                table: "ProjectHealthSnapshots",
                columns: new[] { "ProjectId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationSnapshots_ProjectId_MemberId_PeriodStart_~",
                table: "ResourceAllocationSnapshots",
                columns: new[] { "ProjectId", "MemberId", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationSnapshots_ProjectId_PeriodStart",
                table: "ResourceAllocationSnapshots",
                columns: new[] { "ProjectId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskRegisters_OrganizationId_ProjectId",
                table: "RiskRegisters",
                columns: new[] { "OrganizationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskRegisters_OrganizationId_ProjectId_SprintId",
                table: "RiskRegisters",
                columns: new[] { "OrganizationId", "ProjectId", "SprintId" });

            migrationBuilder.CreateIndex(
                name: "IX_VelocitySnapshots_ProjectId_EndDate",
                table: "VelocitySnapshots",
                columns: new[] { "ProjectId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VelocitySnapshots_ProjectId_SprintId",
                table: "VelocitySnapshots",
                columns: new[] { "ProjectId", "SprintId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectHealthSnapshots");

            migrationBuilder.DropTable(
                name: "ResourceAllocationSnapshots");

            migrationBuilder.DropTable(
                name: "RiskRegisters");

            migrationBuilder.DropTable(
                name: "VelocitySnapshots");
        }
    }
}
