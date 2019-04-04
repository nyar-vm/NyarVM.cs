using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ExitCode = Core.Terminal.ExitCode;

namespace Legion.CLI.Commands;

/// <summary>
///     legion spy clr 子模式：CLR / MSIL dump 与分析。
///     支持解析 CLR 后端输出的 <c>.msil</c> 文本，按方法过滤输出，
///     或通过 <c>ildasm</c> 将 <c>.exe</c> / <c>.dll</c> 转为文本后分析。
/// </summary>
internal static partial class LegionSpyClr
{
    /// <summary>
    ///     执行 CLR / MSIL dump。
    /// </summary>
    /// <param name="file">目标文件路径，支持 <c>.msil</c> / <c>.il</c> / <c>.exe</c> / <c>.dll</c>。</param>
    /// <param name="method">方法名过滤器（子串匹配，不区分大小写）。</param>
    /// <param name="list">是否仅列出所有方法签名。</param>
    /// <param name="json">是否以 JSON 格式输出。</param>
    /// <returns>命令退出码。</returns>
    public static async Task<ExitCode> run(string? file, string? method, bool list, bool json)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("用法：legion spy clr <file> [--method <name>] [--list] [--json]");
            Console.Error.WriteLine("  file   目标文件（.msil / .il / .exe / .dll）");
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

        var ext = Path.GetExtension(file).ToLowerInvariant();
        string? msilPath = null;
        string? tempPath = null;

