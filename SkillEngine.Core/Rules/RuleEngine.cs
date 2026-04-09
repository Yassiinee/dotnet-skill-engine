using SkillEngine.Core.Models;

namespace SkillEngine.Core.Rules;

/// <summary>
/// Lightweight rule engine that evaluates a chain of <see cref="ISkillRule"/> predicates
/// against a <see cref="SkillContext"/> before skill execution.
/// All rules must pass for a request to proceed.
/// </summary>
public sealed class RuleEngine
{
    private readonly IReadOnlyList<ISkillRule> _rules;

    public RuleEngine(IEnumerable<ISkillRule> rules)
    {
        _rules = rules.OrderBy(r => r.Priority).ToList().AsReadOnly();
    }

    /// <summary>
    /// Evaluates all registered rules against the given context.
    /// Returns the first violation encountered, or an allowed result.
    /// </summary>
    public RuleResult Evaluate(SkillContext context)
    {
        foreach (ISkillRule rule in _rules)
        {
            RuleResult result = rule.Evaluate(context);
            if (!result.IsAllowed)
                return result;
        }
        return RuleResult.Allow();
    }
}

/// <summary>Defines a single rule predicate applied before skill execution.</summary>
public interface ISkillRule
{
    /// <summary>Lower priority = evaluated first.</summary>
    int Priority { get; }

    /// <summary>Human-readable rule name for diagnostics.</summary>
    string Name { get; }

    RuleResult Evaluate(SkillContext context);
}

/// <summary>Result of a rule evaluation.</summary>
public sealed class RuleResult
{
    public bool IsAllowed { get; private init; }
    public string? Reason { get; private init; }
    public string? ViolatedRule { get; private init; }

    public static RuleResult Allow()
    {
        return new() { IsAllowed = true };
    }

    public static RuleResult Deny(string reason, string ruleName)
    {
        return new()
        {
            IsAllowed = false,
            Reason = reason,
            ViolatedRule = ruleName
        };
    }
}

// ── Built-in Rules ────────────────────────────────────────────────────────────

/// <summary>Blocks execution if the tool name is in a configured deny list.</summary>
public sealed class DenyListRule : ISkillRule
{
    private readonly HashSet<string> _deniedTools;

    public int Priority => 10;
    public string Name => "DenyList";

    public DenyListRule(IEnumerable<string> deniedTools)
    {
        _deniedTools = new HashSet<string>(deniedTools, StringComparer.OrdinalIgnoreCase);
    }

    public RuleResult Evaluate(SkillContext context)
    {
        return _deniedTools.Contains(context.Request.ToolName)
            ? RuleResult.Deny($"Tool '{context.Request.ToolName}' is on the deny list.", Name)
            : RuleResult.Allow();
    }
}

/// <summary>Enforces a maximum parameter count to prevent prompt injection via oversized payloads.</summary>
public sealed class MaxParameterCountRule : ISkillRule
{
    private readonly int _maxCount;

    public int Priority => 20;
    public string Name => "MaxParameterCount";

    public MaxParameterCountRule(int maxCount = 20)
    {
        _maxCount = maxCount;
    }

    public RuleResult Evaluate(SkillContext context)
    {
        return context.Request.Parameters.Count > _maxCount
            ? RuleResult.Deny($"Request exceeds maximum of {_maxCount} parameters.", Name)
            : RuleResult.Allow();
    }
}
