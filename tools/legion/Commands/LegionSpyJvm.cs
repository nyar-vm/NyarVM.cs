using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExitCode = Core.Terminal.ExitCode;

namespace Legion.CLI.Commands;

/// <summary>
///     legion spy jvm 子模式：JVM 字节码反汇编与分析。
///     支持解析 <c>javap -c -p</c> 输出，按方法过滤输出，
///     或将 <c>.jar</c> / <c>.class</c> 通过 <c>javap</c> 转为文本后分析。
/// </summary>
internal static partial class LegionSpyJvm
{
    /// <summary>
    ///     javap 进程执行超时时间（毫秒）。
    /// </summary>
    private const int TimeoutMs = 30_000;

    /// <summary>
    ///     匹配 javap 输出中方法声明行的正则：以修饰符开头，含括号，以分号结尾。
    /// </summary>
    private static readonly Regex MethodLineRegex = new(
        @"^\s*(public|protected|private|static|final|abstract|native|synchronized|transient|volatile|default|strictfp)\b.*\(.*\);?\s*$",
        RegexOptions.Compiled);

    /// <summary>
    ///     匹配 javap -c 输出中字节码指令行：<c>偏移: 助记符 操作数</c>。
    /// </summary>
    private static readonly Regex InstructionLineRegex = new(
        @"^\s*(\d+):\s*(.*)$",
        RegexOptions.Compiled);

