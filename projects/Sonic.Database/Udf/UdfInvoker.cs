using Olympus.Athena.Core;

namespace Olympus.Athena.Udf;

#region UdfInvoker 调用器

/// <summary>
///     UDF 调用器，提供标量 UDF 和聚合 UDF 的统一调用入口
/// </summary>
public static class UdfInvoker
{
    #region 标量调用

    /// <summary>
    ///     调用标量 UDF，捕获执行过程中的异常并转换为错误 <see cref="DataValue" />
    /// </summary>
    /// <param name="registry">UDF 注册表</param>
    /// <param name="name">UDF 名称</param>
    /// <param name="args">输入参数列表</param>
    /// <returns>UDF 计算结果，发生异常时返回包含异常消息的字符串 <see cref="DataValue" /></returns>
    public static DataValue InvokeScalar(UdfRegistry registry, string name, ReadOnlySpan<DataValue> args)
    {
        var udf = registry.GetScalar(name);
        if (udf is null) return $"未找到标量 UDF：{name}";

        try
        {
            return udf.Execute(args);
        }
        catch (Exception ex)
        {
            return $"标量 UDF [{name}] 执行异常：{ex.Message}";
        }
    }

    #endregion

    #region 聚合调用

    /// <summary>
    ///     调用聚合 UDF，对输入值序列进行累积计算并返回聚合结果
    /// </summary>
    /// <param name="registry">UDF 注册表</param>
    /// <param name="name">UDF 名称</param>
    /// <param name="values">输入值序列</param>
    /// <returns>聚合计算结果，发生异常时返回包含异常消息的字符串 <see cref="DataValue" /></returns>
    public static DataValue InvokeAggregate(UdfRegistry registry, string name, IEnumerable<DataValue> values)
    {
        var udf = registry.GetAggregate(name);
        if (udf is null) return $"未找到聚合 UDF：{name}";

        try
        {
            udf.Init();
            foreach (var value in values) udf.Accumulate(value);

            return udf.Finalize();
        }
        catch (Exception ex)
        {
            return $"聚合 UDF [{name}] 执行异常：{ex.Message}";
        }
    }

    #endregion
}

#endregion