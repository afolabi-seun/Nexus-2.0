# Data Model Patterns

## Overview

WEP uses a consistent set of patterns across all 5 services for data access, multi-tenancy, soft deletion, reference codes, and pagination. Understanding these patterns is essential — every service builds on them.

| Pattern | Purpose | Used By |
|---------|---------|---------|
| `TenantScopedRepository<T>` | CRUD with tenant isolation | Profile, Security, Transaction, Wallet |
| `GenericRepository<T>` | CRUD without tenant isolation | Utility (reference data, error codes) |
| `ITenantEntity` | Marks entities for tenant filtering | All tenant-scoped entities |
| `ISoftDeletable` | Marks entities for soft delete | Most entities |
| `IReferenceCodeEntity` | Auto-generates human-readable codes | All entities |
| `PaginatedResponse<T>` | Standardized paginated list responses | Unbounded list endpoints |

---

## Entity Interfaces

### ITenantEntity

Every entity that belongs to a tenant implements this interface. EF Core global query filters use it to enforce tenant isolation automatically.

```csharp
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

### ISoftDeletable

Entities that support soft deletion implement this interface. The base repository's `SoftDeleteAsync` sets `FlgStatus = "D"` instead of removing the row.

```csharp
public interface ISoftDeletable
{
    string FlgStatus { get; set; }
}
```

Status values:
| Value | Meaning |
|-------|---------|
| `A` | Active |
| `D` | Deleted (soft-deleted) |
| `S` | Suspended (wallets, SMEs) |
| `P` | Pending (SMEs during onboarding) |

### IReferenceCodeEntity

Entities that get a human-readable reference code on creation. The base repository auto-generates it if empty.

```csharp
public interface IReferenceCodeEntity
{
    string ReferenceCode { get; set; }

    [NotMapped]
    string ReferencePrefix { get; }  // e.g. "CUS", "SME", "TXN"
}
```

### Putting It Together: Entity Example

```csharp
[Table("customer")]
public class Customer : ITenantEntity, ISoftDeletable, IReferenceCodeEntity
{
    [Key]
    [Column("cust_id")]
    public Guid CustId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }          // ITenantEntity

    [Required]
    [Column("flg_status")]
    public string FlgStatus { get; set; }        // ISoftDeletable

    [Column("reference_code")]
    [MaxLength(25)]
    public string ReferenceCode { get; set; }    // IReferenceCodeEntity

    [NotMapped]
    public string ReferencePrefix => "CUS";      // IReferenceCodeEntity

    // ... domain fields ...
}
```

---

## Reference Codes

Every entity gets a human-readable reference code auto-generated on creation.

### Format

```
{PREFIX}-{yyyyMMdd}-{8-char hex}
```

Example: `CUS-20250615-A1B2C3D4`

### Generator

```csharp
public static class ReferenceCodeGenerator
{
    public static string Generate(string prefix)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var hexPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"{prefix}-{datePart}-{hexPart}";
    }
}
```

### Prefix Table

| Prefix | Entity | Service |
|--------|--------|---------|
| `TNT` | Tenant | Profile |
| `SME` | Sme | Profile |
| `CUS` | Customer | Profile |
| `USR` | SmeUser | Profile |
| `BNF` | Beneficiary | Profile |
| `CRD` | Card | Profile |
| `DEV` | Device | Profile |
| `KYC` | Kyc | Profile |
| `INV` | Invite | Profile |
| `ADT` | AuditLog | Utility |
| `ERL` | ErrorLog | Utility |
| `NTF` | NotificationLog | Utility |
| `ERC` | ErrorCodeEntry | Utility |
| `WHK` | WebhookRegistration | Transaction |
| `SWL` | SmeWallet | Wallet |
| `SUB` | SmeSubWallet | Wallet |
| `CWL` | CustomerWallet | Wallet |
| `CSW` | CustomerSubWallet | Wallet |
| `VAC` | VirtualAccount | Wallet |
| `HLD` | Hold | Wallet |
| `RWD` | Reward | Wallet |
| `LYC` | SmeLoyaltyConfig | Wallet |

### Lookup by Reference Code

All entities can be looked up by reference code:

```
GET /api/v1/{resource}/ref/{referenceCode}
```

The base repository provides `FindByReferenceCodeAsync` which uses `OfType<IReferenceCodeEntity>()` to query across entity types.

---

## TenantScopedRepository\<T\>

The base repository for all tenant-scoped entities. Used by Profile, Security, Transaction, and Wallet services.

### Key Design: Expression-Based PK Selector

Subclasses pass a PK selector expression to the constructor — no reflection, no `EF.Property<Guid>()`, no per-entity overrides:

```csharp
public class CustomerRepository : TenantScopedRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(ProfileDbContext context) : base(context, x => x.CustId) { }

    // Only custom queries here — CRUD is inherited
}

