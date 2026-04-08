namespace SkillEngine.Core.Prompts;

/// <summary>
/// A named prompt template with Scriban-based variable interpolation.
/// Templates use <c>{{ variable_name }}</c> syntax.
/// </summary>
public sealed class PromptTemplate
{
    /// <summary>Unique template name (e.g., "scaffold-controller").</summary>
    public required string Name { get; init; }

    /// <summary>Template source text with Scriban placeholders.</summary>
    public required string Template { get; init; }

    /// <summary>Optional description of what this template produces.</summary>
    public string? Description { get; init; }

    /// <summary>Names of expected variables for documentation/validation.</summary>
    public IReadOnlyList<string> ExpectedVariables { get; init; } = [];
}
