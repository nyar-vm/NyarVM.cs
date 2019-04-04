namespace Nyar.ObjectAlgebra;

/// <summary>
///     OA 术语包装类型。
///     在方言接口定义中，方法参数和返回值使用 Term{T} 表示类型化的 IR 节点。
///     Source Generator 解析 Term{T} 参数，生成泛型工厂接口时将 Term{T} 替换为 E。
///     此类型仅用于编译时标记，运行时不承载实际数据。
/// </summary>
/// <typeparam name="T">表达式携带的类型信息</typeparam>
public readonly struct Term<T>
{
    /// <summary>
    ///     初始化 Term 实例
    /// </summary>
    /// <param name="value">表示值。</param>
    public Term(object? value)
    {
        this.value = value;
    }

    /// <summary>
    ///     表达式的表示值（仅用于编译时标记，运行时不使用）
    /// </summary>
    public object? value { get; }
}