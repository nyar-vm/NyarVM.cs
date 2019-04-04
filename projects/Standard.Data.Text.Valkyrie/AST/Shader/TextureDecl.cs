using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     Shader 纹理声明，定义着色器中使用的纹理资源绑定
/// </summary>
/// <para>示例：</para>
/// <code>
/// @uniform var albedoMap: texture2D @group(1) @binding(0);
/// @uniform var normalMap: texture2D @group(1) @binding(1);
/// </code>
public sealed record TextureDecl : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     纹理变量名    ///
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     纹理类型（如 <c>texture2d&lt;f32&gt;</c>）
    public TypeNode texture_type { get; init; } = null!;

    /// <summary>
    ///     绑定组索引    ///
    /// </summary>
    public int? group { get; init; }

    /// <summary>
    ///     绑定槽索引    ///
    /// </summary>
    public int? binding { get; init; }
}