using System.Text.Json;
using SkillEngine.Core;
using SkillEngine.Core.Abstractions;
using SkillEngine.Core.Models;
using SkillEngine.Mcp.Models;

namespace SkillEngine.Mcp.Endpoints;

/// <summary>
/// Maps the two core MCP HTTP endpoints:
///   GET  /mcp/tools    → Returns the tool manifest for AI model discovery
///   POST /mcp/execute  → Executes a skill and returns the result
/// </summary>
public static class McpEndpoints
{
    public static WebApplication MapMcpEndpoints(this WebApplication app)
    {
        // ── GET /mcp/tools ────────────────────────────────────────────────────
        app.MapGet("/mcp/tools", (ISkillRouter router) =>
        {
            List<McpToolDefinition> tools = router.GetAll().Select(skill =>
            {
                // Parse the skill's input schema JSON into an object for the manifest
                object parsedSchema;
                try { parsedSchema = JsonSerializer.Deserialize<object>(skill.InputSchema)!; }
                catch { parsedSchema = skill.InputSchema; }

                return new McpToolDefinition
                {
                    Name = skill.Name,
                    Description = skill.Description,
                    InputSchema = parsedSchema
                };
            }).ToList();

            return Results.Ok(new McpToolManifest
            {
                SchemaVersion = "1.0",
                ServerName = ".NET MCP Skill Engine",
                ServerVersion = "1.0.0",
                Tools = tools
            });
        })
        .WithName("GetMcpTools")
        .WithSummary("Returns the MCP tool manifest for AI model discovery.")
        .WithTags("MCP");

        // ── POST /mcp/execute ─────────────────────────────────────────────────
        app.MapPost("/mcp/execute", static async (
            McpExecuteRequest executeRequest,
            SkillEngine.Core.Engine.SkillOrchestrator orchestrator,
            CancellationToken cancellationToken) =>
        {
            SkillRequest skillRequest = new()
            {
                ToolName = executeRequest.Tool,
                Parameters = executeRequest.Parameters,
                SessionId = executeRequest.SessionId,
                AgentMetadata = executeRequest.Metadata
            };

            SkillResult result = await orchestrator.InvokeAsync(skillRequest, cancellationToken);

            McpExecuteResponse response = new()
            {
                Success = result.IsSuccess,
                Content = result.Content,
                Data = result.Data,
                DurationMs = (long)result.Duration.TotalMilliseconds,
                Error = result.IsSuccess ? null : new McpErrorDetail
                {
                    Code = result.ErrorCode,
                    Message = result.ErrorMessage
                }
            };

            return result.IsSuccess
                ? Results.Ok(response)
                : Results.UnprocessableEntity(response);
        })
        .WithName("ExecuteMcpTool")
        .WithSummary("Executes a registered skill tool and returns the result.")
        .WithTags("MCP");

        // ── GET /health ───────────────────────────────────────────────────────
        app.MapGet("/health", (ISkillRouter router) => Results.Ok(new
        {
            status = "healthy",
            skills_registered = router.GetAll().Count,
            timestamp = DateTimeOffset.UtcNow
        }))
        .WithName("HealthCheck")
        .WithTags("System");

        return app;
    }
}
