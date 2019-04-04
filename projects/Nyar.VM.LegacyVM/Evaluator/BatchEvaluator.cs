using Nyar.VM.LegacyVM.Algebra.Shell;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     Windows Batch 脚本求值器，将 .bat/.cmd 源码逐行解析并执行。
///     支持：SET、ECHO、CD、DIR、IF、FOR、GOTO、CALL、REM 等。
/// </summary>
public sealed class BatchEvaluator : IShellEvaluator
{
    /// <summary>
    ///     Shell 可执行文件名
    /// </summary>
    public string shell_name => "cmd.exe";

    /// <summary>
    ///     求值 Batch 脚本
    /// </summary>
    /// <param name="source">Batch 源码</param>
    /// <param name="env">运行环境</param>
    /// <returns>执行结果</returns>
    public object evaluate(string source, Dictionary<string, object> env)
    {
        var shell = new ShellEvaluator("cmd.exe", env);
        var lines = source.Split('\n');
        var result = new object();

        // 展开标签，用于 GOTO
        var labels = new Dictionary<string, int>();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith(':') && !line.StartsWith("::"))
            {
                var label = line.TrimStart(':').Trim();
                labels[label] = i;
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r', '\n').Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("REM ", StringComparison.OrdinalIgnoreCase)
                                           || line.StartsWith("::") || line == "REM")
            {
                continue;
            }

            result = execute_line(line, shell, lines, ref i, labels);
        }

        return result;
    }

    #region 行执行

    private static object execute_line(
        string line, ShellEvaluator shell, string[] lines, ref int i,
        Dictionary<string, int> labels)
    {
        var upper = line.ToUpperInvariant();

        if (line.StartsWith('@'))
        {
            line = line[1..].Trim();
            upper = line.ToUpperInvariant();
        }

        if (upper is "ECHO ON" or "ECHO OFF" or "@ECHO ON" or "@ECHO OFF")
        {
            return shell.unit();
        }

        if (upper == "ECHO." || upper == "ECHO")
        {
            Console.WriteLine();
            return "";
        }

        if (upper.StartsWith("ECHO "))
        {
            var text = line["ECHO ".Length..].Trim();
            Console.WriteLine(text);
            return text;
        }

        if (upper.StartsWith("SET "))
        {
            var rest = line["SET ".Length..].Trim();
            var eq = rest.IndexOf('=');
            if (eq > 0)
            {
                var name = rest[..eq].Trim();
                var value = rest[(eq + 1)..].Trim();
                return shell.set_env(shell.str_const(name), shell.str_const(value));
            }
        }

        if (upper.StartsWith("CD ") || upper.StartsWith("CHDIR "))
        {
            var path = line[(line.IndexOf(' ') + 1)..].Trim();
            return shell.cd(shell.str_const(path));
        }

        if (upper is "CD" or "CHDIR")
        {
            var dir = shell.get_cwd();
            Console.WriteLine(dir);
            return dir;
        }

        if (upper == "DIR" || upper.StartsWith("DIR "))
        {
            var dir = shell.get_cwd();
            var result = shell.list_dir(dir);
            Console.WriteLine(result);
            return result;
        }

        if (upper.StartsWith("GOTO "))
        {
            var label = line["GOTO ".Length..].Trim().TrimStart(':');
            if (labels.TryGetValue(label, out var target))
            {
                i = target;
            }

            return shell.unit();
        }

        if (upper.StartsWith("IF "))
        {
            return execute_batch_if(line, shell, lines, ref i, labels);
        }

        if (upper.StartsWith("FOR "))
        {
            return execute_batch_for(line, shell);
        }

        if (upper.StartsWith("CALL "))
        {
            var subCmd = line["CALL ".Length..].Trim();
            return execute_command(subCmd, shell);
        }

        if (upper.StartsWith("EXIT"))
        {
            var codeStr = line.Length > 4 ? line["EXIT".Length..].Trim().TrimStart("/B ") : "0";
            if (int.TryParse(codeStr.Trim(), out var code))
            {
                Environment.Exit(code);
            }

            return shell.unit();
        }

        if (upper == "PAUSE")
        {
            Console.WriteLine("按任意键继续...");
            Console.ReadKey(true);
            return shell.unit();
        }

        if (line.StartsWith(':'))
        {
            return shell.unit();
        }

        return execute_command(line, shell);
    }

    private static object execute_batch_if(
        string line, ShellEvaluator shell, string[] lines, ref int i,
        Dictionary<string, int> labels)
    {
        var upper = line.ToUpperInvariant();
        if (upper.Contains(" EXIST "))
        {
            var existIdx = upper.IndexOf(" EXIST ", StringComparison.Ordinal);
            var filePart = line[(existIdx + " EXIST ".Length)..].Trim();
            var spaceIdx = filePart.IndexOf(' ');
            var file = spaceIdx > 0 ? filePart[..spaceIdx] : filePart;
            var cmd = spaceIdx > 0 ? filePart[(spaceIdx + 1)..].Trim() : "";

            var exists = shell.exists(shell.str_const(file));
            if (exists is bool b && b)
            {
                return execute_line(cmd, shell, lines, ref i, labels);
            }
        }

        var eqIdx = upper.IndexOf("==", StringComparison.Ordinal);
        if (eqIdx > 0)
        {
            var left = line["IF ".Length..eqIdx].Trim();
            var rightStart = eqIdx + 2;

            var spaceAfterRight = line.IndexOf(' ', rightStart);
            var right = spaceAfterRight > 0 ? line[rightStart..spaceAfterRight] : line[rightStart..];
            var cmd = spaceAfterRight > 0 ? line[(spaceAfterRight + 1)..].Trim() : "";

            left = expand_batch_vars(left);
            right = right.Trim();

            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            {
                return execute_line(cmd, shell, lines, ref i, labels);
            }
        }

        return shell.unit();
    }

    private static object execute_batch_for(string line, ShellEvaluator shell)
    {
        var result = shell.exec(shell.str_const("cmd.exe"), [(object)$"/c {line}"]);
        Console.WriteLine(result);
        return result;
    }

    private static object execute_command(string line, ShellEvaluator shell)
    {
        var parts = parse_batch_args(line);
        if (parts.Length == 0)
        {
            return shell.unit();
        }

        var cmd = expand_batch_vars(parts[0]);
        var args = parts.Skip(1).Select(expand_batch_vars).Select(a => (object)a).ToArray();
        var result = shell.exec(shell.str_const(cmd), args);
        Console.Write(result);
        return result;
    }

    private static string[] parse_batch_args(string line)
    {
        var args = new List<string>();
        var current = "";
        var inQuote = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuote = !inQuote;
            }
            else if (c == ' ' && !inQuote)
            {
                if (current.Length > 0)
                {
                    args.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }

        if (current.Length > 0)
        {
            args.Add(current);
        }

        return [.. args];
    }

    private static string expand_batch_vars(string text)
    {
        if (text.StartsWith('%') && text.EndsWith('%') && text.Length > 2)
        {
            var varName = text[1..^1];
            return Environment.GetEnvironmentVariable(varName) ?? text;
        }

        return text;
    }

    #endregion
}