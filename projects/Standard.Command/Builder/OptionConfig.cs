namespace Std.Command.Builder;

/// <summary>
///     命名选项配置
/// </summary>
/// <typeparam name="T">选项值类型</typeparam>
public sealed class OptionConfig<T>
{
    private readonly OptionDef _def;

    internal OptionConfig(OptionDef def)
    {
        _def = def;
    }

    /// <summary>
    ///     设置短名称
    /// </summary>
    public OptionConfig<T> with_short_name(char c)
    {
        _def.short_name = c;
        return this;
    }

    /// <summary>
    ///     设置短名称（取字符串首字符）
    /// </summary>
    public OptionConfig<T> with_short_name(string s)
    {
        _def.short_name = string.IsNullOrEmpty(s) ? null : s[0];
        return this;
    }

    /// <summary>
    ///     设置默认值
    /// </summary>
    public OptionConfig<T> with_default(T value)
    {
        _def.default_value = value;
        return this;
    }

    /// <summary>
    ///     设置描述文本
    /// </summary>
    public OptionConfig<T> with_description(string desc)
    {
        _def.description = desc;
        return this;
    }

    /// <summary>
    ///     设置别名
    /// </summary>
    public OptionConfig<T> with_alias(params string[] aliases)
    {
        foreach (var alias in aliases) _def.aliases.Add(alias);

        return this;
    }

    /// <summary>
    ///     设置环境变量名，未提供选项值时从环境变量读取
    /// </summary>
    public OptionConfig<T> with_environment_variable(string envVar)
    {
        _def.environment_variable = envVar;
        return this;
    }

    /// <summary>
    ///     设置配置键，未提供选项值时从 CommandConfiguration 中读取
    /// </summary>
    /// <param name="configKey">配置键（如 "Database:Host"）</param>
    public OptionConfig<T> with_config_key(string configKey)
    {
        _def.config_key = configKey;
        return this;
    }

    /// <summary>
    ///     设置数值范围验证
    /// </summary>
    public OptionConfig<T> with_range(T minimum, T maximum)
    {
        _def.minimum = minimum;
        _def.maximum = maximum;
        return this;
    }
}