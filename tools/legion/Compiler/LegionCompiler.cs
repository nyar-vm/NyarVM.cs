using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Nyar.Assembler;
using Nyar.Language.Build;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Decode;
using Std.Data.Binary.NyarIR.Encode;

namespace Legion.CLI.Compiler;

/// <summary>
///     最小可用的 Legion 构建器。
///     负责：收集项目源码 -> 调用 ValkyrieCompiler -> 将产物写入 dist。
/// </summary>
public sealed class LegionCompiler
{
    private sealed record FlushedArtifactResult(
        string output_directory,
        List<string> output_files,
        string main_artifact);

    /// <summary>
    ///     前端缓存键。
    ///     只绑定源文件集合与目标，不绑定逻辑入口。
    /// </summary>
    private sealed record FrontendCacheKey(
        string project_dir,
        string canonical_triple,
        string frontend_hash);

    /// <summary>
    ///     已准备好的前端结果及其所属编译器实例。
    /// </summary>
    private sealed record PreparedFrontend(
        ValkyrieCompiler compiler,
        ValkyrieCompiler.PreparedCompilation prepared);

    private readonly ICompilationCache? _cache;
    private readonly NyarDecoder _nyar_decoder = new();
    private readonly NyarEncoder _nyar_encoder = new();
    private readonly PackageGraph _package_graph = new();
    private readonly Dictionary<FrontendCacheKey, PreparedFrontend> _prepared_frontend_cache = new();
    private readonly TargetTripleResolver _target_resolver = new();

    /// <summary>
    ///     创建构建器实例
    /// </summary>
    /// <param name="cache">增量编译缓存，为 null 则执行全量编译</param>
    public LegionCompiler(ICompilationCache? cache = null)
    {
        _cache = cache;
    }

    /// <summary>
    ///     构建项目并将产物写入磁盘
    /// </summary>
    public LegionBuildResult build(CompilationContext context)
    {
        var (artifactSet, _) = compile_core_with_cache(context);
        if (artifactSet is null) return _last_error;

        var flushed = flush_artifacts(artifactSet, context.output_dir);

        return new LegionBuildResult
        {
            success = true,
            output_directory = flushed.output_directory,
            output_files = flushed.output_files,
            main_artifact = flushed.main_artifact,
            run_contract = artifactSet.run_contract
        };
    }

    /// <summary>
    ///     构建项目并返回内存中的产物（不落盘），供 run 命令直接交给 NyarVM 执行
    /// </summary>
    public LegionBuildResult build_in_memory(CompilationContext context)
    {
        var (artifactSet, _) = compile_core_with_cache(context);
        if (artifactSet is null) return _last_error;

        return new LegionBuildResult
        {
            success = true,
            main_artifact = artifactSet.primary_artifact.name,
            main_artifact_content = artifactSet.primary_artifact.content,
            run_contract = artifactSet.run_contract
        };
    }

    public void clean(string projectDir)
    {
        var distDir = Path.Combine(projectDir, "dist");
        if (Directory.Exists(distDir)) Directory.Delete(distDir, true);
    }

    private static string format_errors(string mainError, IReadOnlyList<string> graphErrors)
    {
        if (graphErrors.Count == 0) return mainError;

        return $"{mainError}{Environment.NewLine}{string.Join(Environment.NewLine, graphErrors)}";
    }

    private static string format_exception_chain(Exception exception)
    {
        var builder = new StringBuilder();
        var current = exception;
        var depth = 0;

        while (current is not null)
        {
            if (depth > 0)
            {
                builder.AppendLine();
                builder.AppendLine("--- Inner Exception ---");
            }

            builder.AppendLine(current.ToString());
            current = current.InnerException!;
            depth++;
        }

        return builder.ToString().TrimEnd();
    }

    private static string sanitize_module_name(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name) builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');

