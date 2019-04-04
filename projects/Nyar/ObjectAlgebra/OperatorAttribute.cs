namespace Nyar.ObjectAlgebra;

/// <summary>
///     标记方言接口中的一个操作（语言构造）。
///     每个被标记的方法对应方言中的一个 IR 节点类型。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OperatorAttribute : Attribute
{
    /// <summary>
    ///     初始化 OpAttribute 实例
    /// </summary>
    /// <param name="name">操作名称。</param>
    public OperatorAttribute(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     操作名称（如 "lit"、"add"、"mul"）
    /// </summary>
    public string name { get; }
}