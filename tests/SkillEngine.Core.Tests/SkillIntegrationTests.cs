using Microsoft.Extensions.Logging.Abstractions;
using SkillEngine.Core.Abstractions;
using SkillEngine.Core.Engine;
using SkillEngine.Core.Models;
using SkillEngine.Core.Prompts;
using SkillEngine.Core.Rules;
using SkillEngine.Tools.Skills;
using Xunit;

namespace SkillEngine.Core.Tests;

/// <summary>
/// Integration-style tests verifying the full pipeline:
/// Scaffold → Review → Diagnose → Advise
/// </summary>
public class SkillIntegrationTests
{
    private readonly SkillOrchestrator _orchestrator;

    public SkillIntegrationTests()
    {
        PromptRenderer renderer = new();
        ISkill[] skills =
        [
            new ScaffoldSkill(renderer),
            new CodeReviewSkill(),
            new DiagnosticSkill(),
            new ArchitectureAdvisorSkill()
        ];

        ISkillRouter router = new IntentRouter(skills, NullLogger<IntentRouter>.Instance);
        IContextBuilder ctx = new DefaultContextBuilder();
        RuleEngine rules = new([new MaxParameterCountRule(20)]);
        _orchestrator = new SkillOrchestrator(router, ctx, rules, NullLogger<SkillOrchestrator>.Instance);
    }

    [Theory]
    [InlineData("mvc")]
    [InlineData("cqrs")]
    [InlineData("minimal-api")]
    [InlineData("repository")]
    public async Task ScaffoldSkill_AllPatterns_ReturnSuccessWithCode(string pattern)
    {
        SkillRequest request = new()
        {
            ToolName = "scaffold",
            Parameters = new()
            {
                ["entity"] = "Product",
                ["pattern"] = pattern,
                ["namespace"] = "MyShop"
            }
        };

        SkillResult result = await _orchestrator.InvokeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Content);
        Assert.Contains("Product", result.Content);
    }

    [Fact]
    public async Task ScaffoldSkill_UnknownPattern_ReturnsFailure()
    {
        SkillRequest request = new()
        {
            ToolName = "scaffold",
            Parameters = new() { ["entity"] = "X", ["pattern"] = "hexagonal" }
        };

        SkillResult result = await _orchestrator.InvokeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_PARAM", result.ErrorCode);
    }

    [Fact]
    public async Task CodeReviewSkill_DetectsSecurityIssue()
    {
        SkillRequest request = new()
        {
            ToolName = "review-code",
            Parameters = new()
            {
                ["code"] = "var query = string.Format(\"SELECT * FROM users WHERE id = {0}\", id); // sql query",
                ["focus"] = "security"
            }
        };

        SkillResult result = await _orchestrator.InvokeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Contains("SQL injection", result.Content);
    }

    [Fact]
    public async Task DiagnosticSkill_KnownException_ReturnsExplanation()
    {
        SkillRequest request = new()
        {
            ToolName = "explain-error",
            Parameters = new() { ["error"] = "NullReferenceException: Object reference not set" }
        };

        SkillResult result = await _orchestrator.InvokeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Contains("null", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArchitectureAdvisorSkill_Compare_ReturnsTable()
    {
        SkillRequest request = new()
        {
            ToolName = "advise-architecture",
            Parameters = new()
            {
                ["question"] = "Which pattern should I use?",
                ["pattern"] = "compare"
            }
        };

        SkillResult result = await _orchestrator.InvokeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Contains("CQRS", result.Content);
        Assert.Contains("Clean Architecture", result.Content);
    }
}
