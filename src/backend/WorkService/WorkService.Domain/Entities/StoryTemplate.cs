namespace WorkService.Domain.Entities;

public class StoryTemplate
{
    public Guid StoryTemplateId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultTitle { get; set; }
    public string? DefaultDescription { get; set; }
    public string? DefaultAcceptanceCriteria { get; set; }
    public string DefaultPriority { get; set; } = "Medium";
    public int? DefaultStoryPoints { get; set; }
    public string? DefaultLabelsJson { get; set; }
    public string? DefaultTaskTypesJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
