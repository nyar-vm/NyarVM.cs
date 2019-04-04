namespace Std.Command.TypeConversion;

/// <summary>
///     类型转换器接口，将字符串转换为指定类型
/// </summary>
/// <typeparam name="T">目标类型</typeparam>
public interface ITypeConverter<T>
{
    /// <summary>
    ///     尝试将输入字符串转换为目标类型
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="result">转换结果</param>
    /// <returns>转换是否成功</returns>
    bool try_convert(string input, out T result);

    /// <summary>
    ///     获取格式提示文本，用于帮助页面
    /// </summary>
    /// <returns>格式提示</returns>
    string get_format_hint();
}