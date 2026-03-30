using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FsCheck;
using FsCheck.Xunit;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Services.Jwt;

namespace SecurityService.Tests.Services;

/// <summary>
/// Property-based and unit tests for JwtService.
/// Validates: REQ-002.1, REQ-002.4
/// </summary>
public class JwtServiceTests
{
    private static readonly JwtConfig TestConfig = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!!", // 52 chars
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7
    };

    private static readonly JwtService _service = new(TestConfig);

    private static readonly string[] Roles = { "OrgAdmin", "DeptLead", "Member", "Viewer" };
    private static readonly string[] DeptRoles = { "Manager", "Lead", "Contributor", "" };

    /// <summary>
    /// Property: Every generated access token, when read as raw JWT, contains exactly the claims
    /// (userId, organizationId, departmentId, roleName, departmentRole, deviceId, jti) that were
    /// passed to GenerateAccessToken.
    /// Uses FsCheck Arbitrary generators for Guid and NonEmptyString inputs.
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AccessToken_ContainsAllExpectedClaims(Guid userId, Guid orgId, Guid deptId, NonEmptyString deviceIdNes)
    {
        var roleName = Roles[Math.Abs(userId.GetHashCode()) % Roles.Length];
        var deptRole = DeptRoles[Math.Abs(orgId.GetHashCode()) % DeptRoles.Length];
        var deviceId = deviceIdNes.Get;

        var token = _service.GenerateAccessToken(userId, orgId, deptId, roleName, deptRole, deviceId);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var claims = jwt.Claims.ToList();

        return claims.Any(c => c.Type == "userId" && c.Value == userId.ToString())
            && claims.Any(c => c.Type == "organizationId" && c.Value == orgId.ToString())
            && claims.Any(c => c.Type == "departmentId" && c.Value == deptId.ToString())
            && claims.Any(c => c.Type == "roleName" && c.Value == roleName)
            && claims.Any(c => c.Type == "departmentRole" && c.Value == (deptRole ?? string.Empty))
            && claims.Any(c => c.Type == "deviceId" && c.Value == deviceId)
            && claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrEmpty(c.Value));
    }

    /// <summary>
    /// Property: Generate an access token, then validate it via ValidateToken, and verify the
    /// ClaimsPrincipal contains the correct claims. This is the full round-trip property:
    /// GenerateAccessToken → ValidateToken → ClaimsPrincipal claims match inputs.
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AccessToken_RoundTrip_ValidateTokenReturnsCorrectClaims(Guid userId, Guid orgId, Guid deptId, NonEmptyString deviceIdNes)
    {
        var roleName = Roles[Math.Abs(userId.GetHashCode()) % Roles.Length];
        var deptRole = DeptRoles[Math.Abs(orgId.GetHashCode()) % DeptRoles.Length];
        var deviceId = deviceIdNes.Get;

        var token = _service.GenerateAccessToken(userId, orgId, deptId, roleName, deptRole, deviceId);

        var principal = _service.ValidateToken(token);
        if (principal == null) return false;

        var claims = principal.Claims.ToList();

        return claims.Any(c => c.Type == "userId" && c.Value == userId.ToString())
            && claims.Any(c => c.Type == "organizationId" && c.Value == orgId.ToString())
            && claims.Any(c => c.Type == "departmentId" && c.Value == deptId.ToString())
            && claims.Any(c => c.Type == "roleName" && c.Value == roleName)
            && claims.Any(c => c.Type == "departmentRole" && c.Value == (deptRole ?? string.Empty))
            && claims.Any(c => c.Type == "deviceId" && c.Value == deviceId)
            && claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrEmpty(c.Value));
    }

    /// <summary>
    /// Property: jti is unique across multiple invocations.
    /// Uses FsCheck Arbitrary generators for Guid inputs and PositiveInt for count.
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Jti_IsUnique_AcrossInvocations(Guid userId, Guid orgId, Guid deptId, PositiveInt countPi)
    {
        var n = countPi.Get % 20 + 2; // 2..21
        var jtis = new HashSet<string>();

        for (int i = 0; i < n; i++)
        {
            var token = _service.GenerateAccessToken(userId, orgId, deptId, "Member", "Contributor", "device-1");
            var jti = _service.GetJti(token);
            jtis.Add(jti);
        }

        return jtis.Count == n;
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String_OfExpectedLength()
    {
        var token = _service.GenerateRefreshToken();

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // 64 bytes -> base64 = ceil(64/3)*4 = 88 chars
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateServiceToken_ContainsServiceIdAndServiceNameClaimsOnly()
    {
        var token = _service.GenerateServiceToken("svc-123", "ProfileService");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var claims = jwt.Claims.ToList();

        Assert.Contains(claims, c => c.Type == "serviceId" && c.Value == "svc-123");
        Assert.Contains(claims, c => c.Type == "serviceName" && c.Value == "ProfileService");
        Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Jti);

        // Should NOT contain user-specific claims
        Assert.DoesNotContain(claims, c => c.Type == "userId");
        Assert.DoesNotContain(claims, c => c.Type == "organizationId");
        Assert.DoesNotContain(claims, c => c.Type == "departmentId");
        Assert.DoesNotContain(claims, c => c.Type == "roleName");
    }

    [Fact]
    public void ValidateToken_ReturnsNull_ForExpiredToken()
    {
        // Create a config with 0-minute expiry to force expiration
        var expiredConfig = new JwtConfig
        {
            Issuer = TestConfig.Issuer,
            Audience = TestConfig.Audience,
            SecretKey = TestConfig.SecretKey,
            AccessTokenExpiryMinutes = 0
        };
        var expiredService = new JwtService(expiredConfig);

        var token = expiredService.GenerateAccessToken(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Member", "Contributor", "device-1");

        var result = _service.ValidateToken(token);
        Assert.Null(result);
    }

    [Fact]
    public void ValidateToken_ReturnsNull_ForInvalidToken()
    {
        var result = _service.ValidateToken("this.is.not.a.valid.jwt");
        Assert.Null(result);
    }
}
