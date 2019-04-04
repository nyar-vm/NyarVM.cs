using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.Parser;

/// <summary>
///     TokenStream 的 Valkyrie 专用扩展方法，提供 ValkyrieTokenKind 的便捷操作
/// </summary>
public static class ValkyrieTokenStreamExtensions
{
    /// <summary>
    ///     向前看指定偏移量 Token 的 ValkyrieTokenKind
    /// </summary>
    public static ValkyrieTokenKind peek_valkyrie_kind(this TokenStream source, int offset = 0)
    {
        return (ValkyrieTokenKind)source.peek_kind(offset).value;
    }

    /// <summary>
    ///     检查当前 Token 是否为指定 ValkyrieTokenKind
    /// </summary>
    public static bool check(this TokenStream source, ValkyrieTokenKind kind)
    {
        return source.check(kind.to_node_kind());
    }

    /// <summary>
    ///     检查指定偏移的 Token 是否为指定 ValkyrieTokenKind
    /// </summary>
    public static bool check(this TokenStream source, ValkyrieTokenKind kind, int offset)
    {
        return source.check(kind.to_node_kind(), offset);
    }

    /// <summary>
    ///     如果当前 Kind 匹配则消耗它并返回 true，否则返回 false
    /// </summary>
    public static bool match(this TokenStream source, ValkyrieTokenKind kind)
    {
        return source.match(kind.to_node_kind());
    }

    /// <summary>
    ///     期望当前 Token 是指定的 ValkyrieTokenKind，否则报告诊断并抛出异常
    /// </summary>
    public static GreenLeafNode expect(this TokenStream source, ValkyrieTokenKind kind)
    {
        return source.expect(kind.to_node_kind());
    }

    /// <summary>
    ///     获取当前 Token 的 ValkyrieTokenKind
    /// </summary>
    public static ValkyrieTokenKind current_kind(this TokenStream source)
    {
        return (ValkyrieTokenKind)source.current.kind.value;
    }
}