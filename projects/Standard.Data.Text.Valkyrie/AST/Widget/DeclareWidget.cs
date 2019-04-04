using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST.Widget;

/// <summary>
///     Widget UI 组件声明，用于声明式 UI 构建
/// </summary>
/// <para>示例：</para>
/// <code>
/// widget Button {
///     var text: utf8;
///     var onClick: fn() -> void;
/// 
///     fn render() -> View {
///         View {
///             Text { text: self.text }
///         }
///     }
/// }
/// </code>
public sealed record DeclareWidget : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     Widget 属性列表（对外暴露的可配置项）
    /// </summary>
    public IReadOnlyList<DeclareObjectField> properties { get; init; } = [];

    /// <summary>
    ///     渲染方法，为 <c>null</c> 时使用默认渲染
    /// </summary>
    public DeclareMicro? render_method { get; init; }

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     Widget 名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}