    /// <summary>
    ///     JSON 序列化选项：缩进输出，中文不转义。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    ///     执行 JVM 字节码反汇编。
    /// </summary>
    /// <param name="file">目标文件路径，支持 <c>.jar</c> / <c>.class</c>。</param>
    /// <param name="method">方法名过滤器（子串匹配，不区分大小写）。</param>
    /// <param name="list">是否仅列出所有方法签名。</param>
    /// <param name="json">是否以 JSON 格式输出。</param>
    /// <returns>命令退出码。</returns>
    public static async Task<ExitCode> run(string? file, string? method, bool list, bool json)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("用法：legion spy jvm <file> [--method <name>] [--list] [--json]");
            Console.Error.WriteLine("  file   目标文件（.jar / .class）");
            Console.Error.WriteLine("  --method <name>   输出包含指定名称的方法体");
            Console.Error.WriteLine("  --list            列出所有方法签名");
            Console.Error.WriteLine("  --json            以 JSON 格式输出");
            return ExitCode.InvalidArgs;
        }

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"错误：文件不存在：{file}");
            return ExitCode.FileNotFound;
        }

        var javap = resolveJavap();
        if (javap is null)
        {
            Console.Error.WriteLine("错误：未找到 javap。请确认 JDK 已安装且在 PATH 中。");
            Console.Error.WriteLine("可通过设置 JAVA_HOME 或将 <jdk>/bin 加入 PATH 解决。");
            return ExitCode.CommandNotFound;
        }

        var ext = Path.GetExtension(file).ToLowerInvariant();
        string? tempDir = null;

        try
        {
            List<string> javapOutputs;

            switch (ext)
            {
                case ".class":
                {
                    var (code, output) = await runJavap(javap, file, null);
                    if (code != ExitCode.Success)
                    {
                        return code;
                    }

                    javapOutputs = new List<string> { output };
                    break;
                }

                case ".jar":
                {
                    tempDir = Path.Combine(Path.GetTempPath(), $"legion-jvm-{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);

                    var extracted = extractClassesFromJar(file, tempDir);
                    if (extracted.Count == 0)
                    {
                        Console.Error.WriteLine($"错误：未能从 jar 中提取任何 .class 文件：{file}");
                        return ExitCode.Error;
                    }

                    javapOutputs = new List<string>();
                    foreach (var classFile in extracted)
                    {
                        var (code, output) = await runJavap(javap, classFile, null);
                        if (code != ExitCode.Success)
                        {
                            // 单个 class 失败不致命，记录到 stderr 后继续
                            Console.Error.WriteLine($"警告：javap 处理 {Path.GetFileName(classFile)} 失败");
                            continue;
                        }

                        javapOutputs.Add(output);
                    }

                    if (javapOutputs.Count == 0)
                    {
                        Console.Error.WriteLine("错误：所有 class 的 javap 调用均失败。");
                        return ExitCode.Error;
                    }

                    break;
                }

                default:
                    Console.Error.WriteLine(
                        $"错误：不支持的文件扩展名 '{ext}'，支持 .jar / .class");
                    return ExitCode.InvalidArgs;
            }

            var methods = new List<JvmMethod>();
            foreach (var output in javapOutputs)
            {
                var lines = output.Replace("\r\n", "\n").Split('\n');
                methods.AddRange(parseMethods(lines));
            }

            // 未指定任何过滤条件时，默认等价于 --list
            var effectiveList = list || string.IsNullOrWhiteSpace(method);

            if (effectiveList)
            {
                printList(methods, json);
                return ExitCode.Success;
            }

            return printMethodBlock(methods, method!, json);
        }
        finally
        {
            if (tempDir is not null && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (IOException)
                {
                    // 忽略临时目录删除失败
                }
            }
        }
    }

    /// <summary>
    ///     调用 <c>javap -c -p</c> 反汇编指定的 <c>.class</c> 文件。
    /// </summary>
    /// <param name="javap">javap 可执行文件路径。</param>
    /// <param name="classFile"><c>.class</c> 文件路径。</param>
    /// <param name="cwd">工作目录；为 <c>null</c> 时使用 <paramref name="classFile" /> 所在目录。</param>
    /// <returns>(退出码, javap 标准输出文本)。</returns>
    private static async Task<(ExitCode code, string output)> runJavap(
        string javap, string classFile, string? cwd)
    {
        var psi = new ProcessStartInfo
        {
            FileName = javap,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = cwd ?? Path.GetDirectoryName(classFile) ?? string.Empty,
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add(Path.GetFileName(classFile));

        using var proc = new Process();
        proc.StartInfo = psi;

        try
        {
            proc.Start();
        }
        catch (Win32Exception ex)
        {
            Console.Error.WriteLine($"错误：无法启动 javap：{ex.Message}");
            return (ExitCode.Error, string.Empty);
        }

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();

        var exited = proc.WaitForExit(TimeoutMs);
        if (!exited)
        {
            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // 进程已退出
            }
            catch (Win32Exception)
            {
                // 无法终止进程
            }

            Console.Error.WriteLine($"错误：javap 执行超时（{TimeoutMs / 1000} 秒）。");
            return (ExitCode.Timeout, string.Empty);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (proc.ExitCode != 0)
        {
            Console.Error.WriteLine($"错误：javap 退出码 {proc.ExitCode}。");
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.Error.WriteLine(stderr);
            }

            return (ExitCode.Error, string.Empty);
        }

        return (ExitCode.Success, stdout);
    }

    /// <summary>
    ///     从 jar 中提取所有 <c>.class</c> 文件（排除 <c>module-info</c>）到目标目录。
    /// </summary>
    /// <param name="jarPath">jar 文件路径。</param>
    /// <param name="destDir">解压目标目录。</param>
    /// <returns>解压后的 <c>.class</c> 文件完整路径列表。</returns>
    private static List<string> extractClassesFromJar(string jarPath, string destDir)
    {
        var jar = resolveExecutable("jar") ?? "jar";

        var psi = new ProcessStartInfo
        {
            FileName = jar,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = destDir,
        };
        psi.ArgumentList.Add("xf");
        psi.ArgumentList.Add(jarPath);

        using var proc = new Process();
        proc.StartInfo = psi;

        try
        {
            proc.Start();
        }
        catch (Win32Exception ex)
        {
            Console.Error.WriteLine($"错误：无法启动 jar：{ex.Message}");
            return new List<string>();
        }

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();

        var exited = proc.WaitForExit(TimeoutMs);
        if (!exited)
        {
            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // 进程已退出
            }
            catch (Win32Exception)
            {
                // 无法终止进程
            }

            Console.Error.WriteLine($"错误：jar 执行超时（{TimeoutMs / 1000} 秒）。");
            return new List<string>();
        }

        var stderr = stderrTask.Result;
        if (proc.ExitCode != 0)
        {
            Console.Error.WriteLine($"错误：jar 退出码 {proc.ExitCode}。");
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.Error.WriteLine(stderr);
            }

            return new List<string>();
        }

        return Directory.GetFiles(destDir, "*.class", SearchOption.AllDirectories)
            .Where(p => !Path.GetFileName(p).StartsWith("module-info", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    ///     解析 <c>JAVA_HOME</c> 环境变量或在 <c>PATH</c> 中查找 <c>javap</c>。
    /// </summary>
    /// <returns>javap 可执行文件路径；未找到时返回 <c>null</c>。</returns>
    private static string? resolveJavap()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var candidate = Path.Combine(javaHome, "bin", "javap.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(javaHome, "bin", "javap");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return resolveExecutable("javap");
    }

    /// <summary>
    ///     在 <c>PATH</c> 中查找指定可执行文件（Windows 上自动追加 <c>.exe</c>）。
    /// </summary>
    /// <param name="fileName">要查找的文件名（可不含扩展名）。</param>
    /// <returns>找到的完整路径；未找到时返回 <c>null</c>。</returns>
    private static string? resolveExecutable(string fileName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return null;
        }

        var candidates = new List<string> { fileName };
        if (OperatingSystem.IsWindows() && !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(fileName + ".exe");
        }

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            try
            {
                foreach (var candidate in candidates)
                {
                    var full = Path.Combine(dir.Trim('"'), candidate);
                    if (File.Exists(full))
                    {
                        return full;
                    }
                }
            }
            catch (ArgumentException)
            {
                // 路径格式非法时跳过
            }
        }

        return null;
    }

    /// <summary>
    ///     从 javap 输出中解析出所有方法块。
    ///     方法块从匹配 <see cref="MethodLineRegex" /> 的签名行开始，
    ///     到下一个方法签名行（或文件末尾）前结束。
    /// </summary>
    /// <param name="lines">javap 输出的所有行。</param>
    /// <returns>方法信息列表，按出现顺序排列。</returns>
    private static List<JvmMethod> parseMethods(string[] lines)
    {
        var result = new List<JvmMethod>();

        // 先定位所有方法签名行的索引
        var signatures = new List<(int index, string line)>();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (isMethodSignature(line))
            {
                signatures.Add((i, line.Trim()));
            }
        }

        for (var s = 0; s < signatures.Count; s++)
        {
            var (startIdx, signature) = signatures[s];
            var endIdx = s + 1 < signatures.Count ? signatures[s + 1].index : lines.Length;

            var body = new List<string>();
            for (var j = startIdx; j < endIdx; j++)
            {
                body.Add(lines[j]);
            }

            var name = extractMethodName(signature);
            var instructions = parseInstructions(body);
            result.Add(new JvmMethod(signature, name, startIdx + 1, body, instructions));
        }

        return result;
    }

    /// <summary>
    ///     判断一行是否为 javap 输出中的方法签名行。
    /// </summary>
    /// <param name="line">待判断的行。</param>
    /// <returns>是方法签名行返回 <c>true</c>；否则 <c>false</c>。</returns>
    private static bool isMethodSignature(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.TrimStart();

        // 跳过明显不是声明的行（指令行、注释、大括号、Code: 等）
        if (trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("{", StringComparison.Ordinal)
            || trimmed.StartsWith("}", StringComparison.Ordinal))
        {
            return false;
        }

        if (InstructionLineRegex.IsMatch(trimmed))
        {
            return false;
        }

        // 必须包含括号
        if (!trimmed.Contains('(') || !trimmed.Contains(')'))
        {
            return false;
        }

        return MethodLineRegex.IsMatch(trimmed);
    }

    /// <summary>
    ///     从方法签名中提取方法名部分（去参数列表与返回类型/修饰符）。
    /// </summary>
    /// <param name="signature">方法签名。</param>
    /// <returns>方法名；无法识别时返回完整签名。</returns>
    private static string extractMethodName(string signature)
    {
        var parenIdx = signature.IndexOf('(');
        var head = parenIdx >= 0 ? signature.Substring(0, parenIdx).Trim() : signature.Trim();

        var tokens = head.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return signature;
        }

        return tokens[^1];
    }

    /// <summary>
    ///     从方法体行中解析字节码指令列表。
    /// </summary>
    /// <param name="body">方法体文本行。</param>
    /// <returns>指令列表（偏移 + 助记符文本）。</returns>
    private static List<JvmInstruction> parseInstructions(List<string> body)
    {
        var instructions = new List<JvmInstruction>();

        foreach (var raw in body)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var match = InstructionLineRegex.Match(raw);
            if (!match.Success)
            {
                continue;
            }

            var offset = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var mnemonic = match.Groups[2].Value.Trim();
            instructions.Add(new JvmInstruction(offset, mnemonic));
        }

        return instructions;
    }

    /// <summary>
    ///     输出方法列表（文本或 JSON）。
    /// </summary>
    /// <param name="methods">方法信息列表。</param>
    /// <param name="json">是否以 JSON 格式输出。</param>
    private static void printList(List<JvmMethod> methods, bool json)
    {
        if (json)
        {
            var payload = methods.Select(m => new
            {
                signature = m.signature,
                name = m.name,
                line = m.startLine,
                instructionCount = m.instructions.Count,
            });

            Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
            return;
        }

        if (methods.Count == 0)
        {
            Console.Error.WriteLine("（未发现任何方法）");
            return;
        }

        Console.WriteLine($"共 {methods.Count} 个方法：");
        foreach (var m in methods)
        {
            Console.WriteLine($"  [{m.startLine,5}]  {m.signature}");
        }
    }

    /// <summary>
    ///     输出匹配指定名称的方法体。
    /// </summary>
    /// <param name="methods">所有方法信息。</param>
    /// <param name="method">方法名过滤器（子串匹配，不区分大小写）。</param>
    /// <param name="json">是否以 JSON 格式输出。</param>
    /// <returns>命令退出码；未匹配时返回 <see cref="ExitCode.Error" />。</returns>
    private static ExitCode printMethodBlock(List<JvmMethod> methods, string method, bool json)
    {
        var matches = methods
            .Where(m => m.name.IndexOf(method, StringComparison.OrdinalIgnoreCase) >= 0
                || m.signature.IndexOf(method, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"错误：未找到匹配 '{method}' 的方法。");
            return ExitCode.Error;
        }

        if (json)
        {
            var payload = matches.Select(m => new
            {
                signature = m.signature,
                name = m.name,
                line = m.startLine,
                body = string.Join("\n", m.body),
                instructions = m.instructions.Select(i => new
                {
                    offset = i.offset,
                    mnemonic = i.mnemonic,
                }),
            });

            Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
            return ExitCode.Success;
        }

        foreach (var m in matches)
        {
            foreach (var line in m.body)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine();
        }

        Console.Error.WriteLine($"共匹配 {matches.Count} 个方法。");
        return ExitCode.Success;
    }

    /// <summary>
    ///     JVM 方法信息：签名、名称、起始行号、方法体文本与指令列表。
    /// </summary>
    /// <param name="signature">完整方法签名（如 <c>public static int legion_main(java.lang.String[]);</c>）。</param>
    /// <param name="name">方法名（签名中的最后标识符）。</param>
    /// <param name="startLine">方法在 javap 输出中的起始行号（从 1 开始）。</param>
    /// <param name="body">方法体文本（含方法签名行）。</param>
    /// <param name="instructions">解析出的字节码指令列表。</param>
    private sealed record JvmMethod(
        string signature,
        string name,
        int startLine,
        List<string> body,
        List<JvmInstruction> instructions);

    /// <summary>
    ///     JVM 字节码指令：偏移与助记符（含操作数）。
    /// </summary>
    /// <param name="offset">指令在方法字节码中的偏移量。</param>
    /// <param name="mnemonic">助记符与操作数文本。</param>
    private sealed record JvmInstruction(int offset, string mnemonic);
}
