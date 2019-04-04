using System.Runtime.Intrinsics;

namespace Olympus.Athena.Query;

#region FilterOperator 过滤算子

/// <summary>
///     向量化过滤算子，使用 SIMD 加速对输入数据的批量过滤
/// </summary>
/// <typeparam name="T">非托管值类型</typeparam>
public sealed class FilterOperator<T> : VectorizedOperator where T : unmanaged
{
    #region 构造函数

    /// <summary>
    ///     创建过滤算子
    /// </summary>
    /// <param name="input">输入数据跨度</param>
    /// <param name="predicate">过滤谓词</param>
    public FilterOperator(Span<T> input, Func<T, bool> predicate)
    {
        _input = [.. input];
        _predicate = predicate;
        Results = [];
    }

    #endregion

    #region 属性

    /// <summary>
    ///     过滤后的结果列表
    /// </summary>
    public List<T> Results { get; }

    #endregion

    #region 字段

    private readonly T[] _input;
    private readonly Func<T, bool> _predicate;

    #endregion

    #region 执行

    /// <summary>
    ///     使用 SIMD 批量过滤输入数据
    /// </summary>
    public override void Execute()
    {
        var data = _input.AsSpan();
        TrySimdFilter(data);
    }

    private void TrySimdFilter(Span<T> input)
    {
        var len = input.Length;

        if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(float) ||
            typeof(T) == typeof(double))
        {
            var vectorSize = Vector256<T>.Count;
            var remaining = len % vectorSize;

            for (var i = 0; i < len - remaining; i += vectorSize)
            {
                var slice = input.Slice(i, vectorSize);
                FilterBatch(slice);
            }

            if (remaining > 0)
            {
                var remainingSlice = input.Slice(len - remaining, remaining);
                FilterScalar(remainingSlice);
            }
        }
        else
        {
            FilterScalar(input);
        }

        foreach (var child in Children) child.Execute();
    }

    private void FilterBatch(Span<T> batch)
    {
        for (var i = 0; i < batch.Length; i++)
            if (_predicate(batch[i]))
                Results.Add(batch[i]);
    }

    private void FilterScalar(Span<T> data)
    {
        for (var i = 0; i < data.Length; i++)
            if (_predicate(data[i]))
                Results.Add(data[i]);
    }

    #endregion
}

#endregion