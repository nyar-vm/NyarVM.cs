namespace Std.Command.Metadata;

/// <summary>
///     命令元数据信息，用于 Shell 补全等场景
/// </summary>
public sealed class CommandInfo
{
    /// <summary>
    ///     命令名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     命令描述
    /// </summary>
    public string description { get; init; } = string.Empty;

    /// <summary>
    ///     命令类型
    /// </summary>
    public Type command_type { get; init; } = null!;

    /// <summary>
    ///     子命令列表
    /// </summary>
    public IReadOnlyList<CommandInfo> sub_commands { get; init; } = [];

    /// <summary>
    ///     命名选项列表
    /// </summary>
    public IReadOnlyList<OptionInfo> options { get; init; } = [];

    /// <summary>
    ///     位置参数列表
    /// </summary>
    public IReadOnlyList<ArgumentInfo> arguments { get; init; } = [];
}