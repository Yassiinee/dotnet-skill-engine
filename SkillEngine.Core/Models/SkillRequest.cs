namespace SkillEngine.Core.Models;

/// <summary>
/// Represents an incoming request to invoke a skill via MCP.
/// </summary>
public sealed class SkillRequest
{
    /// <summary>The MCP tool name being invoked (e.g., "scaffold", "review-code").</summary>
    public required string ToolName { get; init; }

    /// <summary>Raw parameters passed by the AI model, keyed by parameter name.</summary>
    public Dictionary<string, object?> Parameters { get; init; } = new();

    /// <summary>Optional session identifier for maintaining state across calls.</summary>
    public string? SessionId { get; init; }

    /// <summary>Optional metadata from the calling agent (model name, version, etc.).</summary>
    public Dictionary<string, string> AgentMetadata { get; init; } = new();

    /// <summary>UTC timestamp of the request.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Retrieves a typed parameter value by name.</summary>
    public T? GetParameter<T>(string name)
    {
        return Parameters.TryGetValue(name, out object? value) && value is T typed ? typed : default;
    }

    /// <summary>Retrieves a required parameter, throwing if missing.</summary>
    public T RequireParameter<T>(string name)
    {
        var result = GetParameter<T>(name);
        return result is null
            ? throw new InvalidOperationException($"Required parameter '{name}' is missing or has an invalid type.")
            : result;
    }
}
