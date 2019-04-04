namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     统一交付结果。
/// </summary>
public sealed class ArtifactSet
{
    public ArtifactSet(
        CompilerArtifact primaryArtifact,
        IReadOnlyList<CompilerArtifact>? sidecarArtifacts = null,
        IReadOnlyList<CompilerArtifact>? debugArtifacts = null,
        RunContract? runContract = null)
    {
        primary_artifact = primaryArtifact;
        sidecar_artifacts = sidecarArtifacts ?? [];
        debug_artifacts = debugArtifacts ?? [];
        run_contract = runContract;
    }

    public CompilerArtifact primary_artifact { get; }

    public IReadOnlyList<CompilerArtifact> sidecar_artifacts { get; }

    public IReadOnlyList<CompilerArtifact> debug_artifacts { get; }

    public RunContract? run_contract { get; private set; }

    /// <summary>
    ///     返回携带指定运行契约的新产物集（不修改原实例）
    /// </summary>
    /// <param name="runContract">运行契约</param>
    /// <returns>新产物集</returns>
    public ArtifactSet with_run_contract(RunContract runContract)
    {
        return new ArtifactSet(primary_artifact, sidecar_artifacts, debug_artifacts, runContract);
    }
}