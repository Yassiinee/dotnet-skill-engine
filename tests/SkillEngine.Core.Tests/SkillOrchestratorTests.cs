using Microsoft.Extensions.Logging.Abstractions;
using SkillEngine.Core.Abstractions;
using SkillEngine.Core.Engine;
using SkillEngine.Core.Models;
using SkillEngine.Core.Rules;
using Xunit;

namespace SkillEngine.Core.Tests;

public class SkillOrchestratorTests
{
    private static SkillOrchestrator BuildOrchestrator(params ISkill[] skills)
    {
        ISkillRouter router = new IntentRouter(skills, NullLogger<IntentRouter>.Instance);
        IContextBuilder contextBuilder = new DefaultContextBuilder();
        RuleEngine ruleEngine = new(Array.Empty<ISkillRule>());
        return new SkillOrchestrator(router, contextBuilder, ruleEngine, NullLogger<SkillOrchestrator>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_UnknownTool_ReturnsNotFound()
    {
        // Arrange
        SkillOrchestrator orchestrator = BuildOrchestrator();
        SkillRequest request = new() { ToolName = "does-not-exist" };

        // Act
        SkillResult result = await orchestrator.InvokeAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TOOL_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task InvokeAsync_KnownTool_ReturnsSuccess()
    {
        // Arrange
        SkillOrchestrator orchestrator = BuildOrchestrator(new FakeSkill("ping", "pong"));
        SkillRequest request = new() { ToolName = "ping" };

        // Act
        SkillResult result = await orchestrator.InvokeAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("pong", result.Content);
    }

    [Fact]
    public async Task InvokeAsync_DenyListRule_BlocksRequest()
    {
        // Arrange
        ISkillRouter router = new IntentRouter([new FakeSkill("blocked-tool", "data")], NullLogger<IntentRouter>.Instance);
        IContextBuilder ctx = new DefaultContextBuilder();
        RuleEngine rules = new([new DenyListRule(["blocked-tool"])]);
        SkillOrchestrator orchestrator = new(router, ctx, rules, NullLogger<SkillOrchestrator>.Instance);

        SkillRequest request = new() { ToolName = "blocked-tool" };

        // Act
        SkillResult result = await orchestrator.InvokeAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("RULE_VIOLATION", result.ErrorCode);
    }

    [Fact]
    public async Task InvokeAsync_CancellationRequested_ReturnsCancelled()
    {
        // Arrange
        SkillOrchestrator orchestrator = BuildOrchestrator(new SlowSkill());
        SkillRequest request = new() { ToolName = "slow" };
        using CancellationTokenSource cts = new(millisecondsDelay: 50);

        // Act
        SkillResult result = await orchestrator.InvokeAsync(request, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CANCELLED", result.ErrorCode);
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeSkill(string name, string response) : ISkill
    {
        public string Name => name;
        public string Description => $"Fake skill: {name}";
        public string InputSchema => "{}";

        public Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SkillResult.Success(response));
        }
    }

    private sealed class SlowSkill : ISkill
    {
        public string Name => "slow";
        public string Description => "A skill that never finishes";
        public string InputSchema => "{}";

        public async Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return SkillResult.Success("done");
        }
    }
}
