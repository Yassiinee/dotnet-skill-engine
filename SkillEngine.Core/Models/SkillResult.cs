namespace SkillEngine.Core;

/// <summary>
/// Represents the output of a skill execution.
/// </summary>
public sealed record SkillResult
{
    /// <summary>Whether the skill executed successfully.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>The primary text output returned to the AI model.</summary>
    public string? Content { get; private init; }

    /// <summary>Structured data output (for skills returning JSON/objects).</summary>
    public object? Data { get; private init; }

    /// <summary>Error message if the skill failed.</summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>Error code for programmatic error handling.</summary>
    public string? ErrorCode { get; private init; }

    /// <summary>Execution duration for observability.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Additional metadata about the execution (token counts, model used, etc.).</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    // ── Factory Methods ──────────────────────────────────────────────────────

    public static SkillResult Success(string content, object? data = null) => new()
    {
        IsSuccess = true,
        Content = content,
        Data = data
    };

    public static SkillResult Failure(string errorMessage, string? errorCode = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode ?? "SKILL_ERROR"
    };

    public static SkillResult NotFound(string toolName) => new()
    {
        IsSuccess = false,
        ErrorMessage = $"No skill registered for tool '{toolName}'.",
        ErrorCode = "TOOL_NOT_FOUND"
    };
}
