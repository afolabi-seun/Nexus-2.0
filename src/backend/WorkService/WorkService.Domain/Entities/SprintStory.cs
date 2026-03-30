namespace WorkService.Domain.Entities;

public class SprintStory
{
    public Guid SprintStoryId { get; set; } = Guid.NewGuid();
    public Guid SprintId { get; set; }
    public Guid StoryId { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    public DateTime? RemovedDate { get; set; }
}
