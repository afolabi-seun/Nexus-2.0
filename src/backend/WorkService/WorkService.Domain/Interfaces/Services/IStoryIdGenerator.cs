namespace WorkService.Domain.Interfaces.Services;

public interface IStoryIdGenerator
{
    Task<(string StoryKey, long SequenceNumber)> GenerateNextIdAsync(Guid projectId, CancellationToken ct = default);
}
