using ProfileService.Domain.Common;

namespace ProfileService.Domain.Entities;

public class Device : IOrganizationEntity
{
    public Guid DeviceId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid TeamMemberId { get; set; }
    public string? DeviceName { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsPrimary { get; set; }
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public TeamMember TeamMember { get; set; } = null!;
}
