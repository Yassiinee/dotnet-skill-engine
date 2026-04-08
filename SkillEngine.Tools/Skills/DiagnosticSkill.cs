using SkillEngine.Core;
using SkillEngine.Core.Abstractions;
using System.Text;

namespace SkillEngine.Tools.Skills;

/// <summary>
/// Explains .NET exception messages, stack traces, and common runtime errors
/// with actionable remediation steps.
/// </summary>
public sealed class DiagnosticSkill : ISkill
{
    public string Name => "explain-error";
    public string Description => "Explains a .NET exception or error message and provides actionable remediation steps.";

    public string InputSchema => """
    {
      "type": "object",
      "properties": {
        "error": {
          "type": "string",
          "description": "The exception type, message, or stack trace to explain"
        },
        "context": {
          "type": "string",
          "description": "Optional: what you were doing when the error occurred"
        }
      },
      "required": ["error"]
    }
    """;

    private static readonly Dictionary<string, DiagnosticEntry> _knownErrors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NullReferenceException"] = new(
            "NullReferenceException",
            "You are accessing a member on an object that is `null`.",
            [
                "Use the null-conditional operator: `myObj?.Property`",
                "Enable C# nullable reference types (`<Nullable>enable</Nullable>`) to catch this at compile time",
                "Use `ArgumentNullException.ThrowIfNull(param)` in method guards",
                "Check if DI is not resolving the service (check service registration)"
            ]),

        ["InvalidOperationException"] = new(
            "InvalidOperationException",
            "An operation was called in an invalid state for the current context.",
            [
                "Check if you are calling an async method synchronously (`.Result` / `.Wait()`)",
                "In ASP.NET Core: verify that services are not used outside their lifetime scope",
                "For LINQ: ensure the sequence is not empty before calling `.First()` — use `.FirstOrDefault()`"
            ]),

        ["DbUpdateException"] = new(
            "DbUpdateException",
            "Entity Framework Core failed to save changes to the database.",
            [
                "Check the inner exception for the specific database error (constraint violation, type mismatch, etc.)",
                "Ensure all required properties are set on the entity",
                "Verify that migrations are up-to-date: `dotnet ef database update`",
                "Check for circular references in the entity graph"
            ]),

        ["HttpRequestException"] = new(
            "HttpRequestException",
            "An HTTP request to an external service failed.",
            [
                "Verify the target URL is correct and reachable",
                "Check network connectivity and firewall rules",
                "Implement retry policies using `Polly`: `AddPolicyHandler(GetRetryPolicy())`",
                "Inspect the `StatusCode` property if the server responded with an error"
            ]),

        ["StackOverflowException"] = new(
            "StackOverflowException",
            "Infinite recursion has exhausted the call stack.",
            [
                "Find and break the recursive cycle — add a base case",
                "In EF Core: this often happens with circular navigation properties — use `.AsNoTracking()` or DTOs",
                "In JSON serialization: use `[JsonIgnore]` on the back-reference property"
            ]),

        ["TaskCanceledException"] = new(
            "TaskCanceledException",
            "An async operation was cancelled, often due to a request timeout or client disconnect.",
            [
                "This is usually normal — check if the client disconnected",
                "Increase timeout: `HttpClient.Timeout = TimeSpan.FromSeconds(30)`",
                "Pass `CancellationToken` through your call chain rather than ignoring it",
                "In ASP.NET Core: filter `OperationCanceledException` from your exception middleware"
            ]),

        ["ObjectDisposedException"] = new(
            "ObjectDisposedException",
            "You are using an object after it has been disposed.",
            [
                "Do not store `IServiceScope` or scoped services as fields in singletons",
                "Avoid using `HttpClient` directly — use `IHttpClientFactory`",
                "Check if a `using` block is disposing the object too early"
            ])
    };

    public Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default)
    {
        var error = request.GetParameter<string>("error");
        if (string.IsNullOrWhiteSpace(error))
            return Task.FromResult(SkillResult.Failure("Parameter 'error' is required.", "MISSING_PARAM"));

        var context = request.GetParameter<string>("context");
        var sb = new StringBuilder();

        // Try to match a known error
        var entry = _knownErrors.FirstOrDefault(kv =>
            error.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;

        if (entry is not null)
        {
            sb.AppendLine($"## 🔴 {entry.Name}");
            sb.AppendLine();
            sb.AppendLine($"**What it means:** {entry.Explanation}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(context))
                sb.AppendLine($"**Your context:** {context}");

            sb.AppendLine();
            sb.AppendLine("### 🛠️ Remediation Steps");
            foreach (var (step, idx) in entry.Steps.Select((s, i) => (s, i + 1)))
                sb.AppendLine($"{idx}. {step}");
        }
        else
        {
            sb.AppendLine("## 🔍 Error Analysis");
            sb.AppendLine();
            sb.AppendLine($"**Error:** `{error}`");

            if (!string.IsNullOrWhiteSpace(context))
                sb.AppendLine($"**Context:** {context}");

            sb.AppendLine();
            sb.AppendLine("### 🛠️ General Troubleshooting");
            sb.AppendLine("1. Read the **inner exception** — it usually contains the real cause");
            sb.AppendLine("2. Search the error on **docs.microsoft.com** or GitHub Issues");
            sb.AppendLine("3. Enable detailed logging: `builder.Logging.SetMinimumLevel(LogLevel.Debug)`");
            sb.AppendLine("4. Attach a debugger and set breakpoints at the throw site");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("_Diagnosed by .NET MCP Skill Engine_");

        return Task.FromResult(SkillResult.Success(sb.ToString(), new { matched_pattern = entry?.Name }));
    }

    private sealed record DiagnosticEntry(string Name, string Explanation, string[] Steps);
}
