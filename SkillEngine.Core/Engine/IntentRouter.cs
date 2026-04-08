using SkillEngine.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace SkillEngine.Core.Engine;

/// <summary>
/// Routes tool names (and simple natural-language intents) to registered <see cref="ISkill"/> implementations.
/// Registration is done at startup; routing is O(1) by exact name, with fuzzy fallback.
/// </summary>
public sealed class IntentRouter : ISkillRouter
{
    private readonly Dictionary<string, ISkill> _registry;
    private readonly ILogger<IntentRouter> _logger;

    public IntentRouter(IEnumerable<ISkill> skills, ILogger<IntentRouter> logger)
    {
        _logger = logger;
        _registry = skills.ToDictionary(
            s => s.Name,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("[Router] Registered {Count} skill(s): {Names}",
            _registry.Count, string.Join(", ", _registry.Keys));
    }

    /// <inheritdoc/>
    public ISkill? Route(string intent)
    {
        // 1. Exact match (case-insensitive)
        if (_registry.TryGetValue(intent, out var skill))
        {
            _logger.LogDebug("[Router] Exact match: '{Intent}' → '{Skill}'", intent, skill.Name);
            return skill;
        }

        // 2. Partial / keyword match (for natural-language intents)
        var normalized = intent.ToLowerInvariant();
        var match = _registry.Values.FirstOrDefault(s =>
            normalized.Contains(s.Name.ToLowerInvariant()));

        if (match is not null)
        {
            _logger.LogDebug("[Router] Fuzzy match: '{Intent}' → '{Skill}'", intent, match.Name);
            return match;
        }

        _logger.LogWarning("[Router] No match found for intent: '{Intent}'", intent);
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ISkill> GetAll() => _registry.Values.ToList().AsReadOnly();
}
