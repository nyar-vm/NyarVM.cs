using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Core.Terminal;
using Legion.CLI.Compiler;
using Legion.CLI.Document;
using Legion.CLI.Runner;
using Nyar.Language.Build;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Formatter;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Language.Von;
using Nyar.Language.WebStyle;
using Nyar.PackageManager.Package;
using Nyar.Types;
using Nyar.VM.NyarVM;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;
using Std.DataProcess.Serialize;

namespace Legion.CLI.Commands;

/// <summary>
///     Legion 命令共享辅助方法
/// </summary>
internal static class LegionHelper
{
    #region 静态字段

    /// <summary>
    ///     构建矩阵构建器
    /// </summary>
    private static readonly CompilationMatrixBuilder _build_matrix = new();

    /// <summary>
    ///     Legion 编译器实例
    /// </summary>
    private static readonly LegionCompiler _legion_compiler = new();

    /// <summary>
    ///     按缓存根目录复用的 Legion 编译器实例。
    /// </summary>
    private static readonly ConcurrentDictionary<string, LegionCompiler> _cached_legion_compilers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     复用后的 `nyar test` 会话。
    /// </summary>
    private sealed record NyarTestSession(byte[] bytecode, string module_name);

    /// <summary>
    ///     复用后的外部 target 测试会话。
    /// </summary>
    private sealed record ExternalTargetTestSession(
        string target,
        string output_directory,
        IReadOnlyDictionary<string, string> artifact_paths);

    /// <summary>
    ///     项目 manifest 缓存项。
    /// </summary>
    private sealed record ManifestCacheEntry(long stamp, LegionManifest? manifest);

    /// <summary>
    ///     workspace 成员缓存项。
    /// </summary>
    private sealed record WorkspaceMembersCacheEntry(long stamp, IReadOnlyList<string> members);

    /// <summary>
    ///     编译上下文缓存项。
    /// </summary>
    private sealed record BuildContextsCacheEntry(IReadOnlyList<CompilationContext> contexts);

    /// <summary>
    ///     workspace 成员项目。
    /// </summary>
    internal sealed record WorkspaceMemberProject(string member_path, string member_name, string member_dir);

    /// <summary>
    ///     测试发现缓存项。
    /// </summary>
    private sealed record TestDiscoveryCacheEntry(string signature, IReadOnlyList<(string name, bool is_test, bool is_benchmark)> functions);

    /// <summary>
    ///     项目文件列表缓存项。
    /// </summary>
    private sealed record ProjectFileListCacheEntry(string signature, IReadOnlyList<string> files);

    /// <summary>
    ///     覆盖率特性推断缓存项。
    /// </summary>
    private sealed record CoverageFeaturesCacheEntry(string signature, IReadOnlyList<string> features);

    /// <summary>
    ///     项目 manifest 缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<string, ManifestCacheEntry> _manifest_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     workspace 成员缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<string, WorkspaceMembersCacheEntry> _workspace_members_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     编译上下文模板缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<string, BuildContextsCacheEntry> _build_contexts_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     测试函数发现缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<string, TestDiscoveryCacheEntry> _test_discovery_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     项目文件列表缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<string, ProjectFileListCacheEntry> _project_file_list_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     覆盖率特性推断缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<string, CoverageFeaturesCacheEntry> _coverage_features_cache = new(StringComparer.OrdinalIgnoreCase);

    #endregion

    #region 属性

    /// <summary>
    ///     获取构建矩阵构建器
    /// </summary>
    internal static CompilationMatrixBuilder build_matrix => _build_matrix;

    /// <summary>
    ///     获取 Legion 编译器实例
    /// </summary>
    internal static LegionCompiler legion_compiler => _legion_compiler;

    /// <summary>
    ///     获取绑定到当前项目缓存根目录的 Legion 编译器。
    /// </summary>
    internal static LegionCompiler get_cached_legion_compiler(string projectDir)
    {
        var cacheRoot = Path.GetFullPath(resolve_cache_root(projectDir));
        return _cached_legion_compilers.GetOrAdd(
            cacheRoot,
            static root => new LegionCompiler(new NyarDatabaseCompilationCache(root)));
    }

    #endregion

    #region 分析编译器（check / lint 共享）

