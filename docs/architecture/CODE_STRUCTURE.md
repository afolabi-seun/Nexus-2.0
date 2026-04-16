# Code Structure

Clean Architecture layers, GenericRepository pattern, folder conventions, entity status management, and middleware pipeline registration.

## Clean Architecture — 4 Layers Per Service

```
{Service}/
├── {Service}.Domain/              # Layer 1: Core (no external dependencies)
├── {Service}.Application/         # Layer 2: DTOs & Validation
├── {Service}.Infrastructure/      # Layer 3: Implementation
├── {Service}.Api/                 # Layer 4: HTTP Interface
└── {Service}.Tests/               # Tests
```

### Layer 1: Domain

Zero external dependencies. Contains:

| Folder | Contents |
|--------|----------|
| `Entities/` | EF Core entity classes (POCOs) |
| `Interfaces/Repositories/` | Repository interfaces, grouped by entity |
| `Interfaces/Services/` | Service interfaces, grouped by feature |
| `Exceptions/` | `DomainException` base, typed exceptions, `ErrorCodes` static class |
| `Helpers/` | Constants (`EntityStatuses`, `RoleNames`), enums |

Dependency rule: Domain references nothing. Everything else references Domain.

### Layer 2: Application

References Domain only. Contains:

| Folder | Contents |
|--------|----------|
| `DTOs/` | Request/response DTOs, grouped by feature |
| `DTOs/ApiResponse.cs` | The `ApiResponse<T>` envelope |
| `DTOs/PaginatedResponse.cs` | Pagination wrapper |
| `Validators/` | FluentValidation validators |
| `Contracts/` | Shared contracts (e.g., `ErrorCodeResponse`) |

### Layer 3: Infrastructure

References Domain and Application. Contains all implementations:

| Folder | Contents |
|--------|----------|
| `Data/` | EF Core `DbContext`, entity configurations |
| `Repositories/Generics/` | `GenericRepository<T>` base class |
| `Repositories/{Entity}/` | Entity-specific repositories |
| `Services/{Feature}/` | Service implementations |
| `Services/ServiceClients/` | Typed HTTP clients for inter-service calls |
| `Services/Outbox/` | Redis outbox publisher |
| `Services/ErrorCodeResolver/` | Error code resolution service |
| `Configuration/` | `DependencyInjection.cs`, `AppSettings.cs` |
| `Migrations/` | EF Core migrations |

### Layer 4: Api

References Infrastructure and Application. The HTTP entry point:

| Folder | Contents |
|--------|----------|
| `Controllers/` | API controllers |
| `Middleware/` | Request pipeline middleware |
| `Extensions/` | `MiddlewarePipelineExtensions`, `ApiResponseExtensions` |
| `Attributes/` | Custom attributes (`[PlatformAdmin]`) |
| `Filters/` | Swagger filters (`HideServiceAuthFilter`) |
| `Program.cs` | Application bootstrap |

## Folder Structure Example (ProfileService)

```
ProfileService/
├── ProfileService.Domain/
│   ├── Entities/
│   │   ├── Organization.cs
│   │   ├── Department.cs
│   │   ├── TeamMember.cs
│   │   ├── Invite.cs
│   │   └── ...
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   ├── Generics/IGenericRepository.cs
│   │   │   ├── Organizations/IOrganizationRepository.cs
│   │   │   ├── Departments/IDepartmentRepository.cs
│   │   │   └── TeamMembers/ITeamMemberRepository.cs
│   │   └── Services/
│   │       ├── Organizations/IOrganizationService.cs
│   │       ├── Departments/IDepartmentService.cs
│   │       └── Preferences/IPreferenceResolver.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   ├── ErrorCodes.cs
│   │   ├── OrganizationNameDuplicateException.cs
│   │   └── ...
│   └── Helpers/
│       └── EntityStatuses.cs
├── ProfileService.Application/
│   ├── DTOs/
│   │   ├── ApiResponse.cs
│   │   ├── PaginatedResponse.cs
│   │   ├── Organizations/CreateOrganizationRequest.cs
│   │   └── Departments/CreateDepartmentRequest.cs
│   └── Validators/
│       ├── CreateOrganizationRequestValidator.cs
│       └── CreateDepartmentRequestValidator.cs
├── ProfileService.Infrastructure/
│   ├── Data/ProfileDbContext.cs
│   ├── Repositories/
│   │   ├── Generics/GenericRepository.cs
│   │   ├── Organizations/OrganizationRepository.cs
│   │   └── Departments/DepartmentRepository.cs
│   ├── Services/
│   │   ├── Organizations/OrganizationService.cs
│   │   ├── Departments/DepartmentService.cs
│   │   ├── Preferences/PreferenceResolver.cs
│   │   ├── ServiceClients/SecurityServiceClient.cs
│   │   ├── ServiceClients/CorrelationIdDelegatingHandler.cs
│   │   ├── Outbox/OutboxService.cs
│   │   └── ErrorCodeResolver/ErrorCodeResolverService.cs
│   ├── Configuration/
│   │   ├── DependencyInjection.cs
│   │   └── AppSettings.cs
│   └── Migrations/
└── ProfileService.Api/
    ├── Controllers/
    │   ├── OrganizationController.cs
    │   └── DepartmentController.cs
    ├── Middleware/
    │   ├── CorrelationIdMiddleware.cs
    │   ├── GlobalExceptionHandlerMiddleware.cs
    │   ├── JwtClaimsMiddleware.cs
    │   ├── TokenBlacklistMiddleware.cs
    │   ├── RoleAuthorizationMiddleware.cs
    │   ├── OrganizationScopeMiddleware.cs
    │   └── RateLimiterMiddleware.cs
    ├── Extensions/
    │   ├── MiddlewarePipelineExtensions.cs
    │   └── ApiResponseExtensions.cs
    └── Program.cs
```

