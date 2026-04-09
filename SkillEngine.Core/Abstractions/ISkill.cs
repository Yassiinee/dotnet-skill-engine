using SkillEngine.Core.Models;

namespace SkillEngine.Core.Abstractions;

/// <summary>
/// Represents a named, executable AI skill.
/// Skills are the fundamental units of capability in the Skill Engine.
/// Each skill corresponds to a tool exposed via MCP.
/// </summary>
public interface ISkill
{
    /// <summary>Unique tool name exposed via MCP (e.g., "scaffold", "review-code").</summary>
    string Name { get; }

    /// <summary>Human-readable description surfaced in the MCP tool manifest.</summary>
    string Description { get; }

    /// <summary>JSON Schema describing the parameters accepted by this skill.</summary>
    string InputSchema { get; }

    /// <summary>
    /// Executes the skill with the given request context.
    /// </summary>
    /// <param name="request">Resolved skill request with parameters and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SkillResult"/> containing the output or error.</returns>
    Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default);
}
