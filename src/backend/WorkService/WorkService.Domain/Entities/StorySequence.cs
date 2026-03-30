namespace WorkService.Domain.Entities;

public class StorySequence
{
    public Guid ProjectId { get; set; }
    public long CurrentValue { get; set; } = 0;
}
