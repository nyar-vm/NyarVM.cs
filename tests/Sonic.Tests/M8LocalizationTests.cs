using System.Globalization;
using Core.Command;

namespace Commander.Testing;

/// <summary>
///     M8 Iris 本地化系统测试，实现了 IDisposable 以确保 Localizer.current 状态不被污染
/// </summary>
public sealed class M8LocalizationTests : IDisposable
{
    private static ILocalizer? s_savedLocalizer;

    public M8LocalizationTests()
    {
        s_savedLocalizer = Localizer.current;
    }

    public void Dispose()
    {
        Localizer.current = s_savedLocalizer!;
    }

    /// <summary>
    ///     确保 Localizer.current 包含内置中英文资源
    /// </summary>
    private static void SetupBuiltInLocalizer()
    {
        var localizer = new ResxLocalizer();

        var zhCN = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help.usage"] = "用法:",
            ["help.subcommands"] = "子命令:",
            ["help.arguments"] = "参数:",
            ["help.options"] = "选项:",
            ["help.available_commands"] = "可用命令:",
            ["help.required"] = "(必填)",
            ["help.optional"] = "(可选)",
            ["help.default_value"] = "，默认值: {0}",
            ["help.default_value_parens"] = "（默认值: {0}）",
            ["help.options_args_hint"] = " [选项] <参数>",
            ["help.run_help_hint"] = "运行 'app <命令> --help' 查看具体命令的帮助。"
        };

