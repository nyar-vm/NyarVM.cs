using Sonic.Command;
using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// 增强型交互式列表选择引擎，支持搜索过滤、AnsiStyling 渲染、键盘快捷键
/// </summary>
internal static class InteractiveSelectEngine
{
    public static string? run_single_select(
        LocalizableString prompt,
        IReadOnlyList<string> items,
        SelectOptions options)
    {
        var state = new SelectState
        {
            prompt = prompt.default_value ?? prompt.key,
            items = items,
            selected_index = System.Math.Min(options.default_index, items.Count - 1),
            page_size = System.Math.Min(options.page_size, items.Count),
            search_enabled = options.search_enabled,
            show_instructions = options.show_instructions,
            is_multi = false,
        };

        update_filtered_indices(state);

        return run_engine(state) ? items[state.filtered_indices[state.selected_index]] : null;
    }

    public static IReadOnlyList<string> run_multi_select(
        LocalizableString prompt,
        IReadOnlyList<string> items,
        MultiSelectOptions options)
    {
        var state = new SelectState
        {
            prompt = prompt.default_value ?? prompt.key,
            items = items,
            selected_index = 0,
            page_size = System.Math.Min(options.page_size, items.Count),
            search_enabled = options.search_enabled,
            show_instructions = options.show_instructions,
            is_multi = true,
            min_selected = options.min_selected,
            max_selected = options.max_selected,
            checked_indices = [..options.default_selected],
        };

        update_filtered_indices(state);

        var confirmed = run_engine(state);
        if (!confirmed)
        {
            return [];
        }

        return state.checked_indices
            .OrderBy(i => i)
            .Select(i => items[i])
            .ToList();
    }

    private static bool run_engine(SelectState state)
    {
        if (!TerminalCapability.supports_cursor_control)
        {
            return run_fallback(state);
        }

        try
        {
            System.Console.Write(AnsiStyling.hide_cursor());
        }
        catch
        {
        }

        var cursorTop = System.Console.CursorTop;
        render_full(state, cursorTop);

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);
            var handled = handle_key(state, key);

