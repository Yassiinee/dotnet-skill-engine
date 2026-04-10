using System.Diagnostics;

namespace SkillEngine.Core.Engine;

/// <summary>
/// Centralized telemetry for the Skill Engine.
/// </summary>
public static class SkillTelemetry
{
    /// <summary>
    /// The <see cref="ActivitySource"/> for all Skill Engine traces.
    /// </summary>
    public static ActivitySource Source { get; } = new("SkillEngine.Core");
}
