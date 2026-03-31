using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs.Comments;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.ActivityLogs;
using WorkService.Domain.Interfaces.Repositories.Comments;
using WorkService.Domain.Interfaces.Services.Comments;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Infrastructure.Services.ServiceClients;

namespace WorkService.Infrastructure.Services.Comments;

public partial class CommentService : ICommentService
{
    [GeneratedRegex(@"@(\w+(?:\.\w+)*)")]
    private static partial Regex MentionRegex();

    private readonly ICommentRepository _commentRepo;
    private readonly IActivityLogRepository _activityLogRepo;
    private readonly IOutboxService _outbox;
    private readonly IProfileServiceClient? _profileClient;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        ICommentRepository commentRepo, IActivityLogRepository activityLogRepo,
        IOutboxService outbox, ILogger<CommentService> logger,
        IProfileServiceClient? profileClient = null)
    {
        _commentRepo = commentRepo; _activityLogRepo = activityLogRepo;
        _outbox = outbox; _logger = logger; _profileClient = profileClient;
    }

    public async Task<object> CreateAsync(Guid organizationId, Guid authorId, object request, CancellationToken ct = default)
    {
        var req = (CreateCommentRequest)request;

        var comment = new Comment
        {
            OrganizationId = organizationId, EntityType = req.EntityType,
            EntityId = req.EntityId, AuthorId = authorId, Content = req.Content,
            ParentCommentId = req.ParentCommentId
        };

        await _commentRepo.AddAsync(comment, ct);

        await _activityLogRepo.AddAsync(new Domain.Entities.ActivityLog
        {
            OrganizationId = organizationId, EntityType = req.EntityType, EntityId = req.EntityId,
            Action = "CommentAdded", ActorId = authorId, ActorName = "System",
            Description = "Comment added"
        }, ct);

        // Resolve @mentions
        var mentions = MentionRegex().Matches(req.Content);
        if (_profileClient != null)
        {
            foreach (Match match in mentions)
            {
                var name = match.Groups[1].Value;
                try
                {
                    var user = await _profileClient.ResolveUserByDisplayNameAsync(organizationId, name, ct)
                        ?? await _profileClient.ResolveUserByEmailAsync(organizationId, name, ct);

                    if (user != null)
                    {
                        var preview = req.Content.Length > 100 ? req.Content[..100] : req.Content;
                        await _outbox.PublishAsync(new
                        {
                            MessageType = "NotificationRequest", Action = "MentionedInComment",
                            NotificationType = "MentionedInComment", EntityType = req.EntityType,
                            EntityId = req.EntityId.ToString(),
                            TemplateVariables = new Dictionary<string, string>
                            {
                                ["MentionedUserId"] = user.Id.ToString(),
                                ["MentionerName"] = "System",
                                ["CommentPreview"] = preview
                            }
                        }, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve mention @{Name}", name);
                }
            }
        }

        return BuildResponse(comment);
    }

    public async Task<object> UpdateAsync(Guid commentId, Guid userId, string content, CancellationToken ct = default)
    {
        var comment = await _commentRepo.GetByIdAsync(commentId, ct)
            ?? throw new CommentNotFoundException(commentId);
        if (comment.AuthorId != userId) throw new CommentNotAuthorException();

        comment.Content = content;
        comment.IsEdited = true;
        comment.DateUpdated = DateTime.UtcNow;
        await _commentRepo.UpdateAsync(comment, ct);

        return BuildResponse(comment);
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid commentId, Guid userId, string userRole, CancellationToken ct = default)
    {
        var comment = await _commentRepo.GetByIdAsync(commentId, ct)
            ?? throw new CommentNotFoundException(commentId);
        if (comment.AuthorId != userId && userRole != "OrgAdmin")
            throw new CommentNotAuthorException();

        comment.FlgStatus = "D";
        comment.DateUpdated = DateTime.UtcNow;
        await _commentRepo.UpdateAsync(comment, ct);
    }

    public async Task<object> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        var comments = await _commentRepo.ListByEntityAsync(entityType, entityId, ct);
        return comments.Select(BuildResponse).ToList();
    }

    private static CommentResponse BuildResponse(Comment c) => new()
    {
        CommentId = c.CommentId, EntityType = c.EntityType, EntityId = c.EntityId,
        AuthorId = c.AuthorId, Content = c.Content, ParentCommentId = c.ParentCommentId,
        IsEdited = c.IsEdited, DateCreated = c.DateCreated, DateUpdated = c.DateUpdated
    };
}
