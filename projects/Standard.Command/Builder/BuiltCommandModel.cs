namespace Std.Command.Builder;

/// <summary>
///     构建器模式生成的命令模型
/// </summary>
public sealed class BuiltCommandModel
{
    /// <summary>命令名称</summary>
    public string name { get; set; } = string.Empty;

    /// <summary>命令描述</summary>
    public string description { get; set; } = string.Empty;

    /// <summary>位置参数列表</summary>
    public List<ArgumentDef> arguments { get; } = [];

    /// <summary>命名选项列表</summary>
    public List<OptionDef> options { get; } = [];

    /// <summary>子命令列表</summary>
    public List<BuiltCommandModel> sub_commands { get; } = [];

    /// <summary>处理程序委托</summary>
    public Delegate? handler { get; set; }

    /// <summary>处理程序类型</summary>
    public Type? handler_type { get; set; }

    /// <summary>
    ///     从处理委托快速构建命令模型
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <param name="handler">处理委托</param>
    /// <returns>构建完成的命令模型</returns>
    public static BuiltCommandModel from_delegate(string name, Delegate handler)
    {
        var config = new CommandConfig(name);
        config.add_options_from_handler(handler);
        config.with_handler(handler);
        return config.build();
    }
}