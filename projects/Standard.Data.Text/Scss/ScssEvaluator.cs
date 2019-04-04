using System.Text;

namespace Std.Data.Text.Scss;

/// <summary>
///     SCSS 表达式求值器
/// </summary>
public sealed class ScssEvaluator
{
    private readonly ScssVariableScope _scope;

    public ScssEvaluator(ScssVariableScope scope)
    {
        _scope = scope;
    }


    /// <summary>
    ///     求值表达式
    /// </summary>
    public string evaluate(string value)
    {
        value = interpolate_variables(value);
        value = evaluate_expressions(value);
        return value.Trim();
    }

    private string interpolate_variables(string value)
    {
        var result = new StringBuilder(value.Length);
        var i = 0;

        while (i < value.Length)
        {
            if (value[i] == '#' && i + 1 < value.Length && value[i + 1] == '{')
            {
                var end = value.IndexOf('}', i + 2);
                if (end >= 0)
                {
                    var varName = value.Substring(i + 2, end - i - 2).Trim();
                    var resolved = resolve_variable_reference(varName);
                    result.Append(resolved);
                    i = end + 1;
                    continue;
                }
            }

            if (value[i] == '$')
            {
                var (varName, consumed) = read_variable_name(value, i + 1);
                if (varName.Length > 0)
                {
                    var resolved = resolve_variable_reference(varName);
                    result.Append(resolved);
                    i += consumed + 1;
                    continue;
                }
            }

            result.Append(value[i]);
            i++;
        }

        return result.ToString();
    }

    private string resolve_variable_reference(string name)
    {
        var resolved = _scope.resolve(name);
        if (resolved != null) return evaluate(resolved);

        return $"${name}";
    }

    private string evaluate_expressions(string value)
    {
        return try_evaluate_arithmetic(value, out var result) ? result : value;
    }

    private bool try_evaluate_arithmetic(string value, out string result)
    {
        result = value;

        var unitSuffix = extract_unit(value, out var numericPart);
        if (numericPart.Length == 0) return false;

        if (!try_parse_arithmetic(numericPart, out var number)) return false;

        if (number == (int)number)
            result = $"{(int)number}{unitSuffix}";
        else
            result = $"{number}{unitSuffix}";

        return true;
    }

    private string extract_unit(string value, out string numericPart)
    {
        var i = value.Length - 1;
        while (i >= 0 && char.IsLetter(value[i])) i--;

        if (i < value.Length - 1)
        {
            numericPart = value[..(i + 1)];
            return value[(i + 1)..];
        }

        numericPart = value;
        return "";
    }

    private bool try_parse_arithmetic(string expr, out float result)
    {
        result = 0;

        expr = expr.Trim();
        if (float.TryParse(expr, out result)) return true;

        var opIndex = find_lowest_precedence_op(expr);
        if (opIndex < 0) return false;

        var leftExpr = expr[..opIndex].Trim();
        var op = expr[opIndex];
        var rightExpr = expr[(opIndex + 1)..].Trim();

        if (!try_parse_arithmetic(leftExpr, out var left) || !try_parse_arithmetic(rightExpr, out var right))
            return false;

        result = op switch
        {
            '+' => left + right,
            '-' => left - right,
            '*' => left * right,
            '/' when right != 0 => left / right,
            '%' when right != 0 => left % right,
            _ => 0
        };

        return true;
    }

    private static int find_lowest_precedence_op(string expr)
    {
        var depth = 0;
        var addSubIndex = -1;
        var mulDivIndex = -1;

        for (var i = 0; i < expr.Length; i++)
        {
            var ch = expr[i];

            if (ch == '(')
            {
                depth++;
                continue;
            }

            if (ch == ')')
            {
                depth--;
                continue;
            }

            if (depth > 0) continue;

            if (ch is '+' or '-' && i > 0 && !is_operator_context(expr, i))
            {
                if (addSubIndex < 0) addSubIndex = i;
            }
            else if (ch is '*' or '/' or '%')
            {
                if (mulDivIndex < 0) mulDivIndex = i;
            }
        }

        return addSubIndex >= 0 ? addSubIndex : mulDivIndex;
    }

    private static bool is_operator_context(string expr, int index)
    {
        if (index == 0) return true;

        var prev = expr[index - 1];
        return prev == '+' || prev == '-' || prev == '*' || prev == '/' || prev == '%' || prev == '(' ||
               (char.IsWhiteSpace(prev) && index >= 2 && is_operator_context(expr, index - 1));
    }

    private static (string name, int consumed) read_variable_name(string source, int start)
    {
        var i = start;
        while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '-' || source[i] == '_')) i++;

        return (source.Substring(start, i - start), i - start);
    }
}