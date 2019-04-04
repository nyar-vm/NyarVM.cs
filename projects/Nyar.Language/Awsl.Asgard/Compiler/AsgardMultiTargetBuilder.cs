using System.Linq;
using System.Text;
using Std.Data.Text.Awsl;
using Nyar.Language.Css;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Language.Valkyrie.Config;
using Nyar.Types;
using Nyar.Types.Targets;
using ValkyrieCompiler = Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler;

namespace Nyar.Language.Awsl.Asgard.Compiler;

/// <summary>
///     VOA 多目标构建器。
///     浏览器 `wasm` 目标使用完整前后端管线（`.v` → WASM + `.awsl` → JS/CSS），
///     其余目标统一只消费 `.v`，直接走 Valkyrie.Compiler 标准编译链。
/// </summary>
public sealed class AsgardMultiTargetBuilder
{
    private readonly ValkyrieCompiler _compiler;
    private readonly AwslParser _awsl_parser;
    private readonly AwslReactiveCompiler _awsl_compiler;

    /// <summary>
    ///     创建 VOA 多目标构建器实例
    /// </summary>
    public AsgardMultiTargetBuilder()
    {
        _compiler = new ValkyrieCompiler();
        _awsl_parser = new AwslParser();
        _awsl_compiler = new AwslReactiveCompiler();
    }

    /// <summary>
    ///     构建项目到指定目标平台。
    /// </summary>
    /// <param name="config">构建配置</param>
    /// <param name="projectDir">项目目录</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="target">目标平台标识（完整 CanonicalTriple 四段式或短名）</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <param name="targetMode">编译目标模式（Dev = HMR 优先 per-file / Prod = 优化合并）</param>
    /// <returns>构建结果</returns>
    public AsgardBuildResult build(VoaBuildConfig config, string projectDir, string outputDir, string target,
        bool verbose, TargetMode targetMode = TargetMode.prod)
    {
        if (!try_resolve_target(target, out var targetInfo))
        {
            return new AsgardBuildResult
            {
                success = false,
                error = $"不支持的编译目标 '{target}'，可选：wasm / wasip1 / wasip2 / clr / jvm / native / nyar / gnosis / node"
            };
        }

        var effectiveOutputDir = get_effective_output_dir(outputDir, targetInfo.canonical_triple);
        if (!Directory.Exists(effectiveOutputDir))
        {
            Directory.CreateDirectory(effectiveOutputDir);
        }

        return targetInfo.host_kind switch
        {
            TargetHostKind.browser => build_browser_target(projectDir, effectiveOutputDir, targetInfo, verbose, config,
                targetMode),
            _ => build_non_wasm_target(projectDir, effectiveOutputDir, targetInfo, verbose, config)
        };
    }

    #region 浏览器目标构建

    /// <summary>
    ///     构建浏览器 WASM 目标。
    ///     支持完整前后端管线（`.v` → WASM + `.awsl` → JS/CSS/HTML）。
    /// </summary>
    private AsgardBuildResult build_browser_target(string projectDir, string outputDir, CompileTargetInfo targetInfo,
        bool verbose, VoaBuildConfig config, TargetMode targetMode = TargetMode.prod)
    {
        var sourceDir = Path.Combine(projectDir, "source");
        if (!Directory.Exists(sourceDir))
        {
            return new AsgardBuildResult { success = false, error = $"源码目录不存在 '{sourceDir}'" };
        }

        var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories);
        var awslFiles = Directory.GetFiles(sourceDir, "*.awsl", SearchOption.AllDirectories);

        if (verbose)
        {
            Console.WriteLine($"  找到 {vFiles.Length} 个 .v 文件，{awslFiles.Length} 个 .awsl 文件");
        }

