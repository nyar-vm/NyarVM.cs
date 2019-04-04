namespace Std.Command.Builder;

/// <summary>
///     位置参数定义
/// </summary>
public sealed class ArgumentDef
{
    /// <summary>
    ///     创建位置参数定义
    /// </summary>
    /// <param name="name">参数名称</param>
    /// <param name="type">参数类型</param>
    public ArgumentDef(string name, Type type)
    {
        this.name = name;
        this.type = type;
    }

    /// <summary>参数名称</summary>
    public string name { get; }

    /// <summary>参数类型</summary>
    public Type type { get; }

    /// <summary>默认值</summary>
    public object? default_value { get; set; }

    /// <summary>参数描述</summary>
    public string description { get; set; } = string.Empty;

    /// <summary>是否必填</summary>
    public bool required { get; set; }

    /// <summary>参数位置</summary>
    public int position { get; set; }
}