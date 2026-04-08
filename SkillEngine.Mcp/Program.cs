using SkillEngine.Core;
using SkillEngine.Mcp.Endpoints;
using SkillEngine.Tools.Skills;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ── Skill Engine ──────────────────────────────────────────────────────────────
builder.Services.AddSkillEngine(options =>
{
    options.MaxParameters = 30;
    options.EnableVerboseLogging = builder.Environment.IsDevelopment();
})
.AddSkill<ScaffoldSkill>()
.AddSkill<CodeReviewSkill>()
.AddSkill<DiagnosticSkill>()
.AddSkill<ArchitectureAdvisorSkill>();

// ── HTTP / Swagger ────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = ".NET MCP Skill Engine",
        Version = "v1",
        Description = "A modular MCP-compatible AI skill server for .NET. " +
                      "Exposes code intelligence tools to Claude, GPT, Gemini, and any MCP agent.",
        Contact = new()
        {
            Name = "Yassine Zakhama",
            Url = new Uri("https://www.linkedin.com/in/yassine-zakhama")
        }
    });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

WebApplication app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET MCP Skill Engine v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors();
app.MapMcpEndpoints();

app.Run();
