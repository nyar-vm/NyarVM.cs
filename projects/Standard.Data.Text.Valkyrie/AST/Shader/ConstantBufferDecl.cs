using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     常量缓冲区声明，用于 Shader 中的 Uniform 常量数据定义
/// </summary>
/// <para>示例：</para>
/// <code>
/// cbuffer CameraData @group(0) @binding(0) {
///     var viewMatrix: mat4;
///     var projMatrix: mat4;
/// }
/// </code>
public sealed record ConstantBufferDecl : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     CBuffer 名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     CBuffer 包含的字段列表
    /// </summary>
    public IReadOnlyList<DeclareObjectField> fields { get; init; } = [];

    /// <summary>
    ///     绑定组索引
    /// </summary>
    public int? group { get; init; }

    /// <summary>
    ///     绑定槽索引
    /// </summary>
    public int? binding { get; init; }
}