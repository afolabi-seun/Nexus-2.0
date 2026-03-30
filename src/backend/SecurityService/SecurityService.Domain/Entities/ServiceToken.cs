namespace SecurityService.Domain.Entities;

public class ServiceToken
{
    public Guid ServiceTokenId { get; set; } = Guid.NewGuid();
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;
}
