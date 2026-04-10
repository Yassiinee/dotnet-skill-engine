using SkillEngine.Core.Models;

namespace SkillEngine.Core.Abstractions;

/// <summary>
/// A middleware that can intercept and augment the skill execution pipeline.
/// </summary>
public interface ISkillMiddleware
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="request">The current skill request.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="SkillResult"/>.</returns>
    Task<SkillResult> InvokeAsync(
        SkillRequest request,
        Func<SkillRequest, Task<SkillResult>> next,
        CancellationToken ct);
}
