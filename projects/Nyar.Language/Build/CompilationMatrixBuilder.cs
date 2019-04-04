using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Language.Von;
using Std.Data.Text.Diagnostics;
using Std.DataProcess.Serialize;

namespace Nyar.Language.Build;

/// <summary>
///     构建矩阵，负责从项目清单中展开 target × entry 的编译任务。
///     将 manifest 的 build 列表和调用方参数转换为 <see cref="CompilationContext" /> 列表。
/// </summary>
public sealed class CompilationMatrixBuilder
{
    private readonly TargetTripleResolver _target_resolver = new();

    /// <summary>
    ///     从项目目录和可选的目标过滤生成编译上下文列表。
    /// </summary>
    public List<CompilationContext> build_contexts(
        string projectDir,
        string? explicitTarget,
        string? output,
        bool verbose)
    {
        var manifest = try_load_manifest(projectDir);
        var targetsToBuild = resolve_target_names(manifest, explicitTarget);

        if (targetsToBuild.Count == 0)
        {
            return [];
        }

        var contexts = new List<CompilationContext>(targetsToBuild.Count);

        foreach (var (targetName, buildTarget) in targetsToBuild)
        {
            var canonicalName = _target_resolver.resolve_canonical_name(targetName);
            var outputDir = output is not null
                ? Path.Combine(output, canonicalName)
                : Path.Combine(projectDir, "dist", canonicalName);

            var targetProfile = _target_resolver.resolve_target(targetName);

            var context = new CompilationContext(canonicalName, projectDir, outputDir)
            {
                verbose = verbose,
                arch_tag = targetProfile?.host_kind ?? default,
                abi = targetProfile?.abi ?? default,
                backend_family = targetProfile?.backend_family ?? default
            };

            if (buildTarget is not null)
            {
                context.build_options = new BuildTargetOptions
                {
                    source_map = ValkyrieValueProjector.bind_bool_field(buildTarget, "source_map") ?? false,
                    type_script = ValkyrieValueProjector.bind_bool_field(buildTarget, "typescript") ?? false,
                    wat = ValkyrieValueProjector.bind_bool_field(buildTarget, "wat") ?? false,
                    msil = ValkyrieValueProjector.bind_bool_field(buildTarget, "msil") ?? false
                };
            }

            contexts.Add(context);
        }

        return contexts;
    }

    private static SerdeValue? try_load_manifest(string projectDir)
    {
        var manifestPath = Path.Combine(projectDir, "legion.von");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var parser = new VonParser(diagnostics);
            var source = File.ReadAllText(manifestPath);
            var manifest = parser.deserialize(source);
            return diagnostics.has_errors ? null : manifest;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static List<(string Name, SerdeValue? Options)> resolve_target_names(
        SerdeValue? manifest,
        string? explicitTarget)
    {
        if (!string.IsNullOrWhiteSpace(explicitTarget))
        {
            var resolver = new TargetTripleResolver();
            var canonicalTarget = resolver.resolve_canonical_name(explicitTarget) ?? explicitTarget;
            var options = manifest is not null
                ? find_build_target_option(manifest, explicitTarget, canonicalTarget)
                : null;
            return [(explicitTarget, options)];
        }

        var buildList = manifest?.get_field("build")?.elements;
        if (buildList is not null && buildList.Count > 0)
        {
            var resolvedTargets = new List<(string Name, SerdeValue? Options)>();
            foreach (var buildTarget in buildList)
            {
                var targetName = ValkyrieValueProjector.bind_utf8_field(buildTarget, "target");
                if (!string.IsNullOrWhiteSpace(targetName))
                {
                    resolvedTargets.Add((targetName, buildTarget));
                }
            }

            return resolvedTargets;
        }

        return [];
    }

    private static SerdeValue? find_build_target_option(SerdeValue manifest, string target, string canonicalTarget)
    {
        return manifest.get_field("build")?.elements?
            .FirstOrDefault(buildTarget =>
            {
                var configuredTarget = ValkyrieValueProjector.bind_utf8_field(buildTarget, "target");
                if (string.IsNullOrWhiteSpace(configuredTarget))
                {
                    return false;
                }

                return string.Equals(configuredTarget, target, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(configuredTarget, canonicalTarget, StringComparison.OrdinalIgnoreCase);
            });
    }
}
