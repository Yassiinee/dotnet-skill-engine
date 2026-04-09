namespace SkillEngine.Mcp.Models;

public class McpToolDefinition
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public object InputSchema { get; set; } = default!;
}

public class McpToolManifest
{
    public string SchemaVersion { get; set; } = "1.0";
    public string ServerName { get; set; } = ".NET MCP Skill Engine";
    public string ServerVersion { get; set; } = "1.0.0";
    public List<McpToolDefinition> Tools { get; set; } = new();
}

public class McpExecuteRequest
{
    public string Tool { get; set; } = default!;

    // Keep flexible for AI input
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public string? SessionId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class McpExecuteResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public object? Data { get; set; }
    public long DurationMs { get; set; }
    public McpErrorDetail? Error { get; set; }
    public string? TraceId { get; set; }
}

public class McpErrorDetail
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}