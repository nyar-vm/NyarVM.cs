using Core.Command;

namespace Std.Command;

/// <summary>
///     默认的命令本地化器实现，直接返回回退值，不做任何翻译�?///
/// </summary>
public sealed class PassthroughLocalizer : ILocalizer
{
    /// <summary>
    ///     返回回退值，不做任何本地化处理�?    ///
    /// </summary>
    /// <param name="key">
    ///     资源键（忽略）�?/param>
    ///     <param name="fallback">
    ///         回退值�?/param>
    ///         <returns>回退值，若回退值为 null 则返回资源键�?/returns>
    public string get_string(string key, string? fallback = null)
    {
        return fallback ?? key;
    }
}