using FluentValidation;
using WorkService.Application.DTOs.Tasks;

namespace WorkService.Application.Validators;

public class TaskStatusRequestValidator : AbstractValidator<TaskStatusRequest>
{
    public TaskStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty();
    }
}
