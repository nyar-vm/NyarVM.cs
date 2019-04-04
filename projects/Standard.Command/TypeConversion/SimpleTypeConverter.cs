namespace Std.Command.TypeConversion;

/// <summary>
///     简单类型转换器基类，通过 <c>TryParse</c> 模式实现字符串到目标类型的转换
///     子类只需覆写 <see cref="try_parse" /> 和 <see cref="get_format_hint" /> 即可
/// </summary>
/// <typeparam name="T">目标类型</typeparam>
public abstract class SimpleTypeConverter<T> : ITypeConverter<T>
{
    /// <summary>
    ///     获取格式提示文本，用于帮助页面显示
    /// </summary>
    /// <returns>格式提示</returns>
    public abstract string get_format_hint();

    /// <summary>
    ///     尝试将输入字符串转换为目标类型，内部委托给 <see cref="try_parse" />
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="result">转换结果</param>
    /// <returns>转换是否成功</returns>
    public bool try_convert(string input, out T result)
    {
        return try_parse(input, out result);
    }

    /// <summary>
    ///     尝试解析字符串为指定类型
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="result">解析结果</param>
    /// <returns>解析是否成功</returns>
    protected abstract bool try_parse(string input, out T result);
}