using System.Reflection;

namespace Std.Template.Site;

/// <summary>
///     插件管理器：负责发现、加载和注册插件
/// </summary>
public sealed class PluginManager
{
    private readonly List<IHtmlPostProcess> _htmlPostProcessors = [];
    private readonly List<IMarkdownTransform> _markdownTransforms = [];
    private readonly List<ISiteHook> _siteHooks = [];

    /// <summary>
    ///     已加载的 Markdown 转换插件列表
    /// </summary>
    public IReadOnlyList<IMarkdownTransform> MarkdownTransforms => _markdownTransforms;

    /// <summary>
    ///     已加载的 HTML 后处理插件列表
    /// </summary>
    public IReadOnlyList<IHtmlPostProcess> HtmlPostProcessors => _htmlPostProcessors;

    /// <summary>
    ///     已加载的站点钩子列表
    /// </summary>
    public IReadOnlyList<ISiteHook> SiteHooks => _siteHooks;

    /// <summary>
    ///     是否有任何已加载的插件
    /// </summary>
    public bool HasPlugins => _markdownTransforms.Count > 0
                              || _htmlPostProcessors.Count > 0
                              || _siteHooks.Count > 0;

    #region 插件加载

    /// <summary>
    ///     从指定目录按约定加载插件（扫描 *.dll 文件）
    /// </summary>
    /// <param name="pluginsDir">插件目录路径</param>
    public void LoadFromDirectory(string pluginsDir)
    {
        if (!Directory.Exists(pluginsDir)) return;

        foreach (var dllPath in Directory.GetFiles(pluginsDir, "*.dll", SearchOption.TopDirectoryOnly))
            LoadAssembly(dllPath);
    }

    /// <summary>
    ///     从配置中的插件路径列表加载
    /// </summary>
    /// <param name="pluginPaths">插件程序集路径列表</param>
    /// <param name="baseDir">用于解析相对路径的基础目录</param>
    public void LoadFromConfig(List<string> pluginPaths, string baseDir)
    {
        foreach (var pluginPath in pluginPaths)
        {
            var fullPath = Path.IsPathRooted(pluginPath)
                ? pluginPath
                : Path.GetFullPath(Path.Combine(baseDir, pluginPath));

            if (File.Exists(fullPath)) LoadAssembly(fullPath);
        }
    }

    /// <summary>
    ///     加载单个程序集中的所有插件类型
    /// </summary>
    private void LoadAssembly(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            DiscoverPlugins(assembly);
        }
        catch (BadImageFormatException)
        {
            Console.Error.WriteLine($"插件程序集格式无效：{assemblyPath}");
        }
        catch (FileLoadException)
        {
            Console.Error.WriteLine($"无法加载插件程序集：{assemblyPath}");
        }
        catch (ReflectionTypeLoadException ex)
        {
            Console.Error.WriteLine($"插件程序集类型加载失败：{assemblyPath}");
            foreach (var loaderEx in ex.LoaderExceptions)
                if (loaderEx != null)
                    Console.Error.WriteLine($"  - {loaderEx.Message}");
        }
    }

    /// <summary>
    ///     在程序集中发现并实例化实现插件接口的类型
    /// </summary>
    private void DiscoverPlugins(Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;

            try
            {
                RegisterIfPlugin(type);
            }
            catch (MissingMethodException)
            {
                Console.Error.WriteLine($"插件类型缺少无参构造函数：{type.FullName}");
            }
            catch (TargetInvocationException ex)
            {
                Console.Error.WriteLine($"插件类型构造失败：{type.FullName} - {ex.InnerException?.Message}");
            }
        }
    }

    /// <summary>
    ///     检查类型是否实现插件接口，如是则实例化并注册
    /// </summary>
    private void RegisterIfPlugin(Type type)
    {
        if (typeof(IMarkdownTransform).IsAssignableFrom(type))
        {
            var instance = (IMarkdownTransform)Activator.CreateInstance(type)!;
            _markdownTransforms.Add(instance);
        }

        if (typeof(IHtmlPostProcess).IsAssignableFrom(type))
        {
            var instance = (IHtmlPostProcess)Activator.CreateInstance(type)!;
            _htmlPostProcessors.Add(instance);
        }

        if (typeof(ISiteHook).IsAssignableFrom(type))
        {
            var instance = (ISiteHook)Activator.CreateInstance(type)!;
            _siteHooks.Add(instance);
        }
    }

    #endregion

    #region 管线执行

    /// <summary>
    ///     依次执行所有 Markdown 转换插件
    /// </summary>
    /// <param name="markdownContent">原始 Markdown 内容</param>
    /// <param name="metadata">页面元数据</param>
    /// <returns>转换后的 Markdown 内容</returns>
    public async Task<string> RunMarkdownTransformsAsync(string markdownContent, PageMetadata metadata)
    {
        var result = markdownContent;

        foreach (var transform in _markdownTransforms)
            try
            {
                result = await transform.TransformAsync(result, metadata);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Markdown 转换插件 [{transform.Name}] 执行失败：{ex.Message}");
            }

        return result;
    }

    /// <summary>
    ///     依次执行所有 HTML 后处理插件
    /// </summary>
    /// <param name="htmlContent">渲染后的 HTML</param>
    /// <param name="metadata">页面元数据</param>
    /// <returns>处理后的 HTML</returns>
    public async Task<string> RunHtmlPostProcessAsync(string htmlContent, PageMetadata metadata)
    {
        var result = htmlContent;

        foreach (var processor in _htmlPostProcessors)
            try
            {
                result = await processor.ProcessAsync(result, metadata);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"HTML 后处理插件 [{processor.Name}] 执行失败：{ex.Message}");
            }

        return result;
    }

    /// <summary>
    ///     执行所有站点钩子的 OnBeforeBuildAsync
    /// </summary>
    public async Task RunBeforeBuildAsync(SiteConfig config)
    {
        foreach (var hook in _siteHooks)
            try
            {
                await hook.OnBeforeBuildAsync(config);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"站点钩子 [{hook.Name}] OnBeforeBuild 执行失败：{ex.Message}");
            }
    }

    /// <summary>
    ///     执行所有站点钩子的 OnAfterBuildAsync
    /// </summary>
    public async Task RunAfterBuildAsync(string outputDir)
    {
        foreach (var hook in _siteHooks)
            try
            {
                await hook.OnAfterBuildAsync(outputDir);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"站点钩子 [{hook.Name}] OnAfterBuild 执行失败：{ex.Message}");
            }
    }

    #endregion
}