            switch (handled)
            {
                case KeyResult.confirm:
                    if (state.is_multi && state.checked_indices.Count < state.min_selected)
                    {
                        state.error_message = $"至少选择 {state.min_selected} 项";
                        render_full(state, cursorTop);
                        continue;
                    }

                    render_final(state, cursorTop);
                    try
                    {
                        System.Console.Write(AnsiStyling.show_cursor());
                    }
                    catch
                    {
                    }

                    return true;

                case KeyResult.cancel:
                    render_cancel(state, cursorTop);
                    try
                    {
                        System.Console.Write(AnsiStyling.show_cursor());
                    }
                    catch
                    {
                    }

                    return false;

                case KeyResult.need_render:
                    state.error_message = null;
                    render_full(state, cursorTop);
                    break;
            }
        }
    }

    private static bool run_fallback(SelectState state)
    {
        System.Console.WriteLine($"? {state.prompt}:");
        for (var i = 0; i < state.items.Count; i++)
        {
            if (state.is_multi)
            {
                var check = state.checked_indices.Contains(i) ? "[x]" : "[ ]";
                System.Console.WriteLine($"  {check} {i + 1}. {state.items[i]}");
            }
            else
            {
                System.Console.WriteLine($"  {i + 1}. {state.items[i]}");
            }
        }

        if (state.is_multi)
        {
            System.Console.Write("输入编号（逗号分隔）: ");
            var input = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            foreach (var part in input.Split(',', ' '))
            {
                if (int.TryParse(part.Trim(), out var idx) && idx >= 1 && idx <= state.items.Count)
                {
                    state.checked_indices.Add(idx - 1);
                }
            }

            return state.checked_indices.Count >= state.min_selected;
        }
        else
        {
            System.Console.Write("输入编号: ");
            var input = System.Console.ReadLine()?.Trim();
            if (int.TryParse(input, out var idx) && idx >= 1 && idx <= state.items.Count)
            {
                state.selected_index = idx - 1;
                return true;
            }

            return false;
        }
    }

    private static KeyResult handle_key(SelectState state, ConsoleKeyInfo key)
    {
        if (state.search_enabled && state.search_mode)
        {
            return handle_search_key(state, key);
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                state.selected_index = System.Math.Max(0, state.selected_index - 1);
                return KeyResult.need_render;

            case ConsoleKey.DownArrow:
                state.selected_index = System.Math.Min(state.filtered_indices.Count - 1, state.selected_index + 1);
                return KeyResult.need_render;

            case ConsoleKey.PageUp:
                state.selected_index = System.Math.Max(0, state.selected_index - state.page_size);
                return KeyResult.need_render;

            case ConsoleKey.PageDown:
                state.selected_index = System.Math.Min(state.filtered_indices.Count - 1, state.selected_index + state.page_size);
                return KeyResult.need_render;

            case ConsoleKey.Home:
                state.selected_index = 0;
                return KeyResult.need_render;

            case ConsoleKey.End:
                state.selected_index = state.filtered_indices.Count - 1;
                return KeyResult.need_render;

            case ConsoleKey.Spacebar when state.is_multi:
                toggle_current(state);
                return KeyResult.need_render;

            case ConsoleKey.A when state.is_multi && (key.Modifiers & ConsoleModifiers.Control) != 0:
                toggle_all(state);
                return KeyResult.need_render;

            case ConsoleKey.Enter:
                return KeyResult.confirm;

            case ConsoleKey.Escape:
                if (state.search_mode)
                {
                    state.search_mode = false;
                    state.search_query = "";
                    update_filtered_indices(state);
                    return KeyResult.need_render;
                }

                return KeyResult.cancel;

            case ConsoleKey.Tab when state.search_enabled:
                state.search_mode = !state.search_mode;
                state.search_query = "";
                if (!state.search_mode)
                {
                    update_filtered_indices(state);
                }

                return KeyResult.need_render;

            default:
                if (state.search_enabled && !char.IsControl(key.KeyChar))
                {
                    state.search_mode = true;
                    state.search_query += key.KeyChar;
                    update_filtered_indices(state);
                    state.selected_index = 0;
                    return KeyResult.need_render;
                }

                return KeyResult.none;
        }
    }

    private static KeyResult handle_search_key(SelectState state, ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                state.search_mode = false;
                if (state.filtered_indices.Count > 0)
                {
                    return KeyResult.confirm;
                }

                state.search_query = "";
                update_filtered_indices(state);
                return KeyResult.need_render;

            case ConsoleKey.Escape:
                state.search_mode = false;
                state.search_query = "";
                update_filtered_indices(state);
                return KeyResult.need_render;

            case ConsoleKey.Backspace:
                if (state.search_query.Length > 0)
                {
                    state.search_query = state.search_query[..^1];
                    update_filtered_indices(state);
                    state.selected_index = 0;
                }

                return KeyResult.need_render;

            case ConsoleKey.UpArrow:
                state.selected_index = System.Math.Max(0, state.selected_index - 1);
                return KeyResult.need_render;

            case ConsoleKey.DownArrow:
                state.selected_index = System.Math.Min(state.filtered_indices.Count - 1, state.selected_index + 1);
                return KeyResult.need_render;

            default:
                if (!char.IsControl(key.KeyChar))
                {
                    state.search_query += key.KeyChar;
                    update_filtered_indices(state);
                    state.selected_index = 0;
                    return KeyResult.need_render;
                }

                return KeyResult.none;
        }
    }

    private static void toggle_current(SelectState state)
    {
        var realIndex = state.filtered_indices[state.selected_index];
        if (state.checked_indices.Contains(realIndex))
        {
            state.checked_indices.Remove(realIndex);
        }
        else
        {
            if (state.max_selected > 0 && state.checked_indices.Count >= state.max_selected)
            {
                state.error_message = $"最多选择 {state.max_selected} 项";
                return;
            }

            state.checked_indices.Add(realIndex);
        }
    }

    private static void toggle_all(SelectState state)
    {
        if (state.checked_indices.Count == state.items.Count)
        {
            state.checked_indices.Clear();
        }
        else
        {
            for (var i = 0; i < state.items.Count; i++)
            {
                state.checked_indices.Add(i);
            }
        }
    }

    private static void update_filtered_indices(SelectState state)
    {
        if (string.IsNullOrEmpty(state.search_query))
        {
            state.filtered_indices = Enumerable.Range(0, state.items.Count).ToList();
        }
        else
        {
            var query = state.search_query.ToLowerInvariant();
            state.filtered_indices = state.items
                .Select((item, idx) => (item, idx))
                .Where(x => x.item.ToLowerInvariant().Contains(query))
                .Select(x => x.idx)
                .ToList();
        }
    }

    private static void render_full(SelectState state, int cursorTop)
    {
        try
        {
            var totalLines = state.page_size + 3;
            System.Console.SetCursorPosition(0, cursorTop);
            for (var i = 0; i < totalLines; i++)
            {
                System.Console.Write(AnsiStyling.clear_line());
            }

            System.Console.SetCursorPosition(0, cursorTop);
        }
        catch (IOException)
        {
            return;
        }

        if (state.search_mode)
        {
            System.Console.WriteLine($"  搜索: {state.search_query}_");
        }
        else
        {
            var promptText = state.is_multi ? $"? {state.prompt} (空格选择，回车确认)" : $"? {state.prompt}";
            System.Console.WriteLine(promptText);
        }

        var start = System.Math.Max(0, state.selected_index - state.page_size / 2);
        var end = System.Math.Min(start + state.page_size, state.filtered_indices.Count);
        start = System.Math.Max(0, end - state.page_size);

        for (var i = start; i < end; i++)
        {
            var realIndex = state.filtered_indices[i];
            var item = state.items[realIndex];
            var isSelected = i == state.selected_index;

            if (state.is_multi)
            {
                var isChecked = state.checked_indices.Contains(realIndex);
                var check = isChecked ? "◉" : "○";
                var cursor = isSelected ? "❯" : " ";

                if (isSelected && TerminalCapability.should_use_ansi)
                {
                    System.Console.WriteLine($"  {cursor} {check} \u001b[36m{item}\u001b[0m");
                }
                else
                {
                    System.Console.WriteLine($"  {cursor} {check} {item}");
                }
            }
            else
            {
                var marker = isSelected ? "❯" : " ";
                var radio = isSelected ? "●" : "○";

                if (isSelected && TerminalCapability.should_use_ansi)
                {
                    System.Console.WriteLine($"  {marker} {radio} \u001b[36m{item}\u001b[0m");
                }
                else
                {
                    System.Console.WriteLine($"  {marker} {radio} {item}");
                }
            }
        }

        if (state.filtered_indices.Count == 0)
        {
            System.Console.WriteLine("  （无匹配项）");
        }

        if (state.error_message is not null)
        {
            if (TerminalCapability.should_use_ansi)
            {
                System.Console.WriteLine($"  \u001b[31m✗ {state.error_message}\u001b[0m");
            }
            else
            {
                System.Console.WriteLine($"  ✗ {state.error_message}");
            }
        }
        else if (state.show_instructions && !state.search_mode)
        {
            var instructions = state.is_multi
                ? "↑↓ 移动 | 空格 选择 | Ctrl+A 全选 | 回车 确认 | Esc 取消"
                : "↑↓ 移动 | 回车 确认 | Esc 取消";

            if (state.search_enabled)
            {
                instructions += " | Tab 搜索";
            }

            if (TerminalCapability.should_use_ansi)
            {
                System.Console.WriteLine($"  \u001b[2m{instructions}\u001b[0m");
            }
            else
            {
                System.Console.WriteLine($"  {instructions}");
            }
        }
    }

    private static void render_final(SelectState state, int cursorTop)
    {
        try
        {
            var totalLines = state.page_size + 3;
            System.Console.SetCursorPosition(0, cursorTop);
            for (var i = 0; i < totalLines; i++)
            {
                System.Console.Write(AnsiStyling.clear_line());
            }

            System.Console.SetCursorPosition(0, cursorTop);
        }
        catch (IOException)
        {
            return;
        }

        if (state.is_multi)
        {
            var selected = state.checked_indices
                .OrderBy(i => i)
                .Select(i => state.items[i]);
            var summary = string.Join(", ", selected);
            System.Console.WriteLine($"? {state.prompt}: {summary}");
        }
        else
        {
            var selected = state.items[state.filtered_indices[state.selected_index]];
            System.Console.WriteLine($"? {state.prompt}: {selected}");
        }
    }

    private static void render_cancel(SelectState state, int cursorTop)
    {
        try
        {
            var totalLines = state.page_size + 3;
            System.Console.SetCursorPosition(0, cursorTop);
            for (var i = 0; i < totalLines; i++)
            {
                System.Console.Write(AnsiStyling.clear_line());
            }

            System.Console.SetCursorPosition(0, cursorTop);
        }
        catch (IOException)
        {
            return;
        }

        System.Console.WriteLine($"? {state.prompt}: （已取消）");
    }

    private enum KeyResult
    {
        none,
        need_render,
        confirm,
        cancel
    }

    private sealed class SelectState
    {
        public string prompt = "";
        public IReadOnlyList<string> items = [];
        public int selected_index;
        public int page_size = 10;
        public bool is_multi;
        public int min_selected = 1;
        public int max_selected;
        public HashSet<int> checked_indices = [];
        public bool search_enabled;
        public bool search_mode;
        public string search_query = "";
        public List<int> filtered_indices = [];
        public bool show_instructions = true;
        public string? error_message;
    }
}
