using Core.Data;

namespace Nyar.Language.Valkyrie.Config;

/// <summary>
///     VOA 代码拆分（Chunk）配置，对应 voa.config.v 中的 [chunk] 节。
///     控制构建产物的拆分粒度和合并策略。
/// </summary>
[Data]
public sealed class VoaChunkConfig
{
    /// <summary>
    ///     WASM 拆分阈值（字节），总体积超过此值自动拆分，默认 51200（50KB）。
    ///     目前 WASM 体积较小，暂不触发拆分；预留此配置为未来大项目准备。
    /// </summary>
    public int wasm_threshold { get; set; } = 51200;

    /// <summary>
    ///     组件 JS 拆分模式。
    ///     <list type="bullet">
    ///         <item><c>"per-component"</c>：每个 .awsl 组件独立输出 JS 文件（默认，推荐）</item>
    ///         <item><c>"bundled"</c>：所有组件合并为单一 JS 文件</item>
    ///     </list>
    /// </summary>
    public string js_mode { get; set; } = "per-component";

    /// <summary>
    ///     CSS 合并模式。
    ///     <list type="bullet">
    ///         <item><c>"merged"</c>：所有组件 CSS 合并为单一文件（默认）</item>
    ///         <item><c>"per-component"</c>：每个组件独立输出 CSS 文件</item>
    ///     </list>
    /// </summary>
    public string css_mode { get; set; } = "merged";
}
