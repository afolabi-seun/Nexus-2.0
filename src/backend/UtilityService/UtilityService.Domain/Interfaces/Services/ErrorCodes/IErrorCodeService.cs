namespace UtilityService.Domain.Interfaces.Services.ErrorCodes;

public interface IErrorCodeService
{
    Task<object> CreateAsync(object request, CancellationToken ct = default);
    Task<IEnumerable<object>> ListAsync(CancellationToken ct = default);
    Task<object> UpdateAsync(string code, object request, CancellationToken ct = default);
    Task DeleteAsync(string code, CancellationToken ct = default);
}
