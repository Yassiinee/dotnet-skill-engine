using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillEngine.Core;
using SkillEngine.Core.Abstractions;
using SkillEngine.Core.Engine;
using SkillEngine.Core.Models;
using SkillEngine.Tools.Skills;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── Banner ────────────────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("""
  ____  _   _ _____ _____   __  __  ____ ____
 |  _ \| \ | | ____|_   _| |  \/  |/ ___|  _ \
 | | | |  \| |  _|   | |   | |\/| | |   | |_) |
 | |_| | |\  | |___  | |   | |  | | |___|  __/
 |____/|_| \_|_____| |_|   |_|  |_|\____|_|
          Skill Engine — Console Demo
""");
Console.ResetColor();

// ── Dependency Injection ──────────────────────────────────────────────────────
ServiceCollection services = new();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddSkillEngine(o => o.EnableVerboseLogging = false)
        .AddSkill<ScaffoldSkill>()
        .AddSkill<CodeReviewSkill>()
        .AddSkill<DiagnosticSkill>()
        .AddSkill<ArchitectureAdvisorSkill>();

ServiceProvider provider = services.BuildServiceProvider();
SkillOrchestrator orchestrator = provider.GetRequiredService<SkillOrchestrator>();
ISkillRouter router = provider.GetRequiredService<ISkillRouter>();

// ── REPL Loop ─────────────────────────────────────────────────────────────────
PrintHelp(router);

while (true)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("skill> ");
    Console.ResetColor();

    string? input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input)) continue;
    if (input is "exit" or "quit") break;
    if (input is "help" or "?") { PrintHelp(router); continue; }

    string[] parts = input.Split(' ', 2);
    string toolName = parts[0].ToLowerInvariant();
    string argsRaw = parts.Length > 1 ? parts[1] : string.Empty;

    // Parse simple key=value pairs after the tool name
    Dictionary<string, object?> parameters = ParseArgs(argsRaw);

    SkillRequest request = new()
    {
        ToolName = toolName,
        Parameters = parameters,
        AgentMetadata = new() { ["model"] = "console-demo" }
    };

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($"▶ Invoking: {toolName}");
    Console.ResetColor();

    SkillResult result = await orchestrator.InvokeAsync(request);

    Console.WriteLine();
    if (result.IsSuccess)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(result.Content);
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ✓ Completed in {result.Duration.TotalMilliseconds:F0}ms");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ Error [{result.ErrorCode}]: {result.ErrorMessage}");
        Console.ResetColor();
    }
}

Console.WriteLine("\nGoodbye! 👋");

// ── Helpers ───────────────────────────────────────────────────────────────────

static void PrintHelp(ISkillRouter router)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Available Skills:");
    Console.ResetColor();
    foreach (ISkill skill in router.GetAll())
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"  {skill.Name,-25}");
        Console.ResetColor();
        Console.WriteLine(skill.Description);
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("Usage: <tool-name> [param=value param2=value2 ...]");
    Console.WriteLine("Examples:");
    Console.WriteLine("  scaffold entity=Product pattern=cqrs namespace=MyShop");
    Console.WriteLine("  review-code code=\"var x = null;\" focus=security");
    Console.WriteLine("  explain-error error=NullReferenceException");
    Console.WriteLine("  advise-architecture question=\"What pattern should I use?\" pattern=compare");
    Console.WriteLine("  help   — Show this help");
    Console.WriteLine("  exit   — Quit");
    Console.ResetColor();
}

static Dictionary<string, object?> ParseArgs(string raw)
{
    Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);
    if (string.IsNullOrWhiteSpace(raw)) return result;

    // Support: key=value pairs, values with spaces must use quotes: key="some value"
    System.Text.RegularExpressions.MatchCollection matches =
        System.Text.RegularExpressions.Regex.Matches(raw, @"(\w[\w\-]*)=""([^""]*)""|(\w[\w\-]*)=(\S+)");

    foreach (System.Text.RegularExpressions.Match m in matches)
    {
        if (m.Groups[1].Success)
            result[m.Groups[1].Value] = m.Groups[2].Value;
        else
            result[m.Groups[3].Value] = m.Groups[4].Value;
    }

    return result;
}
