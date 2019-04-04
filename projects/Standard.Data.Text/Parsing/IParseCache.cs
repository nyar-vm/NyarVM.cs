using Std.Data.Text.Syntax;

namespace Std.Data.Text.Parsing;

/// <summary>
///     语法分析缓存接口，为增量语法分析提供缓存能力
/// </summary>
public interface IParseCache
{
    /// <summary>
    ///     获取指定源码标识的已缓存语法树
    /// </summary>
    bool try_get(string sourceId, out SyntaxTree tree);

    /// <summary>
    ///     缓存语法树
    /// </summary>
    void set(string sourceId, SyntaxTree tree);

    /// <summary>
    ///     失效指定源码标识的缓存
    /// </summary>
    void invalidate(string sourceId);

    /// <summary>
    ///     清空所有缓存
    /// </summary>
    void clear();
}