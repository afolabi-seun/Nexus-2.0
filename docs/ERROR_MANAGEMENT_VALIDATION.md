# Validation Pipeline

## Overview

Validation errors are the **most common error type** in WEP. They are handled entirely at the controller layer — before any service or repository code runs — via a three-stage pipeline:

```
Request Body
  │
  ▼
┌──────────────────────────┐
│ 1. NullBodyFilter        │  Missing body? → 422 immediately
├──────────────────────────┤
│ 2. FluentValidation      │  Field rules fail? → 422 with field errors
│    (auto-validation)     │
├──────────────────────────┤
│ 3. Model State Factory   │  ASP.NET model binding errors → 422
│    (ConfigureApiBehavior) │
└──────────────────────────┘
  │
  ▼
Controller Action (only reached if all 3 stages pass)
```

All three stages return the same `ApiResponse<T>` envelope with `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, and HTTP 422.

---

## Stage 1: NullBodyFilter

Catches requests with a missing or null body before FluentValidation even runs. Registered as a global action filter on all controllers.

```csharp
// Core/Filters/NullBodyFilter.cs
public class NullBodyFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var param in context.ActionDescriptor.Parameters)
        {
            if (param.BindingInfo?.BindingSource?.Id == "Body" &&
                context.ActionArguments.TryGetValue(param.Name, out var value) && value == null)
            {
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = false, ErrorCode = "VALIDATION_ERROR", ErrorValue = 1000,
                    Message = "Request body is required.",
                    ResponseCode = "99", ResponseDescription = "Validation failed"
                }) { StatusCode = 422 };
                return;
            }
        }
    }
}
```

Response when body is null:
```json
// POST /api/v1/customers  (no body)
// HTTP 422
{
  "responseCode": "99",
  "responseDescription": "Validation failed",
  "success": false,
  "data": null,
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Request body is required.",
  "correlationId": "...",
  "errors": null
}
```

Registration: `Core/Extensions/ControllerServiceExtensions.cs`
```csharp
services.AddControllers(options =>
{
    options.Filters.Add<NullBodyFilter>();
});
```

---

## Stage 2: FluentValidation Auto-Validation

FluentValidation validators are auto-discovered from the assembly and run automatically on every `[FromBody]` parameter before the controller action executes.

### Registration

```csharp
// Core/Extensions/ApplicationServiceExtensions.cs
services.AddValidatorsFromAssemblyContaining<ProfileDbContext>();

// Core/Extensions/ControllerServiceExtensions.cs
services.AddFluentValidationAutoValidation();
```

`AddValidatorsFromAssemblyContaining` scans the assembly for all classes inheriting `AbstractValidator<T>` and registers them. `AddFluentValidationAutoValidation` hooks them into the ASP.NET model validation pipeline so they run automatically — no manual `validator.Validate()` calls needed.

### Validator Example

```csharp
// Validators/CustomerCreateRequestValidator.cs
public class CustomerCreateRequestValidator : AbstractValidator<CustomerCreateRequest>
{
    private const string NigerianPhoneRegex = @"^(0\d{10}|\+234\d{10})$";