public class SmeRepository : TenantScopedRepository<Sme>, ISmeRepository
{
    public SmeRepository(ProfileDbContext context) : base(context, x => x.SmeId) { }
}
```

The base class compiles the expression into a getter, builds a setter via `Expression.Assign`, and constructs ID predicates dynamically:

```csharp
protected TenantScopedRepository(ProfileDbContext context, Expression<Func<T, Guid>> keySelector)
{
    _context = context;
    _keySelector = keySelector;
    _getKey = keySelector.Compile();                    // x => x.CustId → Func<T, Guid>

    // Build setter: x => x.CustId → (x, v) => x.CustId = v
    var member = (MemberExpression)keySelector.Body;
    var param = keySelector.Parameters[0];
    var valueParam = Expression.Parameter(typeof(Guid), "v");
    _setKey = Expression.Lambda<Action<T, Guid>>(
        Expression.Assign(member, valueParam), param, valueParam).Compile();
}
```

### Provided CRUD Operations

| Method | Behavior |
|--------|----------|
| `FindByIdAsync(tenantId, id)` | Lookup by PK with tenant filter |
| `FindByReferenceCodeAsync(tenantId, refCode)` | Lookup by reference code |
| `FindAllAsync(tenantId)` | All entities (capped at 1000) |
| `CreateAsync(tenantId, entity)` | Sets TenantId, auto-generates PK if empty, auto-generates ReferenceCode, catches PostgreSQL constraints |
| `UpdateAsync(tenantId, id, updateAction)` | Find + apply action + save, catches PostgreSQL constraints |
| `SoftDeleteAsync(tenantId, id)` | Sets `FlgStatus = "D"` if entity implements `ISoftDeletable` |

### Auto-Generation on Create

```csharp
public virtual async Task<T> CreateAsync(Guid tenantId, T entity)
{
    entity.TenantId = tenantId;

    // Auto-generate PK if not set
    if (_getKey(entity) == Guid.Empty)
        _setKey(entity, Guid.NewGuid());

    // Auto-generate reference code if not set
    if (entity is IReferenceCodeEntity refEntity && string.IsNullOrEmpty(refEntity.ReferenceCode))
        refEntity.ReferenceCode = ReferenceCodeGenerator.Generate(refEntity.ReferencePrefix);

    _context.Set<T>().Add(entity);
    await _context.SaveChangesAsync();  // PostgreSQL constraint handling wraps this
    return entity;
}
```

### PostgreSQL Constraint Handling

Both `CreateAsync` and `UpdateAsync` catch `DbUpdateException` and map PostgreSQL error states to `DomainException`:

| PostgreSQL State | Meaning | Maps To | HTTP |
|------------------|---------|---------|------|
| `23505` | Unique violation | `CONFLICT` | 409 |
| `23503` | Foreign key violation | `NOT_FOUND` | 400 |
| Other | Unknown constraint | `CONFLICT` | 409 |

The constraint name and detail are included in the error message:

```
"Duplicate value violates unique constraint 'ix_customer_phone_no': Key (phone_no)=(+2348012345678) already exists."
```

### Virtual Methods

All methods are `virtual` — subclasses can override when needed:

```csharp
// Default: base handles everything
public class BeneficiaryRepository : TenantScopedRepository<Beneficiary>, IBeneficiaryRepository
{
    public BeneficiaryRepository(ProfileDbContext context) : base(context, x => x.BeneficiaryId) { }
}

