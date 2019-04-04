namespace Std.App.Client;

/// <summary>
///     组件树，描述 UI 结构。根节点表示整个应用界面。
/// </summary>
public sealed class ComponentTree
{
    private IComponent? _root;

    /// <summary>
    ///     组件树的根节点
    /// </summary>
    public IComponent? Root
    {
        get => _root;
        set
        {
            _root = value;
            OnRootChanged?.Invoke(value);
        }
    }

    /// <summary>
    ///     根节点变更事件
    /// </summary>
    public event Action<IComponent?>? OnRootChanged;

    /// <summary>
    ///     从根节点开始深度优先遍历所有组件
    /// </summary>
    public IEnumerable<IComponent> Traverse()
    {
        if (_root == null) yield break;

        var stack = new Stack<IComponent>();
        stack.Push(_root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            for (var i = current.Children.Count - 1; i >= 0; i--) stack.Push(current.Children[i]);
        }
    }

    /// <summary>
    ///     根据 ID 查找组件
    /// </summary>
    /// <param name="id">组件 ID</param>
    public IComponent? FindById(string id)
    {
        foreach (var component in Traverse())
            if (component.Id == id)
                return component;

        return null;
    }
}