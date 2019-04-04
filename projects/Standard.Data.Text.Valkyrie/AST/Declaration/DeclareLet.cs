using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     变量声明语句
/// </summary>
/// <para>示例：</para>
/// <code>
/// let x: i32 = 42;
/// let mut y = 0;            // IsMutable = true
/// let z;                    // VarType = null, Initializer = null
/// let (a, b) = (1, 2);      // pattern = TuplePattern
/// </code>
public sealed record DeclareLet : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     是否为可变变量（<c>var mut</c>）
    /// </summary>
    public bool is_mutable { get; init; }

    /// <summary>
    ///     变量类型注解，可为 <c>null</c> 表示类型推断
    /// </summary>
    public TypeNode? var_type { get; init; }

    /// <summary>
    ///     初始化表达式，可为 <c>null</c>
    /// </summary>
    public ValkyrieNode? initializer { get; init; }

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     变量名称（简单标识符模式）
    /// </summary>
    public IdentifierNode? name { get; init; } = new();

    /// <summary>
    ///     变量模式（支持解构，如元组、结构体等）
    /// </summary>
    public PatternNode? pattern { get; init; }
}