        return builder.ToString();
    }

    private static IEnumerable<CompilerArtifact> enumerate_artifacts(ArtifactSet artifactSet)
    {
        yield return artifactSet.primary_artifact;

        foreach (var sidecar in artifactSet.sidecar_artifacts) yield return sidecar;

        foreach (var debugArtifact in artifactSet.debug_artifacts) yield return debugArtifact;
    }

    private static FlushedArtifactResult flush_artifacts(ArtifactSet artifactSet, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        try
        {
            write_artifacts_to_directory(artifactSet, outputDir);
            var latestSessionPath = Path.Combine(outputDir, "latest-session.txt");
            if (File.Exists(latestSessionPath))
            {
                File.Delete(latestSessionPath);
            }

            return create_flushed_result(artifactSet, outputDir);
        }
        catch (IOException)
        {
            var fallbackRoot = Path.Combine(outputDir, ".sessions");
            Directory.CreateDirectory(fallbackRoot);

            var fallbackDir = Path.Combine(
                fallbackRoot,
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N")[..8]);

            write_artifacts_to_directory(artifactSet, fallbackDir);
            atomic_write_all_text(Path.Combine(outputDir, "latest-session.txt"), fallbackDir);
            return create_flushed_result(artifactSet, fallbackDir);
        }
    }

    private static void write_artifacts_to_directory(ArtifactSet artifactSet, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        foreach (var artifact in enumerate_artifacts(artifactSet))
        {
            var filePath = Path.Combine(outputDir, artifact.name);
            atomic_write_all_bytes(filePath, artifact.content);
        }

        if (artifactSet.run_contract is not null)
        {
            var content = $"""
                           logical_entry: {artifactSet.run_contract.logical_entry}
                           physical_entry: {artifactSet.run_contract.physical_entry}
                           invocation: {artifactSet.run_contract.invocation_shape}
                           validate: {artifactSet.run_contract.validation_command}
                           """;
            atomic_write_all_text(Path.Combine(outputDir, "run-contract.txt"), content);
        }
    }

    /// <summary>
    ///     原子写入文本文件：先写入临时文件再原子替换，多进程并发写入时读者不会读到半写入状态。
    /// </summary>
    private static void atomic_write_all_text(string filePath, string content)
    {
        var tempPath = filePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            throw;
        }
    }

    /// <summary>
    ///     原子写入二进制文件：先写入临时文件再原子替换。
    /// </summary>
    private static void atomic_write_all_bytes(string filePath, byte[] content)
    {
        var tempPath = filePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(tempPath, content);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            throw;
        }
    }

    private static FlushedArtifactResult create_flushed_result(ArtifactSet artifactSet, string outputDir)
    {
        var outputFiles = enumerate_artifacts(artifactSet)
            .Select(artifact => artifact.name)
            .ToList();

        return new FlushedArtifactResult(
            outputDir,
            outputFiles,
            Path.Combine(outputDir, artifactSet.primary_artifact.name));
    }

    #region 核心编译逻辑

    private LegionBuildResult _last_error = new();

    /// <summary>
    ///     核心编译逻辑：解析、编译、返回 ArtifactSet。失败时返回 null 并设置 _last_error。
    ///     返回的第二个值表示产物是否来自缓存（用于日志）。
    /// </summary>
    private (ArtifactSet?, bool) compile_core_with_cache(CompilationContext context)
    {
        if (_target_resolver.resolve_compilation_target(context.canonical_triple) is null)
        {
            _last_error = new LegionBuildResult
            {
                success = false,
                error = $"不支持的目标三元组 '{context.canonical_triple}'"
            };
            return (null, false);
        }

        var target = _target_resolver.resolve_compilation_target(context.canonical_triple);
        if (target is null)
        {
            _last_error = new LegionBuildResult
            {
                success = false,
                error = $"无法解析目标三元组 '{context.canonical_triple}'"
            };
            return (null, false);
        }

        var packageGraph = _package_graph.resolve(
            context.project_dir,
            target,
            context.verbose,
            context.include_test_sources);
        if (packageGraph.files.Length == 0)
        {
            _last_error = new LegionBuildResult
            {
                success = false,
                error = "未找到可编译的 Valkyrie 源文件，请检查 source/、script/ 目录以及依赖包。"
            };
            return (null, false);
        }

        var sourceFiles = packageGraph.files.Select(f => Path.GetFullPath(f))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        var moduleName = Path.GetFileName(context.project_dir);
        var frontendHash = compute_frontend_hash(sourceFiles, context.canonical_triple);
        var artifactHash = compute_artifact_hash(sourceFiles, context);

        if (_cache is not null)
        {
            if (_cache.try_get_ir(moduleName, context.canonical_triple, artifactHash, out var irEntry) && irEntry is not null)
            {
                var cachedArtifactSet = deserialize_artifact_set(irEntry.ir_data);
                if (cachedArtifactSet is not null) return (cachedArtifactSet, true);
            }

            var artifactSet = do_compile(context, packageGraph, sourceFiles, frontendHash);
            if (artifactSet is not null)
            {
                var artifactBytes = serialize_artifact_set(artifactSet);
                _cache.put_ir(moduleName, context.canonical_triple, artifactHash,
                    new IrCacheEntry
                    {
                        ir_kind = "artifact-set",
                        ir_hash = artifactHash,
                        canonical_triple = context.canonical_triple,
                        ir_data = artifactBytes,
                        created_at = DateTimeOffset.UtcNow
                    });
            }

            return (artifactSet, false);
        }

        return (do_compile(context, packageGraph, sourceFiles, frontendHash), false);
    }

    /// <summary>
    ///     执行编译的具体逻辑（不含缓存判断）
    /// </summary>
    private ArtifactSet? do_compile(
        CompilationContext context,
        PackageGraphResult packageGraphResult,
        string[] sourceFiles,
        string frontendHash)
    {
        var moduleName = sanitize_module_name(Path.GetFileName(context.project_dir));
        var frontendPlan = new BuildPlan(
            moduleName,
            context.canonical_triple,
            context.project_dir,
            true);
        var plan = new BuildPlan(
            moduleName,
            context.canonical_triple,
            context.project_dir,
            true,
            preferred_logical_entry: context.preferred_logical_entry,
            build_options: context.build_options);
        ValkyrieCompiler? compiler = null;

        try
        {
            var cacheKey = new FrontendCacheKey(
                Path.GetFullPath(context.project_dir),
                context.canonical_triple,
                frontendHash);

            if (!_prepared_frontend_cache.TryGetValue(cacheKey, out var preparedFrontend))
            {
                compiler = new ValkyrieCompiler();
                preparedFrontend = new PreparedFrontend(compiler, compiler.prepare_files(sourceFiles, frontendPlan));
                _prepared_frontend_cache[cacheKey] = preparedFrontend;
            }
            else
            {
                compiler = preparedFrontend.compiler;
            }

            return preparedFrontend.compiler.compile_prepared(preparedFrontend.prepared, plan);
        }
        catch (Exception ex)
        {
            var diagnosticText = string.Join(
                Environment.NewLine,
                (compiler?.diagnostics.get_diagnostics() ?? [])
                    .Select(diagnostic => $"{diagnostic.severity}: {diagnostic.message}"));
            var exceptionText = format_exception_chain(ex);

            _last_error = new LegionBuildResult
            {
                success = false,
                error = string.IsNullOrWhiteSpace(diagnosticText)
                    ? format_errors(exceptionText, packageGraphResult.errors)
                    : format_errors($"{exceptionText}{Environment.NewLine}{diagnosticText}", packageGraphResult.errors)
            };
            return null;
        }
    }

    /// <summary>
    ///     计算前端阶段复用用的组合哈希。
    /// </summary>
    private static string compute_frontend_hash(string[] filePaths, string canonicalTriple)
    {
        var combined = new StringBuilder();
        foreach (var path in filePaths)
        {
            combined.Append(path);
            combined.Append('\0');
            var fileBytes = File.ReadAllBytes(path);
            combined.Append(Convert.ToHexStringLower(SHA256.HashData(fileBytes)));
            combined.Append('\0');
        }

        combined.Append(canonicalTriple);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(combined.ToString())));
    }

    /// <summary>
    ///     计算最终产物缓存哈希。
    ///     与前端缓存不同，此处需要纳入逻辑入口，避免不同测试入口串用同一产物。
    /// </summary>
    private static string compute_artifact_hash(string[] filePaths, CompilationContext context)
    {
        var combined = new StringBuilder();
        combined.Append(compute_frontend_hash(filePaths, context.canonical_triple));
        combined.Append('\0');
        combined.Append(compute_toolchain_fingerprint());
        combined.Append('\0');
        combined.Append(context.preferred_logical_entry ?? string.Empty);
        combined.Append('\0');
        combined.Append(context.include_test_sources ? '1' : '0');
        combined.Append('\0');
        combined.Append(context.build_options?.source_map == true ? '1' : '0');
        combined.Append(context.build_options?.type_script == true ? '1' : '0');
        combined.Append(context.build_options?.wat == true ? '1' : '0');
        combined.Append(context.build_options?.msil == true ? '1' : '0');
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(combined.ToString())));
    }

    /// <summary>
    ///     计算当前工具链指纹。
    ///     产物缓存除了依赖源码，还必须绑定 `legion`、语言前端与后端所在程序集，
    ///     否则后端修复后会错误复用旧的 `ArtifactSet`。
    /// </summary>
    private static string compute_toolchain_fingerprint()
    {
        var combined = new StringBuilder();
        append_assembly_fingerprint(combined, typeof(LegionCompiler).Assembly.Location);
        append_assembly_fingerprint(combined, typeof(ValkyrieCompiler).Assembly.Location);
        append_assembly_fingerprint(combined, typeof(CompilationOptions).Assembly.Location);
        return combined.ToString();
    }

    /// <summary>
    ///     追加单个程序集的轻量级文件指纹。
    /// </summary>
    private static void append_assembly_fingerprint(StringBuilder builder, string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
        {
            return;
        }

        var fileInfo = new FileInfo(assemblyPath);
        builder.Append(Path.GetFullPath(assemblyPath));
        builder.Append('|');
        builder.Append(fileInfo.Length);
        builder.Append('|');
        builder.Append(fileInfo.LastWriteTimeUtc.Ticks);
        builder.Append('\0');
    }

    /// <summary>
    ///     将 ArtifactSet 序列化为二进制，用于缓存存储
    /// </summary>
    private static byte[] serialize_artifact_set(ArtifactSet artifactSet)
    {
        var allArtifacts = new List<CompilerArtifact>();
        allArtifacts.Add(artifactSet.primary_artifact);
        allArtifacts.AddRange(artifactSet.sidecar_artifacts);
        allArtifacts.AddRange(artifactSet.debug_artifacts);

        var estimatedSize = 1024 + allArtifacts.Sum(a =>
            Encoding.UTF8.GetByteCount(a.name) + Encoding.UTF8.GetByteCount(a.media_type) + a.content.Length + 12);
        var writer = new ByteBufferWriter(estimatedSize);
        writer.write_i32_le(allArtifacts.Count);

        foreach (var artifact in allArtifacts)
        {
            var nameBytes = Encoding.UTF8.GetBytes(artifact.name);
            writer.write_i32_le(nameBytes.Length);
            writer.write(nameBytes);

            var mediaTypeBytes = Encoding.UTF8.GetBytes(artifact.media_type);
            writer.write_i32_le(mediaTypeBytes.Length);
            writer.write(mediaTypeBytes);

            writer.write_i32_le(artifact.content.Length);
            writer.write(artifact.content);
        }

        if (artifactSet.run_contract is not null)
        {
            writer.write_u8(1);
            var logicalBytes = Encoding.UTF8.GetBytes(artifactSet.run_contract.logical_entry);
            writer.write_i32_le(logicalBytes.Length);
            writer.write(logicalBytes);

            var physicalBytes = Encoding.UTF8.GetBytes(artifactSet.run_contract.physical_entry);
            writer.write_i32_le(physicalBytes.Length);
            writer.write(physicalBytes);

            var invocationBytes = Encoding.UTF8.GetBytes(artifactSet.run_contract.invocation_shape);
            writer.write_i32_le(invocationBytes.Length);
            writer.write(invocationBytes);

            var validationBytes = Encoding.UTF8.GetBytes(artifactSet.run_contract.validation_command);
            writer.write_i32_le(validationBytes.Length);
            writer.write(validationBytes);
        }
        else
        {
            writer.write_u8(0);
        }

        return writer.to_array();
    }

    /// <summary>
    ///     从缓存中还原 ArtifactSet
    /// </summary>
    private static ArtifactSet? deserialize_artifact_set(ReadOnlySpan<byte> data)
    {
        try
        {
            var buffer = new ByteBuffer([.. data]);
            var count = buffer.read_i32_le();
            var artifacts = new List<CompilerArtifact>(count);

            for (var i = 0; i < count; i++)
            {
                var nameLength = buffer.read_i32_le();
                var name = Encoding.UTF8.GetString(buffer.read_bytes(nameLength));

                var mediaTypeLength = buffer.read_i32_le();
                var mediaType = Encoding.UTF8.GetString(buffer.read_bytes(mediaTypeLength));

                var contentLength = buffer.read_i32_le();
                var content = buffer.read_bytes(contentLength).ToArray();

                artifacts.Add(new CompilerArtifact(name, content, mediaType));
            }

            var primaryArtifact = artifacts[0];
            var sidecarArtifacts = artifacts.Skip(1).Take(artifacts.Count > 0 ? artifacts.Count - 1 : 0).ToList();
            var debugArtifacts = new List<CompilerArtifact>();

            RunContract? runContract = null;
            var hasRunContract = buffer.read_u8();
            if (hasRunContract == 1)
            {
                var logicalLength = buffer.read_i32_le();
                var logicalEntry = Encoding.UTF8.GetString(buffer.read_bytes(logicalLength));

                var physicalLength = buffer.read_i32_le();
                var physicalEntry = Encoding.UTF8.GetString(buffer.read_bytes(physicalLength));

                var invocationLength = buffer.read_i32_le();
                var invocationShape = Encoding.UTF8.GetString(buffer.read_bytes(invocationLength));

                var validationLength = buffer.read_i32_le();
                var validationCommand = Encoding.UTF8.GetString(buffer.read_bytes(validationLength));

                runContract = new RunContract(logicalEntry, physicalEntry, invocationShape, validationCommand);
            }

            return new ArtifactSet(primaryArtifact, sidecarArtifacts, debugArtifacts, runContract);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
