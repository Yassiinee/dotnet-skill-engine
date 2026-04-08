using SkillEngine.Core.Abstractions;

namespace SkillEngine.Core.Engine;

/// <summary>
/// Default context builder that enriches requests with session state and metadata.
/// Extend this class or replace it via DI to inject knowledge retrieval (RAG).
/// </summary>
public class DefaultContextBuilder : IContextBuilder
{
    private readonly Dictionary<string, Dictionary<string, object?>> _sessions = new();

    /// <inheritdoc/>
    public Task<SkillContext> BuildAsync(SkillRequest request, CancellationToken cancellationToken = default)
    {
        // Retrieve or create session state
        if (!_sessions.TryGetValue(request.SessionId ?? "__default", out var sessionState))
        {
            sessionState = new Dictionary<string, object?>();
            _sessions[request.SessionId ?? "__default"] = sessionState;
        }

        var context = new SkillContext
        {
            Request = request,
            SessionState = sessionState,
            CallingModel = request.AgentMetadata.GetValueOrDefault("model"),
            // KnowledgeSnippets can be populated here via a vector store / RAG pipeline
            KnowledgeSnippets = []
        };

        return Task.FromResult(context);
    }
}
