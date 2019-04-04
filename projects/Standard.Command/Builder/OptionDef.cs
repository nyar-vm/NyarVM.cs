namespace Std.Command.Builder;

/// <summary>
///     命名选项定义
/// </summary>
public sealed class OptionDef
{
    /// <summary>
    ///     创建命名选项定义
    /// </summary>
    /// <param name="name">选项长名称</param>
    /// <param name="type">选项值类型</param>
    public OptionDef(string name, Type type)
    {
        this.name = name;
        this.type = type;
    }

    /// <summary>选项长名称</summary>
    public string name { get; }

    /// <summary>选项值类型</summary>
    public Type type { get; }

    /// <summary>短名称</summary>
    public char? short_name { get; set; }

    /// <summary>默认值</summary>
    public object? default_value { get; set; }

    /// <summary>选项描述</summary>
    public string description { get; set; } = string.Empty;

    /// <summary>别名列表</summary>
    public List<string> aliases { get; } = [];

    /// <summary>是否为标志选项（bool 类型）</summary>
    public bool is_flag => type == typeof(bool);

    /// <summary>环境变量名，未提供选项值时从环境变量读取</summary>
    public string? environment_variable { get; set; }

    /// <summary>配置键，未提供选项值时从配置中读取（优先级低于环境变量）</summary>
    public string? config_key { get; set; }

    /// <summary>数值范围最小值</summary>
    public object? minimum { get; set; }

    /// <summary>数值范围最大值</summary>
    public object? maximum { get; set; }
}