namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     <see cref="ArtifactSet" /> 扩展方法。
/// </summary>
public static class ArtifactSetExtensions
{
    /// <summary>
    ///     枚举产物集中的所有产物。
    /// </summary>
    /// <param name="artifactSet">产物集</param>
    /// <returns>所有编译产物</returns>
    public static IEnumerable<CompilerArtifact> enumerate_artifacts(this ArtifactSet artifactSet)
    {
        yield return artifactSet.primary_artifact;

        foreach (var artifact in artifactSet.sidecar_artifacts) yield return artifact;

        foreach (var artifact in artifactSet.debug_artifacts) yield return artifact;
    }
}