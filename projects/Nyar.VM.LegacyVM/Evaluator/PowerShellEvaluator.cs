using Nyar.VM.LegacyVM.Algebra.Shell;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     PowerShell 脚本求值器，将 .ps1 源码逐行解析并执行。
///     支持：变量、cmdlet 调用、管道、重定向、if/foreach 等、class 定义、enum 定义、
///     begin/process/end 块、-match/-replace 运算符。
/// </summary>
public sealed class PowerShellEvaluator : IShellEvaluator
{
    /// <summary>
    ///     Shell 可执行文件名
    /// </summary>
    public string shell_name => "powershell.exe";

    /// <summary>
    ///     已定义的 PowerShell 类，键为类名，值为类信息
    /// </summary>
    private static readonly Dictionary<string, PsClassInfo> _defined_classes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     已定义的 PowerShell 枚举，键为枚举名，值为枚举值列表
    /// </summary>
    private static readonly Dictionary<string, List<string>> _defined_enums = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     求值 PowerShell 脚本
    /// </summary>
    /// <param name="source">PowerShell 源码</param>
    /// <param name="env">运行环境</param>
    /// <returns>执行结果</returns>
    public object evaluate(string source, Dictionary<string, object> env)
    {
        var shell = new ShellEvaluator("powershell.exe", env);
        var lines = source.Split('\n');
        var result = new object();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r', '\n').Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith('<'))
            {
                continue;
            }

            result = execute_line(line, shell, lines, ref i);
        }

