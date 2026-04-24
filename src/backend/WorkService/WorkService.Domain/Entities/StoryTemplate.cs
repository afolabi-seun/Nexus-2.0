namespace WorkService.Domain.Entities;

/// <summary>
/// Reusable template for pre-filling story creation forms.
/// Stores default values for common story fields. Scoped to an organization.
/// Soft-deleted via IsActive flag.
/// </summary>
public class StoryTemplate
{
    public Guid StoryTemplateId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }

    /// <summary>Template display name (e.g., "Bug Report", "Feature Request").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of when to use this template.</summary>
    public string? Description { get; set; }

    /// <summary>Pre-filled title pattern (e.g., "[BUG] ").</summary>
    public string? DefaultTitle { get; set; }

    /// <summary>Pre-filled description with sections/headings.</summary>
    public string? DefaultDescription { get; set; }

    /// <summary>Pre-filled acceptance criteria.</summary>
    public string? DefaultAcceptanceCriteria { get; set; }

    /// <summary>Default priority (Critical, High, Medium, Low).</summary>
    public string DefaultPriority { get; set; } = "Medium";

    /// <summary>Default story type (Feature, Bug, Improvement, Epic, Task).</summary>
    public string DefaultStoryType { get; set; } = "Feature";

    /// <summary>Default story point estimate.</summary>
    public int? DefaultStoryPoints { get; set; }

    /// <summary>JSON array of default label names to apply.</summary>
    public string? DefaultLabelsJson { get; set; }

    /// <summary>JSON array of default task types to auto-create (e.g., ["Development", "QA"]).</summary>
    public string? DefaultTaskTypesJson { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
