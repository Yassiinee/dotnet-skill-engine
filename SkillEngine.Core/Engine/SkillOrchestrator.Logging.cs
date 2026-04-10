using Microsoft.Extensions.Logging;

namespace SkillEngine.Core.Engine;

public sealed partial class SkillOrchestrator
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[Orchestrator] Invoking tool '{Tool}' | Session: {Session}")]
    private partial void LogInvocation(string tool, string session);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[Orchestrator] No skill found for tool '{Tool}'")]
    private partial void LogSkillNotFound(string tool);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[Orchestrator] Skill '{Skill}' completed in {Duration}ms | Success: {Success}")]
    private partial void LogSkillCompleted(string skill, double duration, bool success);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[Orchestrator] Tool '{Tool}' execution was cancelled.")]
    private partial void LogExecutionCancelled(string tool);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "[Orchestrator] Unhandled exception executing tool '{Tool}'")]
    private partial void LogUnhandledException(Exception ex, string tool);
}
