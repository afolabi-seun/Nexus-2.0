namespace WorkService.Application.DTOs.StoryTemplates;

public class StoryTemplateResponse
{
    public Guid StoryTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultTitle { get; set; }
    public string? DefaultDescription { get; set; }
    public string? DefaultAcceptanceCriteria { get; set; }
    public string DefaultPriority { get; set; } = "Medium";
    public int? DefaultStoryPoints { get; set; }
    public List<string>? DefaultLabels { get; set; }
    public List<string>? DefaultTaskTypes { get; set; }
    public DateTime DateCreated { get; set; }
}

public class CreateStoryTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultTitle { get; set; }
    public string? DefaultDescription { get; set; }
    public string? DefaultAcceptanceCriteria { get; set; }
    public string DefaultPriority { get; set; } = "Medium";
    public int? DefaultStoryPoints { get; set; }
    public List<string>? DefaultLabels { get; set; }
    public List<string>? DefaultTaskTypes { get; set; }
}

public class UpdateStoryTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DefaultTitle { get; set; }
    public string? DefaultDescription { get; set; }
    public string? DefaultAcceptanceCriteria { get; set; }
    public string? DefaultPriority { get; set; }
    public int? DefaultStoryPoints { get; set; }
    public List<string>? DefaultLabels { get; set; }
    public List<string>? DefaultTaskTypes { get; set; }
}