        try
        {
            switch (ext)
            {
                case ".msil":
                case ".il":
                    msilPath = file;
                    break;

                case ".exe":
                case ".dll":
                    {
                        var converted = await tryConvertWithIldasm(file);
                        if (converted.path is null)
                        {
                            Console.Error.WriteLine("错误：ildasm 不可用。");
                            Console.Error.WriteLine(
                                $"请先转换为文本格式：ildasm \"{file}\" /text > \"{file}.msil\"");
                            Console.Error.WriteLine(
                                $"然后运行：legion spy clr \"{file}.msil\" --method <name>");
                            return converted.exitCode;
                        }

                        msilPath = converted.path;
                        tempPath = converted.tempPath;
                        break;
                    }

                default:
                    Console.Error.WriteLine(
                        $"错误：不支持的文件扩展名 '{ext}'，支持 .msil / .il / .exe / .dll");
                    return ExitCode.InvalidArgs;
            }

            var lines = await File.ReadAllLinesAsync(msilPath);
            var methods = parseMethods(lines);

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
            if (tempPath is not null && File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                    // 忽略临时文件删除失败
                }
            }
        }
    }

    /// <summary>
    ///     尝试通过 <c>ildasm</c> 将 <c>.exe</c> / <c>.dll</c> 转为 MSIL 文本。
    /// </summary>
    /// <param name="file">输入的程序集文件路径。</param>
    /// <returns>转换结果：成功时包含临时 MSIL 文件路径；失败时包含退出码。</returns>
    private static async Task<(string? path, string? tempPath, ExitCode exitCode)> tryConvertWithIldasm(
        string file)
    {
        var ildasm = resolveIldasm();
        if (ildasm is null)
        {
            return (null, null, ExitCode.CommandNotFound);
        }

        var tempDir = Path.GetTempPath();
        var tempName = $"{Path.GetFileNameWithoutExtension(file)}_{Guid.NewGuid():N}.msil";
        var tempPath = Path.Combine(tempDir, tempName);

        var psi = new ProcessStartInfo
        {
            FileName = ildasm,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(file);
        psi.ArgumentList.Add("/text");
        psi.ArgumentList.Add("/utf8");

        using var proc = new Process();
        proc.StartInfo = psi;

        try
        {
            proc.Start();
        }
        catch (Win32Exception ex)
        {
            Console.Error.WriteLine($"错误：无法启动 ildasm：{ex.Message}");
            return (null, null, ExitCode.Error);
        }

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (proc.ExitCode != 0)
        {
            Console.Error.WriteLine($"错误：ildasm 退出码 {proc.ExitCode}。");
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.Error.WriteLine(stderr);
            }

            return (null, tempPath, ExitCode.Error);
        }

        await File.WriteAllTextAsync(tempPath, stdout, Encoding.UTF8);
        return (tempPath, tempPath, ExitCode.Success);
    }

    /// <summary>
    ///     解析 <c>ILDASM_PATH</c> 环境变量或在 <c>PATH</c> 中查找 <c>ildasm</c>。
    /// </summary>
    /// <returns>ildasm 可执行文件路径；未找到时返回 <c>null</c>。</returns>
    private static string? resolveIldasm()
    {
        var envPath = Environment.GetEnvironmentVariable("ILDASM_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        return findOnPath("ildasm.exe") ?? findOnPath("ildasm");
    }

    /// <summary>
    ///     在 <c>PATH</c> 中查找指定可执行文件。
    /// </summary>
    /// <param name="fileName">要查找的文件名。</param>
    /// <returns>找到的完整路径；未找到时返回 <c>null</c>。</returns>
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

    /// <summary>
    ///     从 MSIL 文本行中解析出所有方法块。
    /// </summary>
    /// <param name="lines">MSIL 文本的所有行。</param>
    /// <returns>方法信息列表，按出现顺序排列。</returns>
    private static List<MsilMethod> parseMethods(string[] lines)
    {
        var result = new List<MsilMethod>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith(".method", StringComparison.Ordinal))
            {
                continue;
            }

            var signature = extractSignature(trimmed);
            var (endLine, balanced) = findBlockEnd(lines, i);
            if (!balanced)
            {
                // 不闭合的方法：截断到文件末尾，仍然收录
                endLine = lines.Length - 1;
            }

            var body = new List<string>();
            for (var j = i; j <= endLine && j < lines.Length; j++)
            {
                body.Add(lines[j]);
            }

            result.Add(new MsilMethod(signature, extractMethodName(signature), i + 1, body));
            i = endLine;
        }

        return result;
    }

    /// <summary>
    ///     从 <c>.method</c> 行中提取方法签名（去除 <c>.method</c> 前缀与末尾的 <c>{</c>）。
    /// </summary>
    /// <param name="methodLine">以 <c>.method</c> 开头的行（已去前导空白）。</param>
    /// <returns>清理后的方法签名。</returns>
    private static string extractSignature(string methodLine)
    {
        var sig = methodLine.Substring(".method".Length).Trim();
        var braceIdx = sig.IndexOf('{');
        if (braceIdx >= 0)
        {
            sig = sig.Substring(0, braceIdx).Trim();
        }

        return sig.TrimEnd();
    }

    /// <summary>
    ///     从方法签名中提取方法名部分（去参数列表与返回类型）。
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
    ///     从 <c>.method</c> 所在行起，查找其方法体大括号闭合的行号。
    /// </summary>
    /// <param name="lines">MSIL 全部行。</param>
    /// <param name="methodLineIndex"><c>.method</c> 所在行索引。</param>
    /// <returns>(结束行索引, 是否成功匹配到闭合括号)。</returns>
    private static (int endLine, bool balanced) findBlockEnd(string[] lines, int methodLineIndex)
    {
        var depth = 0;
        var seenOpen = false;

        for (var i = methodLineIndex; i < lines.Length; i++)
        {
            foreach (var ch in lines[i])
            {
                if (ch == '{')
                {
                    depth++;
                    seenOpen = true;
                }
                else if (ch == '}')
                {
                    depth--;
                }

                if (seenOpen && depth == 0)
                {
                    return (i, true);
                }
            }
        }

        return (lines.Length - 1, false);
    }

    /// <summary>
    ///     输出方法列表（文本或 JSON）。
    /// </summary>
    /// <param name="methods">方法信息列表。</param>
    /// <param name="json">是否以 JSON 格式输出。</param>
    private static void printList(List<MsilMethod> methods, bool json)
    {
        if (json)
        {
            var payload = methods.Select(m => new
            {
                signature = m.signature,
                name = m.name,
                line = m.startLine,
                size = m.body.Count,
            });

            var document = JsonSerializer.Serialize(payload, JsonOptions);
            Console.WriteLine(document);
            return;
        }

        if (methods.Count == 0)
        {
            Console.Error.WriteLine("（未发现任何 .method）");
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
    /// <returns>命令退出码；未匹配时返回 <see cref="ExitCode.Error"/>。</returns>
    private static ExitCode printMethodBlock(List<MsilMethod> methods, string method, bool json)
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
                body = string.Join(Environment.NewLine, m.body),
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
    ///     MSIL 方法信息：签名、名称、起始行号与方法体文本。
    /// </summary>
    /// <param name="signature">完整方法签名（如 <c>public static int32 core.primitive.__i32_add(int32, int32)</c>）。</param>
    /// <param name="name">方法名（签名中的最后标识符）。</param>
    /// <param name="startLine">方法在源文件中的起始行号（从 1 开始）。</param>
    /// <param name="body">方法体文本（含 <c>.method</c> 行与闭合大括号）。</param>
    private sealed record MsilMethod(string signature, string name, int startLine, List<string> body);

    /// <summary>
    ///     JSON 序列化选项：缩进输出，中文不转义。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
