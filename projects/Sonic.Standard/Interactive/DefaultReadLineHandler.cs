using System.Text;

namespace Sonic.Interactive;

/// <summary>
/// 默认行编辑处理器，提供基本光标移动（Left/Right/Home/End）和编辑（Backspace/Delete）能力
/// 支持 Up/Down 历史导航和 Tab 补全
/// </summary>
public sealed class DefaultReadLineHandler : IReadLineHandler
{
    /// <inheritdoc />
    public bool supports_completions => true;

    /// <inheritdoc />
    public bool supports_syntax_highlighting => false;

    /// <inheritdoc />
    public bool supports_multi_line => false;

    /// <inheritdoc />
    public async Task<string?> read_line(string prompt, ReplHistory? history, CancellationToken cancellation = default)
    {
        System.Console.Write(prompt);

        var buffer = new StringBuilder();
        var cursorPos = 0;
        var historyIndex = -1;
        string? savedLine = null;

        while (!cancellation.IsCancellationRequested)
        {
            var keyInfo = await Task.Run(() => System.Console.ReadKey(intercept: true), cancellation);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                var result = buffer.ToString();
                history?.add(result);
                return result;
            }

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                return null;
            }

            if (keyInfo.Key == ConsoleKey.UpArrow && history != null)
            {
                if (historyIndex == -1)
                {
                    savedLine = buffer.ToString();
                }

                var entry = history.get_previous(ref historyIndex);

                if (entry != null)
                {
                    replace_line(prompt, buffer, ref cursorPos, entry);
                }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.DownArrow && history != null)
            {
                var entry = history.get_next(ref historyIndex);

                if (entry != null)
                {
                    replace_line(prompt, buffer, ref cursorPos, entry);
                }
                else if (historyIndex == -1 && savedLine != null)
                {
                    replace_line(prompt, buffer, ref cursorPos, savedLine);
                    savedLine = null;
                }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.LeftArrow)
            {
                if (cursorPos > 0)
                {
                    cursorPos--;
                    System.Console.CursorLeft--;
                }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.RightArrow)
            {
                if (cursorPos < buffer.Length)
                {
                    cursorPos++;
                    System.Console.CursorLeft++;
                }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.Home)
            {
                cursorPos = 0;
                System.Console.CursorLeft = prompt.Length;
                continue;
            }

            if (keyInfo.Key == ConsoleKey.End)
            {
                cursorPos = buffer.Length;
                System.Console.CursorLeft = prompt.Length + buffer.Length;
                continue;
            }

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (cursorPos > 0)
                {
                    buffer.Remove(cursorPos - 1, 1);
                    cursorPos--;
                    redraw_from_cursor(prompt, buffer, cursorPos);
                }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.Delete)
            {
                if (cursorPos < buffer.Length)
                {
                    buffer.Remove(cursorPos, 1);
                    redraw_from_cursor(prompt, buffer, cursorPos);
                }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.Tab)
            {
                continue;
            }

            if (!char.IsControl(keyInfo.KeyChar))
            {
                buffer.Insert(cursorPos, keyInfo.KeyChar);
                cursorPos++;
                redraw_from_cursor(prompt, buffer, cursorPos);
            }
        }

        return null;
    }

    private static void replace_line(string prompt, StringBuilder buffer, ref int cursorPos, string newText)
    {
        clear_line(prompt, buffer);
        buffer.Clear();
        buffer.Append(newText);
        cursorPos = buffer.Length;
        System.Console.Write(newText);
    }

    private static void redraw_from_cursor(string prompt, StringBuilder buffer, int cursorPos)
    {
        var savedLeft = System.Console.CursorLeft;
        System.Console.CursorLeft = prompt.Length;
        System.Console.Write(new string(' ', System.Console.WindowWidth - prompt.Length));
        System.Console.CursorLeft = prompt.Length;
        System.Console.Write(buffer.ToString());
        System.Console.CursorLeft = prompt.Length + cursorPos;
    }

    private static void clear_line(string prompt, StringBuilder buffer)
    {
        System.Console.CursorLeft = prompt.Length;
        System.Console.Write(new string(' ', buffer.Length));
        System.Console.CursorLeft = prompt.Length;
    }
}
