namespace Core.Console;

/// <summary>
///     控制台 I/O 适配器接口
/// </summary>
public interface IConsoleAdapter
{
    /// <summary>
    ///     写入字符串
    /// </summary>
    void write(string text);

    /// <summary>
    ///     写入字符串并换行
    /// </summary>
    void write_line(string text);

    /// <summary>
    ///     读取一行输入
    /// </summary>
    string read_line();

    /// <summary>
    ///     读取全部输入
    /// </summary>
    string read_all();
}