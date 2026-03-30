namespace WorkService.Domain.Entities;

public class StoryLabel
{
    public Guid StoryLabelId { get; set; } = Guid.NewGuid();
    public Guid StoryId { get; set; }
    public Guid LabelId { get; set; }
}
