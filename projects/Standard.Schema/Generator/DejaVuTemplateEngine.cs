namespace Hermes.Generator;

public sealed class DejaVuTemplateEngine
{
    private readonly DejaVuRenderer _renderer;
    private readonly string? _templateDir;

    public DejaVuTemplateEngine(string? templateDir = null)
    {
        _templateDir = templateDir;
        TemplateManager? manager = null;
        if (templateDir != null)
        {
            var loader = new FileSystemTemplateLoader(templateDir);
            manager = new TemplateManager(loader);
        }

        _renderer = new DejaVuRenderer(DejaVuLanguage.Dora, manager);
    }

    public string Render(string templateContent, SchemaIR schema)
    {
        var context = BuildContext(schema);
        return _renderer.Render(templateContent, context);
    }

    public string RenderFile(string templatePath, SchemaIR schema)
    {
        var template = File.ReadAllText(templatePath);
        return Render(template, schema);
    }

    private Dictionary<string, object> BuildContext(SchemaIR schema)
    {
        var context = new Dictionary<string, object>
        {
            ["namespace"] = schema.Namespace,
            ["classes"] = schema.Classes.Select(c => new Dictionary<string, object>
            {
                ["name"] = c.Name,
                ["fields"] = c.fields.Select(f => new Dictionary<string, object>
                {
                    ["name"] = f.Name,
                    ["type"] = f.FieldType.TypeName,
                    ["isOptional"] = f.IsOptional,
                    ["attributes"] = f.Attributes.Select(a => a.Name).ToList()
                }).ToList(),
                ["attributes"] = c.Attributes.Select(a => a.Name).ToList()
            }).ToList(),
            ["enums"] = schema.Enums.Select(e => new Dictionary<string, object>
            {
                ["name"] = e.Name,
                ["members"] = e.Members.Select(m => new Dictionary<string, object>
                {
                    ["name"] = m.Name
                }).ToList()
            }).ToList(),
            ["storages"] = schema.Storages.Select(s => new Dictionary<string, object>
            {
                ["name"] = s.Name,
                ["models"] = s.Models.Select(m => m.Name).ToList()
            }).ToList(),
            ["services"] = schema.Services.Select(s => new Dictionary<string, object>
            {
                ["name"] = s.Name,
                ["endpoints"] = s.Endpoints.Select(e => new Dictionary<string, object>
                {
                    ["name"] = e.Name
                }).ToList()
            }).ToList()
        };
        return context;
    }
}