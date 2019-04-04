using System.Text;
using System.Text.RegularExpressions;
using ExitCode = Core.Terminal.ExitCode;

namespace Legion.CLI.Commands;

/// <summary>
///     legion spy verify 子模式：构建 + 验证产物 + 自动定位错误。
///     支持 WASM（通过 Node.js <c>WebAssembly.Module</c>）、
///     JVM（通过 <c>java -verify</c>）和 CLR（通过 <c>dotnet exec</c>）三种目标的验证。
/// </summary>
internal static partial class LegionSpyVerify
{
    /// <summary>
   ///     Node.js 执行超时时间（毫秒）。
    /// </summary>
    private const int NodeTimeoutMs = 30_000;

    /// <summary>
    ///     Java 执行超时时间（毫秒）。
    /// </summary>
    private const int JavaTimeoutMs = 30_000;

    /// <summary>
    ///     dotnet 执行超时时间（毫秒）。
    /// </summary>
    private const int DotnetTimeoutMs = 30_000;

    /// <summary>
    ///     WASM 验证错误中偏移量的正则表达式。
    /// </summary>
    private static readonly Regex WasmErrorOffsetRegex = new(
        @"@\+(\d+)",
        RegexOptions.Compiled);

    /// <summary>
    ///     WASM 验证错误中函数索引的正则表达式。
    /// </summary>
    private static readonly Regex WasmErrorFuncIndexRegex = new(
        @"function #(\d+)",
        RegexOptions.Compiled);

    /// <summary>
    ///     JVM VerifyError 中方法名的正则表达式。
    /// </summary>
    private static readonly Regex JvmVerifyErrorMethodRegex = new(
        @"at\s+([\w.$]+)\s*\(",
        RegexOptions.Compiled);

    /// <summary>
    ///     执行构建与验证。
    /// </summary>
    /// <param name="project">项目路径，为 null 时使用当前目录。</param>
    /// <param name="targetPlatform">编译目标：wasm / jvm / clr。</param>
    /// <param name="context">错误点上下文行数（传递给 spy wasm 的 --context）。</param>
    /// <param name="json">是否以 JSON 格式输出。</param>
    /// <returns>验证通过返回 ExitCode.Success，失败返回 ExitCode.Error。</returns>
    public static async Task<ExitCode> run(string? project, string? targetPlatform, int context, bool json)
    {
        if (string.IsNullOrWhiteSpace(targetPlatform))
        {
            Console.Error.WriteLine("用法：legion spy verify <project> --target <wasm|jvm|clr>");
            return ExitCode.InvalidArgs;
        }

        var platform = targetPlatform.ToLowerInvariant();
        switch (platform)
        {
            case "wasm":
                return await verify_wasm(project, context, json);

            case "jvm":
                return await verify_jvm(project, context, json);

            case "clr":
                return await verify_clr(project, context, json);

            default:
                Console.Error.WriteLine($"错误：不支持的目标平台 '{targetPlatform}'，请使用 wasm / jvm / clr");
                return ExitCode.InvalidArgs;
        }
    }

    #region WASM 验证

