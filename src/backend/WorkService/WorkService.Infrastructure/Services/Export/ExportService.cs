using System.Text;
using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Interfaces.Services.Export;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.Export;

public class ExportService : IExportService
{
    private readonly WorkDbContext _db;

    public ExportService(WorkDbContext db) => _db = db;

    public async Task<byte[]> ExportStoriesCsvAsync(Guid organizationId, Guid? projectId, Guid? sprintId, CancellationToken ct = default)
    {
        var query = _db.Stories
            .Where(s => s.OrganizationId == organizationId);

        if (projectId.HasValue) query = query.Where(s => s.ProjectId == projectId.Value);
        if (sprintId.HasValue) query = query.Where(s => s.SprintId == sprintId.Value);

        var stories = await query
            .OrderBy(s => s.StoryKey)
            .Select(s => new
            {
                s.StoryKey, s.Title, s.Status, s.Priority,
                s.StoryPoints, s.AssigneeId, s.SprintId,
                s.DepartmentId, s.DateCreated
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("StoryKey,Title,Status,Priority,StoryPoints,AssigneeId,DateCreated");

        foreach (var s in stories)
        {
            sb.AppendLine($"{Escape(s.StoryKey)},{Escape(s.Title)},{s.Status},{s.Priority},{s.StoryPoints},{s.AssigneeId},{s.DateCreated:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportTimeEntriesCsvAsync(Guid organizationId, Guid? projectId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var query = _db.TimeEntries
            .Where(t => t.OrganizationId == organizationId);

        if (dateFrom.HasValue) query = query.Where(t => t.Date >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(t => t.Date <= dateTo.Value);

        var entries = await query
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                t.Date, t.DurationMinutes, t.Notes, t.IsBillable,
                t.Status, t.MemberId, t.StoryId, t.DateCreated
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date,DurationMinutes,Hours,Notes,Billable,Status,MemberId,StoryId,DateCreated");

        foreach (var t in entries)
        {
            var hours = Math.Round(t.DurationMinutes / 60.0, 2);
            sb.AppendLine($"{t.Date:yyyy-MM-dd},{t.DurationMinutes},{hours},{Escape(t.Notes)},{t.IsBillable},{t.Status},{t.MemberId},{t.StoryId},{t.DateCreated:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
