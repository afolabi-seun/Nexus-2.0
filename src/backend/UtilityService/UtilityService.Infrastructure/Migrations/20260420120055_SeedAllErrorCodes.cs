using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UtilityService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAllErrorCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ErrorCodeEntries_Code",
                table: "ErrorCodeEntries");

            migrationBuilder.InsertData(
                table: "ErrorCodeEntries",
                columns: new[] { "ErrorCodeEntryId", "Code", "DateCreated", "DateUpdated", "Description", "HttpStatusCode", "ResponseCode", "ServiceName", "Value" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000001000"), "VALIDATION_ERROR", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Request validation failed. Check the errors array for details.", 400, "96", "All", 1000 },
                    { new Guid("b0000000-0000-0000-0000-000000002001"), "INVALID_CREDENTIALS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The provided username or password is incorrect.", 401, "03", "SecurityService", 2001 },
                    { new Guid("b0000000-0000-0000-0000-000000002002"), "ACCOUNT_LOCKED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Account has been locked due to too many failed attempts.", 401, "03", "SecurityService", 2002 },
                    { new Guid("b0000000-0000-0000-0000-000000002003"), "ACCOUNT_INACTIVE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Account is deactivated. Contact your administrator.", 401, "03", "SecurityService", 2003 },
                    { new Guid("b0000000-0000-0000-0000-000000002004"), "PASSWORD_REUSE_NOT_ALLOWED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot reuse a previous password.", 400, "96", "SecurityService", 2004 },
                    { new Guid("b0000000-0000-0000-0000-000000002005"), "PASSWORD_RECENTLY_USED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This password was used recently. Choose a different one.", 400, "96", "SecurityService", 2005 },
                    { new Guid("b0000000-0000-0000-0000-000000002006"), "FIRST_TIME_USER_RESTRICTED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "First-time users must change their password before proceeding.", 400, "96", "SecurityService", 2006 },
                    { new Guid("b0000000-0000-0000-0000-000000002007"), "OTP_EXPIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The OTP code has expired. Request a new one.", 400, "96", "SecurityService", 2007 },
                    { new Guid("b0000000-0000-0000-0000-000000002008"), "OTP_VERIFICATION_FAILED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The OTP code is incorrect.", 400, "96", "SecurityService", 2008 },
                    { new Guid("b0000000-0000-0000-0000-000000002009"), "OTP_MAX_ATTEMPTS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Maximum OTP verification attempts exceeded.", 400, "96", "SecurityService", 2009 },
                    { new Guid("b0000000-0000-0000-0000-000000002010"), "RATE_LIMIT_EXCEEDED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Too many requests. Please wait before trying again.", 429, "08", "SecurityService", 2010 },
                    { new Guid("b0000000-0000-0000-0000-000000002011"), "INSUFFICIENT_PERMISSIONS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You do not have permission to perform this action.", 403, "03", "SecurityService", 2011 },
                    { new Guid("b0000000-0000-0000-0000-000000002012"), "TOKEN_REVOKED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This token has been revoked.", 401, "03", "SecurityService", 2012 },
                    { new Guid("b0000000-0000-0000-0000-000000002013"), "REFRESH_TOKEN_REUSE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Refresh token reuse detected. All sessions revoked for security.", 401, "03", "SecurityService", 2013 },
                    { new Guid("b0000000-0000-0000-0000-000000002016"), "SERVICE_NOT_AUTHORIZED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Service-to-service authentication failed.", 403, "03", "SecurityService", 2016 },
                    { new Guid("b0000000-0000-0000-0000-000000002017"), "SUSPICIOUS_LOGIN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Login attempt flagged as suspicious.", 403, "03", "SecurityService", 2017 },
                    { new Guid("b0000000-0000-0000-0000-000000002018"), "PASSWORD_COMPLEXITY_FAILED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Password does not meet complexity requirements.", 400, "96", "SecurityService", 2018 },
                    { new Guid("b0000000-0000-0000-0000-000000002019"), "ORGANIZATION_MISMATCH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You cannot access resources from another organization.", 403, "03", "SecurityService", 2019 },
                    { new Guid("b0000000-0000-0000-0000-000000002020"), "DEPARTMENT_ACCESS_DENIED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You do not have access to this department.", 403, "03", "SecurityService", 2020 },
                    { new Guid("b0000000-0000-0000-0000-000000002021"), "NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The requested resource was not found.", 404, "07", "SecurityService", 2021 },
                    { new Guid("b0000000-0000-0000-0000-000000002022"), "CONFLICT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A conflict occurred with the current state of the resource.", 409, "06", "SecurityService", 2022 },
                    { new Guid("b0000000-0000-0000-0000-000000002023"), "SERVICE_UNAVAILABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The service is temporarily unavailable.", 503, "98", "SecurityService", 2023 },
                    { new Guid("b0000000-0000-0000-0000-000000002024"), "SESSION_EXPIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Your session has expired. Please log in again.", 401, "03", "SecurityService", 2024 },
                    { new Guid("b0000000-0000-0000-0000-000000002025"), "INVALID_DEPARTMENT_ROLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified department role is invalid.", 400, "96", "SecurityService", 2025 },
                    { new Guid("b0000000-0000-0000-0000-000000003001"), "EMAIL_ALREADY_REGISTERED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This email address is already registered.", 409, "06", "ProfileService", 3001 },
                    { new Guid("b0000000-0000-0000-0000-000000003002"), "INVITE_EXPIRED_OR_INVALID", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The invitation has expired or is invalid.", 400, "96", "ProfileService", 3002 },
                    { new Guid("b0000000-0000-0000-0000-000000003003"), "MAX_DEVICES_REACHED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Maximum number of registered devices reached.", 400, "96", "ProfileService", 3003 },
                    { new Guid("b0000000-0000-0000-0000-000000003004"), "LAST_ORGADMIN_CANNOT_DEACTIVATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot deactivate the last organization admin.", 409, "06", "ProfileService", 3004 },
                    { new Guid("b0000000-0000-0000-0000-000000003005"), "ORGANIZATION_NAME_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "An organization with this name already exists.", 409, "06", "ProfileService", 3005 },
                    { new Guid("b0000000-0000-0000-0000-000000003006"), "STORY_PREFIX_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This story prefix is already in use.", 409, "06", "ProfileService", 3006 },
                    { new Guid("b0000000-0000-0000-0000-000000003007"), "STORY_PREFIX_IMMUTABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Story prefix cannot be changed after creation.", 400, "96", "ProfileService", 3007 },
                    { new Guid("b0000000-0000-0000-0000-000000003008"), "DEPARTMENT_NAME_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A department with this name already exists.", 409, "06", "ProfileService", 3008 },
                    { new Guid("b0000000-0000-0000-0000-000000003009"), "DEPARTMENT_CODE_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A department with this code already exists.", 409, "06", "ProfileService", 3009 },
                    { new Guid("b0000000-0000-0000-0000-000000003010"), "DEFAULT_DEPARTMENT_CANNOT_DELETE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The default department cannot be deleted.", 409, "06", "ProfileService", 3010 },
                    { new Guid("b0000000-0000-0000-0000-000000003011"), "MEMBER_ALREADY_IN_DEPARTMENT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This member is already assigned to this department.", 409, "06", "ProfileService", 3011 },
                    { new Guid("b0000000-0000-0000-0000-000000003012"), "MEMBER_MUST_HAVE_DEPARTMENT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A member must belong to at least one department.", 400, "96", "ProfileService", 3012 },
                    { new Guid("b0000000-0000-0000-0000-000000003013"), "INVALID_ROLE_ASSIGNMENT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified role assignment is invalid.", 400, "96", "ProfileService", 3013 },
                    { new Guid("b0000000-0000-0000-0000-000000003014"), "INVITE_EMAIL_ALREADY_MEMBER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This email is already a member of the organization.", 409, "06", "ProfileService", 3014 },
                    { new Guid("b0000000-0000-0000-0000-000000003015"), "ORGANIZATION_MISMATCH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You cannot access resources from another organization.", 403, "03", "ProfileService", 3015 },
                    { new Guid("b0000000-0000-0000-0000-000000003016"), "RATE_LIMIT_EXCEEDED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Too many requests. Please wait before trying again.", 429, "08", "ProfileService", 3016 },
                    { new Guid("b0000000-0000-0000-0000-000000003017"), "DEPARTMENT_HAS_ACTIVE_MEMBERS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot delete a department with active members.", 409, "06", "ProfileService", 3017 },
                    { new Guid("b0000000-0000-0000-0000-000000003018"), "MEMBER_NOT_IN_DEPARTMENT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The member is not in the specified department.", 400, "96", "ProfileService", 3018 },
                    { new Guid("b0000000-0000-0000-0000-000000003019"), "INVALID_AVAILABILITY_STATUS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified availability status is invalid.", 400, "96", "ProfileService", 3019 },
                    { new Guid("b0000000-0000-0000-0000-000000003020"), "STORY_PREFIX_INVALID_FORMAT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Story prefix must be 2-5 uppercase letters.", 400, "96", "ProfileService", 3020 },
                    { new Guid("b0000000-0000-0000-0000-000000003021"), "NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The requested resource was not found.", 404, "07", "ProfileService", 3021 },
                    { new Guid("b0000000-0000-0000-0000-000000003022"), "CONFLICT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A conflict occurred with the current state of the resource.", 409, "06", "ProfileService", 3022 },
                    { new Guid("b0000000-0000-0000-0000-000000003023"), "SERVICE_UNAVAILABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The service is temporarily unavailable.", 503, "98", "ProfileService", 3023 },
                    { new Guid("b0000000-0000-0000-0000-000000003024"), "DEPARTMENT_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified department was not found.", 404, "07", "ProfileService", 3024 },
                    { new Guid("b0000000-0000-0000-0000-000000003025"), "MEMBER_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified team member was not found.", 404, "07", "ProfileService", 3025 },
                    { new Guid("b0000000-0000-0000-0000-000000003026"), "INVALID_PREFERENCE_VALUE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The preference value is invalid.", 400, "96", "ProfileService", 3026 },
                    { new Guid("b0000000-0000-0000-0000-000000003027"), "PREFERENCE_KEY_UNKNOWN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified preference key is not recognized.", 400, "96", "ProfileService", 3027 },
                    { new Guid("b0000000-0000-0000-0000-000000003028"), "INSUFFICIENT_PERMISSIONS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You do not have permission to perform this action.", 403, "03", "ProfileService", 3028 },
                    { new Guid("b0000000-0000-0000-0000-000000003029"), "ORGADMIN_REQUIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This action requires OrgAdmin role or higher.", 403, "03", "ProfileService", 3029 },
                    { new Guid("b0000000-0000-0000-0000-000000003030"), "DEPTLEAD_REQUIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This action requires DeptLead role or higher.", 403, "03", "ProfileService", 3030 },
                    { new Guid("b0000000-0000-0000-0000-000000003031"), "PLATFORM_ADMIN_REQUIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This action requires PlatformAdmin role.", 403, "03", "ProfileService", 3031 },
                    { new Guid("b0000000-0000-0000-0000-000000004001"), "STORY_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified story was not found.", 404, "07", "WorkService", 4001 },
                    { new Guid("b0000000-0000-0000-0000-000000004002"), "TASK_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified task was not found.", 404, "07", "WorkService", 4002 },
                    { new Guid("b0000000-0000-0000-0000-000000004003"), "SPRINT_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified sprint was not found.", 404, "07", "WorkService", 4003 },
                    { new Guid("b0000000-0000-0000-0000-000000004004"), "INVALID_STORY_TRANSITION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Invalid story status transition.", 400, "96", "WorkService", 4004 },
                    { new Guid("b0000000-0000-0000-0000-000000004005"), "INVALID_TASK_TRANSITION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Invalid task status transition.", 400, "96", "WorkService", 4005 },
                    { new Guid("b0000000-0000-0000-0000-000000004006"), "SPRINT_NOT_IN_PLANNING", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sprint must be in Planning status for this operation.", 400, "96", "WorkService", 4006 },
                    { new Guid("b0000000-0000-0000-0000-000000004007"), "STORY_ALREADY_IN_SPRINT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This story is already assigned to the sprint.", 409, "06", "WorkService", 4007 },
                    { new Guid("b0000000-0000-0000-0000-000000004008"), "STORY_NOT_IN_SPRINT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This story is not in the specified sprint.", 400, "96", "WorkService", 4008 },
                    { new Guid("b0000000-0000-0000-0000-000000004009"), "SPRINT_OVERLAP", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sprint dates overlap with an existing sprint.", 400, "96", "WorkService", 4009 },
                    { new Guid("b0000000-0000-0000-0000-000000004010"), "LABEL_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified label was not found.", 404, "07", "WorkService", 4010 },
                    { new Guid("b0000000-0000-0000-0000-000000004011"), "LABEL_NAME_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A label with this name already exists.", 409, "06", "WorkService", 4011 },
                    { new Guid("b0000000-0000-0000-0000-000000004012"), "COMMENT_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified comment was not found.", 404, "07", "WorkService", 4012 },
                    { new Guid("b0000000-0000-0000-0000-000000004013"), "STORY_REQUIRES_ASSIGNEE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Story must have an assignee for this transition.", 400, "96", "WorkService", 4013 },
                    { new Guid("b0000000-0000-0000-0000-000000004014"), "STORY_REQUIRES_TASKS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Story must have tasks for this transition.", 400, "96", "WorkService", 4014 },
                    { new Guid("b0000000-0000-0000-0000-000000004015"), "STORY_REQUIRES_POINTS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Story must have story points assigned.", 400, "96", "WorkService", 4015 },
                    { new Guid("b0000000-0000-0000-0000-000000004016"), "ONLY_ONE_ACTIVE_SPRINT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Only one sprint can be active at a time per project.", 400, "96", "WorkService", 4016 },
                    { new Guid("b0000000-0000-0000-0000-000000004017"), "COMMENT_NOT_AUTHOR", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You can only edit or delete your own comments.", 403, "03", "WorkService", 4017 },
                    { new Guid("b0000000-0000-0000-0000-000000004018"), "ASSIGNEE_NOT_IN_DEPARTMENT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The assignee is not a member of the required department.", 400, "96", "WorkService", 4018 },
                    { new Guid("b0000000-0000-0000-0000-000000004019"), "ASSIGNEE_AT_CAPACITY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The assignee has reached their maximum task capacity.", 400, "96", "WorkService", 4019 },
                    { new Guid("b0000000-0000-0000-0000-000000004020"), "STORY_KEY_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "No story found with the specified key.", 404, "07", "WorkService", 4020 },
                    { new Guid("b0000000-0000-0000-0000-000000004021"), "SPRINT_ALREADY_ACTIVE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This sprint is already active.", 409, "06", "WorkService", 4021 },
                    { new Guid("b0000000-0000-0000-0000-000000004022"), "SPRINT_ALREADY_COMPLETED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This sprint has already been completed.", 409, "06", "WorkService", 4022 },
                    { new Guid("b0000000-0000-0000-0000-000000004023"), "INVALID_STORY_POINTS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Story points must be a valid Fibonacci number.", 400, "96", "WorkService", 4023 },
                    { new Guid("b0000000-0000-0000-0000-000000004024"), "INVALID_PRIORITY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified priority level is invalid.", 400, "96", "WorkService", 4024 },
                    { new Guid("b0000000-0000-0000-0000-000000004025"), "INVALID_TASK_TYPE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified task type is invalid.", 400, "96", "WorkService", 4025 },
                    { new Guid("b0000000-0000-0000-0000-000000004026"), "STORY_IN_ACTIVE_SPRINT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot modify a story that is in an active sprint.", 409, "06", "WorkService", 4026 },
                    { new Guid("b0000000-0000-0000-0000-000000004027"), "TASK_IN_PROGRESS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot delete a task that is in progress.", 409, "06", "WorkService", 4027 },
                    { new Guid("b0000000-0000-0000-0000-000000004028"), "SEARCH_QUERY_TOO_SHORT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Search query must be at least 2 characters.", 400, "96", "WorkService", 4028 },
                    { new Guid("b0000000-0000-0000-0000-000000004029"), "MENTION_USER_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "One or more mentioned users were not found.", 404, "07", "WorkService", 4029 },
                    { new Guid("b0000000-0000-0000-0000-000000004030"), "ORGANIZATION_MISMATCH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You cannot access resources from another organization.", 403, "03", "WorkService", 4030 },
                    { new Guid("b0000000-0000-0000-0000-000000004031"), "DEPARTMENT_ACCESS_DENIED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You do not have access to this department.", 403, "03", "WorkService", 4031 },
                    { new Guid("b0000000-0000-0000-0000-000000004032"), "INSUFFICIENT_PERMISSIONS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You do not have permission to perform this action.", 403, "03", "WorkService", 4032 },
                    { new Guid("b0000000-0000-0000-0000-000000004033"), "SPRINT_END_BEFORE_START", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sprint end date must be after start date.", 400, "96", "WorkService", 4033 },
                    { new Guid("b0000000-0000-0000-0000-000000004034"), "STORY_SEQUENCE_INIT_FAILED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Failed to initialize story sequence for the project.", 500, "98", "WorkService", 4034 },
                    { new Guid("b0000000-0000-0000-0000-000000004035"), "HOURS_MUST_BE_POSITIVE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hours logged must be a positive number.", 400, "96", "WorkService", 4035 },
                    { new Guid("b0000000-0000-0000-0000-000000004036"), "NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The requested resource was not found.", 404, "07", "WorkService", 4036 },
                    { new Guid("b0000000-0000-0000-0000-000000004037"), "CONFLICT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A conflict occurred with the current state of the resource.", 409, "06", "WorkService", 4037 },
                    { new Guid("b0000000-0000-0000-0000-000000004038"), "SERVICE_UNAVAILABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The service is temporarily unavailable.", 503, "98", "WorkService", 4038 },
                    { new Guid("b0000000-0000-0000-0000-000000004039"), "STORY_DESCRIPTION_REQUIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Story description is required for this transition.", 400, "96", "WorkService", 4039 },
                    { new Guid("b0000000-0000-0000-0000-000000004040"), "MAX_LABELS_PER_STORY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Maximum number of labels per story reached.", 400, "96", "WorkService", 4040 },
                    { new Guid("b0000000-0000-0000-0000-000000004041"), "PROJECT_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified project was not found.", 404, "07", "WorkService", 4041 },
                    { new Guid("b0000000-0000-0000-0000-000000004042"), "PROJECT_NAME_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A project with this name already exists.", 409, "06", "WorkService", 4042 },
                    { new Guid("b0000000-0000-0000-0000-000000004043"), "PROJECT_KEY_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A project with this key already exists.", 409, "06", "WorkService", 4043 },
                    { new Guid("b0000000-0000-0000-0000-000000004044"), "PROJECT_KEY_IMMUTABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Project key cannot be changed after creation.", 400, "96", "WorkService", 4044 },
                    { new Guid("b0000000-0000-0000-0000-000000004045"), "PROJECT_KEY_INVALID_FORMAT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Project key must be 2-5 uppercase letters.", 400, "96", "WorkService", 4045 },
                    { new Guid("b0000000-0000-0000-0000-000000004046"), "STORY_PROJECT_MISMATCH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The story does not belong to the specified project.", 400, "96", "WorkService", 4046 },
                    { new Guid("b0000000-0000-0000-0000-000000004050"), "TIMER_ALREADY_ACTIVE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You already have an active timer running.", 409, "06", "WorkService", 4050 },
                    { new Guid("b0000000-0000-0000-0000-000000004051"), "NO_ACTIVE_TIMER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "No active timer found to stop.", 400, "96", "WorkService", 4051 },
                    { new Guid("b0000000-0000-0000-0000-000000004052"), "TIME_ENTRY_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified time entry was not found.", 404, "07", "WorkService", 4052 },
                    { new Guid("b0000000-0000-0000-0000-000000004053"), "COST_RATE_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A cost rate already exists for this member and effective date.", 409, "06", "WorkService", 4053 },
                    { new Guid("b0000000-0000-0000-0000-000000004054"), "INVALID_COST_RATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cost rate must be a positive number.", 400, "96", "WorkService", 4054 },
                    { new Guid("b0000000-0000-0000-0000-000000004055"), "INVALID_TIME_POLICY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The time policy configuration is invalid.", 400, "96", "WorkService", 4055 },
                    { new Guid("b0000000-0000-0000-0000-000000004056"), "DAILY_HOURS_EXCEEDED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Daily hours limit exceeded per time policy.", 400, "96", "WorkService", 4056 },
                    { new Guid("b0000000-0000-0000-0000-000000004060"), "INVALID_ANALYTICS_PARAMETER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "One or more analytics parameters are invalid.", 400, "96", "WorkService", 4060 },
                    { new Guid("b0000000-0000-0000-0000-000000004061"), "INVALID_RISK_SEVERITY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Risk severity must be Low, Medium, High, or Critical.", 400, "96", "WorkService", 4061 },
                    { new Guid("b0000000-0000-0000-0000-000000004062"), "INVALID_RISK_LIKELIHOOD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Risk likelihood must be between 1 and 5.", 400, "96", "WorkService", 4062 },
                    { new Guid("b0000000-0000-0000-0000-000000004063"), "INVALID_MITIGATION_STATUS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Invalid mitigation status value.", 400, "96", "WorkService", 4063 },
                    { new Guid("b0000000-0000-0000-0000-000000004064"), "RISK_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified risk was not found.", 404, "07", "WorkService", 4064 },
                    { new Guid("b0000000-0000-0000-0000-000000004065"), "SNAPSHOT_GENERATION_FAILED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Failed to generate analytics snapshot.", 500, "98", "WorkService", 4065 },
                    { new Guid("b0000000-0000-0000-0000-000000004070"), "ORGADMIN_REQUIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This action requires OrgAdmin role or higher.", 403, "03", "WorkService", 4070 },
                    { new Guid("b0000000-0000-0000-0000-000000004071"), "DEPTLEAD_REQUIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This action requires DeptLead role or higher.", 403, "03", "WorkService", 4071 },
                    { new Guid("b0000000-0000-0000-0000-000000004072"), "PLATFORM_ADMIN_REQUIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This action requires PlatformAdmin role.", 403, "03", "WorkService", 4072 },
                    { new Guid("b0000000-0000-0000-0000-000000005001"), "SUBSCRIPTION_ALREADY_EXISTS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Organization already has an active subscription.", 409, "06", "BillingService", 5001 },
                    { new Guid("b0000000-0000-0000-0000-000000005002"), "PLAN_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified plan was not found.", 404, "07", "BillingService", 5002 },
                    { new Guid("b0000000-0000-0000-0000-000000005003"), "SUBSCRIPTION_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "No subscription found for this organization.", 404, "07", "BillingService", 5003 },
                    { new Guid("b0000000-0000-0000-0000-000000005004"), "INVALID_UPGRADE_PATH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot upgrade to the specified plan.", 400, "96", "BillingService", 5004 },
                    { new Guid("b0000000-0000-0000-0000-000000005005"), "NO_ACTIVE_SUBSCRIPTION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "No active subscription found.", 400, "96", "BillingService", 5005 },
                    { new Guid("b0000000-0000-0000-0000-000000005006"), "INVALID_DOWNGRADE_PATH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot downgrade to the specified plan.", 400, "96", "BillingService", 5006 },
                    { new Guid("b0000000-0000-0000-0000-000000005007"), "USAGE_EXCEEDS_PLAN_LIMITS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Current usage exceeds the target plan limits.", 403, "03", "BillingService", 5007 },
                    { new Guid("b0000000-0000-0000-0000-000000005008"), "SUBSCRIPTION_ALREADY_CANCELLED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This subscription has already been cancelled.", 409, "06", "BillingService", 5008 },
                    { new Guid("b0000000-0000-0000-0000-000000005009"), "TRIAL_EXPIRED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Your trial period has expired.", 402, "05", "BillingService", 5009 },
                    { new Guid("b0000000-0000-0000-0000-000000005010"), "PAYMENT_PROVIDER_ERROR", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Payment provider returned an error.", 502, "98", "BillingService", 5010 },
                    { new Guid("b0000000-0000-0000-0000-000000005011"), "INVALID_WEBHOOK_SIGNATURE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Webhook signature verification failed.", 400, "96", "BillingService", 5011 },
                    { new Guid("b0000000-0000-0000-0000-000000005012"), "INVALID_WEBHOOK_PAYLOAD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Webhook payload is malformed or missing required fields.", 400, "96", "BillingService", 5012 },
                    { new Guid("b0000000-0000-0000-0000-000000005013"), "FEATURE_NOT_AVAILABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This feature is not available on your current plan.", 403, "03", "BillingService", 5013 },
                    { new Guid("b0000000-0000-0000-0000-000000005014"), "USAGE_LIMIT_REACHED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You have reached the usage limit for this feature.", 403, "03", "BillingService", 5014 },
                    { new Guid("b0000000-0000-0000-0000-000000005015"), "PLAN_ALREADY_EXISTS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A plan with this code already exists.", 409, "06", "BillingService", 5015 },
                    { new Guid("b0000000-0000-0000-0000-000000005016"), "INSUFFICIENT_PERMISSIONS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You do not have permission to perform this action.", 403, "03", "BillingService", 5016 },
                    { new Guid("b0000000-0000-0000-0000-000000005017"), "PLAN_CODE_IMMUTABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan code cannot be changed after creation.", 400, "96", "BillingService", 5017 },
                    { new Guid("b0000000-0000-0000-0000-000000006001"), "AUDIT_LOG_IMMUTABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Audit logs cannot be modified or deleted.", 400, "96", "UtilityService", 6001 },
                    { new Guid("b0000000-0000-0000-0000-000000006002"), "ERROR_CODE_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "An error code with this value already exists.", 409, "06", "UtilityService", 6002 },
                    { new Guid("b0000000-0000-0000-0000-000000006003"), "ERROR_CODE_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified error code was not found.", 404, "07", "UtilityService", 6003 },
                    { new Guid("b0000000-0000-0000-0000-000000006004"), "NOTIFICATION_DISPATCH_FAILED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Failed to dispatch notification.", 500, "98", "UtilityService", 6004 },
                    { new Guid("b0000000-0000-0000-0000-000000006005"), "REFERENCE_DATA_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified reference data was not found.", 404, "07", "UtilityService", 6005 },
                    { new Guid("b0000000-0000-0000-0000-000000006006"), "ORGANIZATION_MISMATCH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "You cannot access resources from another organization.", 403, "03", "UtilityService", 6006 },
                    { new Guid("b0000000-0000-0000-0000-000000006007"), "TEMPLATE_NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The specified template was not found.", 404, "07", "UtilityService", 6007 },
                    { new Guid("b0000000-0000-0000-0000-000000006008"), "NOT_FOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The requested resource was not found.", 404, "07", "UtilityService", 6008 },
                    { new Guid("b0000000-0000-0000-0000-000000006009"), "CONFLICT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A conflict occurred with the current state of the resource.", 409, "06", "UtilityService", 6009 },
                    { new Guid("b0000000-0000-0000-0000-000000006010"), "SERVICE_UNAVAILABLE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The service is temporarily unavailable.", 503, "98", "UtilityService", 6010 },
                    { new Guid("b0000000-0000-0000-0000-000000006011"), "INVALID_NOTIFICATION_TYPE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The notification type is invalid.", 400, "96", "UtilityService", 6011 },
                    { new Guid("b0000000-0000-0000-0000-000000006012"), "INVALID_CHANNEL", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The notification channel is invalid.", 400, "96", "UtilityService", 6012 },
                    { new Guid("b0000000-0000-0000-0000-000000006013"), "RETENTION_PERIOD_INVALID", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The retention period value is invalid.", 400, "96", "UtilityService", 6013 },
                    { new Guid("b0000000-0000-0000-0000-000000006014"), "REFERENCE_DATA_DUPLICATE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Reference data with this code already exists.", 409, "06", "UtilityService", 6014 },
                    { new Guid("b0000000-0000-0000-0000-000000006015"), "OUTBOX_PROCESSING_FAILED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Failed to process outbox message.", 500, "98", "UtilityService", 6015 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeEntries_Code_ServiceName",
                table: "ErrorCodeEntries",
                columns: new[] { "Code", "ServiceName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ErrorCodeEntries_Code_ServiceName",
                table: "ErrorCodeEntries");

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000001000"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002001"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002002"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002003"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002004"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002005"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002006"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002007"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002008"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002009"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002010"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002011"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002012"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002013"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002016"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002017"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002018"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002019"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002020"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002021"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002022"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002023"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002024"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000002025"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003001"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003002"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003003"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003004"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003005"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003006"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003007"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003008"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003009"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003010"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003011"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003012"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003013"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003014"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003015"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003016"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003017"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003018"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003019"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003020"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003021"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003022"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003023"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003024"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003025"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003026"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003027"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003028"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003029"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003030"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000003031"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004001"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004002"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004003"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004004"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004005"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004006"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004007"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004008"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004009"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004010"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004011"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004012"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004013"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004014"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004015"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004016"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004017"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004018"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004019"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004020"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004021"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004022"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004023"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004024"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004025"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004026"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004027"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004028"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004029"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004030"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004031"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004032"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004033"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004034"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004035"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004036"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004037"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004038"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004039"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004040"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004041"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004042"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004043"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004044"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004045"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004046"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004050"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004051"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004052"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004053"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004054"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004055"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004056"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004060"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004061"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004062"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004063"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004064"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004065"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004070"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004071"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000004072"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005001"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005002"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005003"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005004"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005005"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005006"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005007"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005008"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005009"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005010"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005011"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005012"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005013"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005014"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005015"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005016"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000005017"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006001"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006002"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006003"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006004"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006005"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006006"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006007"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006008"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006009"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006010"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006011"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006012"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006013"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006014"));

            migrationBuilder.DeleteData(
                table: "ErrorCodeEntries",
                keyColumn: "ErrorCodeEntryId",
                keyValue: new Guid("b0000000-0000-0000-0000-000000006015"));

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeEntries_Code",
                table: "ErrorCodeEntries",
                column: "Code",
                unique: true);
        }
    }
}
