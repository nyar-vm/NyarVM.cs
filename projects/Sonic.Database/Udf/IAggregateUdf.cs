using Olympus.Athena.Core;

namespace Olympus.Athena.Udf;

/// <summary>
///     聚合 UDF 接口，对多行输入进行累积计算并返回单个聚合结果
/// </summary>
public interface IAggregateUdf
{
    /// <summary>
    ///     获取 UDF 名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     初始化聚合状态，在开始聚合之前调用
    /// </summary>
    void Init();

    /// <summary>
    ///     累加一个值到聚合状态中
    /// </summary>
    /// <param name="value">待累加的值</param>
    void Accumulate(DataValue value);

    /// <summary>
    ///     完成聚合计算并返回最终结果
    /// </summary>
    /// <returns>聚合结果</returns>
    DataValue Finalize();
}