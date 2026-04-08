namespace SkillEngine.Core.Abstractions;

/// <summary>
/// Builds a rich <see cref="SkillContext"/> for a given request,
/// injecting session state, user metadata, and relevant knowledge snippets.
/// </summary>
public interface IContextBuilder
{
    /// <summary>
    /// Enriches the base request with contextual information before skill execution.
    /// </summary>
    /// <param name="request">The incoming skill request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fully populated <see cref="SkillContext"/>.</returns>
    Task<SkillContext> BuildAsync(SkillRequest request, CancellationToken cancellationToken = default);
}
