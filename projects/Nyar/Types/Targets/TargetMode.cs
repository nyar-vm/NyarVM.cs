using System.Text.Json.Serialization;

namespace Nyar.Types.Targets;

/// <summary>
///     编译目标模式，控制产物粒度和优化级别。
///     <list type="bullet">
///         <item><c>Dev</c>：最大粒度 per-file 输出，启用 HMR，关闭优化</item>
///         <item><c>Prod</c>：按配置合并优化输出，启用全部编译优化</item>
///     </list>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetMode
{
    /// <summary>
    ///     开发模式：最大粒度 per-file 输出，HMR 支持，无优化
    /// </summary>
    dev,

    /// <summary>
    ///     生产模式：合并优化输出，全部编译优化
    /// </summary>
    prod
}