using Microsoft.EntityFrameworkCore;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Repositories.ServiceTokens;
using SecurityService.Domain.Interfaces.Services.Jwt;
using SecurityService.Domain.Interfaces.Services.ServiceToken;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Data;
using StackExchange.Redis;

namespace SecurityService.Infrastructure.Services.ServiceToken;

public class ServiceTokenService : IServiceTokenService
{
    private readonly IJwtService _jwtService;
    private readonly IServiceTokenRepository _serviceTokenRepository;
    private readonly SecurityDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;

    // Configurable ACL of allowed service IDs
    private static readonly HashSet<string> AllowedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProfileService",
        "WorkService",
        "UtilityService",
        "SecurityService"
    };

    public ServiceTokenService(
        IJwtService jwtService,
        IServiceTokenRepository serviceTokenRepository,
        SecurityDbContext dbContext,
        IConnectionMultiplexer redis,
        AppSettings appSettings)
    {
        _jwtService = jwtService;
        _serviceTokenRepository = serviceTokenRepository;
        _dbContext = dbContext;
        _redis = redis;
        _appSettings = appSettings;
    }

    public async Task<ServiceTokenResult> IssueTokenAsync(string serviceId, string serviceName,
        CancellationToken ct = default)
    {
        var rawToken = _jwtService.GenerateServiceToken(serviceId, serviceName);
        var tokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken);
        var expiry = _jwtService.GetTokenExpiry(rawToken);

        var entity = new Domain.Entities.ServiceToken
        {
            ServiceId = serviceId,
            ServiceName = serviceName,
            TokenHash = tokenHash,
            ExpiryDate = expiry,
            DateCreated = DateTime.UtcNow,
            IsRevoked = false
        };

        await _serviceTokenRepository.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Cache raw token in Redis with 23-hour TTL
        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            $"service_token:{serviceId}",
            rawToken,
            TimeSpan.FromHours(23));

        var expiresInSeconds = (int)(expiry - DateTime.UtcNow).TotalSeconds;

        return new ServiceTokenResult
        {
            Token = rawToken,
            ExpiresInSeconds = expiresInSeconds
        };
    }

    public async Task<bool> ValidateServiceTokenAsync(string token, CancellationToken ct = default)
    {
        // Validate JWT signature and expiry
        var principal = _jwtService.ValidateToken(token);
        if (principal is null)
            return false;

        var serviceId = principal.FindFirst("serviceId")?.Value;
        if (string.IsNullOrEmpty(serviceId))
            return false;

        // Check ACL
        if (!AllowedServices.Contains(serviceId))
            throw new ServiceNotAuthorizedException($"Service '{serviceId}' is not in the allowed services list.");

        // Check if revoked in DB
        var jti = _jwtService.GetJti(token);
        var isRevoked = await _dbContext.ServiceTokens
            .AnyAsync(st => st.ServiceId == serviceId && st.IsRevoked, ct);

        if (isRevoked)
            return false;

        return true;
    }
}
