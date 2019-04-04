using Std.Data.Text.Syntax;

namespace Std.Data.Text.Glsl;

/// <summary>
///     GLSL 语言配置。
/// </summary>
public sealed class GlslLanguage : Language
{
    /// <summary>
    ///     语言名称。
    /// </summary>
    public override string name => "GLSL";


    /// <summary>
    ///     GLSL 版本（如 450）。
    /// </summary>
    public int version { get; init; } = 450;


    /// <summary>
    ///     是否为 ES 配置文件。
    /// </summary>
    public bool is_es_profile { get; init; }


    /// <summary>
    ///     是否启用计算着色器。
    /// </summary>
    public bool compute_shader { get; init; } = true;


    /// <summary>
    ///     是否启用光线追踪扩展。
    /// </summary>
    public bool ray_tracing { get; init; }
}