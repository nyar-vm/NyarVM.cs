using Sonic.Command;
using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// 增强型交互式确认引擎，支持 Y/N 快捷键、AnsiStyling 渲染、危险操作警告样式
/// </summary>
internal static class InteractiveConfirmEngine
{
    public static bool run(LocalizableString prompt, ConfirmOptions options)
    {
        if (System.Console.IsInputRedirected)
        {
            return options.default_value;
        }

        if (!TerminalCapability.supports_cursor_control)
        {
            return run_fallback(prompt, options);
        }

        var state = new ConfirmState
        {
            prompt = prompt.default_value ?? prompt.key,
            default_value = options.default_value,
            dangerous = options.dangerous,
            yes_label = options.yes_label,
            no_label = options.no_label,
            current_choice = options.default_value,
        };

        return run_interactive(state);
    }

    private static bool run_interactive(ConfirmState state)
    {
        var cursorTop = System.Console.CursorTop;

        try
        {
            System.Console.Write(AnsiStyling.hide_cursor());
        }
        catch
        {
        }

        render_full(state, cursorTop);

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Y:
                    state.current_choice = true;
                    render_final(state, cursorTop);
                    show_cursor();
                    return true;

                case ConsoleKey.N:
                    state.current_choice = false;
                    render_final(state, cursorTop);
                    show_cursor();
                    return false;

                case ConsoleKey.Enter:
                    render_final(state, cursorTop);
                    show_cursor();
                    return state.current_choice;

                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    state.current_choice = !state.current_choice;
                    render_full(state, cursorTop);
                    break;

                case ConsoleKey.Tab:
                    state.current_choice = !state.current_choice;
                    render_full(state, cursorTop);
                    break;

                case ConsoleKey.Escape:
                    state.current_choice = false;
                    render_cancel(state, cursorTop);
                    show_cursor();
                    return false;
            }
        }
    }

    private static bool run_fallback(LocalizableString prompt, ConfirmOptions options)
    {
        var yesNo = options.default_value ? $"{options.yes_label}/{options.no_label.ToLowerInvariant()}" : $"{options.yes_label.ToLowerInvariant()}/{options.no_label}";
        System.Console.Write($"? {prompt.default_value ?? prompt.key} [{yesNo}]: ");

        var input = System.Console.ReadLine()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(input))
        {
            return options.default_value;
        }

        return input is "y" or "yes";
    }

    private static void render_full(ConfirmState state, int cursorTop)
    {
        try
        {
            System.Console.SetCursorPosition(0, cursorTop);
            System.Console.Write(AnsiStyling.clear_line());
            System.Console.SetCursorPosition(0, cursorTop);
        }
        catch (IOException)
        {
            return;
        }

        var useAnsi = TerminalCapability.should_use_ansi;

        if (state.dangerous && useAnsi)
        {
            System.Console.Write(AnsiStyling.bold(AnsiStyling.red($"⚠ {state.prompt}")));
        }
        else if (useAnsi)
        {
            System.Console.Write($"? {AnsiStyling.bold(state.prompt)}");
        }
        else
        {
            System.Console.Write($"? {state.prompt}");
        }

        System.Console.Write("  ");

        var yesSelected = state.current_choice;
        var noSelected = !state.current_choice;

        if (useAnsi)
        {
            var yesText = yesSelected
                ? AnsiStyling.bg_rgb(40, 120, 220, AnsiStyling.white($" {state.yes_label} "))
                : $" {state.yes_label} ";
            var noText = noSelected
                ? AnsiStyling.bg_rgb(120, 120, 120, AnsiStyling.white($" {state.no_label} "))
                : $" {state.no_label} ";

            System.Console.Write(yesText);
            System.Console.Write(" ");
            System.Console.Write(noText);
        }
        else
        {
            var yesText = yesSelected ? $"[{state.yes_label}]" : $" {state.yes_label} ";
            var noText = noSelected ? $"[{state.no_label}]" : $" {state.no_label} ";

            System.Console.Write(yesText);
            System.Console.Write(" ");
            System.Console.Write(noText);
        }

        if (useAnsi)
        {
            System.Console.Write($"  {AnsiStyling.dim("←→/Tab 切换 | Y/N 快捷键 | Enter 确认 | Esc 取消")}");
        }
        else
        {
            System.Console.Write("  (Y/N/Enter/Esc)");
        }

        System.Console.WriteLine();
    }

    private static void render_final(ConfirmState state, int cursorTop)
    {
        try
        {
            System.Console.SetCursorPosition(0, cursorTop);
            System.Console.Write(AnsiStyling.clear_line());
            System.Console.SetCursorPosition(0, cursorTop);
        }
        catch (IOException)
        {
            return;
        }

        var useAnsi = TerminalCapability.should_use_ansi;

        if (state.current_choice)
        {
            if (useAnsi)
            {
                System.Console.WriteLine($"? {state.prompt}: {AnsiStyling.green(state.yes_label)}");
            }
            else
            {
                System.Console.WriteLine($"? {state.prompt}: {state.yes_label}");
            }
        }
        else
        {
            if (useAnsi)
            {
                System.Console.WriteLine($"? {state.prompt}: {AnsiStyling.red(state.no_label)}");
            }
            else
            {
                System.Console.WriteLine($"? {state.prompt}: {state.no_label}");
            }
        }
    }

    private static void render_cancel(ConfirmState state, int cursorTop)
    {
        try
        {
            System.Console.SetCursorPosition(0, cursorTop);
            System.Console.Write(AnsiStyling.clear_line());
            System.Console.SetCursorPosition(0, cursorTop);
        }
        catch (IOException)
        {
            return;
        }

        System.Console.WriteLine($"? {state.prompt}: （已取消）");
    }

    private static void show_cursor()
    {
        try
        {
            System.Console.Write(AnsiStyling.show_cursor());
        }
        catch
        {
        }
    }

    private sealed class ConfirmState
    {
        public string prompt = "";
        public bool default_value = true;
        public bool dangerous;
        public string yes_label = "Y";
        public string no_label = "N";
        public bool current_choice = true;
    }
}