        if (vFiles.Length == 0 && awslFiles.Length == 0)
        {
            return new AsgardBuildResult { success = false, error = "未找到任何源码文件（.v 或 .awsl）" };
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var moduleName = resolve_output_module_name(projectDir, config);
        var allOutputFiles = new List<string>();

        // Phase 1: 编译 .v → WASM（后端）
        if (vFiles.Length > 0)
        {
            if (verbose)
            {
                Console.WriteLine($"  [后端] 编译 {vFiles.Length} 个 .v 文件");
            }

            try
            {
                var artifactSet = compile_files_to_artifacts(vFiles, moduleName, targetInfo);
                var writtenFiles = write_output_files(artifactSet, outputDir, verbose);
                allOutputFiles.AddRange(writtenFiles);
            }
            catch (InvalidOperationException ex)
            {
                return new AsgardBuildResult { success = false, error = ex.Message };
            }
            catch (Exception ex)
            {
                var errorMsg = $"后端编译异常: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                Console.Error.WriteLine(errorMsg);
                return new AsgardBuildResult { success = false, error = errorMsg };
            }
        }

        // Phase 2: 编译 .awsl → JS/CSS（前端）
        var awslResults = new List<AwslCompileResult>();
        if (awslFiles.Length > 0)
        {
            var componentNames = awslFiles
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            _awsl_compiler.register_component_names(componentNames);

            foreach (var awslFile in awslFiles)
            {
                if (verbose)
                {
                    Console.WriteLine($"  [前端] 编译 .awsl：{Path.GetFileName(awslFile)}");
                }

                var source = File.ReadAllText(awslFile);
                try
                {
                    var parseResult = _awsl_parser.parse(source, Path.GetFileName(awslFile));
                    var compileResult = _awsl_compiler.compile(parseResult, moduleName);
                    awslResults.Add(compileResult);
                }
                catch (AwslParser.ParseLoopException ex)
                {
                    var errorMsg = $"AWSL 解析死循环: 文件={Path.GetFileName(awslFile)}, {ex.Message}";
                    Console.Error.WriteLine(errorMsg);
                    return new AsgardBuildResult { success = false, error = errorMsg };
                }
            }
        }

        // Phase 3: 生成前端组合产物（JS 组件合并、CSS 合并、index.html、voa-runtime.js）
        var hasWasmBackend = vFiles.Length > 0;
        if (awslResults.Count > 0 || hasWasmBackend)
        {
            write_frontend_bundle(projectDir, outputDir, moduleName, awslResults, hasWasmBackend, allOutputFiles, verbose,
                config, targetMode);
        }

        if (verbose)
        {
            Console.WriteLine($"VOA 构建完成，共生成 {allOutputFiles.Count} 个文件");
        }

        return new AsgardBuildResult
        {
            success = true,
            output_directory = outputDir,
            output_files = allOutputFiles
        };
    }

    #endregion

    #region 非 WASM 目标构建

    /// <summary>
    ///     构建非 WASM 目标（clr / jvm / native / nyar / gnosis），
    ///     仅消费 `.v`，编译流统一委托给 Valkyrie.Compiler。
    /// </summary>
    private AsgardBuildResult build_non_wasm_target(string projectDir, string outputDir, CompileTargetInfo targetInfo,
        bool verbose, VoaBuildConfig config)
    {
        var sourceDir = Path.Combine(projectDir, "source");
        if (!Directory.Exists(sourceDir))
            return new AsgardBuildResult
            {
                success = false,
                error = $"源码目录不存在 '{sourceDir}'"
            };

        var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories);
        var awslFiles = Directory.GetFiles(sourceDir, "*.awsl", SearchOption.AllDirectories);

        if (verbose) Console.WriteLine($"  找到 {vFiles.Length} 个 .v 文件，{awslFiles.Length} 个 .awsl 文件");

        if (vFiles.Length == 0 && awslFiles.Length == 0)
            return new AsgardBuildResult
            {
                success = false,
                error = "未找到任何源码文件（.v 或 .awsl）"
            };

        if (awslFiles.Length > 0 && verbose)
        {
            Console.WriteLine(
                $"  跳过 {awslFiles.Length} 个 .awsl 文件：当前目标 `{targetInfo.canonical_triple}` 仅消费 .v，AWSL 仅支持浏览器 wasm + js glue");
        }

        if (vFiles.Length == 0)
        {
            return new AsgardBuildResult
            {
                success = false,
                error = $"目标 `{targetInfo.canonical_triple}` 仅支持 `.v` 源文件；`.awsl` 仅支持浏览器 `wasm + js glue`。"
            };
        }

