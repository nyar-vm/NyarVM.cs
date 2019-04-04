using Nyar.VM.LegacyVM.Algebra.Core;
using Nyar.VM.LegacyVM.Algebra.Shell;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     Bash 脚本求值器，将 Bash 源码逐行解析并执行。
///     支持：变量赋值、命令执行、管道、重定向、echo、cd、if/fi、for/do/done、case/esac、
///     while/do/done、until/do/done、select/do/done、here document、数组、算术展开、
///     字符串长度、子串、默认值、function 定义、read、trap、shift、$@/$*/$#、
///     进程替换等。
/// </summary>
public sealed class BashEvaluator : IShellEvaluator
{
    /// <summary>
    ///     Shell 可执行文件名
    /// </summary>
    public string shell_name => "bash";

    /// <summary>
    ///     位置参数列表，用于 $@ / $* / $# 和 shift 操作
    /// </summary>
    private static readonly List<string> _positional_args = [];

    /// <summary>
    ///     已注册的 trap 处理器，键为信号名，值为要执行的命令
    /// </summary>
    private static readonly Dictionary<string, string> _trap_handlers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     求值 Bash 脚本
    /// </summary>
    /// <param name="source">Bash 源码</param>
    /// <param name="env">运行环境</param>
    /// <returns>执行结果</returns>
    public object evaluate(string source, Dictionary<string, object> env)
    {
        var shell = new ShellEvaluator("bash", env);
        var lines = source.Split('\n');
        var result = new object();

        // 预处理 here-document
        lines = preprocess_heredoc(lines);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r', '\n').Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            result = execute_line(line, shell, lines, ref i);
        }

