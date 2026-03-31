using WorkService.Application.DTOs.Labels;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.Labels;
using WorkService.Domain.Interfaces.Services.Labels;

namespace WorkService.Infrastructure.Services.Labels;

public class LabelService : ILabelService
{
    private readonly ILabelRepository _labelRepo;

    public LabelService(ILabelRepository labelRepo) => _labelRepo = labelRepo;

    public async Task<object> CreateAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (CreateLabelRequest)request;
        var existing = await _labelRepo.GetByNameAsync(organizationId, req.Name, ct);
        if (existing != null) throw new LabelNameDuplicateException(req.Name);

        var label = new Label { OrganizationId = organizationId, Name = req.Name, Color = req.Color };
        await _labelRepo.AddAsync(label, ct);
        return BuildResponse(label);
    }

    public async Task<object> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var labels = await _labelRepo.ListAsync(organizationId, ct);
        return labels.Select(BuildResponse).ToList();
    }

    public async Task<object> UpdateAsync(Guid labelId, object request, CancellationToken ct = default)
    {
        var req = (UpdateLabelRequest)request;
        var label = await _labelRepo.GetByIdAsync(labelId, ct)
            ?? throw new LabelNotFoundException(labelId);

        if (req.Name != null) label.Name = req.Name;
        if (req.Color != null) label.Color = req.Color;
        await _labelRepo.UpdateAsync(label, ct);
        return BuildResponse(label);
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid labelId, CancellationToken ct = default)
    {
        var label = await _labelRepo.GetByIdAsync(labelId, ct)
            ?? throw new LabelNotFoundException(labelId);
        await _labelRepo.RemoveAsync(label, ct);
    }

    private static LabelResponse BuildResponse(Label l) => new()
    {
        LabelId = l.LabelId, Name = l.Name, Color = l.Color
    };
}