        var moduleName = resolve_output_module_name(projectDir, config);
        var allOutputFiles = new List<string>();

        if (vFiles.Length > 0)
        {
            if (verbose) Console.WriteLine($"  编译 {vFiles.Length} 个 .v 文件");

            try
            {
                var artifactSet = compile_files_to_artifacts(vFiles, moduleName, targetInfo);
                var writtenFiles = write_output_files(artifactSet, outputDir, verbose);
                allOutputFiles.AddRange(writtenFiles);
            }
            catch (InvalidOperationException ex)
            {
                return new AsgardBuildResult
                {
                    success = false,
                    error = ex.Message
                };
            }
            catch (Exception ex)
            {
                var errorMsg = $"后端编译异常: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                Console.Error.WriteLine(errorMsg);
                return new AsgardBuildResult { success = false, error = errorMsg };
            }
        }

        if (verbose) Console.WriteLine($"VOA 构建完成，共生成 {allOutputFiles.Count} 个文件");

        return new AsgardBuildResult
        {
            success = true,
            output_directory = outputDir,
            output_files = allOutputFiles
        };
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     将编译输出文件写入磁盘。
    /// </summary>
    private static List<string> write_output_files(ArtifactSet artifactSet, string outputDir, bool verbose)
    {
        var writtenFiles = new List<string>();
        var writtenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in artifactSet.enumerate_artifacts())
        {
            if (!writtenNames.Add(file.name))
            {
                continue;
            }

            var filePath = Path.Combine(outputDir, file.name);
            File.WriteAllBytes(filePath, file.content);
            writtenFiles.Add(filePath);

            if (verbose) Console.WriteLine($"  生成：{file.name}（{file.content.Length} 字节）");
        }

