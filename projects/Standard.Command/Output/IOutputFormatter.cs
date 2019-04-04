namespace Std.Command.Output;

/// <summary>
///     输出格式化器接口
/// </summary>
/// <typeparam name="T">要格式化的数据类型</typeparam>
public interface IOutputFormatter<T>
{
    /// <summary>
    ///     将数据格式化为字符串输出
    /// </summary>
    /// <param name="data">要格式化的数据</param>
    /// <returns>格式化后的字符串</returns>
    string format(T data);

    /// <summary>
    ///     检查是否支持指定的输出格式
    /// </summary>
    /// <param name="format">目标输出格式</param>
    /// <returns>是否支持该格式</returns>
    bool supports_format(OutputFormat format);
}