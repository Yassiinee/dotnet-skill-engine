namespace SkillEngine.Core;

/// <summary>
/// Enriched context injected into a skill before execution.
/// Contains session state, resolved knowledge snippets, and agent information.
/// </summary>
public sealed class SkillContext
{
    /// <summary>The original request that triggered this context.</summary>
    public required SkillRequest Request { get; init; }

    /// <summary>Session-scoped state, persisted across multiple skill calls.</summary>
    public Dictionary<string, object?> SessionState { get; init; } = new();

    /// <summary>Relevant knowledge snippets retrieved for this request.</summary>
    public IReadOnlyList<KnowledgeSnippet> KnowledgeSnippets { get; init; } = [];

    /// <summary>Name of the AI model making the request (e.g., "claude-3-5-sonnet").</summary>
    public string? CallingModel { get; init; }

    /// <summary>Correlation ID for distributed tracing.</summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
}

/// <summary>A piece of domain knowledge retrieved and injected into skill context.</summary>
public sealed class KnowledgeSnippet
{
    public required string Topic { get; init; }
    public required string Content { get; init; }
    public string? Source { get; init; }
    public float RelevanceScore { get; init; }
}
