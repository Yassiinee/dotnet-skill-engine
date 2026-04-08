using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SkillEngine.Core.Engine;

/// <summary>
/// Central orchestrator that coordinates routing, context building, rule validation,
/// and skill execution. This is the primary entry point for the Skill Engine.
/// </summary>
public sealed class SkillOrchestrator
{
    private readonly ISkillRouter _router;
    private readonly IContextBuilder _contextBuilder;
    private readonly RuleEngine _ruleEngine;
    private readonly ILogger<SkillOrchestrator> _logger;

    public SkillOrchestrator(
        ISkillRouter router,
        IContextBuilder contextBuilder,
        RuleEngine ruleEngine,
        ILogger<SkillOrchestrator> logger)
    {
        _router = router;
        _contextBuilder = contextBuilder;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    /// <summary>
    /// Resolves, validates, and executes the appropriate skill for a given request.
    /// </summary>
    public async Task<SkillResult> InvokeAsync(SkillRequest request, CancellationToken cancellationToken = default)
    {
        Stopwatch sw = Stopwatch.StartNew();

        _logger.LogInformation("[Orchestrator] Invoking tool '{Tool}' | Session: {Session}",
            request.ToolName, request.SessionId ?? "none");

        try
        {
            // 1. Route to skill
            ISkill? skill = _router.Route(request.ToolName);
            if (skill is null)
            {
                _logger.LogWarning("[Orchestrator] No skill found for tool '{Tool}'", request.ToolName);
                return SkillResult.NotFound(request.ToolName);
            }

            // 2. Build enriched context
            SkillContext context = await _contextBuilder.BuildAsync(request, cancellationToken);

            // 3. Validate rules before execution
            RuleResult ruleResult = _ruleEngine.Evaluate(context);
            if (!ruleResult.IsAllowed)
            {
                _logger.LogWarning("[Orchestrator] Rule engine blocked request: {Reason}", ruleResult.Reason);
                return SkillResult.Failure(ruleResult.Reason ?? "Request blocked by policy.", "RULE_VIOLATION");
            }

            // 4. Execute skill
            _logger.LogDebug("[Orchestrator] Executing skill '{Skill}'", skill.Name);
            SkillResult result = await skill.ExecuteAsync(request, cancellationToken);

            sw.Stop();
            _logger.LogInformation("[Orchestrator] Skill '{Skill}' completed in {Ms}ms | Success: {Ok}",
                skill.Name, sw.ElapsedMilliseconds, result.IsSuccess);

            return result with { Duration = sw.Elapsed };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[Orchestrator] Tool '{Tool}' execution was cancelled.", request.ToolName);
            return SkillResult.Failure("Execution cancelled.", "CANCELLED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Orchestrator] Unhandled exception executing tool '{Tool}'", request.ToolName);
            return SkillResult.Failure($"Internal error: {ex.Message}", "INTERNAL_ERROR");
        }
    }
}
