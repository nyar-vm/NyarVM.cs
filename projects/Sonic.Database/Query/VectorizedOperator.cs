namespace Olympus.Athena.Query;

#region VectorizedOperator 向量化算子供类

/// <summary>
///     向量化执行算子的抽象基类，支持流水线式链接子算子
/// </summary>
public abstract class VectorizedOperator
{
    /// <summary>
    ///     创建向量化算子
    /// </summary>
    protected VectorizedOperator()
    {
        Children = [];
    }

    /// <summary>
    ///     子算子列表，构成执行流水线
    /// </summary>
    public List<VectorizedOperator> Children { get; }

    /// <summary>
    ///     执行当前算子
    /// </summary>
    public abstract void Execute();

    /// <summary>
    ///     添加子算子
    /// </summary>
    /// <param name="op">子算子</param>
    public void AddChild(VectorizedOperator op)
    {
        Children.Add(op);
    }
}

#endregion