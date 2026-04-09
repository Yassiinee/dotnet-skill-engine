using System.Text;
using SkillEngine.Core.Models;

namespace SkillEngine.Tools.Skills;

/// <summary>
/// Advises on .NET architecture patterns: Clean Architecture, CQRS, DDD, Vertical Slices, etc.
/// Returns structured guidance, pros/cons, and code structure recommendations.
/// </summary>
public sealed class ArchitectureAdvisorSkill : ISkill
{
    public string Name => "advise-architecture";
    public string Description => "Advises on .NET architecture patterns (Clean Architecture, CQRS, DDD, Vertical Slices) for a given use-case or question.";

    public string InputSchema => """
    {
      "type": "object",
      "properties": {
        "question": {
          "type": "string",
          "description": "Your architecture question or description of the system you are building"
        },
        "pattern": {
          "type": "string",
          "enum": ["clean-architecture", "cqrs", "ddd", "vertical-slice", "microservices", "compare"],
          "description": "Specific pattern to explain, or 'compare' to compare all"
        },
        "team_size": {
          "type": "string",
          "enum": ["solo", "small", "medium", "large"],
          "description": "Team size to tailor complexity recommendations"
        }
      },
      "required": ["question"]
    }
    """;

    private static readonly Dictionary<string, ArchitecturePattern> _patterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["clean-architecture"] = new(
            "Clean Architecture",
            "Separates the system into concentric layers: Domain → Application → Infrastructure → Presentation. " +
            "Dependencies always point inward; the domain has zero external dependencies.",
            [
                "✅ Clear separation of concerns",
                "✅ Highly testable domain logic",
                "✅ Easy to swap infrastructure (DB, queue, etc.)",
                "✅ Works for medium-to-large teams",
            ],
            [
                "⚠️ Boilerplate-heavy for simple CRUD apps",
                "⚠️ Steep learning curve for small teams",
                "⚠️ Can lead to over-engineering for simple APIs",
            ],
            """
            MyApp/
            ├── Domain/          # Entities, Value Objects, Domain Events, Interfaces
            ├── Application/     # Use Cases, CQRS Handlers, DTOs, Ports
            ├── Infrastructure/  # EF Core, External APIs, File System, Messaging
            └── Presentation/    # ASP.NET Core Controllers or Minimal API
            """),

        ["cqrs"] = new(
            "CQRS (Command Query Responsibility Segregation)",
            "Separates read (Query) and write (Command) models. Commands mutate state; Queries return data. " +
            "Often combined with MediatR in .NET.",
            [
                "✅ Optimized read/write models independently",
                "✅ Clear intent in code (Commands vs Queries)",
                "✅ Scales independently",
                "✅ Works well with Event Sourcing",
            ],
            [
                "⚠️ More files and types to maintain",
                "⚠️ Eventual consistency can be complex",
                "⚠️ Overkill for simple CRUD",
            ],
            """
            Features/
            ├── Orders/
            │   ├── GetOrderQuery.cs          # Query
            │   ├── GetOrderQueryHandler.cs   # Query Handler
            │   ├── CreateOrderCommand.cs     # Command
            │   └── CreateOrderHandler.cs     # Command Handler
            """),

        ["ddd"] = new(
            "Domain-Driven Design (DDD)",
            "Models complex business domains using Ubiquitous Language, Aggregates, Entities, Value Objects, " +
            "Domain Events, and Bounded Contexts.",
            [
                "✅ Aligns code with business domain",
                "✅ Manages complexity in large systems",
                "✅ Encourages rich domain models",
                "✅ Natural fit for microservices",
            ],
            [
                "⚠️ Requires deep domain knowledge up front",
                "⚠️ Heavy investment — wrong for simple apps",
                "⚠️ Needs experienced team to apply well",
            ],
            """
            Domain/
            ├── Orders/              # Bounded Context: Orders
            │   ├── Order.cs         # Aggregate Root (Entity)
            │   ├── OrderItem.cs     # Entity
            │   ├── Money.cs         # Value Object
            │   ├── OrderPlaced.cs   # Domain Event
            │   └── IOrderRepo.cs    # Repository Interface (port)
            """),

