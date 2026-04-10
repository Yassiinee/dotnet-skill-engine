using Microsoft.Extensions.Logging;
using SkillEngine.Core.Abstractions;
using SkillEngine.Core.Models;
using SkillEngine.Core.Rules;

namespace SkillEngine.Core.Engine.Middlewares;

/// <summary>
/// Middleware that evaluates the rule engine before proceeding to the next step.
/// </summary>
public sealed class ValidationMiddleware : ISkillMiddleware
{
    private readonly RuleEngine _ruleEngine;
    private readonly IContextBuilder _contextBuilder;
    private readonly ILogger<ValidationMiddleware> _logger;

    public ValidationMiddleware(
        RuleEngine ruleEngine,
        IContextBuilder contextBuilder,
        ILogger<ValidationMiddleware> logger)
    {
        _ruleEngine = ruleEngine;
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    public async Task<SkillResult> InvokeAsync(
        SkillRequest request,
        Func<SkillRequest, Task<SkillResult>> next,
        CancellationToken ct)
    {
        _logger.LogDebug("[Validation] Building context for tool '{Tool}'", request.ToolName);
        
        // Build enriched context
        SkillContext context = await _contextBuilder.BuildAsync(request, ct);

        // Validate rules
        RuleResult ruleResult = _ruleEngine.Evaluate(context);
        if (!ruleResult.IsAllowed)
        {
            _logger.LogWarning("[Validation] Rule engine blocked request: {Reason}", ruleResult.Reason);
            return SkillResult.Failure(ruleResult.Reason ?? "Request blocked by policy.", "RULE_VIOLATION");
        }

        return await next(request);
    }
}
