using Nyar.Language;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     语言运行时配置，封装每种语言与统一求值器之间的差异。
///     每种语言仅需提供此配置，解析和求值由统一管线处理。
/// </summary>
public class LanguageRuntimeConfig
{
    /// <summary>
    ///     目标方言
    /// </summary>
    public DialectTarget dialect { get; init; }

    /// <summary>
    ///     内置函数注册表（函数名 → 实现）
    /// </summary>
    public Dictionary<string, Func<object[], object>> builtin_functions { get; init; } = new();

    /// <summary>
    ///     变量前缀（如 Bash 的 $），用于 Shell 语言
    /// </summary>
    public string? variable_prefix { get; init; }

    /// <summary>
    ///     创建标准语言配置
    /// </summary>
    /// <param name="dialect">方言目标</param>
    /// <param name="builtins">内置函数表</param>
    public static LanguageRuntimeConfig create(DialectTarget dialect, Dictionary<string, Func<object[], object>>? builtins = null)
    {
        return new LanguageRuntimeConfig
        {
            dialect = dialect,
            builtin_functions = builtins ?? new Dictionary<string, Func<object[], object>>()
        };
    }
}