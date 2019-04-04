namespace Nyar.PackageManager.Build;

/// <summary>
///     单次构建结果。
///     遵循"交付契约成立"为准的成功定义。
/// </summary>
public sealed record BuildResult
{
    /// <summary>
    ///     是否构建成功
    /// </summary>
    public bool success { get; init; }

    /// <summary>
    ///     模块名称
    /// </summary>
    public string module_name { get; init; } = string.Empty;

    /// <summary>
    ///     目标三元组
    /// </summary>
    public string canonical_triple { get; init; } = string.Empty;

    /// <summary>
    ///     产物集（成功时有值）
    /// </summary>
    public IArtifactSet? artifact_set { get; init; }

    /// <summary>
    ///     输出目录
    /// </summary>
    public string? output_directory { get; init; }

    /// <summary>
    ///     失败阶段（失败时有值）
    /// </summary>
    public BuildStage? failed_stage { get; init; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public string? error_message { get; init; }

    /// <summary>
    ///     修复建议
    /// </summary>
    public string? fix_suggestion { get; init; }

    /// <summary>
    ///     构建耗时
    /// </summary>
    public TimeSpan elapsed { get; init; }

    /// <summary>
    ///     从异常创建失败结果
    /// </summary>
    /// <param name="stage">失败阶段</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="triple">目标三元组</param>
    /// <param name="ex">异常</param>
    /// <param name="elapsed">耗时</param>
    /// <returns>失败结果</returns>
    public static BuildResult failure(BuildStage stage, string moduleName, string triple, Exception ex,
        TimeSpan elapsed)
    {
        var fixSuggestion = stage switch
        {
            BuildStage.lex => "请检查源码中的非法字符或词法错误。",
            BuildStage.parse => "请检查源码的语法结构是否正确。",
            BuildStage.semantic => "请检查变量定义、类型引用或作用域是否正确。",
            BuildStage.hir => "内部错误：HIR 构建失败。请提交 bug 报告。",
            BuildStage.mir => "内部错误：MIR 构建失败。请确认 IKun IR 是否正确。",
            BuildStage.lir => "内部错误：LIR 构建失败。请确认 MIR → LIR 降级路径。",
            BuildStage.emit => "后端发射失败。请确认目标后端是否已正确注册。",
            BuildStage.packaging => "产物打包失败。请确认目标平台的打包策略配置。",
            BuildStage.flush => "产物写入磁盘失败。请检查磁盘空间和输出目录权限。",
            _ => "未知错误。"
        };

        return new BuildResult
        {
            success = false,
            module_name = moduleName,
            canonical_triple = triple,
            failed_stage = stage,
            error_message = ex.Message,
            fix_suggestion = fixSuggestion,
            elapsed = elapsed
        };
    }

    /// <summary>
    ///     从产物集创建成功结果
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="triple">目标三元组</param>
    /// <param name="artifactSet">产物集</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="elapsed">耗时</param>
    /// <returns>成功结果</returns>
    public static BuildResult succeeded(string moduleName, string triple,
        IArtifactSet artifactSet, string outputDirectory, TimeSpan elapsed)
    {
        return new BuildResult
        {
            success = true,
            module_name = moduleName,
            canonical_triple = triple,
            artifact_set = artifactSet,
            output_directory = outputDirectory,
            elapsed = elapsed
        };
    }
}