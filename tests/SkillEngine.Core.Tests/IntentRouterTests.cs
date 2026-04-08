using Microsoft.Extensions.Logging.Abstractions;
using SkillEngine.Core;
using SkillEngine.Core.Engine;
using Xunit;

namespace SkillEngine.Core.Tests;

public class IntentRouterTests
{
    private static IntentRouter BuildRouter(params ISkill[] skills)
        => new(skills, NullLogger<IntentRouter>.Instance);

    [Fact]
    public void Route_ExactMatch_ReturnsCorrectSkill()
    {
        // Arrange
        IntentRouter router = BuildRouter(new NamedSkill("scaffold"), new NamedSkill("review-code"));

        // Act
        ISkill? skill = router.Route("scaffold");

        // Assert
        Assert.NotNull(skill);
        Assert.Equal("scaffold", skill.Name);
    }

    [Fact]
    public void Route_CaseInsensitive_ReturnsSkill()
    {
        // Arrange
        IntentRouter router = BuildRouter(new NamedSkill("Scaffold"));

        // Act
        ISkill? skill = router.Route("SCAFFOLD");

        // Assert
        Assert.NotNull(skill);
    }

    [Fact]
    public void Route_FuzzyMatch_ReturnsSkill()
    {
        // Arrange
        IntentRouter router = BuildRouter(new NamedSkill("scaffold"));

        // Act
        ISkill? skill = router.Route("please scaffold my entity for me");

        // Assert
        Assert.NotNull(skill);
        Assert.Equal("scaffold", skill.Name);
    }

    [Fact]
    public void Route_NoMatch_ReturnsNull()
    {
        // Arrange
        IntentRouter router = BuildRouter(new NamedSkill("scaffold"));

        // Act
        ISkill? skill = router.Route("something-totally-unknown");

        // Assert
        Assert.Null(skill);
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredSkills()
    {
        // Arrange
        IntentRouter router = BuildRouter(new NamedSkill("a"), new NamedSkill("b"), new NamedSkill("c"));

        // Act
        IReadOnlyList<ISkill> all = router.GetAll();

        // Assert
        Assert.Equal(3, all.Count);
    }

    // ── Fake ──────────────────────────────────────────────────────────────────

    private sealed class NamedSkill(string name) : ISkill
    {
        public string Name => name;
        public string Description => $"Skill: {name}";
        public string InputSchema => "{}";
        public Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(SkillResult.Success("ok"));
    }
}
