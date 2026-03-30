namespace WorkService.Domain.Interfaces.Services;

public interface ILabelService
{
    Task<object> CreateAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<object> ListAsync(Guid organizationId, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid labelId, object request, CancellationToken ct = default);
    Task DeleteAsync(Guid labelId, CancellationToken ct = default);
}