        return result;
    }

    #region 行执行

    private static object execute_line(string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        // 变量赋值
        if (try_parse_ps_assignment(line, out var varName, out var varValue))
        {
            return shell.set_env(shell.str_const(varName), shell.str_const(varValue));
        }

        // Write-Output / echo
        if (try_parse_ps_echo(line, out var echoText))
        {
            Console.WriteLine(echoText);
            return echoText;
        }

        // Get-Location / pwd
        if (line is "Get-Location" or "pwd")
        {
            var dir = shell.get_cwd();
            Console.WriteLine(dir);
            return dir;
        }

        // Set-Location / cd / sl
        if (try_parse_ps_cd(line, out var cdPath))
        {
            return shell.cd(shell.str_const(cdPath));
        }

        // Get-ChildItem / ls / dir
        if (try_parse_ps_ls(line, out var lsPath))
        {
            var dir = string.IsNullOrEmpty(lsPath) ? shell.get_cwd() : shell.str_const(lsPath);
            var result = shell.list_dir(dir);
            Console.WriteLine(result);
            return result;
        }

        // Get-Content / cat / type
        if (try_parse_ps_read_file(line, out var readPath))
        {
            var content = shell.read_file(shell.str_const(readPath));
            Console.WriteLine(content);
            return content;
        }

        // Set-Content / Out-File
        if (try_parse_ps_write_file(line, out var writePath, out var writeContent))
        {
            return shell.write_file(shell.str_const(writePath), shell.str_const(writeContent));
        }

        // Remove-Item / rm / del
        if (try_parse_ps_delete(line, out var deletePath))
        {
            return shell.delete_file(shell.str_const(deletePath));
        }

        // Test-Path
        if (try_parse_ps_test_path(line, out var testPath))
        {
            return shell.exists(shell.str_const(testPath));
        }

        // Get-Process
        if (line.StartsWith("Get-Process"))
        {
            var result = shell.exec(shell.str_const("powershell.exe"),
                [(object)$"-Command \"{line}\""]);
            Console.WriteLine(result);
            return result;
        }

        // exit
        if (line.StartsWith("exit"))
        {
            var code = 0;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out code);
            }

            Environment.Exit(code);
        }

        // switch 块
        if (line.StartsWith("switch ("))
        {
            return execute_ps_switch_block(line, shell, lines, ref i);
        }

        // while 循环
        if (line.StartsWith("while ("))
        {
            return execute_ps_while_block(line, shell, lines, ref i);
        }

        // function 定义
        if (line.StartsWith("function "))
        {
            return execute_ps_function_def(line, shell, lines, ref i);
        }

        // class 定义
        if (line.StartsWith("class "))
        {
            return execute_ps_class_def(line, shell, lines, ref i);
        }

        // enum 定义
        if (line.StartsWith("enum "))
        {
            return execute_ps_enum_def(line, shell, lines, ref i);
        }

        // begin 块
        if (line.TrimStart().StartsWith("begin {"))
        {
            return execute_ps_begin_block(shell, lines, ref i);
        }

        // process 块
        if (line.TrimStart().StartsWith("process {"))
        {
            return execute_ps_process_block(shell, lines, ref i);
        }

        // end 块
        if (line.TrimStart().StartsWith("end {"))
        {
            return execute_ps_end_block(shell, lines, ref i);
        }

        // -match / -replace 运算符
        if (line.Contains("-match ") || line.Contains("-replace "))
        {
            return execute_ps_match_replace(line, shell);
        }

        // try/catch 块
        if (line.StartsWith("try {"))
        {
            return execute_ps_try_block(shell, lines, ref i);
        }

        // if 块
        if (line.StartsWith("if ("))
        {
            return execute_ps_if_block(line, shell, lines, ref i);
        }

        // foreach 块
        if (line.StartsWith("foreach ("))
        {
            return execute_ps_foreach_block(line, shell, lines, ref i);
        }

        // 管道
        if (line.Contains('|'))
        {
            return execute_ps_pipeline(line, shell);
        }

        // 普通 cmdlet 调用
        return execute_ps_command(line, shell);
    }

    private static bool try_parse_ps_assignment(string line, out string name, out string value)
    {
        name = "";
        value = "";

        if (!line.StartsWith('$'))
        {
            return false;
        }

        var eq = line.IndexOf('=');
        if (eq <= 1 || eq == line.Length - 1)
        {
            return false;
        }

        name = line[1..eq].Trim();
        value = line[(eq + 1)..].Trim();
        value = strip_quotes(value);
        return !string.IsNullOrEmpty(name);
    }

    private static bool try_parse_ps_echo(string line, out string text)
    {
        text = "";
        if (line.StartsWith("Write-Output "))
        {
            text = line["Write-Output ".Length..].Trim();
        }
        else if (line.StartsWith("Write-Host "))
        {
            text = line["Write-Host ".Length..].Trim();
        }
        else if (line.StartsWith("echo "))
        {
            text = line["echo ".Length..].Trim();
        }
        else
        {
            return false;
        }

        text = strip_quotes(text);
        return true;
    }

    private static bool try_parse_ps_cd(string line, out string path)
    {
        path = "";
        if (line.StartsWith("Set-Location "))
        {
            path = line["Set-Location ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("cd "))
        {
            path = line["cd ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("sl "))
        {
            path = line["sl ".Length..].Trim();
            return true;
        }

        return false;
    }

    private static bool try_parse_ps_ls(string line, out string path)
    {
        path = "";
        if (line == "Get-ChildItem" || line == "ls" || line == "dir")
        {
            return true;
        }

        if (line.StartsWith("Get-ChildItem "))
        {
            path = line["Get-ChildItem ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("ls "))
        {
            path = line["ls ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("dir "))
        {
            path = line["dir ".Length..].Trim();
            return true;
        }

        return false;
    }

    private static bool try_parse_ps_read_file(string line, out string path)
    {
        path = "";
        if (line.StartsWith("Get-Content "))
        {
            path = line["Get-Content ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("cat "))
        {
            path = line["cat ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("type "))
        {
            path = line["type ".Length..].Trim();
            return true;
        }

        return false;
    }

    private static bool try_parse_ps_write_file(string line, out string path, out string content)
    {
        path = "";
        content = "";

        if (line.Contains("| Set-Content ") || line.Contains("| Out-File "))
        {
            var pipeIdx = line.IndexOf('|');
            content = line[..pipeIdx].Trim();
            var rest = line[(pipeIdx + 1)..].Trim();
            var spIdx = rest.IndexOf(' ');
            path = spIdx > 0 ? rest[(spIdx + 1)..].Trim() : "";
            return !string.IsNullOrEmpty(path);
        }

        return false;
    }

    private static bool try_parse_ps_delete(string line, out string path)
    {
        path = "";
        if (line.StartsWith("Remove-Item "))
        {
            path = line["Remove-Item ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("rm "))
        {
            path = line["rm ".Length..].Trim();
            return true;
        }

        if (line.StartsWith("del "))
        {
            path = line["del ".Length..].Trim();
            return true;
        }

        return false;
    }

    private static bool try_parse_ps_test_path(string line, out string path)
    {
        path = "";
        if (line.StartsWith("Test-Path "))
        {
            path = line["Test-Path ".Length..].Trim();
            return true;
        }

        return false;
    }

    private static object execute_ps_command(string line, ShellEvaluator shell)
    {
        line = expand_ps_vars(line);

        var result = shell.exec(shell.str_const("powershell.exe"),
            [(object)$"-Command \"{line}\""]);
        Console.Write(result);
        return result;
    }

    private static object execute_ps_pipeline(string line, ShellEvaluator shell)
    {
        line = expand_ps_vars(line);

        var result = shell.exec(shell.str_const("powershell.exe"),
            [(object)$"-Command \"{line}\""]);
        Console.WriteLine(result);
        return result;
    }

    private static object execute_ps_if_block(
        string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        var condEnd = line.IndexOf(") {", StringComparison.Ordinal);
        if (condEnd < 0)
        {
            return shell.unit();
        }

        var condition = line[4..(condEnd + 1)];

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        var elseLines = new List<string>();
        if (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith("else {"))
        {
            i += 2;
            while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
            {
                elseLines.Add(lines[i]);
                i++;
            }
        }

        var condResult = shell.exec(shell.str_const("powershell.exe"),
            [(object)$"-Command \"{condition}\""]);
        var condBool = !string.IsNullOrEmpty(condResult?.ToString())
                       && !condResult!.ToString()!.StartsWith("False");

        if (condBool)
        {
            return execute_ps_block(bodyLines, shell);
        }

        return execute_ps_block(elseLines, shell);
    }

    private static object execute_ps_foreach_block(
        string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        var varEnd = line.IndexOf(" in ", StringComparison.Ordinal);
        if (varEnd < 0)
        {
            return shell.unit();
        }

        var varName = line[9..varEnd].Trim().TrimStart('$');
        var rest = line[(varEnd + " in ".Length)..];
        var collectionEnd = rest.IndexOf(") {", StringComparison.Ordinal);
        if (collectionEnd < 0)
        {
            return shell.unit();
        }

        var collectionExpr = rest[..collectionEnd].Trim().TrimStart('$');
        var items = Environment.GetEnvironmentVariable(collectionExpr)?.Split(',', ' ') ?? [];

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        foreach (var item in items)
        {
            shell.set_env(shell.str_const(varName), shell.str_const(item.Trim()));
            execute_ps_block(bodyLines, shell);
        }

        return shell.unit();
    }

    private static object execute_ps_block(List<string> lines, ShellEvaluator shell)
    {
        var result = shell.unit();

        for (var j = 0; j < lines.Count; j++)
        {
            var line = lines[j].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line == "{" || line == "}")
            {
                continue;
            }

            result = execute_line(line, shell, [.. lines], ref j);
        }

        return result;
    }

    private static object execute_ps_switch_block(
        string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        var condEnd = line.IndexOf(") {", StringComparison.Ordinal);
        if (condEnd < 0)
        {
            return shell.unit();
        }

        var switchValue = line["switch (".Length..condEnd].Trim();
        switchValue = expand_ps_vars(switchValue);

        i++;
        var cases = new Dictionary<string, List<string>>();
        List<string>? currentCase = null;

        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            var currLine = lines[i].Trim();

            if (currLine.StartsWith("default {") || currLine.StartsWith("Default {"))
            {
                currentCase = [];
                cases["__default__"] = currentCase;
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
                {
                    currentCase.Add(lines[i]);
                    i++;
                }
            }
            else if (currLine == "{")
            {
            }
            else
            {
                var valueEnd = currLine.IndexOf(" {", StringComparison.Ordinal);
                if (valueEnd > 0)
                {
                    var caseValue = currLine[..valueEnd].Trim();
                    currentCase = [];
                    cases[caseValue] = currentCase;
                    i++;
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
                    {
                        currentCase.Add(lines[i]);
                        i++;
                    }
                }
            }

            i++;
        }

        foreach (var kv in cases)
        {
            if (kv.Key == "__default__")
            {
                return execute_ps_block(kv.Value, shell);
            }

            if (string.Equals(kv.Key, switchValue, StringComparison.OrdinalIgnoreCase))
            {
                return execute_ps_block(kv.Value, shell);
            }
        }

        return shell.unit();
    }

    private static object execute_ps_while_block(
        string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        var condEnd = line.IndexOf(") {", StringComparison.Ordinal);
        if (condEnd < 0)
        {
            return shell.unit();
        }

        var condition = line["while (".Length..(condEnd + 1)];

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        var loopCount = 0;
        var maxIterations = 10000;
        object result = shell.unit();

        while (loopCount < maxIterations)
        {
            var condResult = shell.exec(shell.str_const("powershell.exe"),
                [(object)$"-Command \"{condition}\""]);
            var condBool = !string.IsNullOrEmpty(condResult?.ToString())
                           && !condResult!.ToString()!.StartsWith("False");

            if (!condBool)
            {
                break;
            }

            result = execute_ps_block(bodyLines, shell);
            loopCount++;
        }

        if (loopCount >= maxIterations)
        {
            Console.WriteLine($"[PowerShell] 警告：while 循环达到最大迭代次数 {maxIterations}，已中断");
        }

        return result;
    }

    private static object execute_ps_function_def(
        string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        var header = line["function ".Length..].Trim();
        var spaceIdx = header.IndexOf(' ');
        if (spaceIdx < 0)
        {
            var parenIdx = header.IndexOf('(');
            if (parenIdx > 0)
            {
                spaceIdx = parenIdx;
            }
        }

        var funcName = spaceIdx > 0 ? header[..spaceIdx].Trim() : header.Trim();

        if (!line.Contains('{'))
        {
            i++;
            while (i < lines.Length && !lines[i].Trim().Contains('{'))
            {
                i++;
            }
        }

        var bodyLines = new List<string>();
        if (line.Contains('{') && !line.TrimEnd().EndsWith("{"))
        {
            var braceIdx = line.IndexOf('{');
            var afterBrace = line[(braceIdx + 1)..].TrimEnd('}').Trim();
            if (!string.IsNullOrEmpty(afterBrace))
            {
                bodyLines.Add(afterBrace);
            }

            if (!line.TrimEnd().EndsWith('}'))
            {
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
                {
                    bodyLines.Add(lines[i]);
                    i++;
                }
            }
        }
        else
        {
            i++;
            while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
            {
                bodyLines.Add(lines[i]);
                i++;
            }
        }

        shell.set_env(shell.str_const(funcName), shell.str_const(funcName));

        return shell.unit();
    }

    private static object execute_ps_try_block(
        ShellEvaluator shell, string[] lines, ref int i)
    {
        var tryLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            tryLines.Add(lines[i]);
            i++;
        }

        var catchLines = new List<string>();
        if (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith("catch"))
        {
            i++;
            if (lines[i].Contains('{'))
            {
                if (!lines[i].TrimEnd().EndsWith("{"))
                {
                    var braceIdx = lines[i].IndexOf('{');
                    var afterBrace = lines[i][(braceIdx + 1)..].TrimEnd('}').Trim();
                    if (!string.IsNullOrEmpty(afterBrace))
                    {
                        catchLines.Add(afterBrace);
                    }

                    if (!lines[i].TrimEnd().EndsWith('}'))
                    {
                        i++;
                        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
                        {
                            catchLines.Add(lines[i]);
                            i++;
                        }
                    }
                }
                else
                {
                    i++;
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
                    {
                        catchLines.Add(lines[i]);
                        i++;
                    }
                }
            }
        }

        try
        {
            return execute_ps_block(tryLines, shell);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PowerShell] catch 捕获异常：{ex.Message}");
            return execute_ps_block(catchLines, shell);
        }
    }

    private static object execute_ps_class_def(string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        var header = line["class ".Length..].Trim();

        var braceIdx = header.IndexOf('{');
        var colonIdx = header.IndexOf(':');

        string className;
        string? baseClass = null;

        if (braceIdx > 0)
        {
            if (colonIdx > 0 && colonIdx < braceIdx)
            {
                className = header[..colonIdx].Trim();
                baseClass = header[(colonIdx + 1)..braceIdx].Trim();
            }
            else
            {
                className = header[..braceIdx].Trim();
            }
        }
        else
        {
            className = header.Trim();
        }

        var bodyLines = new List<string>();
        if (!line.TrimEnd().EndsWith("{"))
        {
            i++;
            while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
            {
                bodyLines.Add(lines[i]);
                i++;
            }
        }
        else
        {
            i++;
            while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
            {
                bodyLines.Add(lines[i]);
                i++;
            }
        }

        var properties = new List<string>();
        var methods = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        List<string>? currentMethod = null;
        var currentMethodName = "";

        foreach (var bodyLine in bodyLines)
        {
            var trimmed = bodyLine.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.Contains('(') && trimmed.Contains(')') && trimmed.Contains('{'))
            {
                var parenOpen = trimmed.IndexOf('(');
                currentMethodName = trimmed[..parenOpen].Trim();

                var lastBracket = currentMethodName.LastIndexOf(']');
                if (lastBracket >= 0)
                {
                    currentMethodName = currentMethodName[(lastBracket + 1)..].Trim();
                }

                currentMethod = [];
                methods[currentMethodName] = currentMethod;
                continue;
            }

            if (trimmed == "}" && currentMethod != null)
            {
                currentMethod = null;
                currentMethodName = "";
                continue;
            }

            if (currentMethod != null)
            {
                currentMethod.Add(trimmed);
                continue;
            }

            if (trimmed.StartsWith('['))
            {
                var dollarIdx = trimmed.IndexOf('$');
                if (dollarIdx >= 0)
                {
                    var propName = trimmed[dollarIdx..].Trim();
                    properties.Add(propName);
                }
            }
        }

        var classInfo = new PsClassInfo(className, baseClass, properties, methods);
        _defined_classes[className] = classInfo;

        shell.set_env(shell.str_const(className), shell.str_const(className));

        Console.WriteLine($"[PowerShell] 定义类: {className}" +
                          (baseClass != null ? $" (继承自 {baseClass})" : "") +
                          $"，属性 {properties.Count} 个，方法 {methods.Count} 个");

        return shell.unit();
    }

    private static object execute_ps_enum_def(string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        var header = line["enum ".Length..].Trim();
        var braceIdx = header.IndexOf('{');
        var enumName = braceIdx > 0 ? header[..braceIdx].Trim() : header.Trim();

        var enumValues = new List<string>();

        if (!line.TrimEnd().EndsWith("{"))
        {
            i++;
        }

        while (i < lines.Length)
        {
            var currLine = lines[i].Trim();
            if (currLine.StartsWith('}'))
            {
                break;
            }

            if (!string.IsNullOrEmpty(currLine) && !currLine.StartsWith('#'))
            {
                var value = currLine.TrimEnd(',');
                enumValues.Add(value);
            }

            i++;
        }

        _defined_enums[enumName] = enumValues;

        shell.set_env(shell.str_const(enumName), shell.str_const(string.Join(",", enumValues)));

        Console.WriteLine($"[PowerShell] 定义枚举: {enumName}，值: {string.Join(", ", enumValues)}");

        return shell.unit();
    }

    private static object execute_ps_begin_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        Console.WriteLine("[PowerShell] 执行 begin 块");
        return execute_ps_block(bodyLines, shell);
    }

    private static object execute_ps_process_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        Console.WriteLine("[PowerShell] 执行 process 块");
        return execute_ps_block(bodyLines, shell);
    }

    private static object execute_ps_end_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        Console.WriteLine("[PowerShell] 执行 end 块");
        return execute_ps_block(bodyLines, shell);
    }

    private static object execute_ps_match_replace(string line, ShellEvaluator shell)
    {
        line = expand_ps_vars(line);

        var matchIdx = line.IndexOf("-match ", StringComparison.OrdinalIgnoreCase);
        if (matchIdx >= 0)
        {
            var left = line[..matchIdx].Trim();
            var right = line[(matchIdx + "-match ".Length)..].Trim();

            if (left.Contains('='))
            {
                var eqIdx = left.IndexOf('=');
                left = left[(eqIdx + 1)..].Trim();
            }

            left = strip_quotes(left);
            right = strip_quotes(right);

            try
            {
                var regex = new System.Text.RegularExpressions.Regex(right);
                var isMatch = regex.IsMatch(left);
                Console.WriteLine(isMatch);
                return isMatch;
            }
            catch (ArgumentException)
            {
                Console.WriteLine(false);
                return false;
            }
        }

        var replaceIdx = line.IndexOf("-replace ", StringComparison.OrdinalIgnoreCase);
        if (replaceIdx >= 0)
        {
            var left = line[..replaceIdx].Trim();
            var right = line[(replaceIdx + "-replace ".Length)..].Trim();

            if (left.Contains('='))
            {
                var eqIdx = left.IndexOf('=');
                left = left[(eqIdx + 1)..].Trim();
            }

            left = strip_quotes(left);

            string pattern;
            string replacement = "";
            var commaIdx = right.IndexOf(',');
            if (commaIdx >= 0)
            {
                pattern = strip_quotes(right[..commaIdx].Trim());
                replacement = strip_quotes(right[(commaIdx + 1)..].Trim());
            }
            else
            {
                pattern = strip_quotes(right);
            }

            try
            {
                var result = System.Text.RegularExpressions.Regex.Replace(left, pattern, replacement);
                Console.WriteLine(result);
                return result;
            }
            catch (ArgumentException)
            {
                Console.WriteLine(left);
                return left;
            }
        }

        return shell.unit();
    }

    private static string strip_quotes(string text)
    {
        if (text.Length >= 2)
        {
            if ((text[0] == '"' && text[^1] == '"') || (text[0] == '\'' && text[^1] == '\''))
            {
                return text[1..^1];
            }
        }

        return text;
    }

    private static string expand_ps_vars(string text)
    {
        var result = text;

        var idx = result.IndexOf("$env:", StringComparison.OrdinalIgnoreCase);
        while (idx >= 0)
        {
            var end = idx + "$env:".Length;
            while (end < result.Length && (char.IsLetterOrDigit(result[end]) || result[end] == '_'))
            {
                end++;
            }

            var varName = result[(idx + "$env:".Length)..end];
            var varValue = Environment.GetEnvironmentVariable(varName) ?? "";
            result = result[..idx] + varValue + result[end..];
            idx = result.IndexOf("$env:", idx + varValue.Length, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    #endregion
}

/// <summary>
///     PowerShell 类信息，记录类名、基类、属性和方法
/// </summary>
internal sealed class PsClassInfo
{
    /// <summary>
    ///     类名
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     基类名，可为 null
    /// </summary>
    public string? base_class { get; }

    /// <summary>
    ///     属性列表
    /// </summary>
    public List<string> properties { get; }

    /// <summary>
    ///     方法字典，键为方法名，值为方法体行列表
    /// </summary>
    public Dictionary<string, List<string>> methods { get; }

    /// <summary>
    ///     创建 PowerShell 类信息
    /// </summary>
    /// <param name="name">类名</param>
    /// <param name="baseClass">基类名</param>
    /// <param name="properties">属性列表</param>
    /// <param name="methods">方法字典</param>
    public PsClassInfo(string name, string? baseClass, List<string> properties,
        Dictionary<string, List<string>> methods)
    {
        this.name = name;
        base_class = baseClass;
        this.properties = properties;
        this.methods = methods;
    }
}