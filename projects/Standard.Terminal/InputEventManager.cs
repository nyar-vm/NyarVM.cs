using System.Reflection;
using Std.Terminal.Controls;

namespace Std.Terminal;

/// <summary>
///     输入事件管理器，处理键盘输入并分派给当前焦点控件
/// </summary>
public static class InputEventManager
{
    /// <summary>
    ///     处理输入循环，调度给当前活跃控件
    /// </summary>
    /// <param name="focusedControl">当前焦点控件</param>
    /// <param name="tabStopControls">可切换的控件列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task process_input(View focusedControl, List<View> tabStopControls,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!System.Console.KeyAvailable)
            {
                await Task.Delay(16, cancellationToken);
                continue;
            }

            var key = System.Console.ReadKey(true);
            var handled = dispatch_key(focusedControl, tabStopControls, key);

            if (!handled)
                if (key.Key == ConsoleKey.Escape)
                    return;
        }
    }

    /// <summary>
    ///     将键盘输入分派到焦点控件
    /// </summary>
    /// <param name="focusedControl">焦点控件</param>
    /// <param name="tabStopControls">Tab 可切换控件</param>
    /// <param name="key">键盘输入</param>
    /// <returns>是否已处理</returns>
    public static bool dispatch_key(View focusedControl, List<View> tabStopControls, ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Tab)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                focus_previous(focusedControl, tabStopControls);
            else
                focus_next(focusedControl, tabStopControls);

            return true;
        }

        if (focusedControl is TextBox textBox) return dispatch_text_box(textBox, key);

        if (focusedControl is CheckBox checkBox) return dispatch_check_box(checkBox, key);

        if (focusedControl is Button button) return dispatch_button(button, key);

        if (is_list_view(focusedControl)) return dispatch_list_view(focusedControl, key);

        return focusedControl.OnKeyDown(key);
    }

    private static bool is_list_view(View view)
    {
        var type = view.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListView<>);
    }

    private static bool dispatch_text_box(TextBox textBox, ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Backspace:
                textBox.delete_char_before();
                return true;
            case ConsoleKey.LeftArrow:
                textBox.move_cursor_left();
                return true;
            case ConsoleKey.RightArrow:
                textBox.move_cursor_right();
                return true;
            case ConsoleKey.Home:
                textBox.move_cursor_home();
                return true;
            case ConsoleKey.End:
                textBox.move_cursor_end();
                return true;
            case ConsoleKey.Tab:
                return false;
            default:
                if (!char.IsControl(key.KeyChar))
                {
                    textBox.insert_char(key.KeyChar);
                    return true;
                }

                return false;
        }
    }

    private static bool dispatch_check_box(CheckBox checkBox, ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Spacebar or ConsoleKey.Enter)
        {
            checkBox.toggle();
            return true;
        }

        return false;
    }

    private static bool dispatch_button(Button button, ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            button.click();
            return true;
        }

        return false;
    }

    private static bool dispatch_list_view(View listView, ConsoleKeyInfo key)
    {
        var type = listView.GetType();

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                type.GetMethod("MoveSelectionUp", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?.Invoke(listView, null);
                return true;
            case ConsoleKey.DownArrow:
                type.GetMethod("MoveSelectionDown",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(listView, null);
                return true;
            case ConsoleKey.Enter:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     将焦点移到下一个控件
    /// </summary>
    public static void focus_next(View current, List<View> controls)
    {
        var currentIndex = controls.IndexOf(current);
        if (currentIndex < 0) return;

        var nextIndex = (currentIndex + 1) % controls.Count;
        current.set_focused(false);
        controls[nextIndex].set_focused(true);
    }

    /// <summary>
    ///     将焦点移到上一个控件
    /// </summary>
    public static void focus_previous(View current, List<View> controls)
    {
        var currentIndex = controls.IndexOf(current);
        if (currentIndex < 0) return;

        var prevIndex = currentIndex == 0 ? controls.Count - 1 : currentIndex - 1;
        current.set_focused(false);
        controls[prevIndex].set_focused(true);
    }
}