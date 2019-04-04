using Olympus.Athena.Core;

namespace Olympus.Athena.Udf;

/// <summary>
///     标量 UDF 接口，对传入的参数行计算并返回单个结果值
/// </summary>
public interface IScalarUdf
{
    /// <summary>
    ///     获取 UDF 名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     执行标量 UDF，对传入的参数进行计算并返回结果
    /// </summary>
    /// <param name="arguments">输入参数列表</param>
    /// <returns>计算结果</returns>
    DataValue Execute(ReadOnlySpan<DataValue> arguments);
}