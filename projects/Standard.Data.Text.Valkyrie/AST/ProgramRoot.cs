using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.AST;

/// <summary>
///     编译单元 —— 单个源文件的 AST 根节点
/// </summary>
/// <para>每个源文件（<c>.v</c> / <c>.script</c> / <c>.schema</c>）解析后生成一个 <see cref="ProgramRoot" />，包含所有顶层声明</para>
/// <para>示例源文件结构：</para>
/// <code>
/// namespace package.point
/// class Point { x: f32; y: f32; }
/// micro distance(a: Point, b: Point) -> f32 { ... }
/// </code>
public record ProgramRoot : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public ProgramRoot()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public ProgramRoot(IReadOnlyList<ValkyrieNode> declarations, string filePath, TextSpan span)
    {
        this.declarations = declarations;
        file_path = filePath;
        this.span = span;
    }

    /// <summary>
    ///     顶层声明列表（结构体、函数、类、枚举等）
    /// </summary>
    public IReadOnlyList<ValkyrieNode> declarations { get; init; } = [];

    /// <summary>
    ///     源文件路径
    /// </summary>
    public string file_path { get; init; } = string.Empty;
}