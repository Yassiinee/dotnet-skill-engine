using Microsoft.Extensions.DependencyInjection;

namespace SkillEngine.Core;

/// <summary>
/// Extension methods for registering Skill Engine services with the .NET DI container.
/// </summary>
public static class SkillEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Skill Engine: orchestrator, router, context builder, rule engine, and prompt renderer.
    /// Call <c>AddSkill&lt;T&gt;()</c> afterwards to register individual skills.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddSkillEngine()
    ///         .AddSkill&lt;ScaffoldSkill&gt;()
    ///         .AddSkill&lt;CodeReviewSkill&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSkillEngine(this IServiceCollection services,
        Action<SkillEngineOptions>? configure = null)
    {
        SkillEngineOptions options = new();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<PromptRenderer>();
        services.AddSingleton<IContextBuilder, DefaultContextBuilder>();
        services.AddSingleton<ISkillRouter>(sp =>
        {
            var skills = sp.GetServices<ISkill>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IntentRouter>>();
            return new IntentRouter(skills, logger);
        });
        services.AddSingleton<RuleEngine>(sp =>
        {
            var rules = sp.GetServices<ISkillRule>();
            return new RuleEngine(rules);
        });
        services.AddSingleton<SkillOrchestrator>();

        // Register default rules
        services.AddSingleton<ISkillRule>(new MaxParameterCountRule(options.MaxParameters));
        if (options.DeniedTools.Count > 0)
            services.AddSingleton<ISkillRule>(new DenyListRule(options.DeniedTools));

        return services;
    }

    /// <summary>
    /// Registers a skill implementation. The skill is registered both as <see cref="ISkill"/>
    /// and as its concrete type for direct resolution.
    /// </summary>
    public static IServiceCollection AddSkill<TSkill>(this IServiceCollection services)
        where TSkill : class, ISkill
    {
        services.AddSingleton<TSkill>();
        services.AddSingleton<ISkill>(sp => sp.GetRequiredService<TSkill>());
        return services;
    }
}

/// <summary>Configuration options for the Skill Engine.</summary>
public sealed class SkillEngineOptions
{
    /// <summary>Maximum parameters allowed per request (default: 20).</summary>
    public int MaxParameters { get; set; } = 20;

    /// <summary>Tools that should be blocked by the deny-list rule.</summary>
    public List<string> DeniedTools { get; set; } = [];

    /// <summary>Whether to enable detailed request/response logging (default: false in prod).</summary>
    public bool EnableVerboseLogging { get; set; } = false;
}
