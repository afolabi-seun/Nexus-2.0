using FluentValidation;
using ProfileService.Application.DTOs.Departments;

namespace ProfileService.Application.Validators;

public class DepartmentPreferencesRequestValidator : AbstractValidator<DepartmentPreferencesRequest>
{
    public DepartmentPreferencesRequestValidator()
    {
        RuleFor(x => x.MaxConcurrentTasksDefault).GreaterThan(0).When(x => x.MaxConcurrentTasksDefault.HasValue);
    }
}
