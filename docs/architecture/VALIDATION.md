# Validation

FluentValidation in the Application layer, controller-level usage, and validation error response shape.

## Overview

Request validation uses [FluentValidation](https://docs.fluentvalidation.net/) in the `{Service}.Application/Validators/` folder. Validators are registered automatically via `AddValidatorsFromAssemblyContaining<>()` in DI configuration and run as part of the ASP.NET model binding pipeline.

## Validator Location and Naming

```
{Service}.Application/
└── Validators/
    ├── CreateOrganizationRequestValidator.cs
    ├── UpdateOrganizationRequestValidator.cs
    ├── CreateDepartmentRequestValidator.cs
    └── ...
```

Convention: `{RequestDtoName}Validator.cs`

## Validator Example

```csharp
public class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StoryIdPrefix).NotEmpty().Matches(@"^[A-Z0-9]{2,10}$")
            .WithMessage("StoryIdPrefix must be 2–10 uppercase alphanumeric characters.");
        RuleFor(x => x.TimeZone).NotEmpty();
        RuleFor(x => x.DefaultSprintDurationWeeks).InclusiveBetween(1, 4);
    }
}
```

## Validation vs Business Rules

| Layer | What it checks | How it fails | HTTP Status |
|-------|---------------|--------------|-------------|
| FluentValidation (Application) | Field format, required fields, length, regex | `ValidationFail()` → 400 | 400 Bad Request |
| Service layer (Infrastructure) | Business rules, uniqueness, state transitions | `throw DomainException` → middleware | Varies (404, 409, etc.) |
| Database constraints (PostgreSQL) | Unique indexes, foreign keys | `DbUpdateException` → middleware | 409 Conflict |

FluentValidation catches malformed input before it reaches the service layer. Business rule validation (e.g., "does this name already exist?") happens in the service layer.

## Validation Error Response

When validation fails, the response uses `errorCode: "VALIDATION_ERROR"` with field-level details in the `errors` array:

```json
{
  "responseCode": "96",
  "responseDescription": "Validation failed.",
  "success": false,
  "data": null,
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Validation failed.",
  "correlationId": "a1b2c3d4e5f6",
  "errors": [
    {
      "field": "OrganizationName",
      "message": "'Organization Name' must not be empty."
    },
    {
      "field": "StoryIdPrefix",
      "message": "StoryIdPrefix must be 2–10 uppercase alphanumeric characters."
    }
  ]
}
```

The `ErrorDetail` type:

```csharp
public class ErrorDetail
{
    public string Field { get; set; }
    public string Message { get; set; }
}
```

## ValidationFail Factory

`ApiResponse<T>.ValidationFail()` produces the validation error response:

```csharp
public static ApiResponse<T> ValidationFail(string message, List<ErrorDetail> errors) => new()
{
    Success = false,
    ErrorValue = 1000,
    ErrorCode = "VALIDATION_ERROR",
    Message = message,
    Errors = errors,
    ResponseCode = "96",
    ResponseDescription = message
};
```

## Common Validation Rules Used

| Rule | Usage |
|------|-------|
| `NotEmpty()` | Required fields |
| `MaximumLength(n)` | String length limits |
| `Matches(regex)` | Format validation (story prefix, project key) |
| `InclusiveBetween(min, max)` | Numeric ranges |
| `IsInEnum()` | Enum validation |
| `Must(predicate)` | Custom business rules |
| `WithMessage(msg)` | Custom error messages |

## Adding a New Validator

1. Create the request DTO in `{Service}.Application/DTOs/`
2. Create `{DtoName}Validator.cs` in `{Service}.Application/Validators/`
3. Extend `AbstractValidator<TDto>`
4. Define rules in the constructor
5. The validator is auto-registered via assembly scanning — no manual DI registration needed

## Related Docs

- [API_RESPONSES.md](API_RESPONSES.md) — Full ApiResponse envelope structure
- [ERROR_HANDLING.md](ERROR_HANDLING.md) — How business rule errors differ from validation errors
- [ERROR_CODES.md](ERROR_CODES.md) — `VALIDATION_ERROR` (1000) is the shared code across all services
