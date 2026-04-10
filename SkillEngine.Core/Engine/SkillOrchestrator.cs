using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SkillEngine.Core.Models;

namespace SkillEngine.Core.Engine;

/// <summary>
/// Central orchestrator that coordinates routing, context building, rule validation,
/// and skill execution. This is the primary entry point for the Skill Engine.
/// </summary>
public sealed partial class SkillOrchestrator
{
    private readonly ISkillRouter _router;
    private readonly IEnumerable<ISkillMiddleware> _middlewares;
    private readonly ILogger<SkillOrchestrator> _logger;

    public SkillOrchestrator(
        ISkillRouter router,
        IEnumerable<ISkillMiddleware> middlewares,
        ILogger<SkillOrchestrator> logger)
    {
        _router = router;
        _middlewares = middlewares;
        _logger = logger;
    }

    /// <summary>
    /// Resolves, validates, and executes the appropriate skill for a given request.
    /// </summary>
    public async Task<SkillResult> InvokeAsync(SkillRequest request, CancellationToken cancellationToken = default)
    {
        using Activity? activity = SkillTelemetry.Source.StartActivity($"Invoke:{request.ToolName}");
        activity?.SetTag("skill.tool_name", request.ToolName);
        activity?.SetTag("skill.session_id", request.SessionId);

        LogInvocation(request.ToolName, request.SessionId ?? "none");

        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            // 1. Route to skill
            ISkill? skill = _router.Route(request.ToolName);
            if (skill is null)
            {
                LogSkillNotFound(request.ToolName);
                return SkillResult.NotFound(request.ToolName);
            }

            // 2. Build the execution pipeline
            // We chain middlewares followed by the final skill execution
            Func<SkillRequest, Task<SkillResult>> pipeline = async (req) =>
            {
                return await skill.ExecuteAsync(req, cancellationToken);
            };

            // Wrap pipeline with middlewares in reverse order
            foreach (var middleware in _middlewares.Reverse())
            {
                var next = pipeline;
                pipeline = (req) => middleware.InvokeAsync(req, next, cancellationToken);
            }

            // 3. Execute the pipeline
            SkillResult result = await pipeline(request);

            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LogSkillCompleted(skill.Name, elapsed.TotalMilliseconds, result.IsSuccess);

            if (result.IsSuccess)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
                activity?.SetTag("error", result.ErrorCode);
            }

            return result with { Duration = elapsed };
        }
        catch (OperationCanceledException)
        {
            LogExecutionCancelled(request.ToolName);
            activity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
            return SkillResult.Failure("Execution cancelled.", "CANCELLED");
        }
        catch (Exception ex)
        {
            LogUnhandledException(ex, request.ToolName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.message", ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            return SkillResult.Failure($"Internal error: {ex.Message}", "INTERNAL_ERROR");
        }
    }
}
