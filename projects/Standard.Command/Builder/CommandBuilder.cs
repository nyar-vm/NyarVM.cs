using System.Reflection;
using Core.Command;

namespace Std.Command.Builder;

/// <summary>
///     命令构建器（兼容层），用于构建器模式中定义和注册命令
///     <para>
///         此类型保留用于桥接和渐进迁移，新增 CLI 工具应使用 attribute-first 模式。
///     </para>
/// </summary>
public sealed class CommandBuilder
{
    private readonly string _app_name;
    private readonly List<CommandConfig> _command_configs = [];
    private readonly string _description;

    private CommandBuilder(string appName, string description)
    {
        _app_name = appName;
        _description = description;
    }

    /// <summary>
    ///     创建命令构建器
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="description">应用描述</param>
    public static CommandBuilder create(string appName, string description = "")
    {
        return new CommandBuilder(appName, description);
    }

    /// <summary>
    ///     添加命令
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <param name="configure">命令配置委托</param>
    public CommandBuilder add_command(string name, Action<CommandConfig> configure)
    {
        var config = new CommandConfig(name);
        configure(config);
        _command_configs.Add(config);
        return this;
    }

    /// <summary>
    ///     从程序集扫描 <see cref="CommandAttribute" /> 标注的类并注册
    /// </summary>
    public CommandBuilder scan_commands_from_assembly(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<CommandAttribute>() != null);

        foreach (var type in commandTypes)
        {
            var attr = type.GetCustomAttribute<CommandAttribute>()!;
            var desc = attr.description ?? string.Empty;
            var cmdConfig = new CommandConfig(attr.name);

            if (!string.IsNullOrEmpty(desc)) cmdConfig.with_description(desc);

            cmdConfig.with_handler<object>();
            cmdConfig.set_handler_type(type);

            _command_configs.Add(cmdConfig);
        }

        return this;
    }

    /// <summary>
    ///     运行命令处理管线，解析命令行参数并路由到对应命令
    /// </summary>
    /// <param name="args">命令行参数</param>
    public void run(string[] args)
    {
        var models = _command_configs.Select(c => c.build()).ToList();

        if (args.Length == 0)
        {
            render_help(models);
            return;
        }

        var commandName = args[0];

        foreach (var model in models)
            if (string.Equals(model.name, commandName, StringComparison.OrdinalIgnoreCase))
            {
                CliArgumentParser.parse_and_execute(model, args[1..]);
                return;
            }

        System.Console.Error.WriteLine($"未知命令: {commandName}");
        System.Console.Error.WriteLine($"运行 '{_app_name} --help' 查看可用命令。");
    }

    private void render_help(List<BuiltCommandModel> models)
    {
        System.Console.WriteLine($"{_app_name} — {_description}");
        System.Console.WriteLine();
        System.Console.WriteLine("可用命令:");

        foreach (var model in models)
        {
            var desc = string.IsNullOrEmpty(model.description) ? "(无描述)" : model.description;
            System.Console.WriteLine($"  {model.name,-16}{desc}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"运行 '{_app_name} <命令> --help' 查看具体命令的帮助。");
    }
}