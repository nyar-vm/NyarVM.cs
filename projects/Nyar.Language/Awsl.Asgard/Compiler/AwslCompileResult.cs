namespace Nyar.Language.Awsl.Asgard.Compiler;

public sealed class AwslCompileResult
{
    /// <summary>
    ///     组件名称
    /// </summary>
    public string component_name { get; init; } = string.Empty;

    /// <summary>
    ///     编译后的 JavaScript 代码
    /// </summary>
    public string java_script { get; init; } = string.Empty;

    /// <summary>
    ///     组件的 Scoped CSS 样式
    /// </summary>
    public string css { get; init; } = string.Empty;

    /// <summary>
    ///     Island 类型：static（纯静态 HTML）、hydrated（客户端水合）、wasm、vue、react
    ///     为 <see langword="null" /> 表示不是 Island 组件（传统模式）
    /// </summary>
    public string? island_type { get; init; }

    /// <summary>
    ///     水合策略：load、idle、visible、interaction、media
    ///     仅在 <see cref="island_type" /> 不为 null 时有效
    /// </summary>
    public string? hydrate_strategy { get; init; }
}
