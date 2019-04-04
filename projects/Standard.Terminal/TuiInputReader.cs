using System.Text;
using Core.Console;

namespace Std.Terminal;

/// <summary>
///     TUI 输入读取器，包装键盘事件实现 IInputReader 接口
/// </summary>
public sealed class TuiInputReader : IInputReader
{
    private readonly Queue<char> _char_buffer;
    private readonly Queue<ConsoleKeyInfo> _key_buffer;

    /// <summary>
    ///     创建 TUI 输入读取器
    /// </summary>
    public TuiInputReader()
    {
        _key_buffer = new Queue<ConsoleKeyInfo>();
        _char_buffer = new Queue<char>();
    }

    /// <inheritdoc />
    public bool is_redirected => false;

    /// <inheritdoc />
    public string? read_line()
    {
        var sb = new StringBuilder();

        while (true)
        {
            if (!_key_buffer.TryDequeue(out var keyInfo)) return null;

            if (keyInfo.Key == ConsoleKey.Enter) return sb.ToString();

            if (keyInfo.KeyChar != '\0' && !char.IsControl(keyInfo.KeyChar)) sb.Append(keyInfo.KeyChar);
        }
    }

    /// <inheritdoc />
    public ConsoleKeyInfo? read_key(bool intercept = false)
    {
        if (_key_buffer.TryDequeue(out var keyInfo)) return keyInfo;

        return null;
    }

    /// <inheritdoc />
    public string? read_password()
    {
        var sb = new StringBuilder();

        while (true)
        {
            if (!_key_buffer.TryDequeue(out var keyInfo)) return null;

            if (keyInfo.Key == ConsoleKey.Enter) return sb.ToString();

            if (keyInfo.Key == ConsoleKey.Escape) return null;

            if (keyInfo.KeyChar != '\0' && !char.IsControl(keyInfo.KeyChar)) sb.Append(keyInfo.KeyChar);
        }
    }

    /// <summary>
    ///     将按键事件送入输入缓冲区
    /// </summary>
    /// <param name="keyInfo">按键信息</param>
    public void feed_key(ConsoleKeyInfo keyInfo)
    {
        _key_buffer.Enqueue(keyInfo);

        if (keyInfo.KeyChar != '\0' && !char.IsControl(keyInfo.KeyChar)) _char_buffer.Enqueue(keyInfo.KeyChar);
    }
}