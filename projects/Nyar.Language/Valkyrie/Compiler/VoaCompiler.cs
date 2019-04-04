using Nyar.Language.Awsl.Asgard.Compiler;
using Nyar.Language.Valkyrie.Config;

namespace Nyar.Language.Valkyrie.Compiler;

/// <summary>
///     VOA 编译器，封装 <see cref="AsgardMultiTargetBuilder" /> 提供简化的构建 API。
///     将底层 snake_case 的 <see cref="AsgardBuildResult" /> 映射为 PascalCase 的 <see cref="BuildResult" />。
/// </summary>
public sealed class VoaCompiler
{
    private readonly AsgardMultiTargetBuilder _builder;
    private readonly VoaConfigLoader _config_loader;

    /// <summary>
    ///     创建 VOA 编译器实例
    /// </summary>
    public VoaCompiler()
    {
        _builder = new AsgardMultiTargetBuilder();
        _config_loader = new VoaConfigLoader();
    }

    /// <summary>
    ///     获取编译过程中的诊断信息
    /// </summary>
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>
    ///     构建项目到指定目标平台
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="target">目标平台标识</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <param name="pwa">是否生成 PWA 资源</param>
    /// <returns>构建结果</returns>
    public BuildResult Build(string projectDir, string outputDir, string target, bool verbose, bool pwa = false)
    {
        var projectConfig = _config_loader.load(projectDir);
        var buildConfig = projectConfig.build ?? new VoaBuildConfig();
        buildConfig.app_name ??= projectConfig.name;
        var result = _builder.build(buildConfig, projectDir, outputDir, target, verbose);

        return new BuildResult
        {
            Success = result.success,
            Error = result.error ?? string.Empty,
            OutputFiles = result.output_files ?? []
        };
    }

    /// <summary>
    ///     构建项目为 WASM 目标
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <returns>构建结果</returns>
    public BuildResult BuildWasm(string projectDir, string outputDir, bool verbose)
    {
        return Build(projectDir, outputDir, "wasm", verbose);
    }
}

/// <summary>
///     构建结果记录
/// </summary>
public sealed record BuildResult
{
    /// <summary>
    ///     构建是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     构建失败时的错误信息
    /// </summary>
    public string Error { get; init; } = "";

    /// <summary>
    ///     构建产出的输出文件路径列表
    /// </summary>
    public List<string> OutputFiles { get; init; } = [];
}

/// <summary>
///     诊断信息集合
/// </summary>
public sealed class DiagnosticBag
{
    /// <summary>
    ///     诊断错误列表
    /// </summary>
    public List<DiagnosticMessage> Errors { get; } = new();
}

/// <summary>
///     单条诊断消息
/// </summary>
public sealed class DiagnosticMessage
{
    /// <summary>
    ///     诊断消息文本
    /// </summary>
    public string Message { get; init; } = "";
}
