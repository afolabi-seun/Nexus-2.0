# Requirements Document

## Introduction

This feature introduces a generic repository base class (`IGenericRepository<T>` / `GenericRepository<T>`) across all five backend services (SecurityService, ProfileService, WorkService, BillingService, UtilityService) to eliminate duplicated CRUD boilerplate. Each service currently implements CRUD operations from scratch in every repository, with `SaveChangesAsync()` called inside each mutation method. The generic repository pattern centralizes common data access operations while allowing service-specific repository interfaces to extend the generic interface with custom query methods. Persistence control moves to the service layer for transactional consistency.

## Glossary

- **Generic_Repository**: A base class (`GenericRepository<T>`) that provides reusable CRUD operations against a `DbContext` using `DbContext.Set<T>()` for entity access.
- **Generic_Interface**: The interface (`IGenericRepository<T>`) that defines the contract for common data access operations shared across all repositories.
- **Service_Specific_Interface**: A repository interface that extends `IGenericRepository<T>` with additional query methods unique to a particular entity or service (e.g., `IStoryRepository : IGenericRepository<Story>`).
- **Service_Specific_Implementation**: A repository class that extends `GenericRepository<T>` and implements the Service_Specific_Interface, providing custom query logic.
- **DbContext**: The Entity Framework Core database context class specific to each service (SecurityDbContext, ProfileDbContext, WorkDbContext, BillingDbContext, UtilityDbContext).
- **Query_Filter**: An EF Core global query filter applied at the DbContext level for organization scoping or soft-delete filtering.
- **Infrastructure_Layer**: The project layer within each service where repository implementations and data access code reside.
- **Domain_Layer**: The project layer within each service where repository interfaces and entity definitions reside.
- **Service_Layer**: The application layer responsible for orchestrating business logic and controlling when `SaveChangesAsync()` is called.
- **Predicate**: A LINQ expression (`Expression<Func<T, bool>>`) used to filter entities in query operations.

## Requirements

### Requirement 1: Generic Repository Interface Definition

**User Story:** As a developer, I want a generic repository interface that defines common CRUD operations, so that all repositories share a consistent data access contract.

#### Acceptance Criteria

1. THE Generic_Interface SHALL define a `GetByIdAsync(Guid id, CancellationToken ct)` method that returns a nullable entity of type T.
2. THE Generic_Interface SHALL define a `GetAllAsync(CancellationToken ct)` method that returns a list of all entities of type T.
3. THE Generic_Interface SHALL define a `FindAsync(Expression<Func<T, bool>> predicate)` method that returns an `IQueryable<T>` for composable queries.
4. THE Generic_Interface SHALL define an `AddAsync(T entity, CancellationToken ct)` method that returns the added entity of type T.
5. THE Generic_Interface SHALL define an `UpdateAsync(T entity, CancellationToken ct)` method that marks the entity as modified.
6. THE Generic_Interface SHALL define a `DeleteAsync(T entity, CancellationToken ct)` method that marks the entity for removal.
7. THE Generic_Interface SHALL define an `AddRangeAsync(IEnumerable<T> entities, CancellationToken ct)` method for batch inserts.
8. THE Generic_Interface SHALL define an `UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct)` method for batch updates.
9. THE Generic_Interface SHALL define a `RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct)` method for batch removals.
10. THE Generic_Interface SHALL constrain the type parameter T to `class` to ensure only reference types are used as entities.
11. THE Generic_Interface SHALL reside in the Domain_Layer of each service.

### Requirement 2: Generic Repository Implementation

**User Story:** As a developer, I want a generic repository base class that implements common CRUD operations using `DbContext.Set<T>()`, so that I do not duplicate data access logic across repositories.

#### Acceptance Criteria

1. THE Generic_Repository SHALL accept a DbContext instance through constructor injection.
2. THE Generic_Repository SHALL use `DbContext.Set<T>()` to access the entity set for all operations.
3. THE Generic_Repository SHALL implement `GetByIdAsync` by calling `FindAsync` on the entity set with the provided Guid identifier.
4. THE Generic_Repository SHALL implement `GetAllAsync` by calling `ToListAsync` on the entity set.
5. THE Generic_Repository SHALL implement `FindAsync` by returning an `IQueryable<T>` filtered by the provided Predicate using `.Where(predicate)`.
6. THE Generic_Repository SHALL implement `AddAsync` by calling `AddAsync` on the entity set and returning the entity.
7. THE Generic_Repository SHALL implement `UpdateAsync` by calling `Update` on the entity set.
8. THE Generic_Repository SHALL implement `DeleteAsync` by calling `Remove` on the entity set.
9. THE Generic_Repository SHALL implement `AddRangeAsync` by calling `AddRangeAsync` on the entity set.
10. THE Generic_Repository SHALL implement `UpdateRangeAsync` by calling `UpdateRange` on the entity set.
11. THE Generic_Repository SHALL implement `RemoveRangeAsync` by calling `RemoveRange` on the entity set.
12. THE Generic_Repository SHALL reside in the Infrastructure_Layer of each service.
13. THE Generic_Repository SHALL accept any DbContext-derived type, enabling use with SecurityDbContext, ProfileDbContext, WorkDbContext, BillingDbContext, and UtilityDbContext.

### Requirement 3: Persistence Control Separation

**User Story:** As a developer, I want the generic repository to not call `SaveChangesAsync()` internally, so that the service layer controls transaction boundaries for consistency.

#### Acceptance Criteria