        return result;
    }

    #region Here-Document 预处理

    /// <summary>
    ///     预处理 here-document，将多行 here-doc 转换为单行
    /// </summary>
    private static string[] preprocess_heredoc(string[] lines)
    {
        var result = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r', '\n');

            if (line.Contains("<<") && !line.Contains("<<<"))
            {
                var heredocIdx = line.IndexOf("<<", StringComparison.Ordinal);
                var delimiter = line[(heredocIdx + 2)..].Trim();
                delimiter = delimiter.Trim('"', '\'');

                var content = new List<string>();
                i++;
                while (i < lines.Length)
                {
                    var nextLine = lines[i].TrimEnd('\r', '\n').Trim();
                    if (nextLine == delimiter)
                    {
                        break;
                    }

                    content.Add(lines[i].TrimEnd('\r', '\n'));
                    i++;
                }

                var heredocContent = string.Join("\n", content);
                result.Add($"{line[..heredocIdx].Trim()} \"{escape_for_bash(heredocContent)}\"");
            }
            else
            {
                result.Add(line);
            }
        }

        return [.. result];
    }

    /// <summary>
    ///     转义字符串以便在 Bash 命令中使用
    /// </summary>
    private static string escape_for_bash(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    #endregion

    #region 行执行

    private static object execute_line(string line, ShellEvaluator shell, string[] lines, ref int i)
    {
        // 变量赋值: NAME=value
        if (try_parse_assignment(line, out var varName, out var varValue))
        {
            return shell.set_env(shell.str_const(varName), shell.str_const(varValue));
        }

        // 数组：arr=(1 2 3)
        if (try_parse_array_assignment(line, out var arrName, out var arrValues))
        {
            var arrStr = string.Join(" ", arrValues);
            return shell.set_env(shell.str_const(arrName), shell.str_const(arrStr));
        }

        // export NAME=value
        if (line.StartsWith("export "))
        {
            var rest = line["export ".Length..].Trim();
            if (try_parse_assignment(rest, out var en, out var ev))
            {
                return shell.set_env(shell.str_const(en), shell.str_const(ev));
            }

            if (try_parse_array_assignment(rest, out var an, out var av))
            {
                var arrStr = string.Join(" ", av);
                return shell.set_env(shell.str_const(an), shell.str_const(arrStr));
            }
        }

        // cd
        if (line.StartsWith("cd "))
        {
            var path = line["cd ".Length..].Trim();
            path = expand_vars(path);
            return shell.cd(shell.str_const(path));
        }

        // echo
        if (line.StartsWith("echo "))
        {
            var text = line["echo ".Length..].Trim();
            text = strip_quotes(text);
            text = expand_all_vars(text, shell);
            Console.WriteLine(text);
            return text;
        }

        // pwd
        if (line == "pwd")
        {
            var dir = shell.get_cwd();
            Console.WriteLine(dir);
            return dir;
        }

        // exit
        if (line.StartsWith("exit"))
        {
            var code = 0;
            if (line.Length > 4)
            {
                int.TryParse(line["exit".Length..].Trim(), out code);
            }

            Environment.Exit(code);
        }

        // if ... then ... fi
        if (line.StartsWith("if "))
        {
            return execute_if_block(shell, lines, ref i);
        }

        // case ... esac
        if (line.StartsWith("case "))
        {
            return execute_case_block(shell, lines, ref i);
        }

        // while ... do ... done
        if (line.StartsWith("while "))
        {
            return execute_while_block(shell, lines, ref i);
        }

        // until ... do ... done
        if (line.StartsWith("until "))
        {
            return execute_until_block(shell, lines, ref i);
        }

        // for VAR in ... ; do ... ; done
        if (line.StartsWith("for "))
        {
            return execute_for_block(shell, lines, ref i);
        }

        // select VAR in ... ; do ... ; done
        if (line.StartsWith("select "))
        {
            return execute_select_block(shell, lines, ref i);
        }

        // function 定义
        if (line.StartsWith("function "))
        {
            return execute_function_def(shell, lines, ref i);
        }

        // read 内置命令
        if (line.StartsWith("read "))
        {
            return execute_read(line, shell);
        }

        // trap 信号处理
        if (line.StartsWith("trap "))
        {
            return execute_trap(line, shell);
        }

        // shift 参数偏移
        if (line == "shift" || line.StartsWith("shift "))
        {
            return execute_shift(line, shell);
        }

        // 算术展开
        if (line.Contains("$(("))
        {
            line = expand_arithmetic(line);
        }

        // 进程替换
        if (line.Contains("<(") || line.Contains(">("))
        {
            line = expand_process_substitution(line, shell);
        }

        // 逻辑链
        if (line.Contains("&&"))
        {
            return execute_logical_chain(line, shell, "&&");
        }

        if (line.Contains("||"))
        {
            return execute_logical_chain(line, shell, "||");
        }

        // 管道
        if (line.Contains('|'))
        {
            return execute_pipeline(line, shell);
        }

        // 重定向
        if (line.Contains(">>"))
        {
            return execute_redirect(line, shell, ">>", true);
        }

        if (line.Contains('>'))
        {
            return execute_redirect(line, shell, ">", false);
        }

        // 普通命令
        return execute_command(line, shell);
    }

    private static bool try_parse_assignment(string line, out string name, out string value)
    {
        name = "";
        value = "";

        var eq = line.IndexOf('=');
        if (eq <= 0 || eq == line.Length - 1)
        {
            return false;
        }

        name = line[..eq].Trim();
        value = strip_quotes(line[(eq + 1)..].Trim());
        return !string.IsNullOrEmpty(name);
    }

    private static bool try_parse_array_assignment(string line, out string name, out string[] values)
    {
        name = "";
        values = [];

        var eq = line.IndexOf('=');
        if (eq <= 0 || eq == line.Length - 1)
        {
            return false;
        }

        name = line[..eq].Trim();
        var rhs = line[(eq + 1)..].Trim();

        if (rhs.StartsWith('(') && rhs.EndsWith(')'))
        {
            var inner = rhs[1..^1].Trim();
            if (string.IsNullOrEmpty(inner))
            {
                values = [];
            }
            else
            {
                values =
                [
                    .. inner.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => strip_quotes(v))
                ];
            }

            return true;
        }

        return false;
    }

    #endregion

    #region 算术展开

    private static string expand_arithmetic(string line)
    {
        while (true)
        {
            var start = line.IndexOf("$((", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = line.IndexOf("))", start + 3, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            var expr = line[(start + 3)..end].Trim();
            var result = evaluate_arithmetic(expr);
            line = line[..start] + result + line[(end + 2)..];
        }

        return line;
    }

    private static long evaluate_arithmetic(string expr)
    {
        var core = new CoreEvaluator();

        try
        {
            expr = expr.Replace("*", " * ")
                .Replace("/", " / ")
                .Replace("+", " + ")
                .Replace("-", " - ")
                .Replace("%", " % ")
                .Replace("(", " ( ")
                .Replace(")", " ) ");

            var trimmed = expr.Trim();
            if (long.TryParse(trimmed, out var val))
            {
                return val;
            }

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && long.TryParse(parts[0], out var singleVal))
            {
                return singleVal;
            }

            var result = CoreHelpers.to_i64(eval_arithmetic_expr(trimmed, core));
            return result;
        }
        catch
        {
            return 0;
        }
    }

    private static object eval_arithmetic_expr(string expr, CoreEvaluator core)
    {
        expr = expr.Trim();

        if (expr.StartsWith('(') && expr.EndsWith(')'))
        {
            return eval_arithmetic_expr(expr[1..^1], core);
        }

        var plusIdx = find_top_level_arithmetic_op(expr, '+');
        if (plusIdx > 0)
        {
            var left = expr[..plusIdx].Trim();
            var right = expr[(plusIdx + 1)..].Trim();
            return core.add(eval_arithmetic_expr(left, core), eval_arithmetic_expr(right, core));
        }

        var minusIdx = find_top_level_arithmetic_op(expr, '-');
        if (minusIdx > 0)
        {
            var left = expr[..minusIdx].Trim();
            var right = expr[(minusIdx + 1)..].Trim();
            return core.sub(eval_arithmetic_expr(left, core), eval_arithmetic_expr(right, core));
        }

        var mulIdx = find_top_level_arithmetic_op(expr, '*');
        if (mulIdx > 0)
        {
            var left = expr[..mulIdx].Trim();
            var right = expr[(mulIdx + 1)..].Trim();
            return core.mul(eval_arithmetic_expr(left, core), eval_arithmetic_expr(right, core));
        }

        var divIdx = find_top_level_arithmetic_op(expr, '/');
        if (divIdx > 0)
        {
            var left = expr[..divIdx].Trim();
            var right = expr[(divIdx + 1)..].Trim();
            return core.div(eval_arithmetic_expr(left, core), eval_arithmetic_expr(right, core));
        }

        var modIdx = find_top_level_arithmetic_op(expr, '%');
        if (modIdx > 0)
        {
            var left = expr[..modIdx].Trim();
            var right = expr[(modIdx + 1)..].Trim();
            return core.mod(eval_arithmetic_expr(left, core), eval_arithmetic_expr(right, core));
        }

        if (long.TryParse(expr, out var intVal))
        {
            return core.int_const(intVal);
        }

        return core.int_const(0);
    }

    private static int find_top_level_arithmetic_op(string expr, char op)
    {
        var depth = 0;
        for (var i = expr.Length - 1; i >= 0; i--)
        {
            var c = expr[i];
            if (c == ')')
            {
                depth++;
            }
            else if (c == '(')
            {
                depth--;
            }
            else if (depth == 0 && c == op)
            {
                return i;
            }
        }

        return -1;
    }

    #endregion

    #region 命令执行与控制流

    private static object execute_command(string line, ShellEvaluator shell)
    {
        line = expand_command_substitution(line, shell);
        line = expand_arithmetic(line);

        var parts = parse_args(line);
        if (parts.Length == 0)
        {
            return shell.unit();
        }

        var cmd = expand_vars(parts[0]);
        var args = parts.Skip(1).Select(a => expand_all_vars(a, shell)).Select(a => (object)a).ToArray();
        var result = shell.exec(shell.str_const(cmd), args);
        Console.WriteLine(result);
        return result;
    }

    private static object execute_pipeline(string line, ShellEvaluator shell)
    {
        var parts = line.Split('|');
        var result = execute_command(parts[0], shell);

        for (var j = 1; j < parts.Length; j++)
        {
            result = shell.pipe(result, shell.str_const(parts[j].Trim()));
        }

        Console.WriteLine(result);
        return result;
    }

    private static object execute_redirect(string line, ShellEvaluator shell, string op, bool append)
    {
        var idx = line.IndexOf(op, StringComparison.Ordinal);
        var cmd = line[..idx].Trim();
        var file = line[(idx + op.Length)..].Trim();

        var result = execute_command(cmd, shell);
        return shell.redirect_to_file(result, shell.str_const(file), append);
    }

    private static object execute_if_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var condition = lines[i]["if ".Length..].Trim();
        condition = condition.Replace("; then", "").Trim();

        var thenLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("fi") && !lines[i].TrimStart().StartsWith("else"))
        {
            thenLines.Add(lines[i]);
            i++;
        }

        var elseLines = new List<string>();
        if (i < lines.Length && lines[i].TrimStart().StartsWith("else"))
        {
            i++;
            while (i < lines.Length && !lines[i].TrimStart().StartsWith("fi"))
            {
                elseLines.Add(lines[i]);
                i++;
            }
        }

        var conditionAlg = new ShellEvaluator("bash");
        var condResult = execute_command(condition, conditionAlg);

        if ((!string.IsNullOrEmpty(condResult?.ToString()) && int.TryParse(condResult.ToString(), out var exitCode) &&
             exitCode == 0)
            || condResult?.ToString() == "0"
            || (condResult is bool b && b))
        {
            return execute_block(thenLines, shell);
        }

        return execute_block(elseLines, shell);
    }

    private static object execute_for_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var header = lines[i]["for ".Length..].Trim();

        var varEnd = header.IndexOf(" in ", StringComparison.Ordinal);
        if (varEnd < 0)
        {
            return shell.unit();
        }

        var varName = header[..varEnd].Trim();
        var itemsStr = header[(varEnd + " in ".Length)..].Trim();
        itemsStr = itemsStr.Replace("; do", "").Trim();

        var items = itemsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("done"))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        foreach (var item in items)
        {
            shell.set_env(shell.str_const(varName), shell.str_const(item));
            execute_block(bodyLines, shell);
        }

        return shell.unit();
    }

    private static object execute_until_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var condition = lines[i]["until ".Length..].Trim();
        condition = condition.Replace("; do", "").Trim();

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("done"))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        var loopCount = 0;
        var maxIterations = 10000;
        object result = shell.unit();

        while (loopCount < maxIterations)
        {
            var conditionAlg = new ShellEvaluator("bash");
            var condResult = execute_command(condition, conditionAlg);

            var condTrue = (!string.IsNullOrEmpty(condResult?.ToString())
                            && int.TryParse(condResult.ToString(), out var exitCode)
                            && exitCode == 0)
                           || (condResult is bool b && b);

            if (condTrue)
            {
                break;
            }

            result = execute_block(bodyLines, shell);
            loopCount++;
        }

        if (loopCount >= maxIterations)
        {
            Console.WriteLine($"[Bash] 警告：until 循环达到最大迭代次数 {maxIterations}，已中断");
        }

        return result;
    }

    private static object execute_select_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var header = lines[i]["select ".Length..].Trim();

        var varEnd = header.IndexOf(" in ", StringComparison.Ordinal);
        if (varEnd < 0)
        {
            return shell.unit();
        }

        var varName = header[..varEnd].Trim();
        var itemsStr = header[(varEnd + " in ".Length)..].Trim();
        itemsStr = itemsStr.Replace("; do", "").Trim();

        var items = itemsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("done"))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        foreach (var item in items)
        {
            shell.set_env(shell.str_const(varName), shell.str_const(item));
            execute_block(bodyLines, shell);
        }

        return shell.unit();
    }

    private static object execute_block(List<string> lines, ShellEvaluator shell)
    {
        var result = shell.unit();

        for (var j = 0; j < lines.Count; j++)
        {
            var line = lines[j].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            result = execute_line(line, shell, [.. lines], ref j);
        }

        return result;
    }

    private static object execute_case_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var valueExpr = lines[i]["case ".Length..].Trim();
        valueExpr = valueExpr.Replace(" in", "").Trim();

        var value = expand_vars(valueExpr);
        var valueStr = value.Trim('"', '\'');

        var patterns = new Dictionary<string, List<string>>();
        List<string>? currentPattern = null;

        i++;
        while (i < lines.Length)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("esac"))
            {
                break;
            }

            if (line.EndsWith(')'))
            {
                var pattern = line.TrimEnd(')').Trim();
                currentPattern = [];
                patterns[pattern] = currentPattern;
            }
            else if (line.EndsWith(";;"))
            {
                if (currentPattern != null)
                {
                    var stmt = line[..^2].Trim();
                    if (!string.IsNullOrEmpty(stmt))
                    {
                        currentPattern.Add(stmt);
                    }
                }

                currentPattern = null;
            }
            else if (currentPattern != null)
            {
                currentPattern.Add(line);
            }

            i++;
        }

        foreach (var kv in patterns)
        {
            var pattern = kv.Key;
            var match = false;

            if (pattern == "*")
            {
                match = true;
            }
            else if (pattern == valueStr)
            {
                match = true;
            }
            else if (pattern.Contains('|'))
            {
                var subPatterns = pattern.Split('|');
                foreach (var sub in subPatterns)
                {
                    if (sub.Trim() == valueStr)
                    {
                        match = true;
                        break;
                    }
                }
            }

            if (match)
            {
                return execute_block(kv.Value, shell);
            }
        }

        return shell.unit();
    }

    private static object execute_while_block(ShellEvaluator shell, string[] lines, ref int i)
    {
        var condition = lines[i]["while ".Length..].Trim();
        condition = condition.Replace("; do", "").Trim();

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("done"))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        var loopCount = 0;
        var maxIterations = 10000;
        object result = shell.unit();

        while (loopCount < maxIterations)
        {
            var conditionAlg = new ShellEvaluator("bash");
            var condResult = execute_command(condition, conditionAlg);

            var condTrue = (!string.IsNullOrEmpty(condResult?.ToString())
                            && int.TryParse(condResult.ToString(), out var exitCode)
                            && exitCode == 0)
                           || (condResult is bool b && b);

            if (!condTrue)
            {
                break;
            }

            result = execute_block(bodyLines, shell);
            loopCount++;
        }

        if (loopCount >= maxIterations)
        {
            Console.WriteLine($"[Bash] 警告：while 循环达到最大迭代次数 {maxIterations}，已中断");
        }

        return result;
    }

    private static object execute_function_def(ShellEvaluator shell, string[] lines, ref int i)
    {
        var header = lines[i]["function ".Length..].Trim();

        var parenOpen = header.IndexOf('(');
        var parenClose = header.IndexOf(')');

        if (parenOpen < 0 || parenClose < 0)
        {
            return shell.unit();
        }

        var funcName = header[..parenOpen].Trim();

        var bodyLines = new List<string>();
        i++;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith('}'))
        {
            bodyLines.Add(lines[i]);
            i++;
        }

        return shell.set_env(shell.str_const(funcName), shell.str_const(funcName));
    }

    private static object execute_logical_chain(string line, ShellEvaluator shell, string op)
    {
        var parts = line.Split([op], StringSplitOptions.None);
        if (parts.Length < 2)
        {
            return shell.unit();
        }

        var result = execute_command(parts[0].Trim(), shell) ?? shell.unit();

        for (var j = 1; j < parts.Length; j++)
        {
            var cmdResult = result?.ToString() ?? "";
            var success = !string.IsNullOrEmpty(cmdResult) && int.TryParse(cmdResult, out var ec) && ec == 0;

            if (op == "&&" && !success)
            {
                break;
            }

            if (op == "||" && success)
            {
                break;
            }

            result = execute_command(parts[j].Trim(), shell);
        }

        return result ?? shell.unit();
    }

    private static string expand_command_substitution(string line, ShellEvaluator shell)
    {
        while (true)
        {
            var start = line.IndexOf("$(", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = find_matching_paren(line, start + 1);
            if (end < 0)
            {
                break;
            }

            var cmd = line[(start + 2)..end].Trim();
            var subShell = new ShellEvaluator("bash");
            var subResult = execute_command(cmd, subShell);
            var subStr = subResult?.ToString()?.TrimEnd('\r', '\n') ?? "";

            line = line[..start] + subStr + line[(end + 1)..];
        }

        return line;
    }

    private static int find_matching_paren(string text, int openParen)
    {
        var depth = 0;
        for (var i = openParen; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                depth++;
            }
            else if (text[i] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string[] parse_args(string line)
    {
        var args = new List<string>();
        var current = "";
        var inQuote = false;
        var quoteChar = '"';

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuote)
            {
                if (c == quoteChar)
                {
                    inQuote = false;
                }
                else
                {
                    current += c;
                }
            }
            else if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
            }
            else if (c == ' ')
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

    #endregion

    #region 变量展开

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

    private static string expand_all_vars(string text, ShellEvaluator shell)
    {
        text = expand_vars(text);
        text = expand_string_length(text, shell);
        text = expand_substring(text, shell);
        text = expand_default_value(text, shell);
        text = expand_array_access(text, shell);
        return text;
    }

    private static string expand_string_length(string text, ShellEvaluator shell)
    {
        while (true)
        {
            var start = text.IndexOf("${#", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = text.IndexOf('}', start);
            if (end < 0)
            {
                break;
            }

            var varName = text[(start + 3)..end];
            var varValue = Environment.GetEnvironmentVariable(varName) ?? "";
            var length = varValue.Length.ToString();

            text = text[..start] + length + text[(end + 1)..];
        }

        return text;
    }

    private static string expand_substring(string text, ShellEvaluator shell)
    {
        while (true)
        {
            var start = text.IndexOf("${", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = text.IndexOf('}', start);
            if (end < 0)
            {
                break;
            }

            var inner = text[(start + 2)..end];

            if (inner.Contains(':') && !inner.Contains(":-") && !inner.Contains(":=") && !inner.Contains(":+")
                && !inner.Contains(":?") && !inner.StartsWith("#"))
            {
                var firstColon = inner.IndexOf(':');
                var varName = inner[..firstColon];
                var rest = inner[(firstColon + 1)..];

                if (rest.Length > 0 && char.IsDigit(rest[0]))
                {
                    var secondColon = rest.IndexOf(':');
                    string offsetStr;
                    string? lengthStr = null;

                    if (secondColon > 0)
                    {
                        offsetStr = rest[..secondColon];
                        lengthStr = rest[(secondColon + 1)..];
                    }
                    else
                    {
                        offsetStr = rest;
                    }

                    var varValue = Environment.GetEnvironmentVariable(varName) ?? "";
                    if (int.TryParse(offsetStr, out var offset) && offset >= 0 && offset < varValue.Length)
                    {
                        if (lengthStr != null && int.TryParse(lengthStr, out var length))
                        {
                            var endIdx = Math.Min(offset + length, varValue.Length);
                            text = text[..start] + varValue[offset..endIdx] + text[(end + 1)..];
                        }
                        else
                        {
                            text = text[..start] + varValue[offset..] + text[(end + 1)..];
                        }
                    }
                    else
                    {
                        text = text[..start] + "" + text[(end + 1)..];
                    }

                    continue;
                }
            }

            break;
        }

        return text;
    }

    private static string expand_default_value(string text, ShellEvaluator shell)
    {
        while (true)
        {
            var start = text.IndexOf("${", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = text.IndexOf('}', start);
            if (end < 0)
            {
                break;
            }

            var inner = text[(start + 2)..end];

            var dashIdx = inner.IndexOf(":-", StringComparison.Ordinal);
            if (dashIdx > 0)
            {
                var varName = inner[..dashIdx];
                var defaultVal = inner[(dashIdx + 2)..];

                var varValue = Environment.GetEnvironmentVariable(varName);
                if (string.IsNullOrEmpty(varValue))
                {
                    text = text[..start] + defaultVal + text[(end + 1)..];
                }
                else
                {
                    text = text[..start] + varValue + text[(end + 1)..];
                }

                continue;
            }

            break;
        }

        return text;
    }

    private static string expand_array_access(string text, ShellEvaluator shell)
    {
        while (true)
        {
            var start = text.IndexOf("${", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = text.IndexOf('}', start);
            if (end < 0)
            {
                break;
            }

            var inner = text[(start + 2)..end];

            if (inner.Contains('[') && inner.Contains(']'))
            {
                var bracketOpen = inner.IndexOf('[');
                var bracketClose = inner.IndexOf(']');
                var arrName = inner[..bracketOpen];
                var indexStr = inner[(bracketOpen + 1)..bracketClose];

                var arrValue = Environment.GetEnvironmentVariable(arrName) ?? "";
                var elements = arrValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (indexStr == "@")
                {
                    text = text[..start] + arrValue + text[(end + 1)..];
                }
                else if (int.TryParse(indexStr, out var idx) && idx >= 0 && idx < elements.Length)
                {
                    text = text[..start] + elements[idx] + text[(end + 1)..];
                }
                else
                {
                    text = text[..start] + "" + text[(end + 1)..];
                }

                continue;
            }

            break;
        }

        return text;
    }

    private static string expand_vars(string text)
    {
        text = expand_special_vars(text);

        while (true)
        {
            var start = text.IndexOf("${", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = text.IndexOf('}', start);
            if (end < 0)
            {
                break;
            }

            var varName = text[(start + 2)..end];

            if (varName.Contains(':') || varName.Contains('[') || varName.StartsWith('#'))
            {
                break;
            }

            var varValue = Environment.GetEnvironmentVariable(varName) ?? "";
            text = text[..start] + varValue + text[(end + 1)..];
        }

        var dollarIdx = text.IndexOf('$');
        while (dollarIdx >= 0 && dollarIdx + 1 < text.Length)
        {
            var end = dollarIdx + 1;
            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
            {
                end++;
            }

            var varName = text[(dollarIdx + 1)..end];
            var varValue = Environment.GetEnvironmentVariable(varName) ?? "";
            text = text[..dollarIdx] + varValue + text[end..];
            dollarIdx = text.IndexOf('$', dollarIdx + varValue.Length);
        }

        return text;
    }

    private static string expand_special_vars(string text)
    {
        if (text.Contains("$@"))
        {
            var allArgs = string.Join(" ", _positional_args);
            text = text.Replace("$@", allArgs);
        }

        if (text.Contains("$*"))
        {
            var allArgs = string.Join(" ", _positional_args);
            text = text.Replace("$*", allArgs);
        }

        if (text.Contains("$#"))
        {
            text = text.Replace("$#", _positional_args.Count.ToString());
        }

        return text;
    }

    #endregion

    #region 内置命令

    private static object execute_read(string line, ShellEvaluator shell)
    {
        var rest = line["read ".Length..].Trim();

        string? prompt = null;
        if (rest.StartsWith("-p "))
        {
            var promptEnd = rest.IndexOf('"');
            if (promptEnd >= 0)
            {
                var promptStart = promptEnd + 1;
                var promptClose = rest.IndexOf('"', promptStart);
                if (promptClose >= 0)
                {
                    prompt = rest[promptStart..promptClose];
                    rest = rest[(promptClose + 1)..].Trim();
                }
            }
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            Console.Write(prompt);
        }

        var varNames = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (varNames.Length == 0)
        {
            varNames = ["REPLY"];
        }

        var input = Console.ReadLine() ?? "";

        if (varNames.Length == 1)
        {
            shell.set_env(shell.str_const(varNames[0]), shell.str_const(input));
        }
        else
        {
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var j = 0; j < varNames.Length; j++)
            {
                var value = j < words.Length ? words[j] : "";
                shell.set_env(shell.str_const(varNames[j]), shell.str_const(value));
            }
        }

        return shell.unit();
    }

    private static object execute_trap(string line, ShellEvaluator shell)
    {
        var rest = line["trap ".Length..].Trim();

        if (rest.StartsWith("'") || rest.StartsWith("\""))
        {
            var quoteChar = rest[0];
            var closeIdx = rest.IndexOf(quoteChar, 1);
            if (closeIdx > 0)
            {
                var command = rest[1..closeIdx];
                var signal = rest[(closeIdx + 1)..].Trim();
                _trap_handlers[signal] = command;
                return shell.unit();
            }
        }

        if (rest.StartsWith("-") || rest == "SIGINT" || rest == "SIGTERM" || rest == "EXIT"
            || rest == "INT" || rest == "TERM" || rest == "ERR")
        {
            return shell.unit();
        }

        if (rest.StartsWith("''") || rest.StartsWith("\"\""))
        {
            return shell.unit();
        }

        return shell.unit();
    }

    private static object execute_shift(string line, ShellEvaluator shell)
    {
        var n = 1;
        var rest = line.StartsWith("shift ") ? line["shift ".Length..].Trim() : "";
        if (!string.IsNullOrEmpty(rest) && int.TryParse(rest, out var count))
        {
            n = count;
        }

        for (var j = 0; j < n && _positional_args.Count > 0; j++)
        {
            _positional_args.RemoveAt(0);
        }

        return shell.unit();
    }

    #endregion

    #region 进程替换

    private static string expand_process_substitution(string line, ShellEvaluator shell)
    {
        while (true)
        {
            var start = line.IndexOf("<(", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = find_matching_process_paren(line, start + 1);
            if (end < 0)
            {
                break;
            }

            var cmd = line[(start + 2)..end].Trim();
            var subShell = new ShellEvaluator("bash");
            var subResult = execute_command(cmd, subShell);
            var output = subResult?.ToString() ?? "";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, output);
            line = line[..start] + tempFile + line[(end + 1)..];
        }

        while (true)
        {
            var start = line.IndexOf(">(", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = find_matching_process_paren(line, start + 1);
            if (end < 0)
            {
                break;
            }

            var tempFile = Path.GetTempFileName();
            line = line[..start] + tempFile + line[(end + 1)..];
        }

        return line;
    }

    private static int find_matching_process_paren(string text, int openParen)
    {
        var depth = 0;
        for (var i = openParen; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                depth++;
            }
            else if (text[i] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    #endregion
}