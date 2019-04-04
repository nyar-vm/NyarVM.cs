using System.Diagnostics;

namespace Nyar.PackageManager.Build;

/// <summary>
///     单次构建控制器。
///     负责：读取源码 → 调用 `ICompiler.CompileToTarget` → 将 `IArtifactSet` 写入磁盘。
/// </summary>
public sealed class BuildController
{
    private readonly ICompiler _compiler;
    private readonly string _output_base_directory;

    /// <summary>
    ///     初始化构建控制器
    /// </summary>
    /// <param name="outputBaseDirectory">产物输出根目录</param>
    /// <param name="compiler">编译器实例</param>
    public BuildController(string outputBaseDirectory, ICompiler compiler)
    {
        _output_base_directory = outputBaseDirectory;
        _compiler = compiler;
    }

    /// <summary>
    ///     执行单次编译构建
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <param name="plan">构建计划</param>
    /// <returns>构建结果</returns>
    public BuildResult build(string source, IBuildPlan plan)
    {
        return build_core(plan, compiler => compiler.compile_to_target(source, plan));
    }

    /// <summary>
    ///     执行多文件编译构建。
    /// </summary>
    /// <param name="sourceFiles">源码文件列表</param>
    /// <param name="plan">构建计划</param>
    /// <returns>构建结果</returns>
    public BuildResult build_files(IReadOnlyList<string> sourceFiles, IBuildPlan plan)
    {
        return build_core(plan, compiler => compiler.compile_files_to_target(sourceFiles, plan));
    }

    #region 阶段分类

    private static BuildStage classify_error(InvalidOperationException ex)
    {
        var message = ex.Message;

        if (message.Contains("词法分析")) return BuildStage.lex;

        if (message.Contains("语法分析")) return BuildStage.parse;

        if (message.Contains("语义分析")) return BuildStage.semantic;

        if (message.Contains("后端") || message.Contains("未找到")) return BuildStage.emit;

        if (message.Contains("打包") || message.Contains("组装")) return BuildStage.packaging;

        return BuildStage.emit;
    }

    #endregion

    #region 编译执行

    private BuildResult build_core(IBuildPlan plan, Func<ICompiler, IArtifactSet> compile)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var artifactSet = compile(_compiler);
            var outputDir = resolve_output_directory(plan);
            flush_artifacts(artifactSet, outputDir);

            stopwatch.Stop();
            return BuildResult.succeeded(plan.module_name, plan.canonical_triple, artifactSet, outputDir,
                stopwatch.Elapsed);
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            var stage = classify_error(ex);
            return BuildResult.failure(stage, plan.module_name, plan.canonical_triple, ex, stopwatch.Elapsed);
        }
        catch (NotSupportedException ex)
        {
            stopwatch.Stop();
            return BuildResult.failure(BuildStage.emit, plan.module_name, plan.canonical_triple, ex, stopwatch.Elapsed);
        }
        catch (IOException ex)
        {
            stopwatch.Stop();
            return BuildResult.failure(BuildStage.flush, plan.module_name, plan.canonical_triple, ex,
                stopwatch.Elapsed);
        }
    }

    #endregion

    #region 产物落盘

    private string resolve_output_directory(IBuildPlan plan)
    {
        return Path.Combine(_output_base_directory, plan.canonical_triple);
    }

    private static void flush_artifacts(IArtifactSet artifactSet, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        write_artifact(outputDir, artifactSet.primary_artifact);

        foreach (var sidecar in artifactSet.sidecar_artifacts) write_artifact(outputDir, sidecar);

        foreach (var debugArtifact in artifactSet.debug_artifacts) write_artifact(outputDir, debugArtifact);

        if (artifactSet.run_contract is not null) write_run_contract(outputDir, artifactSet.run_contract);
    }

    private static void write_artifact(string outputDir, ICompilerArtifact artifact)
    {
        var filePath = Path.Combine(outputDir, artifact.name);
        File.WriteAllBytes(filePath, artifact.content);
    }

    private static void write_run_contract(string outputDir, IRunContract runContract)
    {
        var content = $"""
                       logical_entry: {runContract.logical_entry}
                       physical_entry: {runContract.physical_entry}
                       invocation: {runContract.invocation_shape}
                       validate: {runContract.validation_command}
                       """;

        var filePath = Path.Combine(outputDir, "run-contract.txt");
        File.WriteAllText(filePath, content);
    }

    #endregion
}