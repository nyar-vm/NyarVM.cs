namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     张量维度描述，包含维度名称和大小
/// </summary>
/// <para>示例：</para>
/// <code>
/// var image: Tensor[f32; 28, 28, 1];
/// //                   ^^  ^^  ^
/// //   TensorDimension { Name = "height", Size = 28 }
/// //   TensorDimension { Name = "width", Size = 28 }
/// //   TensorDimension { Name = "channels", Size = 1 }
/// </code>
public sealed record TensorDimension
{
    /// <summary>
    ///     维度名称（如 <c>"height"</c>、<c>"channels"</c>）
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     维度大小
    /// </summary>
    public int size { get; init; }
}