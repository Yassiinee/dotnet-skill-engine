using System.Text;
using SkillEngine.Core.Models;

namespace SkillEngine.Tools.Skills;

/// <summary>
/// Reviews C# code snippets and returns structured feedback on quality,
/// patterns, security, performance, and .NET best practices.
/// </summary>
public sealed class CodeReviewSkill : ISkill
{
    public string Name => "review-code";
    public string Description => "Reviews a C# code snippet and returns structured feedback on quality, patterns, security issues, and performance concerns.";

    public string InputSchema => """
    {
      "type": "object",
      "properties": {
        "code": {
          "type": "string",
          "description": "The C# code snippet to review"
        },
        "focus": {
          "type": "string",
          "enum": ["all", "security", "performance", "architecture", "style"],
          "description": "Area of focus for the review"
        },
        "dotnet_version": {
          "type": "string",
          "description": "Target .NET version (e.g., 'net8.0')"
        }
      },
      "required": ["code"]
    }
    """;

    public Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default)
    {
        string? code = request.GetParameter<string>("code");
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult(SkillResult.Failure("Parameter 'code' is required and cannot be empty.", "MISSING_PARAM"));

        string focus = request.GetParameter<string>("focus") ?? "all";
        string dotnetVersion = request.GetParameter<string>("dotnet_version") ?? "net8.0";

        List<ReviewFinding> findings = new();

        // ── Security Checks ───────────────────────────────────────────────────
        if (focus is "all" or "security")
        {
            if (code.Contains("string.Format") && code.Contains("sql", StringComparison.OrdinalIgnoreCase))
                findings.Add(new("🔴 SECURITY", "SQL injection risk: avoid string interpolation in queries. Use parameterized queries or EF Core.", "security"));

            if (code.Contains("Environment.GetEnvironmentVariable") && code.Contains("Password", StringComparison.OrdinalIgnoreCase))
                findings.Add(new("🟡 SECURITY", "Avoid storing passwords in environment variables. Use Azure Key Vault or ASP.NET Core Secrets.", "security"));

            if (code.Contains(".Result") || code.Contains(".GetAwaiter().GetResult()"))
                findings.Add(new("🟡 SECURITY", "Synchronous .Result or .GetResult() can cause deadlocks in ASP.NET Core. Use async/await throughout.", "security"));
        }

        // ── Performance Checks ────────────────────────────────────────────────
        if (focus is "all" or "performance")
        {
            if (code.Contains("+ \"") || code.Contains("\" +"))
                findings.Add(new("🟡 PERFORMANCE", "String concatenation in loops degrades performance. Use StringBuilder or string interpolation.", "performance"));

            if (code.Contains("Task.Run") && code.Contains("await"))
                findings.Add(new("🟡 PERFORMANCE", "Avoid Task.Run wrapping async methods — this wastes thread pool threads. Use await directly.", "performance"));

            if (code.Contains("ToList()") && code.Contains("Where("))
                findings.Add(new("💡 PERFORMANCE", "Consider using IAsyncEnumerable or deferred LINQ execution instead of materializing before filtering.", "performance"));
        }

        // ── Architecture Checks ───────────────────────────────────────────────
        if (focus is "all" or "architecture")
        {
            if (code.Contains("new ") && code.Contains("=") && !code.Contains("using "))
                findings.Add(new("💡 ARCHITECTURE", "Consider injecting dependencies via constructor instead of using 'new'. This improves testability.", "architecture"));

            if (code.Contains("catch (Exception)") && !code.Contains("when ("))
                findings.Add(new("🟡 ARCHITECTURE", "Catching base Exception hides errors. Catch specific exceptions or use 'when' filters.", "architecture"));

            if (code.Contains("static ") && code.Contains("class "))
                findings.Add(new("💡 ARCHITECTURE", "Static classes limit testability and DI. Prefer interface-based services unless intentionally utility code.", "architecture"));
        }

        // ── Style / Naming Checks ─────────────────────────────────────────────
        if (focus is "all" or "style")
        {
            if (code.Contains("var ") && code.Contains("= new"))
                findings.Add(new("✅ STYLE", "Good use of 'var' with 'new' — this is idiomatic modern C#.", "style"));

            if (!code.Contains("///"))
                findings.Add(new("💡 STYLE", "No XML doc comments found. Add <summary> tags to public types and members for IntelliSense and documentation.", "style"));
        }

        // ── Build Report ──────────────────────────────────────────────────────
        StringBuilder sb = new();
        sb.AppendLine($"## 🔍 Code Review — Focus: {focus.ToUpper()} | Target: {dotnetVersion}");
        sb.AppendLine();

        if (findings.Count == 0)
        {
            sb.AppendLine("✅ **No issues found.** The code looks clean for the selected focus area.");
        }
        else
        {
            sb.AppendLine($"Found **{findings.Count}** finding(s):");
            sb.AppendLine();
            foreach (ReviewFinding f in findings)
            {
                sb.AppendLine($"### {f.Severity}");
                sb.AppendLine(f.Message);
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine($"_Reviewed by .NET MCP Skill Engine | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC_");

        return Task.FromResult(SkillResult.Success(sb.ToString(), new
        {
            findings_count = findings.Count,
            focus,
            by_category = findings.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.Count())
        }));
    }

    private sealed record ReviewFinding(string Severity, string Message, string Category);
}
