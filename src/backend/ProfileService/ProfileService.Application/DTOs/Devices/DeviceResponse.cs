namespace ProfileService.Application.DTOs.Devices;

public class DeviceResponse
{
    public Guid DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LastActiveDate { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
}
