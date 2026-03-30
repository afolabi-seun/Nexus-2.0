using System.Security.Claims;

namespace SecurityService.Domain.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, Guid organizationId, Guid departmentId, string roleName, string departmentRole, string deviceId);
    string GenerateRefreshToken();
    string GenerateServiceToken(string serviceId, string serviceName);
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiry(string token);
    string GetJti(string token);
}
