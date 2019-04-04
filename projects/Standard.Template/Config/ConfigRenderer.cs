namespace Std.Template.Config;

#region 数据模型

/// <summary>
///     配置环境定义
/// </summary>
public class ConfigEnvironment
{
    public string Name { get; set; } = "";
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
///     配置模板定义
/// </summary>
public class ConfigTemplate
{
    public string Name { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public string Language { get; set; } = "dora";
    public Dictionary<string, object> DefaultVariables { get; set; } = new();
}

/// <summary>
///     配置渲染结果
/// </summary>
public class ConfigRenderResult
{
    public string TemplateName { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
///     批量渲染结果
/// </summary>
public class BatchConfigRenderResult
{
    public int TotalCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<ConfigRenderResult> Results { get; init; } = [];
}

#endregion

/// <summary>
///     配置文件渲染器
/// </summary>
public sealed class ConfigRenderer
{
    private readonly List<ConfigEnvironment> _environments = [];
    private readonly Dictionary<string, TemplateManager> _managers = new();
    private readonly Dictionary<string, DejaVuRenderer> _renderers = new();
    private readonly string _sourceDir = "";
    private readonly List<ConfigTemplate> _templates = [];

    public ConfigRenderer(string sourceDir)
    {
        _sourceDir = Path.GetFullPath(sourceDir);
    }

    /// <summary>
    ///     加载配置项目
    /// </summary>
    public void Load()
    {
        LoadEnvironments();
        LoadTemplates();
    }

    #region 加载

    private void LoadEnvironments()
    {
        _environments.Clear();
        var envDir = Path.Combine(_sourceDir, "environments");
        if (!Directory.Exists(envDir)) return;

        foreach (var file in Directory.GetFiles(envDir, "*.yaml"))
        {
            var envName = Path.GetFileNameWithoutExtension(file);
            var env = new ConfigEnvironment { Name = envName };

            var content = File.ReadAllText(file);
            var data = DataConvert.YamlToDictionary(content);
            foreach (var (key, value) in data) env.Variables[key] = value ?? "";

            _environments.Add(env);
        }
    }

    private void LoadTemplates()
    {
        _templates.Clear();
        var templatesDir = Path.Combine(_sourceDir, "templates");
        if (!Directory.Exists(templatesDir)) return;

        LoadTemplateFiles(templatesDir, "*.dora", "dora");
        LoadTemplateFiles(templatesDir, "*.doki", "doki");
    }

    private void LoadTemplateFiles(string templatesDir, string extension, string language)
    {
        foreach (var file in Directory.GetFiles(templatesDir, extension, SearchOption.AllDirectories))
        {
            var template = new ConfigTemplate
            {
                Name = Path.GetRelativePath(templatesDir, file).Replace("\\", "/"),
                SourcePath = file,
                Language = language
            };

            var configFile = Path.ChangeExtension(file, ".yaml");
            if (File.Exists(configFile))
            {
                var content = File.ReadAllText(configFile);
                var parser = new YamlParser();
                var result = parser.parse(content);
                if (result.success && result.value is YamlMapping mapping)
                {
                    template.OutputPath = DataConvert.GetYamlString(mapping, "output");
                    template.Language = DataConvert.GetYamlString(mapping, "language", template.Language);
                }
            }

            if (string.IsNullOrEmpty(template.OutputPath))
                template.OutputPath = Path.ChangeExtension(template.Name, null);

            _templates.Add(template);
        }
    }

    #endregion

    #region 渲染

    /// <summary>
    ///     渲染单个模板
    /// </summary>
    public string RenderTemplate(string templateName, Dictionary<string, object> variables)
    {
        var template = _templates.FirstOrDefault(t => t.Name == templateName);
        if (template == null) throw new FileNotFoundException($"模板 '{templateName}' 不存在。");

        var language = template.Language == "doki" ? DejaVuLanguage.doki : DejaVuLanguage.dora;
        var renderer = GetRenderer(language);

        var mergedVariables = new Dictionary<string, object>(template.DefaultVariables);
        foreach (var kvp in variables) mergedVariables[kvp.Key] = kvp.Value;

        var templateContent = File.ReadAllText(template.SourcePath);
        return renderer.Render(templateContent, mergedVariables);
    }

    /// <summary>
    ///     渲染单个模板到文件
    /// </summary>
    public ConfigRenderResult RenderToFile(string templateName, Dictionary<string, object> variables, string outputDir)
    {
        var template = _templates.FirstOrDefault(t => t.Name == templateName);

        try
        {
            var rendered = RenderTemplate(templateName, variables);

            var outputPath = template != null
                ? Path.Combine(outputDir, template.OutputPath)
                : Path.Combine(outputDir, templateName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, rendered);

            return new ConfigRenderResult
            {
                TemplateName = templateName,
                OutputPath = outputPath,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new ConfigRenderResult
            {
                TemplateName = templateName,
                OutputPath = "",
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    ///     渲染指定环境的所有模板
    /// </summary>
    public BatchConfigRenderResult RenderEnvironment(string environmentName, string outputDir)
    {
        var env = _environments.FirstOrDefault(e => e.Name == environmentName);
        if (env == null) throw new FileNotFoundException($"环境 '{environmentName}' 不存在。");

        var results = new List<ConfigRenderResult>();

        foreach (var template in _templates)
        {
            var result = RenderToFile(template.Name, env.Variables, outputDir);
            results.Add(result);
        }

        return new BatchConfigRenderResult
        {
            TotalCount = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        };
    }

    /// <summary>
    ///     渲染所有环境的所有模板
    /// </summary>
    public Dictionary<string, BatchConfigRenderResult> RenderAllEnvironments(string baseOutputDir)
    {
        var results = new Dictionary<string, BatchConfigRenderResult>();

        foreach (var env in _environments)
        {
            var outputDir = Path.Combine(baseOutputDir, env.Name);
            results[env.Name] = RenderEnvironment(env.Name, outputDir);
        }

        return results;
    }

    /// <summary>
    ///     渲染自定义变量到模板
    /// </summary>
    public string RenderString(string templateContent, Dictionary<string, object> variables, string language = "dora")
    {
        var lang = language == "doki" ? DejaVuLanguage.doki : DejaVuLanguage.dora;
        var renderer = GetRenderer(lang);
        return renderer.Render(templateContent, variables);
    }

    #endregion

    #region 辅助方法

    private DejaVuRenderer GetRenderer(DejaVuLanguage language)
    {
        var key = language.name;
        if (!_renderers.TryGetValue(key, out var renderer))
        {
            if (!_managers.TryGetValue(key, out var manager))
            {
                var loader = new FileSystemTemplateLoader(_sourceDir);
                manager = new TemplateManager(loader);
                _managers[key] = manager;
            }

            renderer = new DejaVuRenderer(language, manager);
            _renderers[key] = renderer;
        }

        return renderer;
    }

    /// <summary>
    ///     获取所有环境名称
    /// </summary>
    public List<string> GetEnvironmentNames()
    {
        return [.. _environments.Select(e => e.Name)];
    }

    /// <summary>
    ///     获取所有模板名称
    /// </summary>
    public List<string> GetTemplateNames()
    {
        return [.. _templates.Select(t => t.Name)];
    }

    /// <summary>
    ///     获取环境变量
    /// </summary>
    public Dictionary<string, object>? GetEnvironmentVariables(string environmentName)
    {
        var env = _environments.FirstOrDefault(e => e.Name == environmentName);
        return env?.Variables;
    }

    #endregion
}