1. THE Generic_Repository SHALL NOT call `SaveChangesAsync()` inside any of the following methods: `AddAsync`, `UpdateAsync`, `DeleteAsync`, `AddRangeAsync`, `UpdateRangeAsync`, `RemoveRangeAsync`.
2. THE Service_Layer SHALL be responsible for calling `SaveChangesAsync()` on the DbContext after invoking one or more repository operations.
3. WHEN multiple repository operations are performed within a single service method, THE Service_Layer SHALL call `SaveChangesAsync()` once after all operations complete to ensure atomicity.

### Requirement 4: Service-Specific Interface Extension

**User Story:** As a developer, I want service-specific repository interfaces to extend the generic interface, so that custom query methods coexist with standard CRUD operations under a single contract.

#### Acceptance Criteria

1. WHEN a service-specific repository requires custom query methods, THE Service_Specific_Interface SHALL extend `IGenericRepository<T>` for the corresponding entity type.
2. THE Service_Specific_Interface SHALL declare only methods that are not already provided by the Generic_Interface.
3. WHEN a service-specific repository has no custom query methods, THE Service_Specific_Interface SHALL extend `IGenericRepository<T>` without adding additional method declarations.

### Requirement 5: Service-Specific Implementation Inheritance

**User Story:** As a developer, I want service-specific repository implementations to inherit from the generic base class, so that CRUD boilerplate is eliminated while custom logic is preserved.

#### Acceptance Criteria

1. THE Service_Specific_Implementation SHALL extend `GenericRepository<T>` and implement the corresponding Service_Specific_Interface.
2. THE Service_Specific_Implementation SHALL pass the service-specific DbContext to the `GenericRepository<T>` base constructor.
3. THE Service_Specific_Implementation SHALL retain all existing custom query methods (pagination, filtering, full-text search, includes, cross-entity counts) without modification to their behavior.
4. THE Service_Specific_Implementation SHALL remove duplicated CRUD methods that are now provided by the Generic_Repository base class.

### Requirement 6: Query Filter Bypass Support

**User Story:** As a developer, I want the generic repository to support bypassing EF Core global query filters, so that cross-organization queries continue to work correctly.

#### Acceptance Criteria

1. THE Generic_Interface SHALL define a `FindWithoutFiltersAsync(Expression<Func<T, bool>> predicate)` method that returns an `IQueryable<T>` with global query filters ignored.
2. THE Generic_Repository SHALL implement `FindWithoutFiltersAsync` by calling `IgnoreQueryFilters()` on the entity set before applying the Predicate.
3. WHEN a Service_Specific_Implementation requires cross-organization queries, THE Service_Specific_Implementation SHALL use `FindWithoutFiltersAsync` or access the DbContext directly to call `IgnoreQueryFilters()`.

### Requirement 7: Per-Service Deployment Independence

**User Story:** As a developer, I want the generic repository to be defined within each service rather than in a shared library, so that each service remains independently deployable.

#### Acceptance Criteria

1. THE Generic_Interface SHALL be defined in the Domain_Layer of each individual service (SecurityService, ProfileService, WorkService, BillingService, UtilityService).
2. THE Generic_Repository SHALL be defined in the Infrastructure_Layer of each individual service.
3. THE Generic_Interface and Generic_Repository SHALL NOT reside in a shared library or shared project referenced across services.

### Requirement 8: Dependency Injection Registration

**User Story:** As a developer, I want service-specific repositories to be registered in the DI container using their specific interfaces, so that the application resolves the correct implementation at runtime.

#### Acceptance Criteria

1. WHEN registering repositories in the DI container, THE Service_Layer SHALL register each Service_Specific_Implementation against its corresponding Service_Specific_Interface (e.g., `IStoryRepository` maps to `StoryRepository`).
2. THE Service_Layer SHALL NOT register `IGenericRepository<T>` as an open generic in the DI container, since consumers depend on service-specific interfaces.
3. WHEN a repository has no custom methods and only uses generic CRUD, THE Service_Layer SHALL register `GenericRepository<T>` directly against `IGenericRepository<T>` for that entity type.

### Requirement 9: Incremental Migration Strategy

**User Story:** As a developer, I want to migrate repositories to the generic pattern incrementally (service by service, repository by repository), so that the migration is low-risk and does not require a single large changeset.

#### Acceptance Criteria

1. WHILE the migration is in progress, THE system SHALL support both migrated repositories (extending GenericRepository) and non-migrated repositories (standalone implementations) within the same service.
2. WHEN a repository is migrated, THE Service_Specific_Implementation SHALL produce identical query results and entity state changes as the original implementation for all existing operations.
3. WHEN a repository is migrated, THE existing unit tests and integration tests for that repository SHALL continue to pass without modification to test assertions.

### Requirement 10: Existing Behavior Preservation

**User Story:** As a developer, I want the migration to preserve all existing repository behaviors, so that no regressions are introduced.

#### Acceptance Criteria

1. WHEN a repository is migrated, THE Service_Specific_Implementation SHALL preserve all existing `Include` and `ThenInclude` navigation property loading in custom query methods.
2. WHEN a repository is migrated, THE Service_Specific_Implementation SHALL preserve all existing pagination logic (Skip/Take with total count) in custom list methods.
3. WHEN a repository is migrated, THE Service_Specific_Implementation SHALL preserve all existing full-text search behavior using PostgreSQL `tsvector` operations.
4. WHEN a repository is migrated, THE Service_Specific_Implementation SHALL preserve all existing `IgnoreQueryFilters()` calls in methods that perform cross-organization queries.
5. WHEN a repository is migrated, THE Service_Specific_Implementation SHALL preserve all existing ordering, filtering, and projection logic in custom query methods.
6. IF a migrated repository previously called `SaveChangesAsync()` inside a mutation method, THEN THE Service_Layer calling that repository SHALL be updated to call `SaveChangesAsync()` after the repository operation to maintain the same persistence timing.
