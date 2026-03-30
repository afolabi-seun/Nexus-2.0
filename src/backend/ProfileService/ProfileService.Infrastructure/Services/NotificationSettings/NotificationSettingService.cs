using Microsoft.AspNetCore.Http;
using ProfileService.Application.DTOs.NotificationSettings;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Domain.Interfaces.Services;

namespace ProfileService.Infrastructure.Services.NotificationSettings;

public class NotificationSettingService : INotificationSettingService
{
    private readonly INotificationSettingRepository _settingRepo;
    private readonly INotificationTypeRepository _typeRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationSettingService(
        INotificationSettingRepository settingRepo,
        INotificationTypeRepository typeRepo,
        IHttpContextAccessor httpContextAccessor)
    {
        _settingRepo = settingRepo;
        _typeRepo = typeRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<object>> GetSettingsAsync(Guid memberId, CancellationToken ct = default)
    {
        var types = await _typeRepo.ListAsync(ct);
        var settings = await _settingRepo.GetByMemberAsync(memberId, ct);
        var settingsDict = settings.ToDictionary(s => s.NotificationTypeId);

        return types.Select(t =>
        {
            settingsDict.TryGetValue(t.NotificationTypeId, out var setting);
            return new NotificationSettingResponse
            {
                NotificationTypeId = t.NotificationTypeId,
                TypeName = t.TypeName,
                IsEmail = setting?.IsEmail ?? true,
                IsPush = setting?.IsPush ?? true,
                IsInApp = setting?.IsInApp ?? true
            };
        });
    }

    public async Task UpdateSettingAsync(Guid memberId, Guid notificationTypeId,
        object request, CancellationToken ct = default)
    {
        var req = (UpdateNotificationSettingRequest)request;

        var existing = await _settingRepo.GetAsync(memberId, notificationTypeId, ct);
        if (existing is not null)
        {
            existing.IsEmail = req.IsEmail;
            existing.IsPush = req.IsPush;
            existing.IsInApp = req.IsInApp;
            await _settingRepo.UpdateAsync(existing, ct);
        }
        else
        {
            // Extract OrganizationId from HttpContext
            Guid orgId = Guid.Empty;
            if (_httpContextAccessor.HttpContext?.Items.TryGetValue("OrganizationId", out var orgIdObj) == true
                && orgIdObj is string orgIdStr && Guid.TryParse(orgIdStr, out var parsed))
            {
                orgId = parsed;
            }

            var setting = new NotificationSetting
            {
                NotificationTypeId = notificationTypeId,
                OrganizationId = orgId,
                TeamMemberId = memberId,
                IsEmail = req.IsEmail,
                IsPush = req.IsPush,
                IsInApp = req.IsInApp
            };
            await _settingRepo.AddAsync(setting, ct);
        }
    }

    public async Task<IEnumerable<object>> ListTypesAsync(CancellationToken ct = default)
    {
        var types = await _typeRepo.ListAsync(ct);
        return types.Select(t => new NotificationTypeResponse
        {
            NotificationTypeId = t.NotificationTypeId,
            TypeName = t.TypeName,
            Description = t.Description
        });
    }
}
