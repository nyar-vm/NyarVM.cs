namespace Std.Terminal.Controls;

/// <summary>
///     树节点数据模型
/// </summary>
/// <typeparam name="T">节点数据类型</typeparam>
public sealed class TreeNode<T>
{
    /// <summary>
    ///     创建树节点
    /// </summary>
    /// <param name="data">节点数据</param>
    /// <param name="text">显示文本</param>
    public TreeNode(T data, string text)
    {
        this.data = data;
        this.text = text;
    }

    /// <summary>
    ///     节点 ID
    /// </summary>
    public string id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     节点数据
    /// </summary>
    public T data { get; set; }

    /// <summary>
    ///     节点显示文本
    /// </summary>
    public string text { get; set; }

    /// <summary>
    ///     子节点集合
    /// </summary>
    public List<TreeNode<T>> children { get; set; } = [];

    /// <summary>
    ///     是否展开
    /// </summary>
    public bool is_expanded { get; set; }

    /// <summary>
    ///     父节点
    /// </summary>
    public TreeNode<T>? parent { get; set; }

    /// <summary>
    ///     添加子节点
    /// </summary>
    /// <param name="child">子节点</param>
    public TreeNode<T> add_child(TreeNode<T> child)
    {
        child.parent = this;
        children.Add(child);
        return this;
    }

    /// <summary>
    ///     添加子节点
    /// </summary>
    /// <param name="data">节点数据</param>
    /// <param name="text">显示文本</param>
    public TreeNode<T> add_child(T data, string text)
    {
        return add_child(new TreeNode<T>(data, text));
    }
}