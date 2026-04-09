using System.Text;
using SkillEngine.Core.Models;

namespace SkillEngine.Tools.Skills;

/// <summary>
/// Generates .NET scaffold code (controllers, services, repositories, DTOs)
/// based on a provided entity name and architecture pattern.
/// </summary>
public sealed class ScaffoldSkill : ISkill
{
    public string Name => "scaffold";
    public string Description => "Generates .NET scaffold code for controllers, services, repositories, DTOs, and CQRS handlers based on entity name and pattern.";

    public string InputSchema => """
    {
      "type": "object",
      "properties": {
        "entity": {
          "type": "string",
          "description": "The entity/model name to scaffold (e.g., 'Product', 'Order')"
        },
        "pattern": {
          "type": "string",
          "enum": ["mvc", "cqrs", "minimal-api", "repository"],
          "description": "Architecture pattern to generate code for"
        },
        "namespace": {
          "type": "string",
          "description": "Root namespace for the generated code"
        },
        "include_tests": {
          "type": "boolean",
          "description": "Whether to include xUnit test stubs"
        }
      },
      "required": ["entity", "pattern"]
    }
    """;

    private readonly PromptRenderer _renderer;

    public ScaffoldSkill(PromptRenderer renderer)
    {
        _renderer = renderer;
    }

    public Task<SkillResult> ExecuteAsync(SkillRequest request, CancellationToken cancellationToken = default)
    {
        string entity = request.GetParameter<string>("entity") ?? "Entity";
        string pattern = request.GetParameter<string>("pattern") ?? "mvc";
        string ns = request.GetParameter<string>("namespace") ?? "MyApp";
        bool includeTests = request.GetParameter<bool?>("include_tests") ?? false;

        StringBuilder sb = new();
        sb.AppendLine($"// 🏗️ Scaffold: {entity} — Pattern: {pattern.ToUpper()}");
        sb.AppendLine($"// Namespace: {ns}");
        sb.AppendLine();

        switch (pattern.ToLowerInvariant())
        {
            case "mvc":
                sb.Append(GenerateMvc(entity, ns, includeTests));
                break;
            case "cqrs":
                sb.Append(GenerateCqrs(entity, ns, includeTests));
                break;
            case "minimal-api":
                sb.Append(GenerateMinimalApi(entity, ns));
                break;
            case "repository":
                sb.Append(GenerateRepository(entity, ns));
                break;
            default:
                return Task.FromResult(SkillResult.Failure($"Unknown pattern '{pattern}'. Valid: mvc, cqrs, minimal-api, repository", "INVALID_PARAM"));
        }

        return Task.FromResult(SkillResult.Success(sb.ToString(), new { entity, pattern, ns }));
    }

    private static string GenerateMvc(string entity, string ns, bool tests)
    {
        return $$"""
// ── Model ─────────────────────────────────────────────────────────
namespace {{ns}}.Models;

public sealed class {{entity}}
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

// ── Service ───────────────────────────────────────────────────────
namespace {{ns}}.Services;

public interface I{{entity}}Service
{
    Task<IEnumerable<{{entity}}>> GetAllAsync(CancellationToken ct = default);
    Task<{{entity}}?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<{{entity}}> CreateAsync({{entity}} entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class {{entity}}Service : I{{entity}}Service
{
    // TODO: inject I{{entity}}Repository
    public Task<IEnumerable<{{entity}}>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
    public Task<{{entity}}?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<{{entity}}> CreateAsync({{entity}} entity, CancellationToken ct = default) => throw new NotImplementedException();
    public Task DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
}

// ── Controller ────────────────────────────────────────────────────
namespace {{ns}}.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class {{entity}}Controller : ControllerBase
{
    private readonly I{{entity}}Service _service;
    public {{entity}}Controller(I{{entity}}Service service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] {{entity}} entity, CancellationToken ct)
    {
        var created = await _service.CreateAsync(entity, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
{{(tests ? GenerateTestStub(entity, ns) : "")}}
""";
    }

    private static string GenerateCqrs(string entity, string ns, bool tests)
    {
        return $$"""
// ── CQRS: {{entity}} ────────────────────────────────────────────────
namespace {{ns}}.Features.{{entity}}s;

// Query
public sealed record Get{{entity}}Query(Guid Id);
public sealed record Get{{entity}}Response(Guid Id, string Name, DateTimeOffset CreatedAt);

// Command
public sealed record Create{{entity}}Command(string Name);
public sealed record Create{{entity}}Response(Guid Id);

// Handler (Query)
public sealed class Get{{entity}}QueryHandler
{
    public Task<Get{{entity}}Response?> HandleAsync(Get{{entity}}Query query, CancellationToken ct)
        => throw new NotImplementedException("TODO: inject your data source");
}

// Handler (Command)
public sealed class Create{{entity}}CommandHandler
{
    public Task<Create{{entity}}Response> HandleAsync(Create{{entity}}Command command, CancellationToken ct)
        => throw new NotImplementedException("TODO: inject your data source");
}
""";
    }

    private static string GenerateMinimalApi(string entity, string ns)
    {
        return $$"""
// ── Minimal API: {{entity}} ─────────────────────────────────────────
// Add this to your Program.cs / endpoint registration file
namespace {{ns}}.Endpoints;

public static class {{entity}}Endpoints
{
    public static IEndpointRouteBuilder Map{{entity}}Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{{entity.ToLower()}}s").WithTags("{{entity}}s");

        group.MapGet("/", async (I{{entity}}Service svc, CancellationToken ct)
            => Results.Ok(await svc.GetAllAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, I{{entity}}Service svc, CancellationToken ct) =>
        {
            var item = await svc.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async ({{entity}} body, I{{entity}}Service svc, CancellationToken ct) =>
        {
            var created = await svc.CreateAsync(body, ct);
            return Results.Created($"/api/{{entity.ToLower()}}s/{created.Id}", created);
        });

        group.MapDelete("/{id:guid}", async (Guid id, I{{entity}}Service svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        return app;
    }
}
""";
    }

    private static string GenerateRepository(string entity, string ns)
    {
        return $$"""
// ── Repository Pattern: {{entity}} ─────────────────────────────────
namespace {{ns}}.Repositories;

public interface I{{entity}}Repository
{
    Task<IEnumerable<{{entity}}>> GetAllAsync(CancellationToken ct = default);
    Task<{{entity}}?> FindAsync(Guid id, CancellationToken ct = default);
    Task AddAsync({{entity}} entity, CancellationToken ct = default);
    Task UpdateAsync({{entity}} entity, CancellationToken ct = default);
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}

// EF Core implementation stub
public class EfCore{{entity}}Repository : I{{entity}}Repository
{
    // private readonly AppDbContext _context;
    // public EfCore{{entity}}Repository(AppDbContext context) => _context = context;

    public Task<IEnumerable<{{entity}}>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
    public Task<{{entity}}?> FindAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
    public Task AddAsync({{entity}} entity, CancellationToken ct = default) => throw new NotImplementedException();
    public Task UpdateAsync({{entity}} entity, CancellationToken ct = default) => throw new NotImplementedException();
    public Task RemoveAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
}
""";
    }

    private static string GenerateTestStub(string entity, string ns)
    {
        return $$"""

// ── xUnit Test Stub ───────────────────────────────────────────────
namespace {{ns}}.Tests;

public class {{entity}}ServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsItems()
    {
        // Arrange
        // var svc = new {{entity}}Service( ... );
        // Act
        // var result = await svc.GetAllAsync();
        // Assert
        // Assert.NotNull(result);
        throw new NotImplementedException("Write your test here");
    }
}
""";
    }
}
