namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     张量类型表达式，表示 <c>Tensor[element_type; dim1, dim2, ...]</c> 形式的类型标注
/// </summary>
/// <para>示例：</para>
/// <code>
/// var image: Tensor[f32; 28, 28, 1];    // ElementType = "f32", Dimensions = [(28), (28), (1)]
/// var seq: Tensor[i64; 512];             // ElementType = "i64", Dimensions = [(512)]
/// </code>
public sealed record TensorTypeExpr : ValkyrieNode
{
    /// <summary>
    ///     元素类型（如 <c>"f32"</c>、<c>"f64"</c>、<c>"i32"</c>）
    /// </summary>
    public string element_type { get; init; } = string.Empty;

    /// <summary>
    ///     各维度定义列表
    /// </summary>
    public IReadOnlyList<TensorDimension> dimensions { get; init; } = [];
}