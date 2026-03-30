namespace SecurityService.Domain.Entities;

public class PasswordHistory
{
    public Guid PasswordHistoryId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