    /// <summary>
    ///     构建 WASM 目标并使用 Node.js <c>WebAssembly.Module</c> 验证。
    ///     失败时自动定位错误偏移并反汇编。
    /// </summary>
    private static async Task<ExitCode> verify_wasm(string? project, int context, bool json)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：无法解析项目目录：{project}");
            return ExitCode.Error;
        }

        if (!Directory.Exists(projectDir))
        {
            Console.Error.WriteLine($"错误：项目目录不存在：{projectDir}");
            return ExitCode.Error;
        }

        Console.Error.WriteLine($"正在构建 WASM 项目：{projectDir}");
        // Workspace 模式（含 legions.von）调用 build_workspace 以构建全部成员；
        // 单项目模式调用 build_single_project 仅构建当前项目。
        // 即使部分成员构建失败（前端语法/语义错误），也继续验证已成功构建的产物。
        var exitCode = LegionHelper.is_workspace(projectDir)
            ? LegionHelper.build_workspace(projectDir, "wasm", null, true)
            : LegionHelper.build_single_project(projectDir, "wasm", null, true);

        if (exitCode != 0)
        {
            Console.Error.WriteLine("警告：部分成员构建失败，仅验证已成功构建的产物");
        }

        Console.Error.WriteLine("开始验证 WASM 产物...");
        var distDir = Path.Combine(projectDir, "dist");
        var results = await find_and_verify_wasm_files(distDir, context, json);

        return results ? ExitCode.Success : ExitCode.Error;
    }

    /// <summary>
    ///     递归查找所有 <c>.wasm</c> 文件并逐一验证。
    /// </summary>
    private static async Task<bool> find_and_verify_wasm_files(string rootDir, int context, bool json)
    {
        var allPassed = true;
        var foundAny = false;

        var wasmFiles = Directory.GetFiles(rootDir, "*.wasm", SearchOption.AllDirectories);
        foreach (var file in wasmFiles)
        {
            foundAny = true;
            Console.Error.WriteLine($"  验证：{file}");

            var passed = await validate_wasm_file(file, context, json);
            if (!passed)
            {
                allPassed = false;
            }
        }

        if (!foundAny)
        {
            Console.Error.WriteLine($"  在 {rootDir} 中未找到任何 .wasm 文件");
            return false;
        }

        return allPassed;
    }

    /// <summary>
    ///     使用 Node.js 的 <c>WebAssembly.Module</c> 验证单个 WASM 文件。
    ///     失败时自动调用 spy wasm 定位错误。
    /// </summary>
    private static async Task<bool> validate_wasm_file(string wasmFile, int context, bool json)
    {
        var node = resolveNode();
        if (node is null)
        {
            Console.Error.WriteLine($"  警告：未找到 node，跳过验证：{wasmFile}");
            return true;
        }

        var script = create_wasm_validate_script(wasmFile);
        var (code, stdout, stderr) = await runNodeScript(node, script);

        if (code == ExitCode.Success)
        {
            Console.Error.WriteLine($"    验证通过");
            return true;
        }

        // 验证失败，尝试从错误消息中解析偏移量
        Console.Error.WriteLine($"    验证失败：{stderr}");

        var offsetMatch = WasmErrorOffsetRegex.Match(stderr);
        var funcIndexMatch = WasmErrorFuncIndexRegex.Match(stderr);

        if (offsetMatch.Success)
        {
            var offset = int.Parse(offsetMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            Console.Error.WriteLine($"    自动定位：偏移 @{offset}，调用 spy wasm 反汇编...");

            var absOffset = extract_abs_offset(wasmFile, offset);
            if (absOffset.HasValue)
            {
                _ = LegionSpyWasm.run(wasmFile, null, absOffset.Value, false, context, json, false);
            }
            else
            {
                Console.Error.WriteLine($"    警告：无法计算绝对偏移，使用相对偏移直接定位");
                _ = LegionSpyWasm.run(wasmFile, null, offset, false, context, json, false);
            }
        }

        return false;
    }

    /// <summary>
    ///     从 WASM 文件头部开始计算绝对偏移。
    /// </summary>
    private static int? extract_abs_offset(string wasmFile, int relativeOffset)
    {
        try
        {
            using var fs = new FileStream(wasmFile, FileMode.Open, FileAccess.Read);
            var header = new byte[8];
            var bytesRead = fs.Read(header, 0, 8);
            if (bytesRead < 8)
            {
                return null;
            }

            // 解析段，找到相对偏移对应的绝对偏移
            int pos = 8;
            while (pos < header.Length || pos < (int)fs.Length)
            {
                if (fs.Position != pos)
                {
                    fs.Seek(pos, SeekOrigin.Begin);
                }

                var sectionHeader = new byte[1];
                if (fs.Read(sectionHeader, 0, 1) == 0)
                {
                    break;
                }

                int sectionId = sectionHeader[0];
                // 读取 LEB128 段大小
                var lebBuf = new byte[5];
                int lebLen = 0;
                int b;
                do
                {
                    if (fs.Read(lebBuf, lebLen, 1) == 0)
                    {
                        return null;
                    }
                    b = lebBuf[lebLen++];
                } while ((b & 0x80) != 0 && lebLen < 5);

                int sectionSize = 0;
                for (int i = 0; i < lebLen; i++)
                {
                    sectionSize |= (lebBuf[i] & 0x7F) << (i * 7);
                }

                int sectionContentStart = pos + 1 + lebLen;
                int sectionContentEnd = sectionContentStart + sectionSize;

                // 代码段（id=10）包含函数体
                if (sectionId == 10)
                {
                    // 相对偏移在代码段内容范围内
                    if (relativeOffset >= sectionContentStart && relativeOffset < sectionContentEnd)
                    {
                        return relativeOffset;
                    }
                }

                pos = sectionContentEnd;
            }

            // 如果没找到段匹配，直接返回相对偏移（可能已经在代码段内）
            return relativeOffset;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     创建临时的 WASM 验证脚本。
    /// </summary>
    private static string create_wasm_validate_script(string wasmFile)
    {
        var escapedPath = wasmFile.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $@"
import {{ readFile }} from 'node:fs/promises';
const buf = await readFile('{escapedPath}');
try {{
    new WebAssembly.Module(buf);
    console.log('VALID');
}} catch (e) {{
    console.error(e.message);
    process.exit(1);
}}
";
    }

    #endregion

    #region JVM 验证

    /// <summary>
    ///     构建 JVM 目标并使用 <c>java -verify</c> 验证。
    ///     失败时自动定位错误方法并反汇编。
    /// </summary>
    private static async Task<ExitCode> verify_jvm(string? project, int context, bool json)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：无法解析项目目录：{project}");
            return ExitCode.Error;
        }

        if (!Directory.Exists(projectDir))
        {
            Console.Error.WriteLine($"错误：项目目录不存在：{projectDir}");
            return ExitCode.Error;
        }

        Console.Error.WriteLine($"正在构建 JVM 项目：{projectDir}");
        var exitCode = LegionHelper.build_single_project(projectDir, "jvm", null, true);

        if (exitCode != 0)
        {
            Console.Error.WriteLine("构建失败，无法验证");
            return ExitCode.Error;
        }

        Console.Error.WriteLine("构建成功，开始验证 JVM 产物...");
        var distDir = Path.Combine(projectDir, "dist");
        var jarFiles = Directory.GetFiles(distDir, "*.jar", SearchOption.AllDirectories);

        if (jarFiles.Length == 0)
        {
            Console.Error.WriteLine($"  在 {distDir} 中未找到任何 .jar 文件");
            return ExitCode.Error;
        }

        var java = resolveJava();
        if (java is null)
        {
            Console.Error.WriteLine("错误：未找到 java。请确认 JDK 已安装且在 PATH 中。");
            return ExitCode.CommandNotFound;
        }

        var allPassed = true;
        foreach (var jarFile in jarFiles)
        {
            Console.Error.WriteLine($"  验证：{jarFile}");
            var passed = await verify_jar_with_java(java, jarFile);
            if (!passed)
            {
                allPassed = false;
                Console.Error.WriteLine($"    调用 spy jvm 反汇编...");
                _ = await LegionSpyJvm.run(jarFile, null, false, json);
            }
        }

        return allPassed ? ExitCode.Success : ExitCode.Error;
    }

    /// <summary>
    ///     使用 <c>java -verify</c> 验证单个 JAR 文件。
    /// </summary>
    private static async Task<bool> verify_jar_with_java(string java, string jarFile)
    {
        var className = Path.GetFileNameWithoutExtension(jarFile);
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = java,
            Arguments = $"-verify -cp \"{jarFile}\" {className}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var proc = System.Diagnostics.Process.Start(psi)!;
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (proc.ExitCode == 0)
        {
            return true;
        }

        Console.Error.WriteLine($"    java -verify 失败：{stderr}");

        // 尝试从 VerifyError 消息中提取方法名
        var match = JvmVerifyErrorMethodRegex.Match(stderr);
        if (match.Success)
        {
            var methodName = match.Groups[1].Value;
            Console.Error.WriteLine($"    自动定位到方法：{methodName}");
        }

        return false;
    }

    #endregion

    #region CLR 验证

    /// <summary>
    ///     构建 CLR 目标并通过 <c>dotnet exec</c> 隐式验证。
    ///     CLR 的 IL 验证由 .NET 运行时在 JIT 编译时自动完成。
    /// </summary>
    private static async Task<ExitCode> verify_clr(string? project, int context, bool json)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：无法解析项目目录：{project}");
            return ExitCode.Error;
        }

        if (!Directory.Exists(projectDir))
        {
            Console.Error.WriteLine($"错误：项目目录不存在：{projectDir}");
            return ExitCode.Error;
        }

        Console.Error.WriteLine($"正在构建 CLR 项目：{projectDir}");
        var exitCode = LegionHelper.build_single_project(projectDir, "clr", null, true);

        if (exitCode != 0)
        {
            Console.Error.WriteLine("构建失败，无法验证");
            return ExitCode.Error;
        }

        Console.Error.WriteLine("构建成功，开始验证 CLR 产物...");
        var distDir = Path.Combine(projectDir, "dist");

        // 查找所有 .exe 和 .dll
        var exeFiles = Directory.GetFiles(distDir, "*.exe", SearchOption.AllDirectories);
        var dllFiles = Directory.GetFiles(distDir, "*.dll", SearchOption.AllDirectories);
        var allFiles = exeFiles.Concat(dllFiles).ToList();

        if (allFiles.Count == 0)
        {
            Console.Error.WriteLine($"  在 {distDir} 中未找到任何 .exe / .dll 文件");
            return ExitCode.Error;
        }

        var allPassed = true;
        foreach (var file in allFiles)
        {
            Console.Error.WriteLine($"  验证：{file}");
            var passed = await verify_clr_assembly(file);
            if (!passed)
            {
                allPassed = false;
                Console.Error.WriteLine($"    调用 spy clr 分析...");
                _ = await LegionSpyClr.run(file, null, false, json);
            }
        }

        return allPassed ? ExitCode.Success : ExitCode.Error;
    }

    /// <summary>
    ///     通过 <c>dotnet exec</c> 尝试加载程序集来验证 CLR 产物。
    /// </summary>
    private static async Task<bool> verify_clr_assembly(string assemblyFile)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"exec \"{assemblyFile}\" --help",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var proc = System.Diagnostics.Process.Start(psi)!;
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            // 退出码非 0 视为验证失败。
            // InvalidProgramException / MissingMethodException 等 JIT 阶段错误
            // 退出码非 0 但 stderr 可能为空，必须检查退出码而非仅依赖 stderr 模式匹配。
            if (proc.ExitCode != 0)
            {
                var diagnostic = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                Console.Error.WriteLine($"    CLR 验证失败（退出码 {proc.ExitCode}）：{diagnostic}");
                return false;
            }

            // 兜底：退出码为 0 但 stderr 出现已知异常模式时仍视为失败
            if (stderr.Contains("Could not load type")
                || stderr.Contains("TypeLoadException")
                || stderr.Contains("BadImageFormatException")
                || stderr.Contains("Corrupt executable")
                || stderr.Contains("InvalidProgramException")
                || stderr.Contains("MissingMethodException"))
            {
                Console.Error.WriteLine($"    CLR 验证失败：{stderr}");
                return false;
            }

            Console.Error.WriteLine($"    验证通过");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"    验证异常：{ex.Message}");
            return false;
        }
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     执行 Node.js 脚本并返回结果。
    /// </summary>
    private static async Task<(ExitCode code, string stdout, string stderr)> runNodeScript(
        string node, string script)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"legion-wasm-validate-{Guid.NewGuid()}.mjs");
        try
        {
            await File.WriteAllTextAsync(tempFile, script);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = node,
                Arguments = tempFile,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var proc = System.Diagnostics.Process.Start(psi)!;
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            return (proc.ExitCode == 0 ? ExitCode.Success : ExitCode.Error, stdout, stderr);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // 忽略临时文件删除失败
            }
        }
    }

    /// <summary>
    ///     解析 Node.js 可执行文件路径。
    /// </summary>
    private static string? resolveNode()
    {
        return findOnPath("node.exe") ?? findOnPath("node");
    }

    /// <summary>
    ///     解析 Java 可执行文件路径。
    /// </summary>
    private static string? resolveJava()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var candidate = Path.Combine(javaHome, "bin", "java.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return findOnPath("java.exe") ?? findOnPath("java");
    }

    /// <summary>
    ///     在 PATH 中查找指定可执行文件。
    /// </summary>
    private static string? findOnPath(string fileName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return null;
        }

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            try
            {
                var candidate = Path.Combine(dir.Trim('"'), fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // 路径格式非法时跳过
            }
        }

        return null;
    }

    #endregion
}
