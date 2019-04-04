using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Types.Externals;
using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

public sealed record HirCallableSemantics(
    bool is_logical_entry,
    HirIntrinsicSymbol? intrinsic,
    IReadOnlyList<ExternalImport> external_import_links)
{
    public bool has_external_import => external_import_links.Count > 0;

    public static HirCallableSemantics from_annotations(Annotations annotations)
    {
        var loweredAnnotations = HirAnnotationLowering.build_annotations(annotations);
        var isLogicalEntry = loweredAnnotations
            .Any(attribute =>
                string.Equals(attribute.name, "main", StringComparison.Ordinal) ||
                string.Equals(attribute.name, "test", StringComparison.Ordinal) ||
                string.Equals(attribute.name, "benchmark", StringComparison.Ordinal));

        HirIntrinsicSymbol? intrinsic = null;
        foreach (var attribute in loweredAnnotations)
        {
            if (!string.Equals(attribute.name, "intrinsic", StringComparison.Ordinal) ||
                attribute.arguments.Count == 0)
                continue;

            var rawName = attribute.arguments[0];
            var normalizedName = rawName.Trim();
            if (HirIntrinsicCatalog.try_resolve(normalizedName, out var resolved))
            {
                intrinsic = resolved;
                break;
            }

            throw new InvalidOperationException(
                $"未知 intrinsic `{normalizedName}`，请先在 `HirIntrinsicCatalog` 中注册。");
        }

        var externalImportLinks = HirAttributeSemantics.collect_callable_external_import_links(loweredAnnotations);
        return new HirCallableSemantics(isLogicalEntry, intrinsic, externalImportLinks);
    }

    public bool try_get_external_import_link(CallingConvention convention, out ExternalImport externalImport)
    {
        foreach (var externalImportLinkCandidate in external_import_links)
            if (externalImportLinkCandidate.convention == convention)
            {
                externalImport = externalImportLinkCandidate;
                return true;
            }

        externalImport = null!;
        return false;
    }
}