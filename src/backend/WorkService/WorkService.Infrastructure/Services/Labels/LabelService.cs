using WorkService.Application.DTOs.Labels;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Labels;
using WorkService.Domain.Interfaces.Services.Labels;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.Labels;

public class LabelService : ILabelService
{
    private readonly ILabelRepository _labelRepo;
    private readonly WorkDbContext _dbContext;

    public LabelService(ILabelRepository labelRepo, WorkDbContext dbContext)
    {
        _labelRepo = labelRepo;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (CreateLabelRequest)request;
        var existing = await _labelRepo.GetByNameAsync(organizationId, req.Name, ct);
        if (existing != null)
            return ServiceResult<object>.Fail(4011, "LABEL_NAME_DUPLICATE", $"A label with name '{req.Name}' already exists.", 409);

        var label = new Label { OrganizationId = organizationId, Name = req.Name, Color = req.Color };
        await _labelRepo.AddAsync(label, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.Created(BuildResponse(label), "Label created successfully.");
    }

    public async Task<ServiceResult<object>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var labels = await _labelRepo.ListAsync(organizationId, ct);
        return ServiceResult<object>.Ok(labels.Select(BuildResponse).ToList(), "Labels retrieved.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid labelId, object request, CancellationToken ct = default)
    {
        var req = (UpdateLabelRequest)request;
        var label = await _labelRepo.GetByIdAsync(labelId, ct);
        if (label == null)
            return ServiceResult<object>.Fail(4010, "LABEL_NOT_FOUND", $"Label {labelId} not found.", 404);

        if (req.Name != null) label.Name = req.Name;
        if (req.Color != null) label.Color = req.Color;
        await _labelRepo.UpdateAsync(label, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.Ok(BuildResponse(label), "Label updated.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(Guid labelId, CancellationToken ct = default)
    {
        var label = await _labelRepo.GetByIdAsync(labelId, ct);
        if (label == null)
            return ServiceResult<object>.Fail(4010, "LABEL_NOT_FOUND", $"Label {labelId} not found.", 404);
        await _labelRepo.DeleteAsync(label, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent("Label deleted.");
    }

    private static LabelResponse BuildResponse(Label l) => new()
    {
        LabelId = l.LabelId, Name = l.Name, Color = l.Color
    };
}
