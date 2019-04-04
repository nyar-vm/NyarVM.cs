using Sonic.Console;
using Sonic.Console;

namespace Sonic.Interactive;

/// <summary>
/// REPL 输入读取器，在行编辑基础上包装 IInputReader
/// </summary>
public sealed class ReplInputReader : IInputReader
{
    /// <inheritdoc />
    public bool is_redirected => false;

    /// <inheritdoc />
    public string? read_line()
    {
        return System.Console.ReadLine();
    }

    /// <inheritdoc />
    public ConsoleKeyInfo? read_key(bool intercept = false)
    {
        return System.Console.ReadKey(intercept);
    }

    /// <inheritdoc />
    public string? read_password()
    {
        var password = new System.Text.StringBuilder();

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                    System.Console.Write("\b \b");
                }

                continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                return null;
            }

            if (char.IsControl(key.KeyChar))
            {
                continue;
            }

            password.Append(key.KeyChar);
            System.Console.Write("*");
        }

        return password.ToString();
    }
}
