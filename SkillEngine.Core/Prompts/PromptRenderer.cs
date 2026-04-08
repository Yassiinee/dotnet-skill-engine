using Scriban;

namespace SkillEngine.Core.Prompts;

/// <summary>
/// Renders <see cref="PromptTemplate"/> instances by substituting variables using Scriban.
/// Thread-safe; instances can be shared as singletons.
/// </summary>
public sealed class PromptRenderer
{
    /// <summary>
    /// Renders a template with the provided variable bindings.
    /// </summary>
    /// <param name="template">The prompt template to render.</param>
    /// <param name="variables">Key-value pairs to bind into the template.</param>
    /// <returns>The rendered prompt string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Scriban parsing fails.</exception>
    public string Render(PromptTemplate template, Dictionary<string, object?> variables)
    {
        var scribanTemplate = Template.Parse(template.Template);

        if (scribanTemplate.HasErrors)
        {
            var errors = string.Join("; ", scribanTemplate.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"Template '{template.Name}' has parse errors: {errors}");
        }

        var context = new TemplateContext();
        var scriptObject = new Scriban.Runtime.ScriptObject();

        foreach (var (key, value) in variables)
            scriptObject.SetValue(key, value, readOnly: false);

        context.PushGlobal(scriptObject);
        return scribanTemplate.Render(context);
    }

    /// <summary>
    /// Renders a raw template string without a named template object.
    /// </summary>
    public string RenderRaw(string templateText, Dictionary<string, object?> variables)
    {
        return Render(new PromptTemplate
        {
            Name = "__inline__",
            Template = templateText
        }, variables);
    }
}
