namespace WorkService.Domain.Interfaces.Services.StoryTemplates;

/// <summary>
/// Manages reusable story templates that pre-fill story creation forms
/// with default values (title, description, priority, story points, labels, task types).
/// Templates are scoped to an organization.
/// </summary>
public interface IStoryTemplateService
{
    Task<object> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid templateId, CancellationToken ct = default);
    Task<object> CreateAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid templateId, object request, CancellationToken ct = default);
    Task DeleteAsync(Guid templateId, CancellationToken ct = default);
}
