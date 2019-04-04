namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     编译产物项。
/// </summary>
public sealed record CompilerArtifact(string name, byte[] content, string media_type);