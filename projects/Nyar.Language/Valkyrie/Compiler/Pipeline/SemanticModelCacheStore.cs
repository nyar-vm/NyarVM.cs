using System.Security.Cryptography;
using System.Text;
using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Formatter;

namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     封装完整语义模型快照的缓存读写，避免编译器同时承担流程编排与缓存编解码职责。
/// </summary>
public static class SemanticModelCacheStore
{
    public static SemanticModel? try_restore(
        ICompilationCache? cache,
        IReadOnlyList<CompilationUnit> stagedUnits,
        string canonicalTriple)
    {
        if (cache is null || stagedUnits.Count == 0)
        {
            return null;
        }

        var filePath = normalize_file_path(stagedUnits[0].file_path);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var astHash = compute_ast_hash(stagedUnits);
        if (!cache.try_get_semantics(filePath, canonicalTriple, astHash, out var entry) || entry is null)
        {
            return null;
        }

        return SemanticModelSnapshotSerializer.deserialize(entry.semantic_data);
    }

    public static void store(
        ICompilationCache? cache,
        IReadOnlyList<CompilationUnit> stagedUnits,
        string canonicalTriple,
        SemanticModel semanticModel)
    {
        if (cache is null || stagedUnits.Count == 0)
        {
            return;
        }

        var astHash = compute_ast_hash(stagedUnits);
        var entry = new SemanticCacheEntry
        {
            ast_hash = astHash,
            canonical_triple = canonicalTriple,
            semantic_data = SemanticModelSnapshotSerializer.serialize(semanticModel),
            created_at = DateTimeOffset.UtcNow
        };

        foreach (var unit in stagedUnits)
        {
            var filePath = normalize_file_path(unit.file_path);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            cache.put_semantics(filePath, canonicalTriple, astHash, entry);
        }
    }

    private static string compute_ast_hash(IReadOnlyList<CompilationUnit> stagedUnits)
    {
        var builder = new StringBuilder();
        foreach (var unit in stagedUnits)
        {
            builder.Append(normalize_file_path(unit.file_path));
            builder.Append('\0');
            builder.Append(ValkyrieFormatter.Format(unit));
            builder.Append('\0');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string normalize_file_path(string? filePath)
    {
        return string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetFullPath(filePath);
    }
}
