namespace Nyar.EGraph;

/// <summary>
///     E-Graph 分析接口，定义分析数据如何创建和合并
/// </summary>
/// <typeparam name="T">语言节点类型</typeparam>
public interface IAnalysis<T> where T : ILanguage<T>
{
    /// <summary>
    ///     分析数据类型
    /// </summary>
    Type data_type { get; }

    /// <summary>
    ///     为新节点创建分析数据
    /// </summary>
    /// <param name="egraph">所属的 E-Graph。</param>
    /// <param name="enode">新节点。</param>
    /// <returns>分析数据。</returns>
    object make(EGraph<T> egraph, T enode);

    /// <summary>
    ///     合并两个分析数据，返回是否发生了变化
    /// </summary>
    /// <param name="to">目标数据，合并结果写入此处。</param>
    /// <param name="from">源数据。</param>
    /// <returns>是否发生了变化。</returns>
    bool merge(ref object to, object from);

    /// <summary>
    ///     创建默认的分析数据
    /// </summary>
    /// <returns>默认分析数据。</returns>
    object make_default();

    /// <summary>
    ///     判断两个分析数据是否兼容，用于合并冲突检测
    /// </summary>
    /// <param name="data1">第一个分析数据。</param>
    /// <param name="data2">第二个分析数据。</param>
    /// <returns>是否兼容。</returns>
    bool is_compatible(object data1, object data2);
}