        ["vertical-slice"] = new(
            "Vertical Slice Architecture",
            "Organizes code by feature (slice) rather than by layer. Each feature folder contains " +
            "everything it needs: endpoint, handler, DB query, validation.",
            [
                "✅ Low coupling between features",
                "✅ Easy to find all code for a feature",
                "✅ Works well with MediatR + FastEndpoints",
                "✅ Good for growing teams",
            ],
            [
                "⚠️ Risk of code duplication across slices",
                "⚠️ Shared logic needs careful placement",
                "⚠️ Less familiar to traditional layered teams",
            ],
            """
            Features/
            ├── CreateOrder/
            │   ├── CreateOrderEndpoint.cs
            │   ├── CreateOrderCommand.cs
            │   ├── CreateOrderHandler.cs
            │   └── CreateOrderValidator.cs
            ├── GetOrder/
            │   ├── GetOrderEndpoint.cs
            │   └── GetOrderQuery.cs
            """),
    };

    public Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default)
    {
        string question = request.GetParameter<string>("question") ?? "";
        string? pattern = request.GetParameter<string>("pattern");
        string teamSize = request.GetParameter<string>("team_size") ?? "medium";

        StringBuilder sb = new();
        sb.AppendLine("## 🏛️ Architecture Advisory — .NET MCP Skill Engine");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(question))
        {
            sb.AppendLine($"**Your question:** {question}");
            sb.AppendLine();
        }

        if (pattern == "compare")
        {
            sb.AppendLine("### 📊 Pattern Comparison");
            sb.AppendLine();
            sb.AppendLine("| Pattern | Best For | Complexity | Team Size |");
            sb.AppendLine("|---------|----------|------------|-----------|");
            sb.AppendLine("| Clean Architecture | Enterprise, DDD systems | Medium–High | Medium–Large |");
            sb.AppendLine("| CQRS | Read-heavy, event-driven | Medium | Small–Large |");
            sb.AppendLine("| DDD | Complex business domains | High | Large |");
            sb.AppendLine("| Vertical Slice | Feature-focused teams | Low–Medium | Any |");
            sb.AppendLine("| Microservices | Scalable distributed systems | Very High | Large |");
            sb.AppendLine();
        }
        else if (pattern is not null && _patterns.TryGetValue(pattern, out ArchitecturePattern? entry))
        {
            sb.AppendLine($"## {entry.Name}");
            sb.AppendLine();
            sb.AppendLine(entry.Description);
            sb.AppendLine();

            sb.AppendLine("### ✅ Pros");
            foreach (string pro in entry.Pros) sb.AppendLine($"- {pro}");
            sb.AppendLine();

            sb.AppendLine("### ⚠️ Cons");
            foreach (string con in entry.Cons) sb.AppendLine($"- {con}");
            sb.AppendLine();

            sb.AppendLine("### 📂 Recommended Structure");
            sb.AppendLine("```");
            sb.AppendLine(entry.Structure.Trim());
            sb.AppendLine("```");
        }
        else
        {
            // Auto-recommend based on team size
            sb.AppendLine("### 💡 Recommendation");
            string recommendation = teamSize switch
            {
                "solo" => "**Vertical Slice Architecture** — minimal boilerplate, feature-focused, easy to navigate alone.",
                "small" => "**Clean Architecture** (simplified) — provides solid separation without overwhelming a small team.",
                "medium" => "**Clean Architecture + CQRS** — scales well, clear intent, great testability.",
                "large" => "**DDD + CQRS + Microservices** — essential for managing complexity at scale.",
                _ => "**Clean Architecture** — the safe default for most .NET teams."
            };
            sb.AppendLine(recommendation);
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("_Advised by .NET MCP Skill Engine | Ask me about: clean-architecture, cqrs, ddd, vertical-slice, compare_");

        return Task.FromResult(SkillResult.Success(sb.ToString(), new { pattern, team_size = teamSize }));
    }

    private sealed record ArchitecturePattern(
        string Name, string Description,
        string[] Pros, string[] Cons,
        string Structure);
}
