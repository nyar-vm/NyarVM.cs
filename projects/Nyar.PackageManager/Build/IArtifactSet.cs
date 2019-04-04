namespace Nyar.PackageManager.Build;

/// <summary>
///     产物集 — 包含主产物、附属产物和调试产物
/// </summary>
public interface IArtifactSet
{
    /// <summary>
    ///     主产物
    /// </summary>
    ICompilerArtifact primary_artifact { get; }

    /// <summary>
    ///     附属产物列表
    /// </summary>
    IReadOnlyList<ICompilerArtifact> sidecar_artifacts { get; }

    /// <summary>
    ///     调试产物列表
    /// </summary>
    IReadOnlyList<ICompilerArtifact> debug_artifacts { get; }

    /// <summary>
    ///     运行契约（可选）
    /// </summary>
    IRunContract? run_contract { get; }
}