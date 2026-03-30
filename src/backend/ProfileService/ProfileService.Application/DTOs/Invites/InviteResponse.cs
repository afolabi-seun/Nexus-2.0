namespace ProfileService.Application.DTOs.Invites;

public class InviteResponse
{
    public Guid InviteId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime DateCreated { get; set; }
}
