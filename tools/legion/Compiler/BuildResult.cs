namespace Legion.CLI.Compiler;

/// <summary>
///     单个 target 的构建结果总结。
/// </summary>
public sealed class BuildResult
{
    /// <summary>
    ///     初始化构建结果。
    /// </summary>
    /// <param name="canonicalTriple">目标 CanonicalTriple</param>
    /// <param name="result">LegionBuildResult 详细结果</param>
    public BuildResult(string canonicalTriple, LegionBuildResult result)
    {
        canonical_triple = canonicalTriple;
        this.result = result;
    }

    /// <summary>
    ///     目标 CanonicalTriple
    /// </summary>
    public string canonical_triple { get; }

    /// <summary>
    ///     详细构建结果
    /// </summary>
    public LegionBuildResult result { get; }

    /// <summary>
    ///     是否构建成功
    /// </summary>
    public bool success => result.success;
}