        return writtenFiles;
    }

    /// <summary>
    ///     写入前端组合产物：per-file 组件 JS、CSS、manifest.json、index.html、voa-runtime.js。
    ///     Dev 模式：强制 per-component（最大粒度，HMR 优先）
    ///     Prod 模式：按 config.chunk 配置决定合并策略
    /// </summary>
    private static void write_frontend_bundle(
        string projectDir,
        string outputDir,
        string moduleName,
        List<AwslCompileResult> awslResults,
        bool hasWasmBackend,
        List<string> allOutputFiles,
        bool verbose,
        VoaBuildConfig config,
        TargetMode targetMode = TargetMode.prod)
    {
        var isDev = targetMode == TargetMode.dev;
        var chunkConfig = config?.chunk ?? new VoaChunkConfig();

        // dev 模式始终 per-component；prod 模式按配置决定
        var jsPerComponent = isDev || string.Equals(chunkConfig.js_mode, "per-component", StringComparison.OrdinalIgnoreCase);
        var cssPerComponent = isDev || string.Equals(chunkConfig.css_mode, "per-component", StringComparison.OrdinalIgnoreCase);

        // 从已有产物中收集 WASM 文件信息
        var wasmInfos = collect_wasm_infos(allOutputFiles);

        // 组件 JS 输出
        if (awslResults.Count > 0)
        {
            if (jsPerComponent)
            {
                write_per_component_js(outputDir, awslResults, allOutputFiles, isDev, verbose);
            }
            else
            {
                var mergedJs = merge_component_js(awslResults);
                if (mergedJs.Length > 0)
                {
                    var jsPath = Path.Combine(outputDir, $"{moduleName}.js");
                    File.WriteAllText(jsPath, mergedJs, Encoding.UTF8);
                    allOutputFiles.Add(jsPath);
                    if (verbose) Console.WriteLine($"  生成：{Path.GetFileName(jsPath)}");
                }
            }
        }

        // CSS 处理
        if (cssPerComponent)
        {
            write_per_component_css(outputDir, awslResults, allOutputFiles, verbose);
        }
        else
        {
            var mergedCss = merge_component_css(awslResults);
            if (mergedCss.Length > 0)
            {
                var cssPath = Path.Combine(outputDir, $"{moduleName}.css");
                File.WriteAllText(cssPath, mergedCss, Encoding.UTF8);
                allOutputFiles.Add(cssPath);
                if (verbose) Console.WriteLine($"  生成：{Path.GetFileName(cssPath)}");
            }
        }

        // 复制 voa-runtime.js
        copy_runtime_js(outputDir, config.runtime, verbose, allOutputFiles);

        // 生成 manifest.json（dev 模式带 HMR 信息，per-component CSS 模式带独立 CSS 路径）
        write_manifest_json(outputDir, moduleName, awslResults, wasmInfos, allOutputFiles, cssPerComponent, verbose);

        // 生成 index.html
        var html = generate_index_html(projectDir, moduleName, awslResults, hasWasmBackend, wasmInfos, cssPerComponent,
            config);
        var htmlPath = Path.Combine(outputDir, "index.html");
        File.WriteAllText(htmlPath, html, Encoding.UTF8);
        allOutputFiles.Add(htmlPath);
        if (verbose) Console.WriteLine("  生成：index.html");
    }

    /// <summary>
    ///     从产物文件路径中收集 WASM 模块信息。
    /// </summary>
    private static List<WasmModuleInfo> collect_wasm_infos(List<string> allOutputFiles)
    {
        var wasmInfos = new List<WasmModuleInfo>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in allOutputFiles)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase))
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                var glueName = $"{name}.mjs";
                if (!seenNames.Add(name))
                {
                    continue;
                }

                wasmInfos.Add(new WasmModuleInfo(name, fileName, glueName));
            }
        }

        return wasmInfos;
    }

    /// <summary>
    ///     每个 AWSL 组件独立输出 JS 文件到 dist/c/ 目录。
    ///     每个文件是自注册 IIFE 模块。
    /// </summary>
    private static void write_per_component_js(
        string outputDir,
        List<AwslCompileResult> awslResults,
        List<string> allOutputFiles,
        bool isDev,
        bool verbose)
    {
        var compDir = Path.Combine(outputDir, "c");
        if (!Directory.Exists(compDir))
        {
            Directory.CreateDirectory(compDir);
        }

        foreach (var r in awslResults)
        {
            if (string.IsNullOrWhiteSpace(r.java_script))
            {
                continue;
            }

            var jsPath = Path.Combine(compDir, $"{r.component_name}.js");
            var content = generate_component_module(r);
            File.WriteAllText(jsPath, content, Encoding.UTF8);
            allOutputFiles.Add(jsPath);

            if (verbose)
            {
                Console.WriteLine($"  生成：c/{r.component_name}.js");
            }
        }
    }

    /// <summary>
    ///     dev 模式下每个组件独立输出 CSS 到 dist/c/{name}.css。
    /// </summary>
    private static void write_per_component_css(
        string outputDir,
        List<AwslCompileResult> awslResults,
        List<string> allOutputFiles,
        bool verbose)
    {
        var compDir = Path.Combine(outputDir, "c");
        if (!Directory.Exists(compDir))
        {
            Directory.CreateDirectory(compDir);
        }

        foreach (var r in awslResults)
        {
            if (string.IsNullOrWhiteSpace(r.css))
            {
                continue;
            }

            var cssPath = Path.Combine(compDir, $"{r.component_name}.css");
            File.WriteAllText(cssPath, r.css, Encoding.UTF8);
            allOutputFiles.Add(cssPath);

            if (verbose)
            {
                Console.WriteLine($"  生成：c/{r.component_name}.css");
            }
        }
    }

    /// <summary>
    ///     生成单个组件的自注册 IIFE 模块。
    /// </summary>
    private static string generate_component_module(AwslCompileResult r)
    {
        var strategy = r.hydrate_strategy ?? "load";
        var island = r.island_type ?? "hydrated";
        var sb = new StringBuilder();
        sb.AppendLine("(function() {");
        sb.AppendLine("  'use strict';");
        sb.AppendLine();
        sb.Append(r.java_script);
        if (!r.java_script.EndsWith('\n'))
        {
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine($"  Voa.registerComponent('{r.component_name}', {{");
        sb.AppendLine($"    strategy: '{strategy}',");
        sb.AppendLine($"    island: '{island}'");
        sb.AppendLine("  });");
        sb.AppendLine("})();");
        return sb.ToString();
    }

    /// <summary>
    ///     生成 manifest.json 产物清单。
    ///     当 cssPerComponent 为 true 时，每个组件含独立 CSS 路径和 mode: dev。
    /// </summary>
    private static void write_manifest_json(
        string outputDir,
        string moduleName,
        List<AwslCompileResult> awslResults,
        List<WasmModuleInfo> wasmInfos,
        List<string> allOutputFiles,
        bool cssPerComponent,
        bool verbose)
    {
        var cssFiles = new List<string>();
        if (!cssPerComponent && awslResults.Any(r => !string.IsNullOrWhiteSpace(r.css)))
        {
            cssFiles.Add($"{moduleName}.css");
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"module\": \"{moduleName}\",");
        sb.AppendLine($"  \"mode\": \"{(cssPerComponent ? "dev" : "prod")}\",");

        // CSS 文件列表
        sb.Append("  \"css\": [");
        for (var i = 0; i < cssFiles.Count; i++)
        {
            sb.Append($"\"{cssFiles[i]}\"");
            if (i < cssFiles.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.AppendLine("],");

        // WASM 模块列表
        sb.AppendLine("  \"wasm\": [");
        for (var i = 0; i < wasmInfos.Count; i++)
        {
            var w = wasmInfos[i];
            var comma = i < wasmInfos.Count - 1 ? "," : "";
            sb.AppendLine($"    {{ \"name\": \"{w.Name}\", \"url\": \"{w.Url}\", \"glue\": \"{w.Glue}\" }}{comma}");
        }

        sb.AppendLine("  ],");

        // 组件列表
        sb.AppendLine("  \"components\": [");
        for (var i = 0; i < awslResults.Count; i++)
        {
            var r = awslResults[i];
            var comma = i < awslResults.Count - 1 ? "," : "";
            var strategy = r.hydrate_strategy ?? "load";
            var island = r.island_type ?? "hydrated";

            if (cssPerComponent && !string.IsNullOrWhiteSpace(r.css))
            {
                sb.AppendLine(
                    $"    {{ \"name\": \"{r.component_name}\", \"js\": \"c/{r.component_name}.js\", \"css\": \"c/{r.component_name}.css\", \"strategy\": \"{strategy}\", \"island\": \"{island}\" }}{comma}");
            }
            else
            {
                sb.AppendLine(
                    $"    {{ \"name\": \"{r.component_name}\", \"js\": \"c/{r.component_name}.js\", \"strategy\": \"{strategy}\", \"island\": \"{island}\" }}{comma}");
            }
        }

        sb.AppendLine("  ]");

        // dev 模式额外输出 HMR 配置
        if (cssPerComponent)
        {
            sb.AppendLine("  , \"hmr\": {");
            sb.AppendLine("    \"enabled\": true,");
            sb.AppendLine("    \"wsEndpoint\": \"ws://localhost:3000/__voa_hmr\"");
            sb.AppendLine("  }");
        }

        sb.AppendLine("}");

        var manifestPath = Path.Combine(outputDir, "manifest.json");
        File.WriteAllText(manifestPath, sb.ToString(), Encoding.UTF8);
        allOutputFiles.Add(manifestPath);

        if (verbose)
        {
            Console.WriteLine("  生成：manifest.json");
        }
    }

    /// <summary>
    ///     合并所有 AWSL 组件的 Scoped CSS。
    /// </summary>
    private static string merge_component_css(List<AwslCompileResult> awslResults)
    {
        return CssMerger.merge_component_css(awslResults.Select(r => (r.component_name, r.css)));
    }

    /// <summary>
    ///     合并所有 AWSL 组件的 JS 代码为单一文件（bundled 模式）。
    /// </summary>
    private static string merge_component_js(List<AwslCompileResult> awslResults)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// VOA 组件 JS — 自动生成（bundled 模式）");
        sb.AppendLine("(function() {");
        sb.AppendLine("  'use strict';");
        sb.AppendLine();

        foreach (var r in awslResults)
        {
            if (!string.IsNullOrWhiteSpace(r.java_script))
            {
                sb.AppendLine($"  // 组件：{r.component_name}");
                sb.Append(r.java_script);
                if (!r.java_script.EndsWith('\n'))
                {
                    sb.AppendLine();
                }

                var strategy = r.hydrate_strategy ?? "load";
                var island = r.island_type ?? "hydrated";
                sb.AppendLine();
                sb.AppendLine($"  Voa.registerComponent('{r.component_name}', {{");
                sb.AppendLine($"    strategy: '{strategy}',");
                sb.AppendLine($"    island: '{island}'");
                sb.AppendLine("  });");
                sb.AppendLine();
            }
        }

        sb.AppendLine("})();");
        return sb.ToString();
    }

    /// <summary>
    ///     从嵌入资源或 runtime/ 目录复制 voa-runtime.js 到输出目录。
    ///     优先从程序集嵌入资源提取，若未找到则回退到文件系统搜索。
    /// </summary>
    private static void copy_runtime_js(string outputDir, string runtimeFileName, bool verbose,
        List<string> allOutputFiles)
    {
        var effectiveRuntimeFileName = string.IsNullOrWhiteSpace(runtimeFileName) ? "voa-runtime.js" : runtimeFileName;
        var destPath = Path.Combine(outputDir, effectiveRuntimeFileName);

        // 优先从嵌入资源提取
        var assembly = typeof(AsgardMultiTargetBuilder).Assembly;
        var resourceName = "Nyar.Language.Awsl.Asgard.Runtime.voa-runtime.js";
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            using (stream)
            {
                using var fs = File.Create(destPath);
                stream.CopyTo(fs);
            }

            allOutputFiles.Add(destPath);
            if (verbose) Console.WriteLine($"  复制：{effectiveRuntimeFileName}（嵌入资源）");
            return;
        }

        // 回退：从文件系统搜索
        var runtimeDir = find_runtime_dir();
        if (runtimeDir is null)
        {
            if (verbose) Console.WriteLine("  警告：未找到 runtime/ 目录，跳过 voa-runtime.js 复制");
            return;
        }

        var srcPath = Path.Combine(runtimeDir, "voa-runtime.js");
        if (!File.Exists(srcPath))
        {
            if (verbose) Console.WriteLine($"  警告：未找到 {srcPath}");
            return;
        }

        File.Copy(srcPath, destPath, true);
        allOutputFiles.Add(destPath);
        if (verbose) Console.WriteLine($"  复制：{effectiveRuntimeFileName}");
    }

    /// <summary>
    ///     查找 VOA 项目的 runtime/ 目录。
    /// </summary>
    private static string? find_runtime_dir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var runtimePath = Path.Combine(dir.FullName, "runtime");
            if (Directory.Exists(runtimePath))
            {
                return runtimePath;
            }

            var parentRuntime = Path.Combine(dir.FullName, "..", "runtime");
            if (Directory.Exists(parentRuntime))
            {
                return Path.GetFullPath(parentRuntime);
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    ///     生成 index.html，通过 manifest.json 实现组件按需加载。
    ///     当 cssPerComponent 为 true 时不引用合并 CSS（由运行时按组件加载）。
    /// </summary>
    private static string generate_index_html(
        string projectDir,
        string moduleName,
        List<AwslCompileResult> awslResults,
        bool hasWasmBackend,
        List<WasmModuleInfo> wasmInfos,
        bool cssPerComponent = false,
        VoaBuildConfig? config = null)
    {
        var templatePath = resolve_template_path(projectDir, config?.html_template);
        if (templatePath is not null)
        {
            return normalize_template_html(File.ReadAllText(templatePath, Encoding.UTF8));
        }

        var runtimeFileName = string.IsNullOrWhiteSpace(config?.runtime) ? "voa-runtime.js" : config.runtime;
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{moduleName}</title>");

        if (!cssPerComponent && awslResults.Any(r => !string.IsNullOrWhiteSpace(r.css)))
        {
            sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{moduleName}.css\">");
        }

        sb.AppendLine($"  <script src=\"{runtimeFileName}\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div id=\"app\">");

        foreach (var r in awslResults)
        {
            var islandAttr = r.island_type is not null
                ? $" data-island=\"{r.island_type}\" data-component=\"{r.component_name}\""
                : $" data-component=\"{r.component_name}\"";

            if (r.hydrate_strategy is not null)
            {
                islandAttr += $" data-hydrate=\"{r.hydrate_strategy}\"";
            }

            sb.AppendLine($"    <div{islandAttr}></div>");
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("  <script>");
        sb.AppendLine("    Voa.boot({");
        sb.AppendLine("      manifestUrl: 'manifest.json'");

        if (hasWasmBackend && wasmInfos.Count > 0)
        {
            sb.AppendLine("      , autoLoadWasm: true");
        }

        sb.AppendLine("    }).then(function(manifest) {");
        sb.AppendLine("      if (Voa.hydrateIslands) {");
        sb.AppendLine("        Voa.hydrateIslands();");
        sb.AppendLine("      }");
        sb.AppendLine("    }).catch(function(e) {");
        sb.AppendLine("      console.error('应用启动失败:', e);");
        sb.AppendLine("    });");
        sb.AppendLine("  </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string get_effective_output_dir(string outputDir, string canonicalTriple)
    {
        var outputName = Path.GetFileName(Path.TrimEndingDirectorySeparator(outputDir));
        if (string.Equals(outputName, canonicalTriple, StringComparison.OrdinalIgnoreCase))
        {
            return outputDir;
        }

        return Path.Combine(outputDir, canonicalTriple);
    }

    private static string resolve_output_module_name(string projectDir, VoaBuildConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.entry))
        {
            return Path.GetFileNameWithoutExtension(config.entry);
        }

        if (!string.IsNullOrWhiteSpace(config.app_name))
        {
            return config.app_name;
        }

        return Path.GetFileName(projectDir);
    }

    private static string? resolve_template_path(string projectDir, string? templatePath)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            return null;
        }

        var candidate = Path.IsPathRooted(templatePath)
            ? templatePath
            : Path.Combine(projectDir, templatePath);
        return File.Exists(candidate) ? candidate : null;
    }

    private static string normalize_template_html(string html)
    {
        return html
            .Replace("src=\"/", "src=\"", StringComparison.Ordinal)
            .Replace("href=\"/", "href=\"", StringComparison.Ordinal)
            .Replace("manifestUrl: '/manifest.json'", "manifestUrl: 'manifest.json'", StringComparison.Ordinal);
    }

    private ArtifactSet compile_to_artifacts(
        string source,
        string moduleName,
        string filePath,
        CompileTargetInfo targetInfo)
    {
        var plan = new BuildPlan(moduleName, targetInfo.canonical_triple, filePath, target_mode: targetInfo.target_mode);
        return _compiler.compile_to_target(source, plan);
    }

    private ArtifactSet compile_files_to_artifacts(
        IReadOnlyList<string> sourceFiles,
        string moduleName,
        CompileTargetInfo targetInfo)
    {
        var primaryFile = sourceFiles.FirstOrDefault() ?? moduleName;
        var plan = new BuildPlan(moduleName, targetInfo.canonical_triple, primaryFile, target_mode: targetInfo.target_mode);
        return _compiler.compile_files_to_target(sourceFiles, plan);
    }

    /// <summary>
    ///     将目标标识解析为 CompileTargetInfo。
    ///     接受完整 CanonicalTriple 四段式或短名（由 CanonicalTripleRegistry 内部展开）。
    /// </summary>
    private static bool try_resolve_target(string target, out CompileTargetInfo targetInfo)
    {
        targetInfo = default;

        try
        {
            var registry = new CanonicalTargetRegistry();
            var profile = registry.resolve(target);
            var compilationTarget = CanonicalTarget.parse(profile.canonical_triple).to_compilation_target();
            targetInfo = new CompileTargetInfo(compilationTarget, profile.canonical_triple, profile.host_kind, profile.target_mode);
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    ///     WASM 模块清单信息。
    /// </summary>
    private readonly record struct WasmModuleInfo(string Name, string Url, string Glue);

    #endregion
}