        var enUS = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help.usage"] = "Usage:",
            ["help.subcommands"] = "Subcommands:",
            ["help.arguments"] = "Arguments:",
            ["help.options"] = "Options:",
            ["help.available_commands"] = "Available commands:",
            ["help.required"] = "(required)",
            ["help.optional"] = "(optional)",
            ["help.default_value"] = ", default: {0}",
            ["help.default_value_parens"] = " (default: {0})",
            ["help.options_args_hint"] = " [options] <arguments>",
            ["help.run_help_hint"] = "Run 'app <command> --help' for more information on a command."
        };

        localizer.add_resources("zh-CN", zhCN);
        localizer.add_resources("en-US", enUS);
        Localizer.current = localizer;
    }

    #region ResxLocalizer

    /// <summary>
    ///     添加资源后能通过精确文化获取
    /// </summary>
    [Fact]
    public void ResxLocalizer_AddAndGet_ExactCulture_ShouldReturnValue()
    {
        var localizer = new ResxLocalizer();
        localizer.add_resource("zh-CN", "greeting", "你好");

        var result = localizer.get_string("greeting", new CultureInfo("zh-CN"));

        Assert.Equal("你好", result);
    }

    /// <summary>
    ///     GetString 对于不存在的键应返回 null
    /// </summary>
    [Fact]
    public void ResxLocalizer_GetString_MissingKey_ShouldReturnNull()
    {
        var localizer = new ResxLocalizer();
        localizer.add_resource("zh-CN", "exists", "存在");

        var result = localizer.get_string("not_found", new CultureInfo("zh-CN"));

        Assert.Null(result);
    }

    /// <summary>
    ///     键名大小写不敏感
    /// </summary>
    [Fact]
    public void ResxLocalizer_GetString_CaseInsensitiveKey_ShouldMatch()
    {
        var localizer = new ResxLocalizer();
        localizer.add_resource("en-US", "Hello.World", "value");

        var result = localizer.get_string("hello.world", new CultureInfo("en-US"));

        Assert.Equal("value", result);
    }

    /// <summary>
    ///     文化名称大小写不敏感
    /// </summary>
    [Fact]
    public void ResxLocalizer_GetString_CaseInsensitiveCultureName_ShouldMatch()
    {
        var localizer = new ResxLocalizer();
        localizer.add_resource("ZH-cn", "key", "值");

        var result = localizer.get_string("key", new CultureInfo("zh-CN"));

        Assert.Equal("值", result);
    }

    /// <summary>
    ///     父文化回退：zh-Hans 应回退到 zh
    /// </summary>
    [Fact]
    public void ResxLocalizer_GetString_ParentCultureFallback_ShouldUseParent()
    {
        var localizer = new ResxLocalizer();
        localizer.add_resource("zh", "msg", "中性中文");

        var result = localizer.get_string("msg", new CultureInfo("zh-CN"));

        Assert.Equal("中性中文", result);
    }

    /// <summary>
    ///     批量添加资源
    /// </summary>
    [Fact]
    public void ResxLocalizer_AddResources_ShouldAddAll()
    {
        var localizer = new ResxLocalizer();
        var entries = new Dictionary<string, string>
        {
            ["a"] = "A值",
            ["b"] = "B值",
            ["c"] = "C值"
        };

        localizer.add_resources("zh-CN", entries);

        Assert.Equal("A值", localizer.get_string("a", new CultureInfo("zh-CN")));
        Assert.Equal("B值", localizer.get_string("b", new CultureInfo("zh-CN")));
        Assert.Equal("C值", localizer.get_string("c", new CultureInfo("zh-CN")));
    }

    /// <summary>
    ///     InvariantCulture 回退链终点应返回 null
    /// </summary>
    [Fact]
    public void ResxLocalizer_GetString_InvariantCulture_ShouldReturnNull()
    {
        var localizer = new ResxLocalizer();

        var result = localizer.get_string("any", CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    #endregion

    #region LocalizableString

    /// <summary>
    ///     LocalizableString.ToString 在有翻译时返回翻译
    /// </summary>
    [Fact]
    public void LocalizableString_ToString_WithTranslation_ShouldReturnLocalized()
    {
        var localizer = new ResxLocalizer();
        localizer.add_resource("en-US", "test.key", "Hello");
        Localizer.current = localizer;

        var str = new LocalizableString { key = "test.key", default_value = "默认" };

        var result = str.to_string(new CultureInfo("en-US"));

        Assert.Equal("Hello", result);
    }

    /// <summary>
    ///     LocalizableString.ToString 在无翻译时返回 DefaultValue
    /// </summary>
    [Fact]
    public void LocalizableString_ToString_NoTranslation_ShouldReturnDefault()
    {
        var localizer = new ResxLocalizer();
        Localizer.current = localizer;

        var str = new LocalizableString { key = "missing", default_value = "默认值" };

        var result = str.to_string(new CultureInfo("zh-CN"));

        Assert.Equal("默认值", result);
    }

    /// <summary>
    ///     隐式转换：string 自动升级为 LocalizableString
    /// </summary>
    [Fact]
    public void LocalizableString_ImplicitConversion_ShouldUseStringAsKeyAndDefault()
    {
        LocalizableString str = "普通字符串";

        Assert.Equal("普通字符串", str.key);
        Assert.Equal("普通字符串", str.default_value);
    }

    #endregion

    #region Localizer 静态类

    /// <summary>
    ///     Localizer.current 默认返回 ResxLocalizer 实例
    /// </summary>
    [Fact]
    public void Localizer_Current_Default_ShouldBeResxLocalizer()
    {
        Localizer.current = null!;

        var current = Localizer.current;

        Assert.IsType<ResxLocalizer>(current);
    }

    /// <summary>
    ///     Localizer.current 可设置为自定义实现
    /// </summary>
    [Fact]
    public void Localizer_Current_Setter_ShouldAcceptCustom()
    {
        var custom = new ResxLocalizer();
        Localizer.current = custom;

        Assert.Same(custom, Localizer.current);
    }

    /// <summary>
    ///     Localizer.get_string 委托到 Current
    /// </summary>
    [Fact]
    public void Localizer_GetString_ShouldDelegateToCurrent()
    {
        var localizer = new ResxLocalizer();
        localizer.add_resource("ja-JP", "yes", "はい");
        Localizer.current = localizer;

        var result = Localizer.get_string("yes");

        Assert.Equal("はい", result);
    }

    #endregion

    #region HelpResources 内置资源

    /// <summary>
    ///     HelpResources 内置资源：zh-CN 应返回中文
    /// </summary>
    [Theory]
    [InlineData("zh-CN", "用法:", "子命令:", "参数:", "选项:", "(必填)", "(可选)")]
    public void HelpResources_BuiltIn_Chinese_ShouldReturnChinese(string cultureName, string usage, string sub,
        string args, string opts, string req, string opt)
    {
        SetupBuiltInLocalizer();

        var culture = new CultureInfo(cultureName);

        Assert.Equal(usage, HelpResources.usage_header.to_string(culture));
        Assert.Equal(sub, HelpResources.sub_commands_header.to_string(culture));
        Assert.Equal(args, HelpResources.arguments_header.to_string(culture));
        Assert.Equal(opts, HelpResources.options_header.to_string(culture));
        Assert.Equal(req, HelpResources.required_tag.to_string(culture));
        Assert.Equal(opt, HelpResources.optional_tag.to_string(culture));
    }

    /// <summary>
    ///     HelpResources 内置资源：en-US 应返回英文
    /// </summary>
    [Fact]
    public void HelpResources_BuiltIn_English_ShouldReturnEnglish()
    {
        SetupBuiltInLocalizer();

        var culture = new CultureInfo("en-US");

        Assert.Equal("Usage:", HelpResources.usage_header.to_string(culture));
        Assert.Equal("Subcommands:", HelpResources.sub_commands_header.to_string(culture));
        Assert.Equal("Arguments:", HelpResources.arguments_header.to_string(culture));
        Assert.Equal("Options:", HelpResources.options_header.to_string(culture));
        Assert.Equal("Available commands:", HelpResources.available_commands_header.to_string(culture));
        Assert.Equal("(required)", HelpResources.required_tag.to_string(culture));
        Assert.Equal("(optional)", HelpResources.optional_tag.to_string(culture));
        Assert.Equal("Run 'app <command> --help' for more information on a command.",
            HelpResources.run_help_hint.to_string(culture));
    }

    #endregion

    #region HelpRenderer 本地化渲染

    /// <summary>
    ///     HelpRenderer 在 zh-CN 下渲染中文标题
    /// </summary>
    [Fact]
    public void HelpRenderer_Render_Chinese_ShouldContainChineseHeaders()
    {
        SetupBuiltInLocalizer();

        var renderer = new HelpRenderer();
        var command = new CommandInfo
        {
            name = "build",
            description = "构建项目"
        };

        var output = renderer.render(command, new CultureInfo("zh-CN"));

        Assert.Contains("用法:", output);
        Assert.Contains(" [选项] <参数>", output);
    }

    /// <summary>
    ///     HelpRenderer 在 en-US 下渲染英文标题
    /// </summary>
    [Fact]
    public void HelpRenderer_Render_English_ShouldContainEnglishHeaders()
    {
        SetupBuiltInLocalizer();

        var renderer = new HelpRenderer();
        var command = new CommandInfo
        {
            name = "build",
            description = "Build project"
        };

        var output = renderer.render(command, new CultureInfo("en-US"));

        Assert.Contains("Usage:", output);
        Assert.Contains(" [options] <arguments>", output);
    }

    /// <summary>
    ///     HelpRenderer.RenderAll 在 en-US 下渲染英文
    /// </summary>
    [Fact]
    public void HelpRenderer_RenderAll_English_ShouldContainEnglishHeaders()
    {
        SetupBuiltInLocalizer();

        var renderer = new HelpRenderer();
        var commands = new[]
        {
            new CommandInfo { name = "build", description = "Build" },
            new CommandInfo { name = "test", description = "Test" }
        };

        var output = renderer.render_all(commands, new CultureInfo("en-US"));

        Assert.Contains("Available commands:", output);
        Assert.Contains("Run 'app <command> --help'", output);
        Assert.DoesNotContain("可用命令", output);
    }

    #endregion
}