// Override: custom logic needed
public class CustomerRepository : TenantScopedRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(ProfileDbContext context) : base(context, x => x.CustId) { }

    // Custom queries beyond base CRUD
    public async Task<Customer?> FindByPhoneAsync(Guid tenantId, string phoneNo) { ... }
    public async Task<(List<Customer> Items, int TotalCount)> SearchAsync(...) { ... }
}
```

---

## GenericRepository\<T\>

Used by UtilityCoreService for non-tenant-scoped entities (audit logs, error logs, error codes, notification logs). Same expression-based PK pattern and PostgreSQL constraint handling, but without tenant isolation:

```csharp
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> FindByReferenceCodeAsync(string referenceCode);
    Task<List<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(Guid id, Action<T> updateAction);
    Task DeleteAsync(Guid id);  // Hard delete (not soft)
}
```

Key difference: `DeleteAsync` performs a hard delete (removes the row) since utility entities like error codes don't use soft deletion.

---

## Multi-Tenant Isolation

Tenant isolation is enforced at three levels:

### Level 1: EF Core Global Query Filters

The `DbContext` applies `HasQueryFilter` on every tenant-scoped entity:

```csharp
// ProfileDbContext.OnModelCreating
modelBuilder.Entity<Sme>().HasQueryFilter(e => e.TenantId == TenantId);
modelBuilder.Entity<SmeUser>().HasQueryFilter(e => e.TenantId == TenantId);
modelBuilder.Entity<Customer>().HasQueryFilter(e => e.TenantId == TenantId);
modelBuilder.Entity<Device>().HasQueryFilter(e => e.TenantId == TenantId);
// ... every tenant-scoped entity
```

This means **every LINQ query** automatically filters by `TenantId` — even if the developer forgets to add a `Where` clause. The filter is applied at the SQL level by EF Core.

### Level 2: TenantScopeMiddleware

Sets `TenantId` on the `DbContext` per-request:

```
Authenticated request
  │
  ▼
JwtClaimsMiddleware extracts TenantId from JWT → HttpContext.Items["TenantId"]
  │
  ▼
TenantScopeMiddleware reads HttpContext.Items["TenantId"]
  ├── Validates no route/query tenantId mismatch (throws TENANT_MISMATCH if so)
  ├── Sets DbContext.TenantId = tenantId
  └── For service-auth tokens: reads X-Tenant-Id header instead
```

### Level 3: Repository SetTenant

Every repository method calls `SetTenant(tenantId)` before querying:

```csharp
public virtual async Task<T?> FindByIdAsync(Guid tenantId, Guid id)
{
    SetTenant(tenantId);  // Sets DbContext.TenantId for the global query filter
    return await _context.Set<T>().FirstOrDefaultAsync(BuildIdPredicate(id));
}
```

### Bypassing Tenant Scope

Some queries need to bypass tenant filtering (e.g. login lookup by username across all tenants):

```csharp
public async Task<Customer?> FindByIdentityUnscopedAsync(string identity)
{
    return await _context.Customers
        .IgnoreQueryFilters()  // Bypasses the TenantId filter
        .Include(c => c.Sme)
        .FirstOrDefaultAsync(c =>
            c.Username == identity || c.EmailAddress == identity || c.PhoneNo == identity);
}
```

`IgnoreQueryFilters()` is used sparingly — only for cross-tenant lookups during authentication.

---

## Pagination

### PaginatedResponse\<T\>

Unbounded list endpoints return `PaginatedResponse<T>`:

```csharp
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

Response:
```json
{
  "data": {
    "items": [...],
    "totalCount": 150,
    "page": 1,
    "pageSize": 20
  }
}
```

### Query Parameters

All paginated endpoints accept `page` and `pageSize`:

```
GET /api/v1/customers?page=2&pageSize=10
```

Defaults: `page=1`, `pageSize=20`.

### Repository Pattern

Paginated queries return a tuple of `(items, totalCount)`:

```csharp
public async Task<(List<Customer> Items, int TotalCount)> SearchAsync(
    Guid tenantId, string? name, string? phone, string? kycStatus,
    string? flgStatus, int page, int pageSize)
{
    SetTenant(tenantId);
    var query = _context.Customers.AsQueryable();

    // Apply filters
    if (!string.IsNullOrWhiteSpace(name))
        query = query.Where(c => c.FirstName.ToLower().Contains(name.ToLower())
                               || c.LastName.ToLower().Contains(name.ToLower()));
    if (!string.IsNullOrWhiteSpace(phone))
        query = query.Where(c => c.PhoneNo.Contains(phone));

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(c => c.DateCreated)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (items, totalCount);
}
```

