namespace SkillEngine.Core.Abstractions;

/// <summary>
/// Routes an incoming intent string to the appropriate registered <see cref="ISkill"/>.
/// </summary>
public interface ISkillRouter
{
    /// <summary>
    /// Resolves the skill best matching the given tool name or natural-language intent.
    /// </summary>
    /// <param name="intent">The tool name or intent string from the MCP request.</param>
    /// <returns>The matching skill, or <c>null</c> if none is found.</returns>
    ISkill? Route(string intent);

    /// <summary>Returns all registered skills.</summary>
    IReadOnlyList<ISkill> GetAll();
}
