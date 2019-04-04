using Core.Console;

namespace Std.Terminal.Components;

/// <summary>
///     选择提示组件，显示选项列表并让用户选择一项�?///
/// </summary>
/// <typeparam name="T">选项值的类型�?/typeparam>
public sealed class SelectionPrompt<T>
{
    private readonly IConsole _console;
    private readonly List<SelectionItem> _items;

    /// <summary>
    ///     初始�?<see cref="SelectionPrompt" /> 的新实例�?    ///
    /// </summary>
    /// <param name="console">控制台实例�?/param>
    public SelectionPrompt(IConsole console)
    {
        _console = console;
        _items = [];
    }

    /// <summary>
    ///     获取或设置提示消息�?    ///
    /// </summary>
    public string message { get; set; } = "请选择:";

    /// <summary>
    ///     添加一个选项�?    ///
    /// </summary>
    /// <param name="value">
    ///     选项值�?/param>
    ///     <param name="display">显示文本�?/param>
    public void add_choice(T value, string display)
    {
        _items.Add(new SelectionItem(value, display));
    }

    /// <summary>
    ///     显示选项列表并获取用户选择�?    ///
    /// </summary>
    /// <returns>用户选择的值�?/returns>
    public T show()
    {
        _console.@out.WriteLine(message);
        for (var i = 0; i < _items.Count; i++) _console.@out.WriteLine($"  [{i + 1}] {_items[i].display}");

        _console.@out.Write("请输入编�? ");
        while (true)
        {
            var input = _console.@in.ReadLine()?.Trim();
            if (int.TryParse(input, out var index) && index >= 1 && index <= _items.Count)
                return _items[index - 1].value;

            _console.@out.Write($"无效输入，请输入 1-{_items.Count} 之间的编�? ");
        }
    }

    private readonly struct SelectionItem
    {
        public T value { get; }
        public string display { get; }

        public SelectionItem(T value, string display)
        {
            this.value = value;
            this.display = display;
        }
    }
}