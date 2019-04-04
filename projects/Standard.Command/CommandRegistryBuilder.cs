using Std.Command.Builder;

namespace Std.Command;

/// <summary>
///     命令注册构建器（兼容层）
///     <para>
///         此类型保留用于桥接、测试和渐进迁移，但不再是仓库 CLI 的正式默认入口。
///         新增 CLI 工具必须使用 attribute-first 模式（<c>[Command]</c> / <c>[Argument]</c> / <c>[Option]</c> / <c>[Subcommand]</c>），
///         通过 <c>CommandApp.run&lt;TRootCommand&gt;()</c> 启动。
///     </para>
/// </summary>
public sealed class CommandRegistryBuilder
{
    internal List<BuiltCommandModel> _commands { get; } = [];

    /// <summary>
    ///     注册一个命令及其处理委托
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <param name="handler">处理委托，参数名将自动映射为命令行选项和位置参数</param>
    public void add(string name, Delegate handler)
    {
        var model = BuiltCommandModel.from_delegate(name, handler);
        _commands.Add(model);
    }

    /// <summary>
    ///     注册一个命令，支持通过 CommandConfig 详细配置
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <param name="configure">配置委托</param>
    public void add(string name, Action<CommandConfig> configure)
    {
        var config = new CommandConfig(name);
        configure(config);
        _commands.Add(config.build());
    }
}