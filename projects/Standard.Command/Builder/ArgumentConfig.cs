namespace Std.Command.Builder;

/// <summary>
///     位置参数配置
/// </summary>
/// <typeparam name="T">参数类型</typeparam>
public sealed class ArgumentConfig<T>
{
    private readonly ArgumentDef _def;

    internal ArgumentConfig(ArgumentDef def)
    {
        _def = def;
    }

    /// <summary>
    ///     设置默认值
    /// </summary>
    public ArgumentConfig<T> with_default(T value)
    {
        _def.default_value = value;
        return this;
    }

    /// <summary>
    ///     设置描述文本
    /// </summary>
    public ArgumentConfig<T> with_description(string desc)
    {
        _def.description = desc;
        return this;
    }

    /// <summary>
    ///     设置是否必填
    /// </summary>
    public ArgumentConfig<T> with_required(bool required = true)
    {
        _def.required = required;
        return this;
    }
}