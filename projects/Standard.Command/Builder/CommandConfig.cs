namespace Std.Command.Builder;

/// <summary>
///     命令配置（兼容层），用于构建器模式中配置单个命令
///     <para>
///         此类型保留用于桥接和渐进迁移，新增 CLI 工具应使用 attribute-first 模式。
///     </para>
/// </summary>
public sealed class CommandConfig
{
    private readonly BuiltCommandModel _model;
    private int _arg_position;

    internal CommandConfig(string name)
    {
        _model = new BuiltCommandModel { name = name };
    }

    internal CommandConfig(BuiltCommandModel model)
    {
        _model = model;
    }

    /// <summary>
    ///     添加位置参数
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="name">参数名称</param>
    /// <param name="configure">参数配置委托</param>
    public CommandConfig add_argument<T>(string name, Action<ArgumentConfig<T>> configure)
    {
        var def = new ArgumentDef(name, typeof(T)) { position = _arg_position++ };
        var config = new ArgumentConfig<T>(def);
        configure(config);
        _model.arguments.Add(def);
        return this;
    }

    /// <summary>
    ///     添加命名选项
    /// </summary>
    /// <typeparam name="T">选项值类型</typeparam>
    /// <param name="longName">长名称</param>
    /// <param name="configure">选项配置委托</param>
    public CommandConfig add_option<T>(string longName, Action<OptionConfig<T>> configure)
    {
        var def = new OptionDef(longName, typeof(T));
        var config = new OptionConfig<T>(def);
        configure(config);
        _model.options.Add(def);
        return this;
    }

    /// <summary>
    ///     添加子命令
    /// </summary>
    /// <param name="name">子命令名称</param>
    /// <param name="configure">子命令配置委托</param>
    public CommandConfig add_sub_command(string name, Action<CommandConfig> configure)
    {
        var subConfig = new CommandConfig(name);
        configure(subConfig);
        _model.sub_commands.Add(subConfig._model);
        return this;
    }

    /// <summary>
    ///     绑定处理程序委托
    /// </summary>
    public CommandConfig with_handler(Delegate handler)
    {
        _model.handler = handler;
        return this;
    }

    /// <summary>
    ///     绑定处理程序类型（支持依赖注入）
    /// </summary>
    /// <typeparam name="THandler">处理程序类型</typeparam>
    public CommandConfig with_handler<THandler>()
    {
        _model.handler_type = typeof(THandler);
        return this;
    }

    /// <summary>
    ///     设置命令描述
    /// </summary>
    public CommandConfig with_description(string description)
    {
        _model.description = description;
        return this;
    }

    internal BuiltCommandModel build()
    {
        return _model;
    }

    internal void set_handler_type(Type handlerType)
    {
        _model.handler_type = handlerType;
    }

    internal void add_options_from_handler(Delegate handler)
    {
        foreach (var param in handler.Method.GetParameters())
        {
            var optDef = new OptionDef(param.Name!, param.ParameterType);
            if (param.HasDefaultValue) optDef.default_value = param.DefaultValue;
            _model.options.Add(optDef);
        }
    }
}