    /// <summary>
    ///     按缓存根目录复用的 <see cref="ValkyrieCompiler" /> 实例。
    ///     与 <see cref="get_cached_legion_compiler" /> 使用相同的缓存根键，确保跨命令复用。
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<ValkyrieCompiler>> _cached_valkyrie_compilers =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     获取或创建绑定到项目缓存根目录的 <see cref="ValkyrieCompiler" /> 实例。
    /// </summary>
    internal static ValkyrieCompiler get_cached_valkyrie_compiler(string projectDir)
    {
        var cacheRoot = Path.GetFullPath(resolve_cache_root(projectDir));
        return _cached_valkyrie_compilers.GetOrAdd(
            cacheRoot,
            static root => new Lazy<ValkyrieCompiler>(
                () => new ValkyrieCompiler(new NyarDatabaseCompilationCache(root)),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    #endregion

    #region 运行方法（run 命令共享）

    /// <summary>
    ///     执行 <c>run</c> 命令。
    /// </summary>
    internal static ExitCode execute_run_command(
        string projectDir,
        string? target,
        string? function,
        string? args,
        bool verbose)
    {
        var targetTriple = target ?? "nyar";
        var contexts = get_build_contexts(projectDir, targetTriple, null, verbose);
        if (contexts.Count == 0)
        {
            Console.WriteLine($"错误：无法为目标 '{targetTriple}' 创建构建上下文");
            return ExitCode.Error;
        }

        foreach (var ctx in contexts)
        {
            var isClr = ctx.canonical_triple.StartsWith("clr-", StringComparison.OrdinalIgnoreCase);

            Console.WriteLine($"正在构建 {projectDir} → {ctx.canonical_triple}...");

            if (isClr)
            {
                return (ExitCode)run_for_clr(projectDir, ctx, function, verbose);
            }

            var compiler = get_cached_legion_compiler(projectDir);
            LegionBuildResult result;
            try
            {
                result = compiler.build_in_memory(ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"构建异常 [{ctx.canonical_triple}]：{format_exception_chain(ex)}");
                return ExitCode.Error;
            }

            if (!result.success)
            {
                Console.WriteLine($"构建失败 [{ctx.canonical_triple}]：{result.error}");
                return ExitCode.Error;
            }

            Console.WriteLine("编译完成");

            var bytecode = result.main_artifact_content;
            if (bytecode.Length == 0)
            {
                Console.WriteLine("错误：编译产物为空");
                return ExitCode.Error;
            }

            var moduleName = Path.GetFileNameWithoutExtension(result.main_artifact);
            if (string.IsNullOrEmpty(moduleName))
            {
                moduleName = Path.GetFileName(projectDir);
            }

            var entryFunction = function
                                ?? result.run_contract?.logical_entry
                                ?? "main";

            Console.WriteLine($"正在执行 {moduleName}::{entryFunction}...");
            var vm = new NyarVm();
            vm.load(bytecode);

            var workspaceRoot = Directory.GetCurrentDirectory();
            NyarHostIntrinsics.register(vm, workspaceRoot);

            if (verbose)
            {
                dump_module_info(vm, moduleName);
            }

            try
            {
                Value[] runArgs;
                if (!string.IsNullOrEmpty(args))
                {
                    var parts = args.Split(',');
                    var argValues = new Value[parts.Length];
                    for (var i = 0; i < parts.Length; i++)
                    {
                        argValues[i] = Value.from_string(parts[i]);
                    }

                    runArgs = [Value.from_object(argValues)];
                }
                else
                {
                    runArgs = [];
                }

                var resultValue = vm.run(moduleName, entryFunction, runArgs);
                Console.WriteLine($"执行结果：{resultValue}");
                if (verbose)
                {
                    Console.WriteLine($"  值类型：{resultValue.type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行错误：{ex.Message}");
                if (ex.InnerException is not null)
                {
                    Console.WriteLine($"内部错误：{ex.InnerException.Message}");
                    Console.WriteLine($"堆栈：{ex.InnerException.StackTrace}");
                }
                else
                {
                    Console.WriteLine($"堆栈：{ex.StackTrace}");
                }

                return ExitCode.Error;
            }
        }

        return ExitCode.Success;
    }

    #endregion

    #region 项目解析

    /// <summary>
    ///     解析项目目录，依次检查指定路径和当前目录
    /// </summary>
    /// <param name="project">项目路径参数</param>
    /// <returns>项目目录路径，未找到则返回 null</returns>
    internal static string? resolve_project_dir(string? project)
    {
        if (project is null)
        {
            return Directory.GetCurrentDirectory();
        }

        if (Directory.Exists(project))
        {
            return Path.GetFullPath(project);
        }

        var candidate = Path.Combine(Directory.GetCurrentDirectory(), project);
        if (Directory.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        return null;
    }

    #endregion

    #region Workspace 支持

    /// <summary>
    ///     从 legions.von 加载 workspace 成员列表
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录</param>
    /// <returns>成员路径列表</returns>
    internal static List<string> load_workspace_members(string workspaceDir)
    {
        var legionsPath = Path.Combine(workspaceDir, "legions.von");
        if (!File.Exists(legionsPath))
        {
            return new List<string>();
        }

        var cacheKey = Path.GetFullPath(workspaceDir);
        var stamp = File.GetLastWriteTimeUtc(legionsPath).Ticks;
        if (_workspace_members_cache.TryGetValue(cacheKey, out var cachedEntry) && cachedEntry.stamp == stamp)
        {
            return [.. cachedEntry.members];
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var parser = new VonParser(diagnostics);
            var source = File.ReadAllText(legionsPath);
            var manifest = parser.deserialize(source);

            var membersField = manifest.get_field("members");
            if (membersField?.elements is not null)
            {
                var members =
                    membersField.elements
                        .Select(e => e.get_string() ?? string.Empty)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                _workspace_members_cache[cacheKey] = new WorkspaceMembersCacheEntry(stamp, members);
                return members;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析 legions.von 失败：{ex.Message}");
        }

        return new List<string>();
    }

    /// <summary>
    ///     解析 workspace 成员项目的完整路径信息。
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录</param>
    /// <returns>成员项目列表</returns>
    internal static List<WorkspaceMemberProject> resolve_workspace_member_projects(string workspaceDir)
    {
        var projects = new List<WorkspaceMemberProject>();
        foreach (var memberPath in load_workspace_members(workspaceDir))
        {
            var memberDir = Path.GetFullPath(Path.Combine(workspaceDir, memberPath));
            if (!Directory.Exists(memberDir))
            {
                continue;
            }

            projects.Add(new WorkspaceMemberProject(memberPath, Path.GetFileName(memberDir), memberDir));
        }

        return projects;
    }

    /// <summary>
    ///     检查目录是否为 Workspace（包含 legions.von）
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <returns>是否为 Workspace</returns>
    internal static bool is_workspace(string projectDir)
    {
        return File.Exists(Path.Combine(projectDir, "legions.von"));
    }

    /// <summary>
    ///     从当前项目目录向上查找 workspace 根目录。
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <returns>workspace 根目录；若不在 workspace 中则返回 null</returns>
    internal static string? resolve_workspace_root(string projectDir)
    {
        var current = Path.GetFullPath(projectDir);
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "legions.von")))
            {
                return current;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                return null;
            }

            current = parent.FullName;
        }

        return null;
    }

    /// <summary>
    ///     解析命令应使用的缓存根目录。
    ///     若项目位于 workspace 中，则统一使用 workspace 的 `.cache/`；否则回退到项目目录。
    /// </summary>
    internal static string resolve_cache_root(string projectDir)
    {
        return resolve_workspace_root(projectDir) ?? Path.GetFullPath(projectDir);
    }

    /// <summary>
    ///     解析当前项目或 workspace 的诊断输出配置。
    /// </summary>
    internal static DiagnosticRenderOptions resolve_diagnostic_options(string projectDir)
    {
        if (is_workspace(projectDir))
        {
            var workspaceRoot = try_load_von(Path.Combine(projectDir, "legions.von"));
            var workspaceSection = workspaceRoot?.get_field("workspace");
            return parse_diagnostic_options(workspaceSection?.get_field("diagnostics"));
        }

        var projectManifest = try_load_von(Path.Combine(projectDir, "legion.von"));
        return parse_diagnostic_options(projectManifest?.get_field("diagnostics"));
    }

    /// <summary>
    ///     解析当前项目或 workspace 的诊断输出配置，并应用命令行覆盖项。
    /// </summary>
    internal static bool try_resolve_diagnostic_options(
        string projectDir,
        string? format,
        string? level,
        string? color,
        string? lang,
        out DiagnosticRenderOptions options,
        out string? error)
    {
        options = resolve_diagnostic_options(projectDir);
        error = null;

        if (!string.IsNullOrWhiteSpace(format))
        {
            if (!try_parse_diagnostic_format(format, out var resolvedFormat))
            {
                error = "选项 --format 仅支持 pretty / short / detail / json";
                return false;
            }

            options = apply_diagnostic_format(format, options);
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            if (!try_parse_diagnostic_level(level, out var resolvedLevel))
            {
                error = "选项 --level 仅支持 fatal / error / warning / info / hint / debug / trace";
                return false;
            }

            options = options with
            {
                minimum_severity = resolvedLevel
            };
        }

        if (!string.IsNullOrWhiteSpace(color))
        {
            if (!try_parse_diagnostic_color(color, out var resolvedColor))
            {
                error = "选项 --color 仅支持 auto / always / never";
                return false;
            }

            options = options with
            {
                color = resolvedColor
            };
        }

        return true;
    }

    #endregion

    #region 并发安全辅助

    /// <summary>
    ///     原子写入文本文件：先写入同目录临时文件，再通过 <see cref="File.Move(string,string)" /> 原子替换目标。
    ///     多进程同时写入同一路径时，读者只会看到完整内容，不会读到半写入状态。
    ///     写入失败时保留旧文件不变。
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="content">要写入的文本内容</param>
    private static void atomic_write_all_text(string filePath, string content)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = filePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception)
        {
            // 原子替换失败时清理临时文件，旧文件保持不变。
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // 清理失败不影响主流程。
            }

            throw;
        }
    }

    /// <summary>
    ///     原子写入二进制文件：先写入同目录临时文件，再通过 <see cref="File.Move(string,string)" /> 原子替换目标。
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="content">要写入的二进制内容</param>
    private static void atomic_write_all_bytes(string filePath, byte[] content)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

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

    #endregion

    #region 诊断与上下文

    /// <summary>
    ///     读取 `von` 配置文件。
    /// </summary>
    private static SerdeValue? try_load_von(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var parser = new VonParser(diagnostics);
            var source = File.ReadAllText(filePath);
            var root = parser.deserialize(source);
            return diagnostics.has_errors ? null : root;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     将 `diagnostics.*` 配置解析为命令层输出选项。
    /// </summary>
    private static DiagnosticRenderOptions parse_diagnostic_options(SerdeValue? diagnosticsField)
    {
        var format = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_utf8_field(diagnosticsField, "format")
            : null;
        var level = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_utf8_field(diagnosticsField, "level")
            : null;
        var color = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_utf8_field(diagnosticsField, "color")
            : null;
        var language = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_utf8_field(diagnosticsField, "language")
            : null;
        var digits = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_i32_field(diagnosticsField, "digits")
            : null;
        var symbol = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_utf8_field(diagnosticsField, "symbol")
            : null;
        var showCode = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_bool_field(diagnosticsField, "show_code")
            : null;
        var showHints = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_bool_field(diagnosticsField, "show_hints")
            : null;
        var showHelp = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_bool_field(diagnosticsField, "show_help")
            : null;
        var showNote = diagnosticsField is { type: SerdeValueType.@object, fields: not null }
            ? ValkyrieValueProjector.bind_bool_field(diagnosticsField, "show_note")
            : null;

        var resolvedFormat = try_parse_diagnostic_format(format, out var parsedFormat)
            ? parsedFormat
            : DiagnosticFormat.Pretty;
        var resolvedLevel = try_parse_diagnostic_level(level, out var parsedLevel)
            ? parsedLevel
            : DiagnosticSeverity.warning;
        var resolvedColor = try_parse_diagnostic_color(color, out var parsedColor)
            ? parsedColor
            : DiagnosticColorMode.Auto;
        var resolvedLanguage = try_parse_diagnostic_language(language, out var parsedLanguage)
            ? parsedLanguage
            : DiagnosticLanguage.En;
        var resolvedSymbol = try_parse_diagnostic_symbol(symbol, out var parsedSymbol)
            ? parsedSymbol
            : DiagnosticSymbolSet.Unicode;
        var resolvedDigits = Math.Max(digits ?? 4, 1);
        var resolvedShowHints = showHints ?? ((showHelp ?? true) || (showNote ?? true));
        var options = new DiagnosticRenderOptions(
            resolvedFormat,
            resolvedLevel,
            resolvedColor,
            resolvedSymbol,
            resolvedLanguage,
            resolvedDigits,
            showCode ?? true,
            resolvedShowHints);
        return apply_diagnostic_format(format, options);
    }

    /// <summary>
    ///     解析诊断格式字符串。
    /// </summary>
    private static bool try_parse_diagnostic_format(
        string? value,
        out DiagnosticFormat format)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "pretty":
            case null:
            case "":
                format = DiagnosticFormat.Pretty;
                return true;
            case "short":
                format = DiagnosticFormat.Short;
                return true;
            case "detail":
                format = DiagnosticFormat.Detail;
                return true;
            case "json":
                format = DiagnosticFormat.Custom;
                return true;
            default:
                format = DiagnosticFormat.Pretty;
                return false;
        }
    }

    /// <summary>
    ///     将格式字符串映射到 Legion 自定义诊断适配器。
    /// </summary>
    private static DiagnosticRenderOptions apply_diagnostic_format(
        string? value,
        DiagnosticRenderOptions options)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "json" => options with
            {
                format = DiagnosticFormat.Custom,
                custom_format = "json",
                custom_adapter = LegionJsonDiagnosticAdapter.instance
            },
            "pretty" => options with
            {
                format = DiagnosticFormat.Pretty,
                custom_format = null,
                custom_adapter = null
            },
            "short" => options with
            {
                format = DiagnosticFormat.Short,
                custom_format = null,
                custom_adapter = null
            },
            "detail" => options with
            {
                format = DiagnosticFormat.Detail,
                custom_format = null,
                custom_adapter = null
            },
            null or "" => options with
            {
                format = DiagnosticFormat.Pretty,
                custom_format = null,
                custom_adapter = null
            },
            _ => options with
            {
                custom_format = null,
                custom_adapter = null
            }
        };
    }

    /// <summary>
    ///     解析诊断级别字符串。
    /// </summary>
    private static bool try_parse_diagnostic_level(string? value, out DiagnosticSeverity severity)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "f":
            case "fatal":
                severity = DiagnosticSeverity.fatal;
                return true;
            case "e":
            case "error":
                severity = DiagnosticSeverity.error;
                return true;
            case "w":
            case "warning":
            case null:
            case "":
                severity = DiagnosticSeverity.warning;
                return true;
            case "i":
            case "info":
                severity = DiagnosticSeverity.info;
                return true;
            case "h":
            case "hint":
                severity = DiagnosticSeverity.hint;
                return true;
            case "d":
            case "debug":
                severity = DiagnosticSeverity.debug;
                return true;
            case "t":
            case "trace":
                severity = DiagnosticSeverity.trace;
                return true;
            default:
                severity = DiagnosticSeverity.warning;
                return false;
        }
    }

    /// <summary>
    ///     解析诊断颜色模式。
    /// </summary>
    private static bool try_parse_diagnostic_color(
        string? value,
        out DiagnosticColorMode color)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "auto":
            case null:
            case "":
                color = DiagnosticColorMode.Auto;
                return true;
            case "always":
                color = DiagnosticColorMode.Always;
                return true;
            case "never":
                color = DiagnosticColorMode.Never;
                return true;
            default:
                color = DiagnosticColorMode.Auto;
                return false;
        }
    }

    /// <summary>
    ///     解析诊断输出语言。
    /// </summary>
    private static bool try_parse_diagnostic_language(
        string? value,
        out DiagnosticLanguage language)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "en":
            case null:
            case "":
                language = DiagnosticLanguage.En;
                return true;
            case "zh":
            case "zh-cn":
            case "zh-hans":
                language = DiagnosticLanguage.ZhHans;
                return true;
            default:
                language = DiagnosticLanguage.En;
                return false;
        }
    }

    /// <summary>
    ///     解析诊断符号字符集。
    /// </summary>
    private static bool try_parse_diagnostic_symbol(
        string? value,
        out DiagnosticSymbolSet symbol)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "unicode":
            case null:
            case "":
                symbol = DiagnosticSymbolSet.Unicode;
                return true;
            case "ascii":
                symbol = DiagnosticSymbolSet.Ascii;
                return true;
            default:
                symbol = DiagnosticSymbolSet.Unicode;
                return false;
        }
    }

    /// <summary>
    ///     获取可复用的编译上下文模板。
    /// </summary>
    internal static List<CompilationContext> get_build_contexts(
        string projectDir,
        string? explicitTarget,
        string? output,
        bool verbose)
    {
        var fullProjectDir = Path.GetFullPath(projectDir);
        var manifestPath = Path.Combine(fullProjectDir, "legion.von");
        var manifestStamp = File.Exists(manifestPath)
            ? File.GetLastWriteTimeUtc(manifestPath).Ticks
            : -1L;
        var normalizedOutput = string.IsNullOrWhiteSpace(output)
            ? string.Empty
            : Path.GetFullPath(output);
        var signature =
            $"{fullProjectDir}|{manifestStamp}|{explicitTarget ?? string.Empty}|{normalizedOutput}|{(verbose ? '1' : '0')}";

        if (_build_contexts_cache.TryGetValue(signature, out var cachedEntry))
        {
            return cachedEntry.contexts.Select(clone_compilation_context).ToList();
        }

        var contexts = _build_matrix.build_contexts(fullProjectDir, explicitTarget, output, verbose);
        var templates = contexts.Select(clone_compilation_context).ToList();
        _build_contexts_cache[signature] = new BuildContextsCacheEntry(templates);
        return contexts.Select(clone_compilation_context).ToList();
    }

    #endregion

    #region 检查方法

    /// <summary>
    ///     单次 check 命令内复用的源码闭包缓存。
    /// </summary>
    private sealed record CheckSourceClosureEntry(string[] files, List<string> errors);

    /// <summary>
    ///     执行 check 命令。
    /// </summary>
    internal static ExitCode execute_check_command(
        string projectDir,
        string? explicitTarget,
        bool verbose,
        DiagnosticRenderOptions diagnosticOptions)
    {
        var totals = is_workspace(projectDir)
            ? check_workspace(projectDir, explicitTarget, verbose, diagnosticOptions)
            : check_single_project(projectDir, explicitTarget, verbose, diagnosticOptions,
                new ConcurrentDictionary<string, CheckSourceClosureEntry>());
        DiagnosticRenderer.write_summary(diagnosticOptions, "check", totals.errors, totals.warnings);
        return totals.errors > 0 ? ExitCode.Error : ExitCode.Success;
    }

    /// <summary>
    ///     检查单个项目的全部构建上下文。
    /// </summary>
    private static (int errors, int warnings) check_single_project(
        string projectDir,
        string? explicitTarget,
        bool verbose,
        DiagnosticRenderOptions diagnosticOptions,
        ConcurrentDictionary<string, CheckSourceClosureEntry> sourceClosureCache)
    {
        var contexts = get_build_contexts(projectDir, explicitTarget, null, verbose);
        if (contexts.Count == 0 && string.IsNullOrWhiteSpace(explicitTarget))
        {
            contexts = get_build_contexts(projectDir, "nyar", null, verbose);
        }

        if (contexts.Count == 0)
        {
            Console.Error.WriteLine("错误：未指定检查目标，且 legion.von 中无 build 列表");
            return (1, 0);
        }

        var results = new (int errors, int warnings)[contexts.Count];
        var targetResolver = new TargetTripleResolver();
        Parallel.For(0, contexts.Count, i =>
        {
            results[i] = check_single_context(
                projectDir, contexts[i], explicitTarget, verbose, diagnosticOptions,
                targetResolver, sourceClosureCache);
        });

        var totals = (errors: 0, warnings: 0);
        foreach (var r in results)
        {
            totals.errors += r.errors;
            totals.warnings += r.warnings;
        }

        return totals;
    }

    /// <summary>
    ///     检查 workspace 中的所有成员项目。
    /// </summary>
    private static (int errors, int warnings) check_workspace(
        string workspaceDir,
        string? explicitTarget,
        bool verbose,
        DiagnosticRenderOptions diagnosticOptions)
    {
        var totals = (errors: 0, warnings: 0);
        var sourceClosureCache = new ConcurrentDictionary<string, CheckSourceClosureEntry>();
        var members = load_workspace_members(workspaceDir);
        if (members.Count == 0)
        {
            Console.Error.WriteLine("错误：legions.von 中无 members 或 members 列表为空");
            return (1, 0);
        }

        var results = new (int errors, int warnings)[members.Count];
        Parallel.For(0, members.Count, i =>
        {
            var memberPath = members[i];
            var memberDir = Path.GetFullPath(Path.Combine(workspaceDir, memberPath));
            if (!Directory.Exists(memberDir))
            {
                results[i] = (1, 0);
                return;
            }

            results[i] = check_single_project(memberDir, explicitTarget, verbose, diagnosticOptions, sourceClosureCache);
        });

        foreach (var r in results)
        {
            totals.errors += r.errors;
            totals.warnings += r.warnings;
        }

        return totals;
    }

    /// <summary>
    ///     对单个编译上下文执行与 <c>build</c> 一致的完整检查。
    /// </summary>
    private static (int errors, int warnings) check_single_context(
        string projectDir,
        CompilationContext compilationContext,
        string? explicitTarget,
        bool verbose,
        DiagnosticRenderOptions diagnosticOptions,
        TargetTripleResolver targetResolver,
        ConcurrentDictionary<string, CheckSourceClosureEntry> sourceClosureCache)
    {
        var compilationTarget = targetResolver.resolve_compilation_target(compilationContext.canonical_triple);
        if (compilationTarget is null)
        {
            Console.Error.WriteLine($"检查失败 [{compilationContext.canonical_triple}]：不支持的目标三元组");
            return (1, 0);
        }

        var sourceClosureKey =
            $"{Path.GetFullPath(projectDir)}|{compilationContext.canonical_triple}|{(verbose ? '1' : '0')}";
        var sourceClosure = sourceClosureCache.GetOrAdd(
            sourceClosureKey,
            _ =>
            {
                var packageGraph = new PackageGraph().resolve(projectDir, compilationTarget, verbose, includeTests: false);
                return new CheckSourceClosureEntry(packageGraph.files, packageGraph.errors);
            });
        if (sourceClosure.files.Length == 0)
        {
            Console.Error.WriteLine($"检查失败 [{compilationContext.canonical_triple}]：未找到可检查的 Valkyrie 源文件");
            return (1, 0);
        }

        var sourceFiles = sourceClosure.files
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var plan = new BuildPlan(
            Path.GetFileName(projectDir),
            compilationContext.canonical_triple,
            projectDir,
            true,
            preferred_logical_entry: compilationContext.preferred_logical_entry);

        var compiler = get_cached_valkyrie_compiler(projectDir);

        try
        {
            compiler.check_files(sourceFiles, plan);
            DiagnosticRenderer.write_context_heading(
                diagnosticOptions,
                "Checking",
                projectDir,
                compilationContext.canonical_triple);
            return DiagnosticRenderer.write_check_diagnostics(
                compiler.diagnostics.messages,
                diagnosticOptions,
                projectDir);
        }
        catch (Exception ex)
        {
            DiagnosticRenderer.write_context_heading(
                diagnosticOptions,
                "Checking",
                projectDir,
                compilationContext.canonical_triple);
            var counts = DiagnosticRenderer.write_check_diagnostics(
                compiler.diagnostics.messages,
                diagnosticOptions,
                projectDir);
            var errorCount = compiler.diagnostics.messages.Count(message => message.severity.is_error_level());
            if (errorCount == 0)
            {
                errorCount = 1;
            }

            Console.Error.WriteLine($"check failed ({compilationContext.canonical_triple}): {ex.Message}");
            return (errorCount, counts.warnings);
        }
    }

    #endregion

    #region 构建方法

    /// <summary>
    ///     Workspace 模式：读取 legions.von 中的 members 列表，依次构建每个成员项目。
    ///     产物输出路径为 dist/&lt;canonical-triple&gt;/&lt;member-name&gt;/
    ///     Workspace 模式自动启用增量编译缓存。
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录</param>
    /// <param name="target">编译目标，为 null 则使用各成员默认目标</param>
    /// <param name="output">输出路径</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>退出码</returns>
    internal static int build_workspace(string workspaceDir, string? target, string? output, bool verbose)
    {
        var members = load_workspace_members(workspaceDir);
        if (members.Count == 0)
        {
            Console.WriteLine("错误：legions.von 中无 members 或 members 列表为空");
            return 1;
        }

        Console.WriteLine($"发现 Workspace，共 {members.Count} 个成员项目（增量缓存已启用）");
        var hasFailure = false;
        var distRoot = output is not null ? output : Path.Combine(workspaceDir, "dist");
        var workItems = new List<(string member_path, string member_name, CompilationContext context)>();

        foreach (var memberPath in members)
        {
            var memberDir = Path.GetFullPath(Path.Combine(workspaceDir, memberPath));
            if (!Directory.Exists(memberDir))
            {
                Console.WriteLine($"跳过不存在的成员目录：{memberPath}");
                continue;
            }

            var memberName = Path.GetFileName(memberDir);
            var contexts = get_build_contexts(memberDir, target, null, verbose);
            if (contexts.Count == 0)
            {
                Console.WriteLine($"跳过成员 {memberPath}：未找到构建目标");
                continue;
            }

            foreach (var context in contexts)
            {
                context.output_dir = Path.Combine(distRoot, context.canonical_triple, memberName);
                workItems.Add((memberPath, memberName, context));
            }
        }

        var results = new (LegionBuildResult? result, Exception? exception)[workItems.Count];
        Parallel.For(0, workItems.Count, i =>
        {
            try
            {
                var cache = new NyarDatabaseCompilationCache(workspaceDir);
                var compiler = new LegionCompiler(cache);
                results[i] = (compiler.build(workItems[i].context), null);
            }
            catch (Exception ex)
            {
                results[i] = (null, ex);
            }
        });

        string? currentMemberPath = null;
        for (var i = 0; i < workItems.Count; i++)
        {
            var workItem = workItems[i];
            if (!string.Equals(currentMemberPath, workItem.member_path, StringComparison.Ordinal))
            {
                currentMemberPath = workItem.member_path;
                Console.WriteLine($"--- 构建成员：{workItem.member_path} ---");
            }

            Console.WriteLine($"  正在构建 {workItem.member_path} → {workItem.context.canonical_triple}...");

            var buildResult = results[i];
            if (buildResult.exception is not null)
            {
                Console.WriteLine($"  构建异常 [{workItem.context.canonical_triple}]：{format_exception_chain(buildResult.exception)}");
                hasFailure = true;
                continue;
            }

            if (buildResult.result is null || !buildResult.result.success)
            {
                Console.WriteLine($"  构建失败 [{workItem.context.canonical_triple}]：{buildResult.result?.error}");
                hasFailure = true;
                continue;
            }

            Console.WriteLine($"  构建完成 → {buildResult.result.output_directory}");
            if (verbose)
            {
                foreach (var file in buildResult.result.output_files)
                {
                    Console.WriteLine($"    产出：{file}");
                }
            }
        }

        return hasFailure ? 1 : 0;
    }

    /// <summary>
    ///     单项目模式：读取 legion.von 构建单个项目
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="target">编译目标</param>
    /// <param name="output">输出路径</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>退出码</returns>
    internal static int build_single_project(string projectDir, string? target, string? output, bool verbose)
    {
        var contexts = get_build_contexts(projectDir, target, output, verbose);
        if (contexts.Count == 0)
        {
            Console.WriteLine("错误：未指定构建目标，且 legion.von 中无 build 列表");
            return 1;
        }

        var hasFailure = false;
        var cacheRoot = resolve_cache_root(projectDir);
        var results = new (LegionBuildResult? result, Exception? exception)[contexts.Count];
        Parallel.For(0, contexts.Count, i =>
        {
            try
            {
                var compiler = new LegionCompiler(new NyarDatabaseCompilationCache(cacheRoot));
                results[i] = (compiler.build(contexts[i]), null);
            }
            catch (Exception ex)
            {
                results[i] = (null, ex);
            }
        });

        for (var i = 0; i < contexts.Count; i++)
        {
            var context = contexts[i];
            Console.WriteLine($"正在构建 {projectDir} → {context.canonical_triple}...");

            var buildResult = results[i];
            if (buildResult.exception is not null)
            {
                Console.WriteLine($"构建异常 [{context.canonical_triple}]：{format_exception_chain(buildResult.exception)}");
                hasFailure = true;
                continue;
            }

            if (buildResult.result is null || !buildResult.result.success)
            {
                Console.WriteLine($"构建失败 [{context.canonical_triple}]：{buildResult.result?.error}");
                hasFailure = true;
                continue;
            }

            Console.WriteLine($"构建完成 → {buildResult.result.output_directory}");
            if (verbose)
            {
                foreach (var file in buildResult.result.output_files)
                {
                    Console.WriteLine($"  产出：{file}");
                }
            }
        }

        return hasFailure ? 1 : 0;
    }

    /// <summary>
    ///     执行 clean 命令。
    /// </summary>
    /// <param name="project">项目路径参数</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <returns>退出码</returns>
    internal static int execute_clean_command(string? project, bool verbose)
    {
        var projectDir = resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return 1;
        }

        var directories = new[]
        {
            Path.Combine(projectDir, "dist"),
            Path.Combine(projectDir, ".cache")
        };

        var removedAny = false;
        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            try
            {
                Directory.Delete(directory, true);
                removedAny = true;
                if (verbose)
                {
                    Console.WriteLine($"已清理：{directory}");
                }
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"清理失败：{directory}，{ex.Message}");
                return 1;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"清理失败：{directory}，{ex.Message}");
                return 1;
            }
        }

        if (!removedAny && verbose)
        {
            Console.WriteLine("未发现可清理的构建目录");
        }

        return 0;
    }

    /// <summary>
    ///     执行 coverage 命令。
    /// </summary>
    /// <param name="project">项目路径参数</param>
    /// <returns>退出码</returns>
    internal static int execute_coverage_command(string? project)
    {
        var projectDir = resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return 1;
        }

        var members = is_workspace(projectDir) ? load_workspace_members(projectDir) : ["."];
        return run_coverage_for_members(projectDir, members);
    }

    /// <summary>
    ///     执行 benchmark 命令。
    /// </summary>
    /// <param name="project">项目路径参数</param>
    /// <param name="runs">运行次数</param>
    /// <param name="target">目标平台</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <returns>退出码</returns>
    internal static int execute_benchmark_command(string? project, int runs, string? target, bool verbose)
    {
        var projectDir = resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return 1;
        }

        var targets = resolve_test_targets(target);
        var aggregatedResults = new List<(string Project, string Test, string Target, double CompileMs, double RuntimeMs)>();

        if (is_workspace(projectDir))
        {
            var members = resolve_workspace_member_projects(projectDir);
            var bag = new ConcurrentBag<(string Project, string Test, string Target, double CompileMs, double RuntimeMs)>();
            Parallel.ForEach(members, member =>
            {
                var memberResults = bench_project(member.member_dir, runs, targets, verbose);
                foreach (var r in memberResults)
                {
                    bag.Add((member.member_name, r.Test, r.Target, r.CompileMs, r.RuntimeMs));
                }
            });
            aggregatedResults.AddRange(bag);
        }
        else
        {
            var projectName = Path.GetFileName(projectDir);
            var projectResults = bench_project(projectDir, runs, targets, verbose);
            aggregatedResults.AddRange(projectResults.Select(result =>
                (projectName, result.Test, result.Target, result.CompileMs, result.RuntimeMs)));
        }

        if (aggregatedResults.Count == 0)
        {
            Console.WriteLine("未发现可执行的基准测试");
            return 0;
        }

        print_bench_report(aggregatedResults, runs, projectDir);
        return 0;
    }

    #endregion

    #region 运行方法

    /// <summary>
    ///     构建并执行 CLR 目标项目
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="context">编译上下文</param>
    /// <param name="function">入口函数名</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>退出码</returns>
    internal static int run_for_clr(string projectDir, CompilationContext context, string? function, bool verbose)
    {
        var compiler = get_cached_legion_compiler(projectDir);
        LegionBuildResult result;
        try
        {
            result = compiler.build(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"构建异常 [{context.canonical_triple}]：{format_exception_chain(ex)}");
            return 1;
        }

        if (!result.success)
        {
            Console.WriteLine($"构建失败 [{context.canonical_triple}]：{result.error}");
            return 1;
        }

        Console.WriteLine("编译完成");

        var artifactPath = result.main_artifact;
        if (string.IsNullOrEmpty(artifactPath) || !File.Exists(artifactPath))
        {
            Console.WriteLine($"错误：产物不存在：{artifactPath}");
            return 1;
        }

        var tempRunDirectory = string.Empty;
        try
        {
            tempRunDirectory = Path.Combine(
                Path.GetTempPath(),
                "legion-clr-run",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRunDirectory);

            foreach (var outputFile in result.output_files)
            {
                var sourcePath = Path.Combine(result.output_directory, outputFile);
                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                var targetPath = Path.Combine(tempRunDirectory, outputFile);
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath, true);
            }

            artifactPath = Path.Combine(tempRunDirectory, Path.GetFileName(artifactPath));
            if (!File.Exists(artifactPath))
            {
                Console.WriteLine($"错误：临时运行产物不存在：{artifactPath}");
                return 1;
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"错误：复制 CLR 运行产物失败：{ex.Message}");
            return 1;
        }

        var moduleName = Path.GetFileNameWithoutExtension(artifactPath);
        if (string.IsNullOrEmpty(moduleName))
        {
            moduleName = Path.GetFileName(projectDir);
        }

        Console.WriteLine($"正在执行 {moduleName}::{function ?? moduleName}...");

        var runner = new ClrRunner();
        if (!runner.is_available())
        {
            Console.WriteLine("错误：dotnet 运行时未安装或不在 PATH 中");
            return 1;
        }

        var runResult = runner.run(artifactPath, function);
        if (runResult.success)
        {
            if (!string.IsNullOrEmpty(runResult.stdout))
            {
                Console.Write(runResult.stdout);
            }

            if (!string.IsNullOrEmpty(tempRunDirectory) && Directory.Exists(tempRunDirectory))
            {
                try
                {
                    Directory.Delete(tempRunDirectory, true);
                }
                catch
                {
                    // 临时目录清理失败不影响本次执行结果
                }
            }

            return 0;
        }

        if (!string.IsNullOrEmpty(runResult.stderr))
        {
            Console.Error.WriteLine(runResult.stderr);
        }

        if (!string.IsNullOrEmpty(tempRunDirectory) && Directory.Exists(tempRunDirectory))
        {
            try
            {
                Directory.Delete(tempRunDirectory, true);
            }
            catch
            {
                // 临时目录清理失败不影响失败结果上报
            }
        }

        return 1;
    }

    #endregion

    #region 测试方法

    /// <summary>
    ///     解析 target 参数，返回目标标签列表。"all" 展开为 ["nyar", "clr", "jvm", "node"]
    /// </summary>
    /// <param name="target">目标参数</param>
    /// <returns>目标标签列表</returns>
    internal static List<string> resolve_test_targets(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ["nyar"];
        }

        if (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase))
        {
            return ["nyar", "clr", "jvm", "node"];
        }

        return
        [
            .. target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
        ];
    }

    /// <summary>
    ///     从 .v 测试文件中发现测试函数
    /// </summary>
    /// <param name="testFiles">测试文件路径数组</param>
    /// <returns>测试函数列表</returns>
    internal static List<(string name, bool is_test, bool is_benchmark)> discover_test_functions(string[] testFiles)
    {
        var normalizedFiles = testFiles
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var signature = build_file_set_signature(normalizedFiles);
        var cacheKey = Convert.ToHexStringLower(Encoding.UTF8.GetBytes(signature));
        if (_test_discovery_cache.TryGetValue(cacheKey, out var cachedEntry) &&
            string.Equals(cachedEntry.signature, signature, StringComparison.Ordinal))
        {
            return [.. cachedEntry.functions];
        }

        var functions = new List<(string name, bool is_test, bool is_benchmark)>();

        foreach (var file in normalizedFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                var pendingAttr = "";
                var pendingModifier = "";

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith("[test]"))
                    {
                        pendingAttr = "test";
                        continue;
                    }

                    if (trimmed.StartsWith("[benchmark]"))
                    {
                        pendingAttr = "benchmark";
                        continue;
                    }

                    if (trimmed.Contains(" micro ") || trimmed.StartsWith("micro "))
                    {
                        var isTest = pendingAttr == "test" || pendingModifier == "test";
                        var isBenchmark = pendingAttr == "benchmark" || pendingModifier == "benchmark";

                        if (isTest || isBenchmark)
                        {
                            var name = extract_function_name(trimmed);
                            if (name is not null)
                            {
                                functions.Add((name, isTest, isBenchmark));
                            }
                        }

                        pendingAttr = "";
                        pendingModifier = "";
                        continue;
                    }

                    if (trimmed.StartsWith("test micro ") || trimmed.StartsWith("test "))
                    {
                        pendingModifier = "test";
                        continue;
                    }

                    if (trimmed.StartsWith("benchmark micro ") || trimmed.StartsWith("benchmark "))
                    {
                        pendingModifier = "benchmark";
                        continue;
                    }

                    if (trimmed.StartsWith("tests "))
                    {
                        var name = extract_function_name(trimmed);
                        if (name is not null)
                        {
                            functions.Add((name, true, false));
                        }

                        continue;
                    }

                    pendingAttr = "";
                }
            }
            catch
            {
            }
        }

        _test_discovery_cache[cacheKey] = new TestDiscoveryCacheEntry(signature, functions);
        return functions;
    }

    /// <summary>
    ///     从行文本中提取函数名
    /// </summary>
    /// <param name="line">源码行</param>
    /// <returns>函数名，无法提取则返回 null</returns>
    internal static string? extract_function_name(string line)
    {
        line = line.Trim();
        var idx = line.IndexOf(" micro ");
        if (idx >= 0)
        {
            line = line[(idx + 7)..];
        }
        else if (line.StartsWith("micro "))
        {
            line = line[6..];
        }
        else if (line.StartsWith("tests "))
        {
            line = line[6..];
        }
        else
        {
            return null;
        }

        var paren = line.IndexOf('(');
        if (paren > 0)
        {
            var name = line[..paren].Trim();
            name = name.Trim('`');
            return name;
        }

        return null;
    }

    /// <summary>
    ///     尝试加载项目 manifest（legion.von）
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <returns>LegionManifest 实例，加载失败则返回 null</returns>
    internal static LegionManifest? try_load_manifest_for_project(string projectDir)
    {
        var fullProjectDir = Path.GetFullPath(projectDir);
        var manifestPath = Path.Combine(fullProjectDir, "legion.von");
        var stamp = File.Exists(manifestPath)
            ? File.GetLastWriteTimeUtc(manifestPath).Ticks
            : -1L;
        if (_manifest_cache.TryGetValue(fullProjectDir, out var cachedEntry) && cachedEntry.stamp == stamp)
        {
            return cachedEntry.manifest;
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var parser = new VonParser(diagnostics);
            var manifest = LegionManifest.load(fullProjectDir, content => parser.deserialize(content));
            _manifest_cache[fullProjectDir] = new ManifestCacheEntry(stamp, manifest);
            return manifest;
        }
        catch
        {
            _manifest_cache[fullProjectDir] = new ManifestCacheEntry(stamp, null);
            return null;
        }
    }

    /// <summary>
    ///     克隆编译上下文，避免调用方修改缓存模板。
    /// </summary>
    private static CompilationContext clone_compilation_context(CompilationContext context)
    {
        var clone = new CompilationContext(context.canonical_triple, context.project_dir, context.output_dir)
        {
            arch_tag = context.arch_tag,
            abi = context.abi,
            backend_family = context.backend_family,
            verbose = context.verbose,
            preferred_logical_entry = context.preferred_logical_entry,
            include_test_sources = context.include_test_sources,
            build_options = context.build_options is null
                ? null
                : new BuildTargetOptions
                {
                    source_map = context.build_options.source_map,
                    type_script = context.build_options.type_script,
                    wat = context.build_options.wat,
                    msil = context.build_options.msil
                }
        };
        clone.source_files.AddRange(context.source_files);
        return clone;
    }

    /// <summary>
    ///     构建项目文件列表缓存签名。
    /// </summary>
    private static string build_project_file_list_signature(string[] absoluteDirectories, string pattern)
    {
        var signatureBuilder = new StringBuilder();
        signatureBuilder.Append(pattern);
        signatureBuilder.Append('\n');

        foreach (var directory in absoluteDirectories.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            signatureBuilder.Append(directory);
            signatureBuilder.Append('|');
            signatureBuilder.Append(Directory.Exists(directory) ? '1' : '0');
            signatureBuilder.Append('|');

            if (!Directory.Exists(directory))
            {
                signatureBuilder.Append('\n');
                continue;
            }

            var directories = Directory
                .EnumerateDirectories(directory, "*", SearchOption.AllDirectories)
                .Append(directory)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
            foreach (var item in directories)
            {
                signatureBuilder.Append(item);
                signatureBuilder.Append(':');
                signatureBuilder.Append(Directory.GetLastWriteTimeUtc(item).Ticks);
                signatureBuilder.Append('|');
            }

            signatureBuilder.Append('\n');
        }

        return signatureBuilder.ToString();
    }

    /// <summary>
    ///     构建文件集合签名。
    /// </summary>
    private static string build_file_set_signature(IEnumerable<string> files)
    {
        var normalizedFiles = files
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var signatureBuilder = new StringBuilder();
        foreach (var file in normalizedFiles)
        {
            signatureBuilder.Append(file);
            signatureBuilder.Append('|');
            signatureBuilder.Append(File.GetLastWriteTimeUtc(file).Ticks);
            signatureBuilder.Append('|');
            signatureBuilder.Append(new FileInfo(file).Length);
            signatureBuilder.Append('\n');
        }

        return signatureBuilder.ToString();
    }

    /// <summary>
    ///     获取项目中的所有源码 `.v` 文件。
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <returns>源码文件路径数组</returns>
    internal static string[] get_project_v_files(string projectDir)
    {
        return get_project_files(projectDir, ["source", "test"], "*.v");
    }

    /// <summary>
    ///     获取项目 `test/` 目录中的 `.v` 文件。
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <returns>测试文件路径数组</returns>
    internal static string[] get_project_test_v_files(string projectDir)
    {
        return get_project_files(projectDir, ["test"], "*.v");
    }

    /// <summary>
    ///     按目录列表收集项目文件，并复用目录时间戳签名缓存结果。
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="relativeDirectories">相对目录列表</param>
    /// <param name="pattern">文件匹配模式</param>
    /// <returns>文件路径数组</returns>
    internal static string[] get_project_files(
        string projectDir,
        IReadOnlyList<string> relativeDirectories,
        string pattern)
    {
        var fullProjectDir = Path.GetFullPath(projectDir);
        var absoluteDirectories = relativeDirectories
            .Select(directory => Path.Combine(fullProjectDir, directory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var signature = build_project_file_list_signature(absoluteDirectories, pattern);
        var cacheKey = $"{fullProjectDir}|{string.Join(";", relativeDirectories)}|{pattern}";
        if (_project_file_list_cache.TryGetValue(cacheKey, out var cachedEntry) &&
            cachedEntry.signature == signature)
        {
            return [.. cachedEntry.files];
        }

        var files = new List<string>();
        foreach (var directory in absoluteDirectories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            files.AddRange(Directory.GetFiles(directory, pattern, SearchOption.AllDirectories));
        }

        var normalizedFiles = files
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _project_file_list_cache[cacheKey] = new ProjectFileListCacheEntry(signature, normalizedFiles);
        return normalizedFiles;
    }

    /// <summary>
    ///     编译并运行一个测试函数（NyarVM 内存模式）
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="functionName">函数名</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>测试结果元组</returns>
    internal static (bool Success, bool IsCompileError, string? Error) build_and_run_test(
        string projectDir, string functionName, bool verbose)
    {
        var nyarSession = compile_nyar_test_session(projectDir);
        return nyarSession.Session is not null
            ? run_nyar_test_in_session(nyarSession.Session, functionName)
            : (false, true, nyarSession.Error ?? "编译失败");
    }

    /// <summary>
    ///     为 `nyar test` 一次性编译完整测试模块，供多个测试入口复用。
    /// </summary>
    private static (NyarTestSession? Session, string? Error) compile_nyar_test_session(
        string projectDir)
    {
        var compiler = get_cached_legion_compiler(projectDir);
        var contexts = get_build_contexts(projectDir, "nyar", null, false);
        if (contexts.Count == 0)
        {
            return (null, "无 nyar 构建上下文");
        }

        var context = contexts[0];
        context.preferred_logical_entry = null;
        context.include_test_sources = true;

        LegionBuildResult buildResult;
        try
        {
            buildResult = compiler.build_in_memory(context);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }

        if (!buildResult.success)
        {
            return (null, buildResult.error ?? "编译失败");
        }

        var bytecode = buildResult.main_artifact_content;
        if (bytecode.Length == 0)
        {
            return (null, "产物为空");
        }

        var moduleName = Path.GetFileNameWithoutExtension(buildResult.main_artifact);
        if (string.IsNullOrEmpty(moduleName))
        {
            moduleName = Path.GetFileName(projectDir);
        }

        return (new NyarTestSession(bytecode, moduleName), null);
    }

    /// <summary>
    ///     在已编译的 `nyar test` 会话中执行单个测试函数。
    /// </summary>
    private static (bool Success, bool IsCompileError, string? Error) run_nyar_test_in_session(
        NyarTestSession session,
        string functionName)
    {
        NyarVm? vm = null;
        try
        {
            vm = new NyarVm();
            vm.load(session.bytecode);
            vm.run(session.module_name, functionName);
            return (true, false, null);
        }
        catch (Exception ex)
        {
            var error = ex.Message;
            if (vm is not null && error.Contains("函数未找的", StringComparison.Ordinal))
            {
                error = $"{error}{Environment.NewLine}{describe_module_functions(vm, session.module_name)}";
            }

            return (false, false, error);
        }
    }

    /// <summary>
    ///     判断当前 target 是否支持单次构建复用。
    /// </summary>
    private static bool supports_reusable_external_test_session(string target)
    {
        return string.Equals(target, "clr", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(target, "jvm", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(target, "node", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     为外部 runner target 一次性编译完整测试产物，供多个测试入口复用。
    /// </summary>
    private static (ExternalTargetTestSession? Session, string? Error) compile_external_test_session(
        string projectDir,
        string target)
    {
        var compiler = get_cached_legion_compiler(projectDir);
        var outputDir = Path.Combine(projectDir, ".cache", "test", target);
        var contexts = get_build_contexts(projectDir, target, outputDir, false);
        if (contexts.Count == 0)
        {
            return (null, $"无 {target} 构建上下文");
        }

        var context = contexts[0];
        context.preferred_logical_entry = null;
        context.include_test_sources = true;

        LegionBuildResult buildResult;
        try
        {
            buildResult = compiler.build(context);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }

        if (!buildResult.success)
        {
            return (null, buildResult.error ?? "编译失败");
        }

        var artifactPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var outputFile in buildResult.output_files)
        {
            artifactPaths[outputFile] = Path.Combine(buildResult.output_directory, outputFile);
        }

        return (new ExternalTargetTestSession(target, buildResult.output_directory, artifactPaths), null);
    }

    /// <summary>
    ///     获取函数短名。
    /// </summary>
    private static string get_short_function_name(string functionName)
    {
        var lastDotIndex = functionName.LastIndexOf('.');
        return lastDotIndex >= 0 ? functionName[(lastDotIndex + 1)..] : functionName;
    }

    /// <summary>
    ///     规范化测试产物基名。
    /// </summary>
    private static string sanitize_test_artifact_name(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Module";
        }

        var chars = name.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray();
        var result = new string(chars);
        if (char.IsDigit(result[0]))
        {
            result = $"M_{result}";
        }

        return result;
    }

    /// <summary>
    ///     获取当前 target 可直接执行的产物后缀。
    /// </summary>
    private static string[] get_runnable_artifact_extensions(string target)
    {
        return target.ToLowerInvariant() switch
        {
            "clr" => [".exe"],
            "jvm" => [".jar"],
            "node" => [".mjs", ".js"],
            _ => []
        };
    }

    /// <summary>
    ///     列出当前会话中的可执行产物。
    /// </summary>
    private static List<string> get_runnable_artifacts(ExternalTargetTestSession session)
    {
        var extensions = get_runnable_artifact_extensions(session.target);
        return session.artifact_paths
            .Where(item => extensions.Any(ext =>
                string.Equals(Path.GetExtension(item.Key), ext, StringComparison.OrdinalIgnoreCase)))
            .Select(item => item.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    ///     为测试函数解析可执行产物路径。
    /// </summary>
    private static string? resolve_external_test_artifact_path(
        ExternalTargetTestSession session,
        string functionName)
    {
        var runnableArtifacts = get_runnable_artifacts(session);
        if (runnableArtifacts.Count == 1)
        {
            return runnableArtifacts[0];
        }

        var baseName = sanitize_test_artifact_name(get_short_function_name(functionName));
        foreach (var extension in get_runnable_artifact_extensions(session.target))
        {
            var fileName = $"{baseName}{extension}";
            if (session.artifact_paths.TryGetValue(fileName, out var artifactPath))
            {
                return artifactPath;
            }
        }

        return null;
    }

    /// <summary>
    ///     描述当前会话中的可执行产物，便于失败时诊断。
    /// </summary>
    private static string describe_runnable_artifacts(ExternalTargetTestSession session)
    {
        var runnableArtifacts = get_runnable_artifacts(session)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();
        if (runnableArtifacts.Count == 0)
        {
            return "无可执行产物";
        }

        return $"可执行产物：{string.Join(", ", runnableArtifacts)}";
    }

    /// <summary>
    ///     测试钩子：根据目标和产物清单解析单测对应的可执行产物。
    /// </summary>
    internal static string? test_resolve_external_test_artifact_path(
        string target,
        IReadOnlyDictionary<string, string> artifactPaths,
        string functionName)
    {
        var session = new ExternalTargetTestSession(target, string.Empty, new Dictionary<string, string>(artifactPaths,
            StringComparer.OrdinalIgnoreCase));
        return resolve_external_test_artifact_path(session, functionName);
    }

    /// <summary>
    ///     测试钩子：描述当前目标的可执行产物列表。
    /// </summary>
    internal static string test_describe_runnable_artifacts(
        string target,
        IReadOnlyDictionary<string, string> artifactPaths)
    {
        var session = new ExternalTargetTestSession(target, string.Empty, new Dictionary<string, string>(artifactPaths,
            StringComparer.OrdinalIgnoreCase));
        return describe_runnable_artifacts(session);
    }

    /// <summary>
    ///     在已编译的外部 target 会话中执行单个测试函数。
    /// </summary>
    private static (bool Success, bool IsCompileError, string? Error) run_external_test_in_session(
        ExternalTargetTestSession session,
        string functionName,
        IRunner runner,
        bool verbose)
    {
        var artifactPath = resolve_external_test_artifact_path(session, functionName);
        if (string.IsNullOrEmpty(artifactPath) || !File.Exists(artifactPath))
        {
            return (false, true, $"未找到测试 `{functionName}` 对应的产物。{describe_runnable_artifacts(session)}");
        }

        var entryPoint = string.Equals(session.target, "jvm", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(Path.GetExtension(artifactPath), ".jar", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(artifactPath)
            : null;
        var runResult = runner.run(artifactPath, entryPoint);
        if (runResult.success)
        {
            if (verbose && !string.IsNullOrEmpty(runResult.stdout))
            {
                Console.WriteLine();
                Console.WriteLine($"      stdout: {runResult.stdout.TrimEnd()}");
            }

            return (true, false, null);
        }

        var errorInfo = runResult.stderr;
        if (string.IsNullOrEmpty(errorInfo))
        {
            errorInfo = runResult.stdout;
        }

        return (false, false, errorInfo);
    }

    /// <summary>
    ///     编译并在指定 target 的 runner 中执行测试函数
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="functionName">函数名</param>
    /// <param name="target">目标平台</param>
    /// <param name="runner">运行器实例</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>测试结果元组</returns>
    internal static (bool Success, bool IsCompileError, string? Error) build_and_run_test_for_target(
        string projectDir, string functionName, string target, IRunner runner, bool verbose)
    {
        var compiler = get_cached_legion_compiler(projectDir);
        try
        {
            var outputDir = Path.Combine(projectDir, ".cache", "test", target);
            var contexts = get_build_contexts(projectDir, target, outputDir, false);
            if (contexts.Count == 0)
            {
                return (false, true, $"无 {target} 构建上下文");
            }

            var context = contexts[0];
            context.preferred_logical_entry = functionName;
            context.include_test_sources = true;
            LegionBuildResult buildResult;
            try
            {
                buildResult = compiler.build(context);
            }
            catch (Exception ex)
            {
                return (false, true, ex.Message);
            }

            if (!buildResult.success)
            {
                return (false, true, buildResult.error ?? "编译失败");
            }

            var artifactPath = buildResult.main_artifact;
            if (string.IsNullOrEmpty(artifactPath) || !File.Exists(artifactPath))
            {
                return (false, true, $"产物不存在：{artifactPath}");
            }

            if (target == "jvm")
            {
                var jarPath = Path.ChangeExtension(artifactPath, ".jar");
                if (!string.IsNullOrWhiteSpace(jarPath) && File.Exists(jarPath))
                {
                    artifactPath = jarPath;
                }
            }

            var moduleName = Path.GetFileNameWithoutExtension(artifactPath);
            if (string.IsNullOrEmpty(moduleName))
            {
                moduleName = Path.GetFileName(projectDir);
            }

            var entryPoint = buildResult.run_contract?.logical_entry ?? functionName;
            if (target == "clr" || target == "jvm")
            {
                entryPoint = moduleName;
            }

            var runResult = runner.run(artifactPath, entryPoint);
            if (runResult.success)
            {
                if (verbose && !string.IsNullOrEmpty(runResult.stdout))
                {
                    Console.WriteLine();
                    Console.WriteLine($"      stdout: {runResult.stdout.TrimEnd()}");
                }

                return (true, false, null);
            }

            var errorInfo = runResult.stderr;
            if (string.IsNullOrEmpty(errorInfo))
            {
                errorInfo = runResult.stdout;
            }

            return (false, false, errorInfo);
        }
        catch (Exception ex)
        {
            return (false, true, ex.Message);
        }
    }

    /// <summary>
    ///     运行单个项目的所有测试用例（单目标）
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="filter">过滤器</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>测试结果统计</returns>
    internal static (int Passed, int Failed, int Skipped) run_tests_for_project(
        string projectDir, string? filter, bool verbose)
    {
        var passed = 0;
        var failed = 0;
        var skipped = 0;

        var testDir = Path.Combine(projectDir, "test");
        if (!Directory.Exists(testDir))
        {
            Console.WriteLine("  无 test/ 目录，跳过");
            return (0, 0, 0);
        }

        var testFiles = get_project_test_v_files(projectDir);
        if (testFiles.Length == 0)
        {
            Console.WriteLine("  test/ 目录为空，跳过");
            return (0, 0, 0);
        }

        var testFunctions = discover_test_functions(testFiles);
        if (testFunctions.Count == 0)
        {
            Console.WriteLine("  未发现 [test] 标注的测试函数");
            return (0, 0, 0);
        }

        var compileCandidates = testFunctions
            .Where(tf =>
                !tf.is_benchmark &&
                (filter is null || tf.name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var nyarSession = (Session: (NyarTestSession?)null, Error: (string?)null);
        if (compileCandidates.Count > 0)
        {
            nyarSession = compile_nyar_test_session(projectDir);
        }

        foreach (var tf in testFunctions)
        {
            if (filter is not null && !tf.name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
                if (verbose)
                {
                    Console.WriteLine($"  - {tf.name} ... 已跳过（过滤）");
                }

                continue;
            }

            if (tf.is_benchmark)
            {
                skipped++;
                if (verbose)
                {
                    Console.WriteLine($"  - {tf.name} ... 已跳过（[benchmark]）");
                }

                continue;
            }

            Console.Write($"  {tf.name} ... ");

            try
            {
                (bool Success, bool IsCompileError, string? Error) result = nyarSession.Session is not null
                    ? run_nyar_test_in_session(nyarSession.Session, tf.name)
                    : (false, true, nyarSession.Error ?? "编译失败");
                if (result.Success)
                {
                    Console.WriteLine("ok");
                    passed++;
                }
                else if (result.IsCompileError)
                {
                    Console.WriteLine("COMPILE ERROR");
                    if (verbose && result.Error is not null)
                    {
                        Console.WriteLine($"      {result.Error}");
                    }

                    failed++;
                }
                else
                {
                    Console.WriteLine("FAILED");
                    if (result.Error is not null)
                    {
                        Console.WriteLine($"      {result.Error}");
                    }

                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR（{ex.Message}）");
                failed++;
            }
        }

        return (passed, failed, skipped);
    }

    /// <summary>
    ///     多 target 运行单项目测试
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="filter">过滤器</param>
    /// <param name="targets">目标列表</param>
    /// <param name="explicitRunner">显式运行器</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>测试结果统计与条目列表</returns>
    internal static (int Passed, int Failed, int Skipped, List<TestResultEntry> Results)
        run_tests_for_project_multi_target(
            string projectDir,
            string? filter,
            List<string> targets,
            string? explicitRunner,
            bool verbose)
    {
        var testDir = Path.Combine(projectDir, "test");
        if (!Directory.Exists(testDir))
        {
            Console.WriteLine("  无 test/ 目录，跳过");
            return (0, 0, 0, []);
        }

        var testFiles = get_project_test_v_files(projectDir);
        if (testFiles.Length == 0)
        {
            Console.WriteLine("  test/ 目录为空，跳过");
            return (0, 0, 0, []);
        }

        var testFunctions = discover_test_functions(testFiles);
        if (testFunctions.Count == 0)
        {
            Console.WriteLine("  未发现 [test] 标注的测试函数");
            return (0, 0, 0, []);
        }

        var manifest = try_load_manifest_for_project(projectDir);
        var runnerConfigs = manifest?.runner_configs;
        var runnableTestFunctions = testFunctions
            .Where(tf =>
                !tf.is_benchmark &&
                (filter is null || tf.name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var totalPassed = 0;
        var totalFailed = 0;
        var totalSkipped = 0;
        var results = new List<TestResultEntry>();

        foreach (var runnerTarget in targets)
        {
            if (string.Equals(runnerTarget, "nyar", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  --- {runnerTarget} ---");
                var nyarSession = runnableTestFunctions.Count > 0
                    ? compile_nyar_test_session(projectDir)
                    : (Session: (NyarTestSession?)null, Error: (string?)null);

                foreach (var tf in testFunctions)
                {
                    if (filter is not null && !tf.name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    {
                        if (verbose)
                        {
                            Console.WriteLine($"    - {tf.name} ... 已跳过（过滤）");
                        }

                        totalSkipped++;
                        results.Add(new TestResultEntry
                        {
                            Name = tf.name,
                            Status = "skip",
                            Target = runnerTarget,
                            Error = "被过滤器排除"
                        });
                        continue;
                    }

                    if (tf.is_benchmark)
                    {
                        if (verbose)
                        {
                            Console.WriteLine($"    - {tf.name} ... 已跳过（[benchmark]）");
                        }

                        totalSkipped++;
                        results.Add(new TestResultEntry
                        {
                            Name = tf.name,
                            Status = "skip",
                            Target = runnerTarget,
                            Error = "[benchmark] 标注"
                        });
                        continue;
                    }

                    Console.Write($"    {tf.name} ... ");

                    try
                    {
                        (bool Success, bool IsCompileError, string? Error) result = nyarSession.Session is not null
                            ? run_nyar_test_in_session(nyarSession.Session, tf.name)
                            : (false, true, nyarSession.Error ?? "编译失败");
                        if (result.Success)
                        {
                            Console.WriteLine("ok");
                            totalPassed++;
                            results.Add(new TestResultEntry
                            {
                                Name = tf.name,
                                Status = "pass",
                                Target = runnerTarget
                            });
                        }
                        else if (result.IsCompileError)
                        {
                            Console.WriteLine("COMPILE ERROR");
                            if (verbose && result.Error is not null)
                            {
                                Console.WriteLine($"      {result.Error}");
                            }

                            totalFailed++;
                            results.Add(new TestResultEntry
                            {
                                Name = tf.name,
                                Status = "compile_error",
                                Target = runnerTarget,
                                Error = result.Error
                            });
                        }
                        else
                        {
                            Console.WriteLine("FAILED");
                            if (result.Error is not null)
                            {
                                Console.WriteLine($"      {result.Error}");
                            }

                            totalFailed++;
                            results.Add(new TestResultEntry
                            {
                                Name = tf.name,
                                Status = "fail",
                                Target = runnerTarget,
                                Error = result.Error
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR（{ex.Message}）");
                        totalFailed++;
                        results.Add(new TestResultEntry
                        {
                            Name = tf.name,
                            Status = "fail",
                            Target = runnerTarget,
                            Error = ex.Message
                        });
                    }
                }

                continue;
            }

            var runner = RunnerResolver.resolve(runnerTarget, runnerConfigs, explicitRunner);
            if (runner is null)
            {
                Console.WriteLine($"  未知目标 '{runnerTarget}'，跳过");
                var skippedCount = testFunctions.Count(tf => !tf.is_benchmark);
                totalSkipped += skippedCount;
                foreach (var tf in testFunctions.Where(tf => !tf.is_benchmark))
                {
                    results.Add(new TestResultEntry
                    {
                        Name = tf.name,
                        Status = "skip",
                        Target = runnerTarget,
                        Error = "未知目标"
                    });
                }

                continue;
            }

            if (!runner.is_available())
            {
                Console.WriteLine($"  跳过 {runnerTarget}：{runnerTarget} 运行时未安装或不在 PATH 中");
                var skippedCount = testFunctions.Count(tf => !tf.is_benchmark);
                totalSkipped += skippedCount;
                foreach (var tf in testFunctions.Where(tf => !tf.is_benchmark))
                {
                    results.Add(new TestResultEntry
                    {
                        Name = tf.name,
                        Status = "skip",
                        Target = runnerTarget,
                        Error = "运行时未安装"
                    });
                }

                continue;
            }

            var useReusableSession = supports_reusable_external_test_session(runnerTarget);
            var externalSession = useReusableSession && runnableTestFunctions.Count > 0
                ? compile_external_test_session(projectDir, runnerTarget)
                : (Session: (ExternalTargetTestSession?)null, Error: (string?)null);

            Console.WriteLine($"  --- {runnerTarget} ---");

            foreach (var tf in testFunctions)
            {
                if (filter is not null && !tf.name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    if (verbose)
                    {
                        Console.WriteLine($"    - {tf.name} ... 已跳过（过滤）");
                    }

                    totalSkipped++;
                    results.Add(new TestResultEntry
                    {
                        Name = tf.name,
                        Status = "skip",
                        Target = runnerTarget,
                        Error = "被过滤器排除"
                    });
                    continue;
                }

                if (tf.is_benchmark)
                {
                    if (verbose)
                    {
                        Console.WriteLine($"    - {tf.name} ... 已跳过（[benchmark]）");
                    }

                    totalSkipped++;
                    results.Add(new TestResultEntry
                    {
                        Name = tf.name,
                        Status = "skip",
                        Target = runnerTarget,
                        Error = "[benchmark] 标注"
                    });
                    continue;
                }

                Console.Write($"    {tf.name} ... ");

                try
                {
                    (bool Success, bool IsCompileError, string? Error) result = useReusableSession
                        ? externalSession.Session is not null
                            ? run_external_test_in_session(externalSession.Session, tf.name, runner, verbose)
                            : (false, true, externalSession.Error ?? "编译失败")
                        : build_and_run_test_for_target(projectDir, tf.name, runnerTarget, runner, verbose);
                    if (result.Success)
                    {
                        Console.WriteLine("ok");
                        totalPassed++;
                        results.Add(new TestResultEntry
                        {
                            Name = tf.name,
                            Status = "pass",
                            Target = runnerTarget
                        });
                    }
                    else if (result.IsCompileError)
                    {
                        Console.WriteLine("COMPILE ERROR");
                        if (verbose && result.Error is not null)
                        {
                            Console.WriteLine($"      {result.Error}");
                        }

                        totalFailed++;
                        results.Add(new TestResultEntry
                        {
                            Name = tf.name,
                            Status = "compile_error",
                            Target = runnerTarget,
                            Error = result.Error
                        });
                    }
                    else
                    {
                        Console.WriteLine("FAILED");
                        if (result.Error is not null)
                        {
                            Console.WriteLine($"      {result.Error}");
                        }

                        totalFailed++;
                        results.Add(new TestResultEntry
                        {
                            Name = tf.name,
                            Status = "fail",
                            Target = runnerTarget,
                            Error = result.Error
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR（{ex.Message}）");
                    totalFailed++;
                    results.Add(new TestResultEntry
                    {
                        Name = tf.name,
                        Status = "fail",
                        Target = runnerTarget,
                        Error = ex.Message
                    });
                }
            }
        }

        return (totalPassed, totalFailed, totalSkipped, results);
    }

    /// <summary>
    ///     多 target 运行 workspace 测试。
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录</param>
    /// <param name="filter">过滤器</param>
    /// <param name="targets">目标列表</param>
    /// <param name="explicitRunner">显式运行器</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>测试结果统计与条目列表</returns>
    internal static (int Passed, int Failed, int Skipped, List<TestResultEntry> Results)
        run_tests_for_workspace_multi_target(
            string workspaceDir,
            string? filter,
            List<string> targets,
            string? explicitRunner,
            bool verbose)
    {
        var members = resolve_workspace_member_projects(workspaceDir);
        if (members.Count == 0)
        {
            Console.Error.WriteLine("错误：legions.von 中无 members 或 members 列表为空");
            return (0, 1, 0, []);
        }

        var totalPassed = 0;
        var totalFailed = 0;
        var totalSkipped = 0;
        var allResults = new List<TestResultEntry>();

        var memberResults = new ConcurrentBag<(int passed, int failed, int skipped, List<TestResultEntry> results)>();

        Parallel.ForEach(members, member =>
        {
            Console.WriteLine($"--- {member.member_name} ---");
            var (passed, failed, skipped, results) = run_tests_for_project_multi_target(
                member.member_dir,
                filter,
                targets,
                explicitRunner,
                verbose);

            foreach (var result in results)
            {
                allResults.Add(new TestResultEntry
                {
                    Name = $"{member.member_name}::{result.Name}",
                    Status = result.Status,
                    Error = result.Error,
                    Target = result.Target
                });
            }

            Console.WriteLine();
            memberResults.Add((passed, failed, skipped, results));
        });

        foreach (var (p, f, s, _) in memberResults)
        {
            totalPassed += p;
            totalFailed += f;
            totalSkipped += s;
        }

        return (totalPassed, totalFailed, totalSkipped, allResults);
    }

    #endregion

    #region 基准测试方法

    /// <summary>
    ///     对项目运行基准测试
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="runs">运行次数</param>
    /// <param name="targets">目标列表</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>基准测试结果列表</returns>
    internal static List<(string Test, string Target, double CompileMs, double RuntimeMs)> bench_project(
        string projectDir, int runs, List<string> targets, bool verbose)
    {
        var compiler = get_cached_legion_compiler(projectDir);
        var results = new List<(string Test, string Target, double CompileMs, double RuntimeMs)>();

        var testDir = Path.Combine(projectDir, "test");
        if (!Directory.Exists(testDir))
        {
            return results;
        }

        var testFiles = get_project_test_v_files(projectDir);
        if (testFiles.Length == 0)
        {
            return results;
        }

        var testFunctions = discover_test_functions(testFiles).Where(f => f.is_benchmark).ToList();
        if (testFunctions.Count == 0)
        {
            Console.WriteLine("  未发现 [benchmark] 标注的函数");
            return results;
        }

        foreach (var benchTarget in targets)
        {
            var manifest = try_load_manifest_for_project(projectDir);
            var runnerConfigs = manifest?.runner_configs;

            var runner = RunnerResolver.resolve(benchTarget, runnerConfigs, null);
            if (runner is null)
            {
                Console.WriteLine($"  未知目标 '{benchTarget}'，跳过基准测试");
                continue;
            }

            if (!runner.is_available())
            {
                Console.WriteLine($"  跳过 {benchTarget}：{benchTarget} 运行时未安装或不在 PATH 中");
                continue;
            }

            foreach (var tf in testFunctions)
            {
                var compileTimes = new List<double>();
                var runtimeTimes = new List<double>();

                for (var r = 0; r < runs; r++)
                {
                    var compileSw = Stopwatch.StartNew();
                    var outputDir = Path.Combine(projectDir, ".cache", "bench", benchTarget);
                    var contexts = get_build_contexts(projectDir, benchTarget, outputDir, verbose);
                    if (contexts.Count == 0)
                    {
                        continue;
                    }

                    LegionBuildResult buildResult;
                    try
                    {
                        buildResult = compiler.build(contexts[0]);
                    }
                    catch
                    {
                        continue;
                    }

                    compileSw.Stop();

                    if (!buildResult.success || string.IsNullOrEmpty(buildResult.main_artifact) ||
                        !File.Exists(buildResult.main_artifact))
                    {
                        continue;
                    }

                    var artifactPath = buildResult.main_artifact;
                    if (benchTarget == "jvm")
                    {
                        var jarPath = Path.ChangeExtension(artifactPath, ".jar");
                        if (!string.IsNullOrWhiteSpace(jarPath) && File.Exists(jarPath))
                        {
                            artifactPath = jarPath;
                        }
                    }

                    var moduleName = Path.GetFileNameWithoutExtension(artifactPath);
                    if (string.IsNullOrEmpty(moduleName))
                    {
                        moduleName = Path.GetFileName(projectDir);
                    }

                    var entryPoint = tf.name;
                    if (benchTarget == "clr" || benchTarget == "jvm")
                    {
                        entryPoint = moduleName;
                    }

                    var runtimeSw = Stopwatch.StartNew();
                    try
                    {
                        runner.run(artifactPath, entryPoint);
                    }
                    catch
                    {
                    }

                    runtimeSw.Stop();

                    compileTimes.Add(compileSw.Elapsed.TotalMilliseconds);
                    runtimeTimes.Add(runtimeSw.Elapsed.TotalMilliseconds);
                }

                if (compileTimes.Count > 0 && runtimeTimes.Count > 0)
                {
                    results.Add((tf.name, benchTarget, compileTimes.Average(), runtimeTimes.Average()));
                }
            }
        }

        return results;
    }

    /// <summary>
    ///     打印基准测试报告
    /// </summary>
    /// <param name="results">基准测试结果</param>
    /// <param name="runs">运行次数</param>
    /// <param name="projectDir">项目目录</param>
    internal static void print_bench_report(
        List<(string Project, string Test, string Target, double CompileMs, double RuntimeMs)> results, int runs,
        string projectDir)
    {
        Console.WriteLine();
        Console.WriteLine($"基准结果（{runs} 次运行）：");
        Console.WriteLine(new string('-', 72));
        Console.WriteLine($"{"项目",-20} {"测试",-14} {"目标",-8} {"编译(ms)",-10} {"运行(ms)",-10}");
        Console.WriteLine(new string('-', 72));

        foreach (var (project, test, target, compileMs, runtimeMs) in results)
        {
            Console.WriteLine($"{project,-20} {test,-14} {target,-8} {compileMs,8:F1}   {runtimeMs,8:F1}");
        }

        Console.WriteLine(new string('-', 72));

        generate_bench_html(results, runs, projectDir);
    }

    /// <summary>
    ///     生成基准测试 HTML 报告
    /// </summary>
    /// <param name="results">基准测试结果</param>
    /// <param name="runs">运行次数</param>
    /// <param name="projectDir">项目目录</param>
    internal static void generate_bench_html(
        List<(string Project, string Test, string Target, double CompileMs, double RuntimeMs)> results, int runs,
        string projectDir)
    {
        if (results.Count == 0)
        {
            return;
        }

        var outputDir = Path.Combine(projectDir, "dist", "legion-benchmark");
        Directory.CreateDirectory(outputDir);

        var htmlPath = Path.Combine(outputDir, "index.html");

        var sorted = results
            .OrderBy(r => r.Project)
            .ThenBy(r => r.Test)
            .ThenBy(r => r.Target)
            .ToList();

        var projects = sorted.Select(r => r.Project).Distinct().Count();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<title>Benchmark Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("  * { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine(
            "  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5f5; color: #333; padding: 40px; }");
        sb.AppendLine(
            "  .container { max-width: 960px; margin: 0 auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); padding: 32px; }");
        sb.AppendLine("  h1 { font-size: 24px; margin-bottom: 8px; }");
        sb.AppendLine("  .summary { color: #666; margin-bottom: 24px; font-size: 14px; }");
        sb.AppendLine("  table { width: 100%; border-collapse: collapse; font-size: 14px; }");
        sb.AppendLine("  thead { background: #2c3e50; color: #fff; }");
        sb.AppendLine("  th { padding: 12px 16px; text-align: left; font-weight: 600; }");
        sb.AppendLine("  td { padding: 10px 16px; border-bottom: 1px solid #eee; }");
        sb.AppendLine("  tbody tr:nth-child(even) { background: #f9f9f9; }");
        sb.AppendLine("  tbody tr:nth-child(odd) { background: #fff; }");
        sb.AppendLine("  tbody tr:hover { background: #eef5ff; }");
        sb.AppendLine("  .num { text-align: right; font-variant-numeric: tabular-nums; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("<h1>Benchmark Report</h1>");
        sb.AppendLine($"<p class=\"summary\">{results.Count} 项基准测试 · {projects} 个项目 · 每项运行 {runs} 次</p>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Project</th>");
        sb.AppendLine("<th>Test</th>");
        sb.AppendLine("<th>Target</th>");
        sb.AppendLine("<th class=\"num\">Compile Time (ms)</th>");
        sb.AppendLine("<th class=\"num\">Runtime (ms)</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        foreach (var (project, test, target, compileMs, runtimeMs) in sorted)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{project}</td>");
            sb.AppendLine($"<td>{test}</td>");
            sb.AppendLine($"<td>{target}</td>");
            sb.AppendLine($"<td class=\"num\">{compileMs:F1}</td>");
            sb.AppendLine($"<td class=\"num\">{runtimeMs:F1}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        atomic_write_all_text(htmlPath, sb.ToString());
        Console.WriteLine($"  HTML 报告已生成: {htmlPath}");
    }

    #endregion

    #region 覆盖率方法

    /// <summary>
    ///     为 Workspace 成员运行覆盖率分析
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录</param>
    /// <param name="members">成员列表</param>
    /// <returns>退出码</returns>
    internal static int run_coverage_for_members(string workspaceDir, List<string> members)
    {
        if (members.Count == 0)
        {
            Console.WriteLine("错误：legions.von 中无 members");
            return 1;
        }

        var knownFeatures = new Dictionary<string, string>
        {
            ["micro"] = "micro 函数",
            ["mezzo"] = "mezzo 函数",
            ["structure"] = "structure 值类型",
            ["class"] = "class 继承",
            ["enums"] = "enums 枚举",
            ["flags"] = "flags 位标志",
            ["union"] = "union 联合类型",
            ["unite"] = "unite 紧凑联合",
            ["trait"] = "trait",
            ["match"] = "模式匹配",
            ["loop"] = "控制流循环",
            ["closure"] = "闭包/lambda",
            ["pipe"] = "管道表达式",
            ["nullable"] = "可空类型",
            ["integer"] = "整数类型",
            ["float"] = "浮点类型",
            ["if-expr"] = "if 表达式",
            ["multi-file"] = "多文件编译",
            ["test"] = "测试框架",
            ["benchmark"] = "基准测试"
        };

        var coverageMap = new Dictionary<string, (bool Covered, List<string> Projects)>();
        foreach (var f in knownFeatures.Keys)
        {
            coverageMap[f] = (false, []);
        }

        foreach (var memberPath in members)
        {
            var memberDir = Path.GetFullPath(Path.Combine(workspaceDir, memberPath));
            if (!Directory.Exists(memberDir))
            {
                continue;
            }

            var memberName = Path.GetFileName(memberDir);
            var inferred = infer_coverage_features(memberDir);

            foreach (var feature in inferred)
            {
                if (coverageMap.TryGetValue(feature, out var info))
                {
                    if (!info.Projects.Contains(memberName))
                    {
                        info.Projects.Add(memberName);
                    }

                    coverageMap[feature] = (true, info.Projects);
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("语法覆盖报告：");
        Console.WriteLine(new string('-', 72));
        Console.WriteLine($"{"特性",-24} {"覆盖",-8} {"测试项目"}");
        Console.WriteLine(new string('-', 72));

        var coveredCount = 0;
        foreach (var (feature, (isCovered, projects)) in coverageMap)
        {
            var displayName = knownFeatures.GetValueOrDefault(feature, feature);
            var status = isCovered ? "\u2713" : "\u2717";
            var projStr = string.Join(", ", projects.Take(3));
            if (projects.Count > 3)
            {
                projStr += $" ... +{projects.Count - 3}";
            }

            if (!isCovered)
            {
                projStr = "-";
            }

            Console.WriteLine($"{displayName,-24} {status,-8} {projStr}");
            if (isCovered)
            {
                coveredCount++;
            }
        }

        Console.WriteLine(new string('-', 72));
        var pct = (double)coveredCount / coverageMap.Count * 100;
        Console.WriteLine($"覆盖率：{coveredCount}/{coverageMap.Count}（{pct:F0}%）");

        var cacheDir = Path.Combine(workspaceDir, ".cache", "converge");
        Directory.CreateDirectory(cacheDir);
        var jsonPath = Path.Combine(cacheDir, "coverage.json");
        var jsonContent = build_coverage_json(knownFeatures, coverageMap, coveredCount);
        atomic_write_all_text(jsonPath, jsonContent);

        var reportDir = Path.Combine(workspaceDir, "dist", "legion-converge");
        Directory.CreateDirectory(reportDir);
        var htmlPath = Path.Combine(reportDir, "index.html");
        var htmlContent = build_coverage_html(knownFeatures, coverageMap, coveredCount);
        atomic_write_all_text(htmlPath, htmlContent);
        Console.WriteLine($"HTML 覆盖率报告已生成：{htmlPath}");

        return 0;
    }

    /// <summary>
    ///     推断项目使用的语法特性
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <returns>特性列表</returns>
    internal static List<string> infer_coverage_features(string projectDir)
    {
        var projectFiles = get_project_v_files(projectDir);
        var signature = build_file_set_signature(projectFiles);
        var cacheKey = Path.GetFullPath(projectDir);
        if (_coverage_features_cache.TryGetValue(cacheKey, out var cachedEntry) &&
            string.Equals(cachedEntry.signature, signature, StringComparison.Ordinal))
        {
            return [.. cachedEntry.features];
        }

        var features = new List<string>();

        void AddOnce(string f)
        {
            if (!features.Contains(f))
            {
                features.Add(f);
            }
        }

        void ScanFile(string file)
        {
            try
            {
                var content = File.ReadAllText(file);

                if (content.Contains("test micro ") || content.Contains("tests ") ||
                    content.Contains("[test]"))
                {
                    AddOnce("test");
                }

                if (content.Contains("micro "))
                {
                    AddOnce("micro");
                }

                if (content.Contains("mezzo "))
                {
                    AddOnce("mezzo");
                }

                if (content.Contains("structure "))
                {
                    AddOnce("structure");
                }

                if (content.Contains("class ") && content.Contains("("))
                {
                    AddOnce("class");
                }

                if (content.Contains("enums "))
                {
                    AddOnce("enums");
                }

                if (content.Contains("flags "))
                {
                    AddOnce("flags");
                }

                if (content.Contains("union "))
                {
                    AddOnce("union");
                }

                if (content.Contains("unite "))
                {
                    AddOnce("unite");
                }

                if (content.Contains("trait "))
                {
                    AddOnce("trait");
                }

                if (content.Contains("match "))
                {
                    AddOnce("match");
                }

                if (content.Contains("while ") || content.Contains("loop ") || content.Contains("for "))
                {
                    AddOnce("loop");
                }

                if (content.Contains("=> ") || content.Contains(".filter(") || content.Contains(".map("))
                {
                    AddOnce("closure");
                }

                if (content.Contains("|>"))
                {
                    AddOnce("pipe");
                }

                if (content.Contains("?") &&
                    (content.Contains("i32?") || content.Contains("string?") || content.Contains("= null")))
                {
                    AddOnce("nullable");
                }

                if (content.Contains(": i8") || content.Contains(": i16") || content.Contains(": i64") ||
                    content.Contains(": u8"))
                {
                    AddOnce("integer");
                }

                if (content.Contains(": f32") || content.Contains(": f64"))
                {
                    AddOnce("float");
                }

                if (content.Contains("= if ") && content.Contains("else"))
                {
                    AddOnce("if-expr");
                }

                if (content.Contains("using "))
                {
                    AddOnce("multi-file");
                }

                if (content.Contains("[benchmark]"))
                {
                    AddOnce("benchmark");
                }
            }
            catch
            {
            }
        }

        foreach (var file in projectFiles)
        {
            ScanFile(file);
        }

        _coverage_features_cache[cacheKey] = new CoverageFeaturesCacheEntry(signature, [.. features]);
        return features;
    }

    /// <summary>
    ///     构建覆盖率 JSON 内容
    /// </summary>
    /// <param name="knownFeatures">已知特性映射</param>
    /// <param name="coverageMap">覆盖率映射</param>
    /// <param name="coveredCount">已覆盖数量</param>
    /// <returns>JSON 字符串</returns>
    internal static string build_coverage_json(Dictionary<string, string> knownFeatures,
        Dictionary<string, (bool Covered, List<string> Projects)> coverageMap, int coveredCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"covered\": {coveredCount},");
        sb.AppendLine($"  \"total\": {coverageMap.Count},");
        var pct = (double)coveredCount / coverageMap.Count * 100;
        sb.AppendLine($"  \"percentage\": {pct:F1},");
        sb.AppendLine("  \"features\": [");
        var first = true;
        foreach (var (feature, (isCovered, projects)) in coverageMap)
        {
            if (!first)
            {
                sb.AppendLine(",");
            }

            first = false;
            var displayName = knownFeatures.GetValueOrDefault(feature, feature);
            var projArr = string.Join("\", \"", projects);
            sb.Append("    { ");
            sb.Append($"\"feature\": \"{feature}\", ");
            sb.Append($"\"display\": \"{displayName}\", ");
            sb.Append($"\"covered\": {(isCovered ? "true" : "false")}, ");
            sb.Append($"\"projects\": [\"{projArr}\"] }}");
        }

        sb.AppendLine();
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    ///     构建覆盖率 HTML 内容
    /// </summary>
    /// <param name="knownFeatures">已知特性映射</param>
    /// <param name="coverageMap">覆盖率映射</param>
    /// <param name="coveredCount">已覆盖数量</param>
    /// <returns>HTML 字符串</returns>
    internal static string build_coverage_html(Dictionary<string, string> knownFeatures,
        Dictionary<string, (bool Covered, List<string> Projects)> coverageMap, int coveredCount)
    {
        var total = coverageMap.Count;
        var pct = (double)coveredCount / total * 100;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<title>Coverage Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("  * { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine(
            "  body { font-family: system-ui, -apple-system, sans-serif; max-width: 900px; margin: 0 auto; padding: 2rem; background: #f0f2f5; color: #333; }");
        sb.AppendLine(
            "  h1 { text-align: center; color: #1a1a2e; font-size: 2rem; margin-bottom: 1.5rem; }");
        sb.AppendLine(
            "  .summary { text-align: center; font-size: 1.4rem; margin: 1.5rem 0; color: #555; }");
        sb.AppendLine("  .summary .pct { font-weight: 700; color: #1a1a2e; }");
        sb.AppendLine(
            "  .progress-container { background: #e0e0e0; border-radius: 12px; height: 28px; margin: 1.5rem 0; overflow: hidden; box-shadow: inset 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine(
            "  .progress-fill { background: linear-gradient(90deg, #43a047, #66bb6a, #aed581); height: 100%; border-radius: 12px; transition: width 0.5s ease; display: flex; align-items: center; justify-content: center; color: #fff; font-size: 0.85rem; font-weight: 600; min-width: 2rem; }");
        sb.AppendLine(
            "  .progress-fill.low { background: linear-gradient(90deg, #e53935, #ef5350, #ef9a9a); }");
        sb.AppendLine(
            "  .progress-fill.medium { background: linear-gradient(90deg, #fb8c00, #ffa726, #ffcc80); }");
        sb.AppendLine(
            "  .progress-fill.high { background: linear-gradient(90deg, #43a047, #66bb6a, #aed581); }");
        sb.AppendLine(
            "  table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }");
        sb.AppendLine(
            "  th { background: #1a1a2e; color: #fff; padding: 14px 18px; text-align: left; font-weight: 600; font-size: 0.95rem; }");
        sb.AppendLine("  td { padding: 12px 18px; border-bottom: 1px solid #eee; }");
        sb.AppendLine("  tr:last-child td { border-bottom: none; }");
        sb.AppendLine("  tr.covered { background: #e8f5e9; }");
        sb.AppendLine("  tr.uncovered { background: #ffebee; }");
        sb.AppendLine("  tr.covered:hover { background: #c8e6c9; }");
        sb.AppendLine("  tr.uncovered:hover { background: #ffcdd2; }");
        sb.AppendLine("  .status-covered { color: #2e7d32; font-weight: 700; }");
        sb.AppendLine("  .status-uncovered { color: #c62828; font-weight: 700; }");
        sb.AppendLine("  .footer { text-align: center; margin-top: 2.5rem; color: #999; font-size: 0.85rem; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>Coverage Report</h1>");
        sb.AppendLine(
            $"<div class=\"summary\">覆盖率：<span class=\"pct\">{coveredCount}/{total}（{pct:F0}%）</span></div>");
        var barClass = pct >= 70 ? "high" : pct >= 40 ? "medium" : "low";
        sb.AppendLine(
            $"<div class=\"progress-container\"><div class=\"progress-fill {barClass}\" style=\"width: {pct:F0}%;\">{pct:F0}%</div></div>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>特性</th><th>状态</th><th>测试项目</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var (feature, (isCovered, projects)) in coverageMap)
        {
            var displayName = knownFeatures.GetValueOrDefault(feature, feature);
            var rowClass = isCovered ? "covered" : "uncovered";
            var status = isCovered ? "\u2713" : "\u2717";
            var statusClass = isCovered ? "status-covered" : "status-uncovered";
            var projStr = string.Join(", ", projects.Take(3));
            if (projects.Count > 3)
            {
                projStr += $" ... +{projects.Count - 3}";
            }

            if (!isCovered)
            {
                projStr = "-";
            }

            sb.AppendLine(
                $"<tr class=\"{rowClass}\"><td>{displayName}</td><td class=\"{statusClass}\">{status}</td><td>{projStr}</td></tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("<div class=\"footer\">Generated by Legion CLI</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    #endregion

    #region 文档方法

    /// <summary>
    ///     Workspace 模式文档生成
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>退出码</returns>
    internal static int run_doc_for_workspace(string workspaceDir, bool verbose)
    {
        var members = load_workspace_members(workspaceDir);
        if (members.Count == 0)
        {
            Console.WriteLine("错误：legions.von 中无 members");
            return 1;
        }

        Console.WriteLine($"发现 Workspace，共 {members.Count} 个成员项目");

        var docGenerator = new LegionDocGenerator();
        var htmlGenerator = new LegionDocHtmlGenerator();
        var allProjects = new List<DocProject>();
        var hasFailure = false;

        foreach (var memberPath in members)
        {
            var memberDir = Path.GetFullPath(Path.Combine(workspaceDir, memberPath));
            if (!Directory.Exists(memberDir))
            {
                if (verbose)
                {
                    Console.WriteLine($"  跳过不存在的成员目录：{memberPath}");
                }

                continue;
            }

            var memberName = Path.GetFileName(memberDir);
            Console.Write($"  正在解析 {memberName}... ");

            try
            {
                var docProject = docGenerator.generate(memberDir);
                allProjects.Add(docProject);
                Console.WriteLine(
                    $"完成（{docProject.modules.Sum(m => m.types.Count)} 类型, {docProject.modules.Sum(m => m.functions.Count)} 函数）");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"失败：{ex.Message}");
                hasFailure = true;
            }
        }

        if (allProjects.Count == 0)
        {
            Console.WriteLine("错误：无有效项目");
            return 1;
        }

        var outputDir = Path.Combine(workspaceDir, "dist", "legion-document");
        Console.WriteLine($"正在生成统一 HTML 文档到 {outputDir}...");

        var themeEngine = new ThemeEngine();
        var themes = themeEngine.compile_themes(workspaceDir, outputDir);
        if (themes.Count > 0)
        {
            Console.WriteLine($"已编译 {themes.Count} 个 SCSS 主题");
        }

        var globalTypeIndex = LegionDocGenerator.build_type_index(allProjects);

        var apiDir = Path.Combine(outputDir, "api");
        htmlGenerator.generate_unified_workspace(allProjects, apiDir, globalTypeIndex);

        var docDir = Path.Combine(outputDir, "doc");
        render_user_documentation(workspaceDir, docDir, themes);

        write_unified_assets(outputDir, htmlGenerator, themes);

        generate_root_index(outputDir);

        Console.WriteLine($"文档已生成到 {outputDir}");
        return hasFailure ? 1 : 0;
    }

    /// <summary>
    ///     单项目模式文档生成
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>退出码</returns>
    internal static int run_doc_for_project(string projectDir, bool verbose)
    {
        var docGenerator = new LegionDocGenerator();
        var htmlGenerator = new LegionDocHtmlGenerator();

        Console.Write("正在解析源码... ");

        DocProject docProject;
        try
        {
            docProject = docGenerator.generate(projectDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"失败：{ex.Message}");
            return 1;
        }

        Console.WriteLine(
            $"完成（{docProject.modules.Sum(m => m.types.Count)} 类型, {docProject.modules.Sum(m => m.functions.Count)} 函数）");

        var outputDir = Path.Combine(projectDir, "dist", "legion-document");
        Console.WriteLine($"正在生成 HTML 文档到 {outputDir}...");

        var themeEngine = new ThemeEngine();
        var themes = themeEngine.compile_themes(projectDir, outputDir);
        if (themes.Count > 0)
        {
            Console.WriteLine($"已编译 {themes.Count} 个 SCSS 主题");
        }

        var localTypeIndex = LegionDocGenerator.build_type_index(docProject);

        var apiDir = Path.Combine(outputDir, "api");
        htmlGenerator.generate(docProject, apiDir, typeIndex: localTypeIndex);

        var docDir = Path.Combine(outputDir, "doc");
        render_user_documentation(projectDir, docDir, themes);

        write_unified_assets(outputDir, htmlGenerator, themes);

        generate_root_index(outputDir);

        Console.WriteLine($"文档已生成到 {outputDir}");
        return 0;
    }

    /// <summary>
    ///     检查项目目录下的 documentation 目录，若存在则渲染用户文档
    /// </summary>
    /// <param name="projectDir">项目根目录</param>
    /// <param name="outputDir">文档输出目录</param>
    /// <param name="themes">已编译的主题列表</param>
    internal static void render_user_documentation(string projectDir, string outputDir,
        List<ThemeEngine.CompiledTheme> themes)
    {
        var userDocRenderer = new UserDocRenderer();
        var sections = new List<UserDocSection>();

        var mainDocDir = Path.Combine(projectDir, "documentation", "pages", "zh-hans");
        if (Directory.Exists(mainDocDir))
        {
            Console.WriteLine("发现用户文档目录，正在渲染...");

            var subDirs = new (string dirName, string sectionName, string sidebarTitle)[]
            {
                ("language", "语言参考", "语言参考"),
                ("guides", "用户指南", "用户指南"),
                ("toolchain", "工具链", "工具链"),
                ("developer", "开发者文档", "开发者文档"),
                ("maintainer", "维护者文档", "维护者文档"),
            };

            foreach (var (dirName, sectionName, sidebarTitle) in subDirs)
            {
                var subDir = Path.Combine(mainDocDir, dirName);
                if (Directory.Exists(subDir))
                {
                    var pages = userDocRenderer.render(subDir, outputDir, sectionName, sidebarTitle);
                    if (pages.Count > 0)
                    {
                        sections.Add(new UserDocSection
                        {
                            name = sectionName,
                            pages = pages
                        });
                        Console.WriteLine($"  {sectionName}：{pages.Count} 个页面");
                    }
                }
            }
        }

        var legionDocDir = Path.Combine(projectDir, "projects", "legion", "documentation", "pages", "zh-hans");
        if (Directory.Exists(legionDocDir))
        {
            var pages = userDocRenderer.render(legionDocDir, outputDir, "Legion 文档", "Legion 包管理器");
            if (pages.Count > 0)
            {
                sections.Add(new UserDocSection
                {
                    name = "Legion 文档",
                    pages = pages
                });
                Console.WriteLine($"  Legion 文档：{pages.Count} 个页面");
            }
        }

        if (sections.Count > 0)
        {
            userDocRenderer.generate_index(outputDir, sections);
            Console.WriteLine($"用户文档已渲染，共 {sections.Sum(s => s.pages.Count)} 个页面");
        }
    }

    /// <summary>
    ///     统一写入共享资源：合并所有 CSS 为单一 legion-document.css，写入 legion-document.js
    ///     遵循 Nyar.Language 的 SCSS → CssStylesheet → CssMerger 统一编译流
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    /// <param name="htmlGenerator">HTML 生成器</param>
    /// <param name="themes">已编译的主题列表</param>
    internal static void write_unified_assets(string outputDir, LegionDocHtmlGenerator htmlGenerator,
        List<ThemeEngine.CompiledTheme> themes)
    {
        var sources = new List<WebStyleAsset>();

        var apiCss = htmlGenerator.get_css();
        if (!string.IsNullOrWhiteSpace(apiCss))
        {
            sources.Add(new WebStyleAsset { Name = "API 文档样式", Content = apiCss });
        }

        var userCss = UserDocRenderer.get_css();
        if (!string.IsNullOrWhiteSpace(userCss))
        {
            sources.Add(new WebStyleAsset { Name = "用户文档样式", Content = userCss });
        }

        foreach (var theme in themes)
        {
            if (!string.IsNullOrWhiteSpace(theme.cssContent))
            {
                sources.Add(new WebStyleAsset
                {
                    Name = $"主题: {theme.name}",
                    Content = theme.cssContent
                });
            }
        }

        var mergedCss = WebStylePipeline.merge(sources, "Legion 文档样式");
        atomic_write_all_text(Path.Combine(outputDir, "legion-document.css"), mergedCss);

        var jsPath = Path.Combine(outputDir, "legion-document.js");
        if (!File.Exists(jsPath))
        {
            atomic_write_all_text(jsPath, LegionDocHtmlGenerator.get_js());
        }

        Console.WriteLine($"统一 CSS 已生成：{sources.Count} 个来源合并为 legion-document.css");
    }

    /// <summary>
    ///     生成根目录导航页，链接到 API 文档和用户文档
    /// </summary>
    /// <param name="outputDir">文档根输出目录</param>
    internal static void generate_root_index(string outputDir)
    {
        var indexPath = Path.Combine(outputDir, "index.html");
        var html = @"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>文档</title>
  <style>
    :root {
      --bg: #ffffff;
      --text: #1a1a1a;
      --link: #3873ad;
      --border: #ddd;
      --card-bg: #fafafa;
      --rust-brown: #c67b34;
    }
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: var(--bg); color: var(--text);
      display: flex; justify-content: center; align-items: center;
      min-height: 100vh; padding: 2rem;
    }
    .container { max-width: 600px; width: 100%; text-align: center; }
    h1 { font-size: 2rem; margin-bottom: 0.5rem; color: var(--rust-brown); }
    .subtitle { color: #666; margin-bottom: 2rem; }
    .cards { display: flex; gap: 1.5rem; justify-content: center; flex-wrap: wrap; }
    .card {
      flex: 1; min-width: 200px; max-width: 280px;
      background: var(--card-bg); border: 1px solid var(--border);
      border-radius: 8px; padding: 2rem 1.5rem; text-decoration: none;
      transition: box-shadow 0.2s, transform 0.2s;
    }
    .card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.1); transform: translateY(-2px); }
    .card-icon { font-size: 2.5rem; margin-bottom: 1rem; }
    .card-title { font-size: 1.25rem; font-weight: 600; color: var(--link); margin-bottom: 0.5rem; }
    .card-desc { font-size: 0.9rem; color: #666; line-height: 1.5; }
    footer { margin-top: 3rem; color: #999; font-size: 0.85rem; }
  </style>
</head>
<body>
  <div class=""container"">
    <h1>文档</h1>
    <p class=""subtitle"">由 <strong>legion doc</strong> 生成</p>
    <div class=""cards"">
      <a class=""card"" href=""api/index.html"">
        <div class=""card-icon"">API</div>
        <div class=""card-title"">API 文档</div>
        <div class=""card-desc"">类型、模块、命名空间的完整 API 参考</div>
      </a>
      <a class=""card"" href=""doc/index.html"">
        <div class=""card-icon"">DOC</div>
        <div class=""card-title"">用户文档</div>
        <div class=""card-desc"">语言参考、用户指南、工具链、维护者文档</div>
      </a>
    </div>
    <footer>
      <p>由 <strong>legion doc</strong> 生成</p>
    </footer>
  </div>
</body>
</html>
";
        atomic_write_all_text(indexPath, html);
    }

    #endregion

    #region 通用辅助

    /// <summary>
    ///     格式化异常链
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <returns>格式化后的异常链文本</returns>
    internal static string format_exception_chain(Exception exception)
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

    /// <summary>
    ///     输出模块信息到控制台
    /// </summary>
    /// <param name="vm">NyarVM 实例</param>
    /// <param name="moduleName">模块名</param>
    internal static void dump_module_info(NyarVm vm, string moduleName)
    {
        var module = vm.get_module(moduleName);
        if (module is not NyarModule nyarModule)
        {
            Console.WriteLine($"  模块类型：{module?.GetType().Name}");
            return;
        }

        Console.WriteLine($"  模块名：{nyarModule.name}");
        Console.WriteLine($"  常量数：{nyarModule.constants.Count}");
        for (var i = 0; i < nyarModule.constants.Count; i++)
        {
            Console.WriteLine($"    const[{i}]: {nyarModule.constants[i]}");
        }

        Console.WriteLine($"  函数数：{nyarModule.functions.Count}");
        foreach (var func in nyarModule.functions)
        {
            Console.WriteLine(
                $"    {func.name} arity={func.arity} locals={func.local_count} offset={func.code_offset} len={func.code_length}");
            if (nyarModule.raw_bytecode is not null && func.code_length > 0)
            {
                var bytes = nyarModule.raw_bytecode.AsSpan(func.code_offset, func.code_length);
                Console.WriteLine($"      字节码({bytes.Length}B): {BitConverter.ToString([.. bytes])}");
            }
        }
    }

    /// <summary>
    ///     返回模块中的函数与导出列表，供测试执行失败时快速定位入口名不一致问题。
    /// </summary>
    /// <param name="vm">NyarVM 实例</param>
    /// <param name="moduleName">模块名称</param>
    /// <returns>模块摘要文本</returns>
    internal static string describe_module_functions(NyarVm vm, string moduleName)
    {
        var module = vm.get_module(moduleName);
        if (module is not NyarModule nyarModule)
        {
            return $"模块 `{moduleName}` 未加载为 `NyarModule`，无法枚举函数。";
        }

        var functionNames = nyarModule.functions
            .Select(function => function.name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var exportNames = nyarModule.exports
            .Select(exportItem => exportItem.name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return
            $"模块函数：{string.Join(", ", functionNames)}{Environment.NewLine}模块导出：{string.Join(", ", exportNames)}";
    }

    /// <summary>
    ///     HTML 转义辅助方法
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转义后的文本</returns>
    internal static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    ///     生成 HTML 测试报告
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    /// <param name="projectName">项目名称</param>
    /// <param name="results">测试结果列表</param>
    internal static void generate_test_report_html(string outputDir, string projectName,
        List<TestResultEntry> results)
    {
        Directory.CreateDirectory(outputDir);

        var passed = results.Count(r => r.Status == "pass");
        var failed = results.Count(r => r.Status == "fail" || r.Status == "compile_error");
        var skipped = results.Count(r => r.Status == "skip");

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"zh-CN\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"UTF-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine($"<title>{EscapeHtml(projectName)} - Test Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("  * { margin: 0; padding: 0; box-sizing: border-box; }");
        html.AppendLine(
            "  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5f5; color: #333; padding: 24px; }");
        html.AppendLine("  .container { max-width: 960px; margin: 0 auto; }");
        html.AppendLine("  h1 { font-size: 24px; margin-bottom: 20px; color: #1a1a1a; }");
        html.AppendLine("  .summary { display: flex; gap: 16px; margin-bottom: 24px; }");
        html.AppendLine(
            "  .summary-card { flex: 1; padding: 16px; border-radius: 8px; text-align: center; color: #fff; }");
        html.AppendLine("  .summary-card.pass { background: #22c55e; }");
        html.AppendLine("  .summary-card.fail { background: #ef4444; }");
        html.AppendLine("  .summary-card.skip { background: #f97316; }");
        html.AppendLine("  .summary-card .count { font-size: 32px; font-weight: 700; }");
        html.AppendLine("  .summary-card .label { font-size: 14px; opacity: 0.9; margin-top: 4px; }");
        html.AppendLine(
            "  table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }");
        html.AppendLine(
            "  th { background: #f8f9fa; padding: 12px 16px; text-align: left; font-size: 13px; font-weight: 600; color: #666; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 2px solid #e5e7eb; }");
        html.AppendLine("  td { padding: 10px 16px; font-size: 14px; border-bottom: 1px solid #f0f0f0; }");
        html.AppendLine("  tr:last-child td { border-bottom: none; }");
        html.AppendLine(
            "  .status-badge { display: inline-block; padding: 2px 10px; border-radius: 12px; font-size: 12px; font-weight: 600; color: #fff; }");
        html.AppendLine("  .status-badge.pass { background: #22c55e; }");
        html.AppendLine("  .status-badge.fail { background: #ef4444; }");
        html.AppendLine("  .status-badge.skip { background: #f97316; }");
        html.AppendLine("  .status-badge.compile_error { background: #f97316; }");
        html.AppendLine(
            "  .error-cell { color: #ef4444; font-size: 13px; max-width: 300px; word-break: break-all; }");
        html.AppendLine("  .empty { text-align: center; padding: 32px; color: #999; font-size: 14px; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class=\"container\">");
        html.AppendLine($"<h1>{EscapeHtml(projectName)} - Test Report</h1>");
        html.AppendLine("<div class=\"summary\">");
        html.AppendLine(
            $"  <div class=\"summary-card pass\"><div class=\"count\">{passed}</div><div class=\"label\">通过</div></div>");
        html.AppendLine(
            $"  <div class=\"summary-card fail\"><div class=\"count\">{failed}</div><div class=\"label\">失败</div></div>");
        html.AppendLine(
            $"  <div class=\"summary-card skip\"><div class=\"count\">{skipped}</div><div class=\"label\">跳过</div></div>");
        html.AppendLine("</div>");

        if (results.Count > 0)
        {
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr><th>测试名称</th><th>状态</th><th>目标平台</th><th>错误信息</th></tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var r in results)
            {
                var statusLabel = r.Status switch
                {
                    "pass" => "通过",
                    "fail" => "失败",
                    "skip" => "跳过",
                    "compile_error" => "编译错误",
                    _ => r.Status
                };
                var errorCell = r.Error is not null
                    ? $"<td class=\"error-cell\">{EscapeHtml(r.Error)}</td>"
                    : "<td></td>";
                html.AppendLine(
                    $"<tr><td>{EscapeHtml(r.Name)}</td><td><span class=\"status-badge {r.Status}\">{statusLabel}</span></td><td>{EscapeHtml(r.Target)}</td>{errorCell}</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }
        else
        {
            html.AppendLine("<div class=\"empty\">无测试结果</div>");
        }

        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        var filePath = Path.Combine(outputDir, "index.html");
        atomic_write_all_text(filePath, html.ToString());
    }

    #endregion
}

/// <summary>
///     单个测试结果条目
/// </summary>
internal sealed class TestResultEntry
{
    /// <summary>
    ///     测试名称
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    ///     测试状态：pass, fail, skip, compile_error
    /// </summary>
    public string Status { get; init; } = "";

    /// <summary>
    ///     错误信息（失败时）
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    ///     目标平台
    /// </summary>
    public string Target { get; init; } = "";
}
