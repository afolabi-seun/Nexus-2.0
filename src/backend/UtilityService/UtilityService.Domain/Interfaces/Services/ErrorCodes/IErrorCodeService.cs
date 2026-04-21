using UtilityService.Domain.Results;

namespace UtilityService.Domain.Interfaces.Services.ErrorCodes;

public interface IErrorCodeService
{
    Task<ServiceResult<object>> CreateAsync(object request, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<object>>> ListAsync(CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(string code, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(string code, CancellationToken ct = default);
}