### Paginated vs Non-Paginated Endpoints

| Type | Endpoints | Response |
|------|-----------|----------|
| Paginated | Customers, transactions, audit logs, error logs, notification logs, reports, reward history | `PaginatedResponse<T>` |
| Plain array | Beneficiaries, cards, devices, sessions, holds, wallets | `List<T>` (bounded by business rules) |

Small bounded lists (e.g. max 5 devices, max 10 cards, max 50 beneficiaries) don't need pagination.

---

## Database Schema Isolation

All 5 services connect to a single PostgreSQL database (`uba_wep`) with schema-based isolation:

| Service | Schema | DbContext |
|---------|--------|-----------|
| UtilityCoreService | `uba_wep_utility` | `UtilityDbContext` |
| SecurityCoreService | `uba_wep_security` | `SecurityDbContext` |
| ProfileCoreService | `uba_wep_profile` | `ProfileDbContext` |
| TransactionCoreService | `uba_wep_transaction` | `TransactionDbContext` |
| WalletCoreService | `uba_wep_wallet` | `WalletDbContext` |

Schema is set via the `SearchPath` parameter in the connection string:

```
Host=localhost;Database=uba_wep;SearchPath=uba_wep_profile;Username=...;Password=...
```

Each service only sees its own tables. Cross-service data access is done via HTTP service clients, never via direct database queries.

---

## Wallet Entity Hierarchy & Spending Limits

```
SmeWallet (one per SME)
├── GlobalSpendingLimit (ceiling for ALL wallets under this SME)
├── SpendingLimit (primary wallet's own limit, ≤ global)
├── SmeSubWallet[] (branch wallets, created by admin)
│     └── SpendingLimit (≤ global)
└── CustomerWallet[] (one per customer)
      ├── SpendingLimit (customer-set, ≤ global)
      └── CustomerSubWallet[] (personal sub-wallets, max 10)
            └── SpendingLimit (≤ parent customer wallet ≤ global)
```

| Wallet Type | Number Prefix | Created By | Limit Set By |
|-------------|---------------|------------|-------------|
| SmeWallet | 70 | Auto (onboarding) | Admin |
| SmeSubWallet | 71 | Admin | Admin |
| CustomerWallet | 72 | Auto (customer creation) | Customer (≤ global) |
| CustomerSubWallet | 73 | Customer | Customer (≤ parent ≤ global) |

Spending limits are enforced monthly via a single Redis accumulator per wallet (`wep:rate:{walletId}:spent`). The accumulator auto-expires at end of each calendar month. Admin can manually reset via `POST /sme-wallets/{smeId}/reset-spending`.

---

## Column Naming Convention

All entities use snake_case column names mapped via `[Column]` attributes:

```csharp
[Column("cust_id")]       public Guid CustId { get; set; }
[Column("tenant_id")]     public Guid TenantId { get; set; }
[Column("first_name")]    public string FirstName { get; set; }
[Column("date_created")]  public DateTime DateCreated { get; set; }
[Column("flg_status")]    public string FlgStatus { get; set; }
[Column("reference_code")] public string ReferenceCode { get; set; }
```

Table names are also snake_case via `[Table]`:

```csharp
[Table("customer")]
public class Customer : ITenantEntity, ISoftDeletable, IReferenceCodeEntity { ... }
```

---

## Summary: Adding a New Entity

To add a new tenant-scoped entity:

1. **Model** — Create class implementing `ITenantEntity`, `ISoftDeletable`, `IReferenceCodeEntity`
2. **DbContext** — Add `DbSet<T>`, add `HasQueryFilter(e => e.TenantId == TenantId)`
3. **Repository interface** — Extend `ITenantScopedRepository<T>`, add custom query methods
4. **Repository class** — Extend `TenantScopedRepository<T>`, pass PK selector: `base(context, x => x.MyEntityId)`
5. **Migration** — `dotnet ef migrations add AddMyEntity`
6. **Reference code prefix** — Set `ReferencePrefix => "MYE"` on the entity

The base repository handles CRUD, PK generation, reference code generation, soft delete, and PostgreSQL constraint mapping automatically.
