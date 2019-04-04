using Std.Data.Text.Syntax;

namespace Std.Data.Text.Lexing;

/// <summary>
///     词法分析缓存接口，为增量词法分析提供缓存能力
/// </summary>
public interface ILexerCache
{
    /// <summary>
    ///     获取指定源码文本的已缓存 Token 列表
    /// </summary>
    bool try_get(string sourceId, out IReadOnlyList<GreenLeafNode> tokens);

    /// <summary>
    ///     缓存指定源码文本的 Token 列表
    /// </summary>
    void set(string sourceId, IReadOnlyList<GreenLeafNode> tokens);

    /// <summary>
    ///     失效指定源码文本的缓存
    /// </summary>
    void invalidate(string sourceId);
}