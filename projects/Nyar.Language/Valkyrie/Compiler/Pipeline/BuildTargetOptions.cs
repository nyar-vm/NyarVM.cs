namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     构建目标的附加选项（从 legion.von 的 build 条目中提取）。
/// </summary>
public sealed class BuildTargetOptions
{
    /// <summary>
    ///     是否生成 Source Map
    /// </summary>
    public bool source_map { get; set; }

    /// <summary>
    ///     是否生成 TypeScript 声明文件
    /// </summary>
    public bool type_script { get; set; }

    /// <summary>
    ///     是否生成 WAT 文本输出
    /// </summary>
    public bool wat { get; set; }

    /// <summary>
    ///     是否生成 MSIL 文本输出
    /// </summary>
    public bool msil { get; set; }
}