    public CustomerCreateRequestValidator()
    {
        RuleFor(x => x.SmeId)
            .NotEmpty().WithMessage("SmeId is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required.")
            .MaximumLength(100).WithMessage("FirstName must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName is required.")
            .MaximumLength(100).WithMessage("LastName must not exceed 100 characters.");

        RuleFor(x => x.PhoneNo)
            .NotEmpty().WithMessage("PhoneNo is required.")
            .Matches(NigerianPhoneRegex).WithMessage("PhoneNo must be a valid Nigerian phone number.")
            .MaximumLength(15).WithMessage("PhoneNo must not exceed 15 characters.");

        RuleFor(x => x.EmailAddress)
            .EmailAddress().WithMessage("EmailAddress must be a valid email address.")
            .MaximumLength(255).WithMessage("EmailAddress must not exceed 255 characters.")
            .When(x => x.EmailAddress != null);  // Only validate if provided

        RuleFor(x => x.Dob)
            .NotNull().WithMessage("Dob is required.");
    }
}
```

Key patterns:
- `.When(x => x.Field != null)` — conditional validation for optional fields
- `.WithMessage(...)` — custom error messages (not framework defaults)
- Regex patterns for Nigerian phone numbers, emails, etc.
- Max length rules matching database column constraints

### Naming Convention

Validators follow the pattern `{RequestDto}Validator.cs` in the `Validators/` folder:

| Validator | DTO | Service |
|-----------|-----|---------|
| `CustomerCreateRequestValidator` | `CustomerCreateRequest` | Profile |
| `OnboardingRequestValidator` | `OnboardingRequest` | Profile |
| `InviteCreateRequestValidator` | `InviteCreateRequest` | Profile |
| `CardCreateRequestValidator` | `CardCreateRequest` | Profile |
| `BeneficiaryCreateRequestValidator` | `BeneficiaryCreateRequest` | Profile |
| `LoginRequestValidator` | `LoginRequest` | Security |
| `ErrorCodeCreateRequestValidator` | `ErrorCodeCreateRequest` | Utility |

---

## Stage 3: Model State Factory (InvalidModelStateResponseFactory)

When FluentValidation (or ASP.NET model binding) adds errors to `ModelState`, the custom `InvalidModelStateResponseFactory` formats them into the standard `ApiResponse` envelope:

```csharp
// Core/Extensions/ControllerServiceExtensions.cs
options.InvalidModelStateResponseFactory = context =>
{
    var fieldErrors = context.ModelState
        .Where(e => e.Value?.Errors.Count > 0)
        .SelectMany(e => e.Value!.Errors.Select(err => new
        {
            Field = e.Key,
            Message = err.ErrorMessage
        }))
        .ToList();

    var correlationId = context.HttpContext.Items["CorrelationId"] as string;

    var response = new ApiResponse<object>
    {
        Success = false,
        ErrorCode = "VALIDATION_ERROR",
        ErrorValue = 1000,
        ResponseCode = "96",
        ResponseDescription = "Validation error",
        Message = "Validation error",
        Data = fieldErrors,       // Field-level errors go in data
        CorrelationId = correlationId
    };

    return new ObjectResult(response) { StatusCode = 422 };
};
```

Note: `SuppressModelStateInvalidFilter = false` (the default) ensures ASP.NET automatically returns 422 when `ModelState` is invalid — the controller action is never invoked.

### Validation Error Response

```json
// POST /api/v1/customers
// { "firstName": "", "phoneNo": "invalid" }
// HTTP 422
{
  "responseCode": "96",
  "responseDescription": "Validation error",
  "success": false,
  "data": [
    { "field": "SmeId", "message": "SmeId is required." },
    { "field": "FirstName", "message": "FirstName is required." },
    { "field": "PhoneNo", "message": "PhoneNo must be a valid Nigerian phone number." },
    { "field": "LastName", "message": "LastName is required." },
    { "field": "Dob", "message": "Dob is required." }
  ],
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Validation error",
  "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
  "errors": null
}
```

Key points:
- Field-level errors are in `data` (not `errors`) — this is by design so clients can iterate the array
- Every failing field gets its own entry with the field name and message
- Multiple errors per field are possible (e.g. both `NotEmpty` and `MaximumLength` fail)
- The `correlationId` is included even on validation errors for traceability

---

## What Validation Does NOT Do

Validation only checks **structural correctness** of the request. It does NOT check:

- Whether the referenced `SmeId` exists (that's the service layer's job)
- Whether the phone number is already registered (that's a uniqueness check in the service/repository)
- Whether the user has permission to create the resource (that's authorization middleware)

This separation means:
- **422** = structurally invalid request (validation)
- **400** = structurally valid but business rule violation (service layer)
- **404** = referenced entity doesn't exist (service layer)
- **409** = duplicate/conflict (service or repository layer)

---

## Consistency Across Services

All 5 services use the same validation setup:
- `NullBodyFilter` registered as a global filter
- `AddFluentValidationAutoValidation()` for auto-validation
- `AddValidatorsFromAssemblyContaining<>()` for auto-discovery
- Same `InvalidModelStateResponseFactory` producing identical 422 responses
- Same `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, `responseCode: "96"` across all services

---

Previous: [Error Code Registry & Resolution](./ERROR_MANAGEMENT_CODES.md) · Next: [Exception Handling Middleware](./ERROR_MANAGEMENT_MIDDLEWARE.md)
