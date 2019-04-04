namespace Nyar.Database.Index;

/// <summary>
///     符号可访问性
/// </summary>
public enum SymbolAccessibility
{
    /// <summary>
    ///     未知
    /// </summary>
    unknown,

    /// <summary>
    ///     公共
    /// </summary>
    @public,

    /// <summary>
    ///     私有
    /// </summary>
    @private,

    /// <summary>
    ///     受保护
    /// </summary>
    @protected,

    /// <summary>
    ///     内部
    /// </summary>
    @internal
}