## GenericRepository Pattern

Every service has an identical `GenericRepository<T>` in `Infrastructure/Repositories/Generics/`:

```csharp
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(DbContext context) { ... }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    public virtual async Task<List<T>> GetAllAsync(CancellationToken ct = default)
    public virtual IQueryable<T> FindAsync(Expression<Func<T, bool>> predicate)
    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    public virtual Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    public virtual Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    public virtual IQueryable<T> FindWithoutFiltersAsync(Expression<Func<T, bool>> predicate)
}
```

Concrete repositories inherit from it and add entity-specific queries:

```csharp
public class OrganizationRepository : GenericRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(ProfileDbContext db) : base(db) { _db = db; }

    public async Task<Organization?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.Organizations.FirstOrDefaultAsync(o => o.OrganizationName == name, ct);
}
```

Key design decisions:
- `GenericRepository` methods stage changes only — they don't call `SaveChangesAsync()`
- `SaveChangesAsync()` is called in the service layer (120 call sites across all services)
- This allows services to compose multiple repository operations in a single transaction
- Each service has its own copy of `GenericRepository` (no shared NuGet package) to maintain microservice independence

## Entity-Named Subfolders

Repositories and services are organized by entity/feature name. Namespaces match folder paths:

```
Repositories/
├── Generics/GenericRepository.cs          → ...Repositories.Generics
├── Organizations/OrganizationRepository.cs → ...Repositories.Organizations
├── Departments/DepartmentRepository.cs     → ...Repositories.Departments
└── TeamMembers/TeamMemberRepository.cs     → ...Repositories.TeamMembers

Services/
├── Organizations/OrganizationService.cs    → ...Services.Organizations
├── Auth/AuthService.cs                     → ...Services.Auth
├── Stripe/StripeWebhookService.cs          → ...Services.Stripe
└── ServiceClients/ProfileServiceClient.cs  → ...Services.ServiceClients
```

## Entity Status Pattern (FlgStatus)

Entities use a string `FlgStatus` field instead of soft delete:

```csharp
public static class EntityStatuses
{
    public const string Active = "A";
    public const string Suspended = "S";
    public const string Deactivated = "D";
}
```

Entities with `FlgStatus`: `Organization`, `Department`, `TeamMember`, `Invite`, `Device`, `PlatformAdmin`.

Status transitions are enforced in the service layer (e.g., `DepartmentService.UpdateStatusAsync()` checks for active members before deactivation).

## Middleware Pipeline Registration

Each service registers its middleware pipeline in `{Service}.Api/Extensions/MiddlewarePipelineExtensions.cs`:

```csharp
public static WebApplication UseProfilePipeline(this WebApplication app)
{
    app.UseCors("NexusPolicy");
    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseMiddleware<RateLimiterMiddleware>();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<JwtClaimsMiddleware>();
    app.UseMiddleware<TokenBlacklistMiddleware>();
    app.UseMiddleware<FirstTimeUserMiddleware>();
    app.UseMiddleware<RoleAuthorizationMiddleware>();
    app.UseMiddleware<OrganizationScopeMiddleware>();
    return app;
}
```

Order matters — `GlobalExceptionHandlerMiddleware` must be early to catch exceptions from all downstream middleware. `CorrelationIdMiddleware` must be before it so error responses include the correlation ID.

## DI Registration

All services, repositories, and infrastructure are registered in `{Service}.Infrastructure/Configuration/DependencyInjection.cs`:

```csharp
public static IServiceCollection AddProfileInfrastructure(this IServiceCollection services, AppSettings appSettings)
{
    // DbContext
    services.AddDbContext<ProfileDbContext>(options =>
        options.UseNpgsql(appSettings.DatabaseConnectionString));

    // Repositories
    services.AddScoped<IOrganizationRepository, OrganizationRepository>();
    services.AddScoped<IDepartmentRepository, DepartmentRepository>();

    // Services
    services.AddScoped<IOrganizationService, OrganizationService>();
    services.AddScoped<IDepartmentService, DepartmentService>();

    // HTTP clients with Polly
    services.AddTransient<CorrelationIdDelegatingHandler>();
    services.AddHttpClient("SecurityService", ...)
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddTransientHttpErrorPolicy(...)
        .AddTransientHttpErrorPolicy(...)
        .AddPolicyHandler(...);

    // Redis
    services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(appSettings.RedisConnectionString));

    return services;
}
```

## Related Docs

- [ERROR_HANDLING.md](ERROR_HANDLING.md) — DomainException hierarchy in the Domain layer
- [VALIDATION.md](VALIDATION.md) — FluentValidation in the Application layer
- [INTER_SERVICE_COMMUNICATION.md](INTER_SERVICE_COMMUNICATION.md) — Service clients in the Infrastructure layer
- [AUTHENTICATION_AND_SECURITY.md](AUTHENTICATION_AND_SECURITY.md) — Middleware pipeline details
