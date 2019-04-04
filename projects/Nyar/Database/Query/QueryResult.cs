using Std.Data.Text.Diagnostics;

namespace Nyar.Database.Query;

/// <summary>
///     查询结果包装
/// </summary>
/// <typeparam name="T">结果值类型</typeparam>
public sealed class QueryResult<T>
{
    /// <summary>
    ///     查询结果值
    /// </summary>
    public T? value { get; init; }

    /// <summary>
    ///     查询过程中产生的诊断信息
    /// </summary>
    public IReadOnlyList<Diagnostic> diagnostics { get; init; } = [];

    /// <summary>
    ///     结果计算时间（UTC）
    /// </summary>
    public DateTime computed_at { get; init; }

    /// <summary>
    ///     输入参数的哈希值，用于判断缓存是否有效
    /// </summary>
    public int input_hash { get; init; }

    /// <summary>
    ///     结果是否有效
    /// </summary>
    public bool is_valid => value is not null || diagnostics.Count > 0;

    /// <summary>
    ///     创建成功的查询结果
    /// </summary>
    /// <param name="value">结果值。</param>
    /// <param name="inputHash">输入哈希。</param>
    /// <returns>查询结果。</returns>
    public static QueryResult<T> success(T value, int inputHash)
    {
        return new QueryResult<T>
        {
            value = value,
            diagnostics = [],
            computed_at = DateTime.UtcNow,
            input_hash = inputHash
        };
    }

    /// <summary>
    ///     创建带诊断的查询结果
    /// </summary>
    /// <param name="value">结果值。</param>
    /// <param name="diagnostics">诊断信息。</param>
    /// <param name="inputHash">输入哈希。</param>
    /// <returns>查询结果。</returns>
    public static QueryResult<T> with_diagnostics(T value, IReadOnlyList<Diagnostic> diagnostics, int inputHash)
    {
        return new QueryResult<T>
        {
            value = value,
            diagnostics = diagnostics,
            computed_at = DateTime.UtcNow,
            input_hash = inputHash
        };
    }
}