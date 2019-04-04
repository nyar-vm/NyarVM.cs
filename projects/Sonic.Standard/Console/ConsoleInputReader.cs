using Core.Console;

namespace Std.Console;

public sealed class ConsoleInputReader : IInputReader
{
    public static readonly ConsoleInputReader instance = new();

    public ConsoleInputReader()
    {
        try
        {
            is_redirected = System.Console.IsInputRedirected;
        }
        catch
        {
            is_redirected = false;
        }
    }

    public bool is_redirected { get; }

    public string? read_line()
    {
        return System.Console.ReadLine();
    }

    public ConsoleKeyInfo? read_key(bool intercept = false)
    {
        if (is_redirected) return null;

        return System.Console.ReadKey(intercept);
    }

    public string? read_password()
    {
        if (is_redirected) return System.Console.ReadLine();

        var password = new StringBuilder();

        while (true)
        {
            var key = System.Console.ReadKey(true);

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

            if (key.Key == ConsoleKey.Escape) return null;

            if (char.IsControl(key.KeyChar)) continue;

            password.Append(key.KeyChar);
            System.Console.Write("*");
        }

        return password.ToString();
    }
}