using Nyar.IR.Intent;

namespace Nyar.EGraph;

/// <summary>
///     E-Graph 中的等价类，包含一组等价的节点
/// </summary>
/// <typeparam name="T">语言节点类型</typeparam>
/// <typeparam name="TData">分析数据类型</typeparam>
public class EClass<T, TData> where T : ILanguage<T>
{
    /// <summary>
    ///     创建等价类
    /// </summary>
    /// <param name="id">等价类标识符。</param>
    /// <param name="data">初始分析数据。</param>
    public EClass(Id id, TData data)
    {
        this.id = id;
        nodes = [];
        this.data = data;
    }

    /// <summary>
    ///     等价类标识符
    /// </summary>
    public Id id { get; }

    /// <summary>
    ///     等价类中的所有节点
    /// </summary>
    public List<T> nodes { get; }

    /// <summary>
    ///     分析数据
    /// </summary>
    public TData data { get; set; }
}