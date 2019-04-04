namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     entry 切片计划，描述单个 [main] 入口点的可达性裁剪和交付策略。
///     在完整包 IR 编译完成后，以每个入口为根做可达性分析，
///     得到最小化的 entry 子图，再分别发射产物。
/// </summary>
public sealed record EntryPlan
{
    /// <summary>
    ///     初始化入口计划。
    /// </summary>
    /// <param name="entryName">入口函数名（[main] 标记的函数）</param>
    /// <param name="canonicalTriple">目标 CanonicalTriple</param>
    /// <param name="prunedFunctionNames">可达性分析后保留的函数名列表</param>
    public EntryPlan(string entryName, string canonicalTriple, IReadOnlyList<string> prunedFunctionNames)
    {
        entry_name = entryName;
        canonical_triple = canonicalTriple;
        pruned_function_names = prunedFunctionNames;
    }

    /// <summary>
    ///     入口函数名（如 "hello_world"、"hello_world_utf8"）
    /// </summary>
    public string entry_name { get; }

    /// <summary>
    ///     目标 CanonicalTriple
    /// </summary>
    public string canonical_triple { get; }

    /// <summary>
    ///     可达性分析后保留的函数名列表
    /// </summary>
    public IReadOnlyList<string> pruned_function_names { get; }

    /// <summary>
    ///     主产物文件名（不含路径）
    /// </summary>
    public string primary_artifact_name => $"{entry_name}{infer_extension(canonical_triple)}";

    /// <summary>
    ///     根据 CanonicalTriple 推断主产物扩展名。
    /// </summary>
    private static string infer_extension(string canonicalTriple)
    {
        if (canonicalTriple.StartsWith("nyar-", StringComparison.OrdinalIgnoreCase)) return ".nyar";

        if (canonicalTriple.StartsWith("gnosis-", StringComparison.OrdinalIgnoreCase)) return ".gnosis";

        if (canonicalTriple.StartsWith("wasm", StringComparison.OrdinalIgnoreCase)) return ".wasm";

        if (canonicalTriple.StartsWith("jvm-", StringComparison.OrdinalIgnoreCase)) return ".jar";

        if (canonicalTriple.StartsWith("clr-", StringComparison.OrdinalIgnoreCase))
        {
            // CLR 产物：新版规范中跨平台 CLR 统一为 .exe；旧版 linux 特例化为 .dll（保持向后兼容）
            if (canonicalTriple.Contains("linux", StringComparison.OrdinalIgnoreCase)) return ".dll";

            return ".exe";
        }

        if (canonicalTriple.StartsWith("spirv-", StringComparison.OrdinalIgnoreCase)) return ".spv";

        if (canonicalTriple.Contains("windows", StringComparison.OrdinalIgnoreCase)) return ".exe";

        return string.Empty;
    }
}