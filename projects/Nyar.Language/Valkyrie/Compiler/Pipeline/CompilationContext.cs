using Nyar.Types.Targets;

namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     单个 target 的编译上下文，包含 CanonicalTarget、包图、staging 环境、诊断和缓存句柄。
///     每次跨 target 编译都应创建独立的 CompilationContext。
/// </summary>
public sealed class CompilationContext
{
    /// <summary>
    ///     初始化编译上下文。
    /// </summary>
    /// <param name="canonicalTriple">完整四段式 CanonicalTarget</param>
    /// <param name="projectDir">项目根目录</param>
    /// <param name="outputDir">产物输出目录</param>
    public CompilationContext(string canonicalTriple, string projectDir, string outputDir)
    {
        canonical_triple = canonicalTriple;
        project_dir = projectDir;
        output_dir = outputDir;
        source_files = [];
    }

    /// <summary>
    ///     完整四段式 CanonicalTarget（如 "clr-microsoft-unknown-managed"）
    /// </summary>
    public string canonical_triple { get; }

    /// <summary>
    ///     项目根目录
    /// </summary>
    public string project_dir { get; }

    /// <summary>
    ///     产物输出目录（如 "dist/clr-microsoft-unknown-managed/"）
    /// </summary>
    public string output_dir { get; set; }

    /// <summary>
    ///     参与编译的源文件路径列表（含项目源码、std、adaptor）
    /// </summary>
    public List<string> source_files { get; }

    /// <summary>
    ///     Staging 环境使用的宿主类型
    /// </summary>
    public TargetHostKind arch_tag { get; set; }

    /// <summary>
    ///     Staging 环境使用的真实 ABI
    /// </summary>
    public TargetAbi abi { get; set; }

    /// <summary>
    ///     目标对应的后端家族
    /// </summary>
    public TargetBackendFamily backend_family { get; set; }

    /// <summary>
    ///     是否为 verbose 模式
    /// </summary>
    public bool verbose { get; set; }

    /// <summary>
    ///     构建附加选项（来自 legion.von 的 build 条目）
    /// </summary>
    public BuildTargetOptions? build_options { get; set; }

    /// <summary>
    ///     当前构建优先保留的逻辑入口。
    ///     主要用于测试过滤时，只编译选中的单个 `[test]` 入口及其可达依赖。
    /// </summary>
    public string? preferred_logical_entry { get; set; }

    /// <summary>
    ///     是否将项目 `test/` 目录下的源文件并入本次编译输入。
    ///     主要供 `legion test` / `legion bench` 使用。
    /// </summary>
    public bool include_test_sources { get; set; }
}
