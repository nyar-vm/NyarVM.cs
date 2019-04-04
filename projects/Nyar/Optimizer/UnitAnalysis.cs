using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.Optimizer;

/// <summary>
///     空分析，不维护任何分析数据
/// </summary>
public class UnitAnalysis : IAnalysis<AlgebraNode>
{
    /// <summary>
    ///     分析数据类型
    /// </summary>
    public Type data_type => typeof(Unit);

    /// <summary>
    ///     为新节点创建空分析数据
    /// </summary>
    /// <param name="egraph">所属的 E-Graph。</param>
    /// <param name="enode">新节点。</param>
    /// <returns>空分析数据。</returns>
    public object make(EGraph<AlgebraNode> egraph, AlgebraNode enode)
    {
        return Unit.instance;
    }

    /// <summary>
    ///     合并空分析数据，始终无变化
    /// </summary>
    /// <param name="to">目标数据。</param>
    /// <param name="from">源数据。</param>
    /// <returns>是否发生了变化。</returns>
    public bool merge(ref object to, object from)
    {
        return false;
    }

    /// <summary>
    ///     创建默认的空分析数据
    /// </summary>
    /// <returns>空分析数据。</returns>
    public object make_default()
    {
        return Unit.instance;
    }

    /// <summary>
    ///     判断两个空分析数据是否兼容，始终返回 true
    /// </summary>
    /// <param name="data1">第一个分析数据。</param>
    /// <param name="data2">第二个分析数据。</param>
    /// <returns>是否兼容。</returns>
    public bool is_compatible(object data1, object data2)
    {
        return true;
    }
}