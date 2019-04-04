using Std.Data.Text.Lua;
using Nyar.VM.LegacyVM.Algebra;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     Lua AST 求值器，使用 LuaLexer + 内建递归下降解析器将 Lua 源码解析为 AST 并求值。
///     支持：local/global 变量赋值、if/elseif/else/then/end、while/do/end、
///     for i=start,end,step do/end、for k,v in pairs(t) do/end、repeat..until、
///     function/end、return、print()、table 字面量 {key = value}、
///     字符串连接 ..、算术/比较/逻辑运算、注释。
/// </summary>
public sealed class LuaAstEvaluator
{
    private readonly Dictionary<string, object> _env;
    private readonly CoreEvaluator _core;

    /// <summary>
    ///     创建 Lua AST 求值器
    /// </summary>
    /// <param name="env">初始变量环境</param>
    public LuaAstEvaluator(Dictionary<string, object> env)
    {
        _env = new Dictionary<string, object>(env);
        _core = new CoreEvaluator();
    }

    /// <summary>
    ///     解析并执行 Lua 源码
    /// </summary>
    /// <param name="source">Lua 源码</param>
    /// <returns>执行结果</returns>
    public object evaluate(string source)
    {
        var lexer = new LuaLexer();
        lexer.set_source(source);

        var tokens = new List<LuaToken>();

        while (true)
        {
            var token = lexer.next_token();

            if (token.Type == LuaTokenType.end_of_file)
            {
                break;
            }

            if (token.Type == LuaTokenType.comment || token.Type == LuaTokenType.long_comment)
            {
                continue;
            }

            tokens.Add(new LuaToken(token.Type, token.Text, token.Line, token.Column));
        }

        var parser = new LuaParser(tokens);
        var ast = parser.parse_block();
        return eval_block(ast);
    }

    #region 求值方法

    private object eval_block(LuaBlock block)
    {
        object result = _core.unit();

        foreach (var stmt in block.statements)
        {
            result = eval_node(stmt);

            if (result is ReturnValue or BreakValue or ContinueValue)
            {
                return result;
            }
        }

        return result;
    }

    private object eval_node(LuaAstNode node)
    {
        return node switch
        {
            LuaBlock b => eval_block(b),
            LuaLocalDecl decl => eval_local_decl(decl),
            LuaAssign assign => eval_assign(assign),
            LuaIfStmt ifStmt => eval_if(ifStmt),
            LuaWhileStmt whileStmt => eval_while(whileStmt),
            LuaNumericForStmt forStmt => eval_numeric_for(forStmt),
            LuaGenericForStmt forStmt => eval_generic_for(forStmt),
            LuaRepeatStmt repeatStmt => eval_repeat(repeatStmt),
            LuaFunctionDef funcDef => eval_function_def(funcDef),
            LuaReturnStmt returnStmt => new ReturnValue(returnStmt.value is not null ? eval_node(returnStmt.value) : _core.unit()),
            LuaBreakStmt => new BreakValue(),
            LuaNumberLiteral num => eval_number(num),
            LuaStringLiteral str => str.value,
            LuaNilLiteral => _core.unit(),
            LuaBoolLiteral b => b.value,
            LuaIdentifier id => _env.TryGetValue(id.name, out var val) ? val : _core.unit(),
            LuaBinaryOp binary => eval_binary(binary),
            LuaUnaryOp unary => eval_unary(unary),
            LuaCall call => eval_call(call),
            LuaMethodCall methodCall => eval_method_call(methodCall),
            LuaFieldAccess fieldAccess => eval_field_access(fieldAccess),
            LuaIndexAccess indexAccess => eval_index_access(indexAccess),
            LuaTableConstructor table => eval_table(table),
            _ => _core.unit()
        };
    }

    private object eval_local_decl(LuaLocalDecl decl)
    {
        for (var i = 0; i < decl.names.Count; i++)
        {
            var value = i < decl.values.Count ? eval_node(decl.values[i]) : _core.unit();
            _env[decl.names[i]] = value;
        }

        return _core.unit();
    }

    private object eval_assign(LuaAssign assign)
    {
        for (var i = 0; i < assign.targets.Count; i++)
        {
            var value = i < assign.values.Count ? eval_node(assign.values[i]) : _core.unit();
            var target = assign.targets[i];

            if (target is LuaIdentifier id)
            {
                _env[id.name] = value;
            }
            else if (target is LuaFieldAccess fa)
            {
                var obj = eval_node(fa.@object);

                if (obj is Dictionary<string, object> dict)
                {
                    dict[fa.field] = value;
                }
            }
            else if (target is LuaIndexAccess ia)
            {
                var obj = eval_node(ia.@object);
                var idx = eval_node(ia.index);

                if (obj is Dictionary<string, object> dict)
                {
                    dict[to_str(idx)] = value;
                }
                else if (obj is List<object> list)
                {
                    var idxInt = (int)to_int64(idx) - 1;

                    if (idxInt >= 0 && idxInt < list.Count)
                    {
                        list[idxInt] = value;
                    }
                }
            }
        }

        return _core.unit();
    }

    private object eval_if(LuaIfStmt ifStmt)
    {
        var condition = eval_node(ifStmt.condition);

        if (to_bool(condition))
        {
            return eval_block(ifStmt.then_block);
        }

        foreach (var (elseIfCond, elseIfBlock) in ifStmt.else_if_clauses)
        {
            if (to_bool(eval_node(elseIfCond)))
            {
                return eval_block(elseIfBlock);
            }
        }

        if (ifStmt.else_block is not null)
        {
            return eval_block(ifStmt.else_block);
        }

        return _core.unit();
    }

    private object eval_while(LuaWhileStmt whileStmt)
    {
        var maxIterations = 10000;
        var iteration = 0;
        object result = _core.unit();

        while (iteration < maxIterations && to_bool(eval_node(whileStmt.condition)))
        {
            result = eval_block(whileStmt.body);

            if (result is BreakValue)
            {
                return _core.unit();
            }

            if (result is ReturnValue)
            {
                return result;
            }

            iteration++;
        }

        if (iteration >= maxIterations)
        {
            Console.WriteLine("[Lua] 警告：while 循环达到最大迭代次数，已中断");
        }

        return result;
    }

    private object eval_numeric_for(LuaNumericForStmt forStmt)
    {
        var start = to_int64(eval_node(forStmt.start));
        var end = to_int64(eval_node(forStmt.end));
        var step = forStmt.step is not null ? to_int64(eval_node(forStmt.step)) : 1L;

        if (step == 0)
        {
            step = 1;
        }

        object result = _core.unit();
        var maxIterations = 10000;
        var iteration = 0;

        if (step > 0)
        {
            for (var i = start; i <= end; i += step)
            {
                if (iteration >= maxIterations)
                {
                    Console.WriteLine("[Lua] 警告：for 循环达到最大迭代次数，已中断");
                    return result;
                }

                _env[forStmt.var_name] = i;
                result = eval_block(forStmt.body);

                if (result is BreakValue)
                {
                    return _core.unit();
                }

                if (result is ReturnValue)
                {
                    return result;
                }

                iteration++;
            }
        }
        else
        {
            for (var i = start; i >= end; i += step)
            {
                if (iteration >= maxIterations)
                {
                    Console.WriteLine("[Lua] 警告：for 循环达到最大迭代次数，已中断");
                    return result;
                }

                _env[forStmt.var_name] = i;
                result = eval_block(forStmt.body);

                if (result is BreakValue)
                {
                    return _core.unit();
                }

                if (result is ReturnValue)
                {
                    return result;
                }

                iteration++;
            }
        }

        return result;
    }

    private object eval_generic_for(LuaGenericForStmt forStmt)
    {
        var iterVal = eval_node(forStmt.iterator_expr);
        object result = _core.unit();

        if (iterVal is Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                if (forStmt.var_names.Count >= 1)
                {
                    _env[forStmt.var_names[0]] = kvp.Key;
                }

                if (forStmt.var_names.Count >= 2)
                {
                    _env[forStmt.var_names[1]] = kvp.Value;
                }

                result = eval_block(forStmt.body);

                if (result is BreakValue)
                {
                    return _core.unit();
                }

                if (result is ReturnValue)
                {
                    return result;
                }
            }
        }
        else if (iterVal is List<object> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (forStmt.var_names.Count >= 1)
                {
                    _env[forStmt.var_names[0]] = i + 1L;
                }

                if (forStmt.var_names.Count >= 2)
                {
                    _env[forStmt.var_names[1]] = list[i] ?? _core.unit();
                }

                result = eval_block(forStmt.body);

                if (result is BreakValue)
                {
                    return _core.unit();
                }

                if (result is ReturnValue)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private object eval_repeat(LuaRepeatStmt repeatStmt)
    {
        var maxIterations = 10000;
        var iteration = 0;
        object result = _core.unit();

        do
        {
            result = eval_block(repeatStmt.body);

            if (result is BreakValue)
            {
                return _core.unit();
            }

            if (result is ReturnValue)
            {
                return result;
            }

            iteration++;
        } while (iteration < maxIterations && !to_bool(eval_node(repeatStmt.condition)));

        if (iteration >= maxIterations)
        {
            Console.WriteLine("[Lua] 警告：repeat 循环达到最大迭代次数，已中断");
        }

        return result;
    }

    private object eval_function_def(LuaFunctionDef funcDef)
    {
        if (string.IsNullOrEmpty(funcDef.name))
        {
            return new BuiltinFunction("anonymous", args =>
            {
                var localEnv = new Dictionary<string, object>(_env);

                for (var i = 0; i < funcDef.parameters.Count && i < args.Length; i++)
                {
                    localEnv[funcDef.parameters[i]] = args[i];
                }

                var innerEval = new LuaAstEvaluator(localEnv);
                return innerEval.eval_block(funcDef.body);
            });
        }

        var impl = new BuiltinFunction(funcDef.name, args =>
        {
            var localEnv = new Dictionary<string, object>(_env);

            for (var i = 0; i < funcDef.parameters.Count && i < args.Length; i++)
            {
                localEnv[funcDef.parameters[i]] = args[i];
            }

            var innerEval = new LuaAstEvaluator(localEnv);
            return innerEval.eval_block(funcDef.body);
        });

        _env[funcDef.name] = impl;
        return _core.unit();
    }

    private object eval_number(LuaNumberLiteral num)
    {
        if (long.TryParse(num.value, out var intVal))
        {
            return intVal;
        }

        if (double.TryParse(num.value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var floatVal))
        {
            return floatVal;
        }

        return 0L;
    }

    private object eval_binary(LuaBinaryOp binary)
    {
        if (binary.@operator == "and")
        {
            var left = eval_node(binary.left);
            return to_bool(left) ? eval_node(binary.right) : left;
        }

        if (binary.@operator == "or")
        {
            var left = eval_node(binary.left);
            return to_bool(left) ? left : eval_node(binary.right);
        }

        var leftVal = eval_node(binary.left);
        var rightVal = eval_node(binary.right);

        return binary.@operator switch
        {
            "+" => _core.add(leftVal, rightVal),
            "-" => _core.sub(leftVal, rightVal),
            "*" => _core.mul(leftVal, rightVal),
            "/" => _core.div(leftVal, rightVal),
            "%" => _core.mod(leftVal, rightVal),
            "^" => Math.Pow(to_float(leftVal), to_float(rightVal)),
            ".." => to_str(leftVal) + to_str(rightVal),
            "==" => _core.eq(leftVal, rightVal),
            "~=" => _core.ne(leftVal, rightVal),
            "<" => _core.lt(leftVal, rightVal),
            "<=" => _core.lte(leftVal, rightVal),
            ">" => _core.gt(leftVal, rightVal),
            ">=" => _core.gte(leftVal, rightVal),
            _ => _core.unit()
        };
    }

    private object eval_unary(LuaUnaryOp unary)
    {
        var operand = eval_node(unary.operand);

        return unary.@operator switch
        {
            "-" => _core.sub(0L, operand),
            "not" => !to_bool(operand),
            "#" => operand switch
            {
                string s => (long)s.Length,
                List<object> list => (long)list.Count,
                Dictionary<string, object> dict => (long)dict.Count,
                _ => 0L
            },
            _ => operand
        };
    }

    private object eval_call(LuaCall call)
    {
        var funcName = extract_func_name(call.function);
        var argVals = call.arguments.Select(eval_node).ToArray();

        if (funcName == "print")
        {
            Console.WriteLine(string.Join("\t", argVals.Select(to_str)));
            return _core.unit();
        }

        if (funcName == "type")
        {
            if (argVals.Length == 0)
            {
                return "nil";
            }

            return argVals[0] switch
            {
                null => "nil",
                long => "number",
                double => "number",
                bool => "boolean",
                string => "string",
                List<object> => "table",
                Dictionary<string, object> => "table",
                BuiltinFunction => "function",
                _ => argVals[0].GetType().Name.ToLower()
            };
        }

        if (funcName == "tostring")
        {
            return argVals.Length > 0 ? to_str(argVals[0]) : "nil";
        }

        if (funcName == "tonumber")
        {
            if (argVals.Length > 0 && argVals[0] is string s && long.TryParse(s, out var v))
            {
                return v;
            }

            return _core.unit();
        }

        if (funcName == "pairs")
        {
            return argVals.Length > 0 ? argVals[0] : _core.unit();
        }

        if (funcName == "ipairs")
        {
            return argVals.Length > 0 ? argVals[0] : _core.unit();
        }

        if (funcName == "table.insert")
        {
            if (argVals is [List<object> list, _, ..])
            {
                list.Add(argVals[1]);
            }

            return _core.unit();
        }

        if (funcName == "table.remove")
        {
            if (argVals is [List<object> { Count: > 0 } list, ..])
            {
                var idx = argVals.Length >= 2 ? (int)to_int64(argVals[1]) - 1 : list.Count - 1;

                if (idx >= 0 && idx < list.Count)
                {
                    var removed = list[idx];
                    list.RemoveAt(idx);
                    return removed ?? _core.unit();
                }
            }

            return _core.unit();
        }

        if (_env.TryGetValue(funcName, out var func))
        {
            if (func is BuiltinFunction builtin)
            {
                return builtin.invoke(argVals);
            }
        }

        Console.WriteLine($"[Lua] 警告：未定义函数 {funcName}");
        return _core.unit();
    }

    private object eval_method_call(LuaMethodCall methodCall)
    {
        var obj = eval_node(methodCall.@object);
        var argVals = methodCall.arguments.Select(eval_node).ToArray();

        var allArgs = new[] { obj }.Concat(argVals).ToArray();

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(methodCall.method, out var method))
        {
            if (method is BuiltinFunction builtin)
            {
                return builtin.invoke(allArgs);
            }
        }

        Console.WriteLine($"[Lua] 警告：未定义方法 {methodCall.method}");
        return _core.unit();
    }

    private object eval_field_access(LuaFieldAccess fieldAccess)
    {
        var obj = eval_node(fieldAccess.@object);

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(fieldAccess.field, out var val))
        {
            return val;
        }

        return _core.unit();
    }

    private object eval_index_access(LuaIndexAccess indexAccess)
    {
        var obj = eval_node(indexAccess.@object);
        var idx = eval_node(indexAccess.index);

        if (obj is Dictionary<string, object> dict)
        {
            var key = to_str(idx);
            return dict.TryGetValue(key, out var val) ? val : _core.unit();
        }

        if (obj is List<object> list)
        {
            var idxInt = (int)to_int64(idx) - 1;

            if (idxInt >= 0 && idxInt < list.Count)
            {
                return list[idxInt] ?? _core.unit();
            }

            return _core.unit();
        }

        if (obj is string s)
        {
            var idxInt = (int)to_int64(idx) - 1;

            if (idxInt >= 0 && idxInt < s.Length)
            {
                return s[idxInt].ToString();
            }

            return _core.unit();
        }

        return _core.unit();
    }

    private object eval_table(LuaTableConstructor table)
    {
        var hasNamedFields = table.fields.Any(f => f is LuaNamedField);

        if (hasNamedFields)
        {
            var dict = new Dictionary<string, object>();
            var positionalIdx = 1L;

            foreach (var field in table.fields)
            {
                if (field is LuaNamedField named)
                {
                    dict[named.key] = eval_node(named.value);
                }
                else if (field is LuaPositionalField positional)
                {
                    dict[positionalIdx.ToString()] = eval_node(positional.value);
                    positionalIdx++;
                }
            }

            return dict;
        }

        var list = new List<object>();

        foreach (var field in table.fields)
        {
            if (field is LuaPositionalField positional)
            {
                list.Add(eval_node(positional.value));
            }
        }

        return list;
    }

    #endregion

    #region 辅助方法

    private static string extract_func_name(LuaAstNode func)
    {
        return func switch
        {
            LuaIdentifier id => id.name,
            LuaFieldAccess fa => $"{extract_func_name(fa.@object)}.{fa.field}",
            _ => "_"
        };
    }

    private static long to_int64(object val)
    {
        return val switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            float f => (long)f,
            string s when long.TryParse(s, out var r) => r,
            _ => 0
        };
    }

    private static double to_float(object val)
    {
        return val switch
        {
            double d => d,
            float f => f,
            long l => l,
            int i => i,
            string s when double.TryParse(s, out var r) => r,
            _ => 0.0
        };
    }

    private static bool to_bool(object val)
    {
        return val switch
        {
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            double d => d != 0,
            null => false,
            string s => s.Length > 0,
            _ => true
        };
    }

    private static string to_str(object? val)
    {
        return val?.ToString() ?? "nil";
    }

    #endregion
}

#region Lua Token 包装

/// <summary>
///     Lua Token 包装
/// </summary>
internal readonly record struct LuaToken(LuaTokenType type, string text, int line, int column);

#endregion

#region Lua AST 节点

/// <summary>
///     Lua AST 节点基类
/// </summary>
internal abstract record LuaAstNode;

/// <summary>
///     语句块
/// </summary>
internal sealed record LuaBlock(IReadOnlyList<LuaAstNode> statements) : LuaAstNode;

/// <summary>
///     local 变量声明
/// </summary>
internal sealed record LuaLocalDecl(IReadOnlyList<string> names, IReadOnlyList<LuaAstNode> values) : LuaAstNode;

/// <summary>
///     赋值语句
/// </summary>
internal sealed record LuaAssign(IReadOnlyList<LuaAstNode> targets, IReadOnlyList<LuaAstNode> values) : LuaAstNode;

/// <summary>
///     if 语句
/// </summary>
internal sealed record LuaIfStmt(
    LuaAstNode condition,
    LuaBlock then_block,
    IReadOnlyList<(LuaAstNode Condition, LuaBlock Block)> else_if_clauses,
    LuaBlock? else_block) : LuaAstNode;

/// <summary>
///     while 循环
/// </summary>
internal sealed record LuaWhileStmt(LuaAstNode condition, LuaBlock body) : LuaAstNode;

/// <summary>
///     数值 for 循环
/// </summary>
internal sealed record LuaNumericForStmt(
    string var_name,
    LuaAstNode start,
    LuaAstNode end,
    LuaAstNode? step,
    LuaBlock body) : LuaAstNode;

/// <summary>
///     泛型 for 循环
/// </summary>
internal sealed record LuaGenericForStmt(
    IReadOnlyList<string> var_names,
    LuaAstNode iterator_expr,
    LuaBlock body) : LuaAstNode;

/// <summary>
///     repeat..until 循环
/// </summary>
internal sealed record LuaRepeatStmt(LuaBlock body, LuaAstNode condition) : LuaAstNode;

/// <summary>
///     函数定义
/// </summary>
internal sealed record LuaFunctionDef(
    string name,
    IReadOnlyList<string> parameters,
    bool is_local,
    LuaBlock body) : LuaAstNode;

/// <summary>
///     return 语句
/// </summary>
internal sealed record LuaReturnStmt(LuaAstNode? value) : LuaAstNode;

/// <summary>
///     break 语句
/// </summary>
internal sealed record LuaBreakStmt : LuaAstNode;

/// <summary>
///     数字字面量
/// </summary>
internal sealed record LuaNumberLiteral(string value) : LuaAstNode;

/// <summary>
///     字符串字面量
/// </summary>
internal sealed record LuaStringLiteral(string value) : LuaAstNode;

/// <summary>
///     nil 字面量
/// </summary>
internal sealed record LuaNilLiteral : LuaAstNode;

/// <summary>
///     布尔字面量
/// </summary>
internal sealed record LuaBoolLiteral(bool value) : LuaAstNode;

/// <summary>
///     标识符
/// </summary>
internal sealed record LuaIdentifier(string name) : LuaAstNode;

/// <summary>
///     二元运算
/// </summary>
internal sealed record LuaBinaryOp(string @operator, LuaAstNode left, LuaAstNode right) : LuaAstNode;

/// <summary>
///     一元运算
/// </summary>
internal sealed record LuaUnaryOp(string @operator, LuaAstNode operand) : LuaAstNode;

/// <summary>
///     函数调用
/// </summary>
internal sealed record LuaCall(LuaAstNode function, IReadOnlyList<LuaAstNode> arguments) : LuaAstNode;

/// <summary>
///     方法调用
/// </summary>
internal sealed record LuaMethodCall(
    LuaAstNode @object,
    string method,
    IReadOnlyList<LuaAstNode> arguments) : LuaAstNode;

/// <summary>
///     字段访问
/// </summary>
internal sealed record LuaFieldAccess(LuaAstNode @object, string field) : LuaAstNode;

/// <summary>
///     索引访问
/// </summary>
internal sealed record LuaIndexAccess(LuaAstNode @object, LuaAstNode index) : LuaAstNode;

/// <summary>
///     table 构造器
/// </summary>
internal sealed record LuaTableConstructor(IReadOnlyList<LuaTableField> fields) : LuaAstNode;

/// <summary>
///     table 字段
/// </summary>
internal abstract record LuaTableField : LuaAstNode;

/// <summary>
///     具名 table 字段
/// </summary>
internal sealed record LuaNamedField(string key, LuaAstNode value) : LuaTableField;

/// <summary>
///     位置 table 字段
/// </summary>
internal sealed record LuaPositionalField(LuaAstNode value) : LuaTableField;

#endregion

#region Lua 递归下降解析器

/// <summary>
///     Lua 递归下降解析器，从 LuaLexer 的 Token 流构建 AST
/// </summary>
internal sealed class LuaParser
{
    private readonly IReadOnlyList<LuaToken> _tokens;
    private int _current;

    internal LuaParser(IReadOnlyList<LuaToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
    }

    /// <summary>
    ///     解析语句块
    /// </summary>
    internal LuaBlock parse_block()
    {
        var statements = new List<LuaAstNode>();

        while (!is_at_end() && !is_block_terminator())
        {
            var stmt = parse_statement();

            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return new LuaBlock(statements);
    }

    #region 语句解析

    private LuaAstNode? parse_statement()
    {
        if (check(LuaTokenType.local))
        {
            return parse_local();
        }

        if (check(LuaTokenType.@if))
        {
            return parse_if();
        }

        if (check(LuaTokenType.@while))
        {
            return parse_while();
        }

        if (check(LuaTokenType.@for))
        {
            return parse_for();
        }

        if (check(LuaTokenType.repeat))
        {
            return parse_repeat();
        }

        if (check(LuaTokenType.function))
        {
            return parse_function(isLocal: false);
        }

        if (check(LuaTokenType.@return))
        {
            return parse_return();
        }

        if (check(LuaTokenType.@break))
        {
            advance();
            return new LuaBreakStmt();
        }

        if (check(LuaTokenType.@do))
        {
            advance();
            var block = parse_block();
            consume(LuaTokenType.end, "期望 'end'");
            return block;
        }

        return parse_expr_stat();
    }

    /// <summary>
    ///     解析 local 声明
    /// </summary>
    private LuaAstNode parse_local()
    {
        consume(LuaTokenType.local, "期望 'local'");

        if (check(LuaTokenType.function))
        {
            return parse_function(isLocal: true);
        }

        var names = new List<string>();
        names.Add(consume(LuaTokenType.name, "期望变量名").text);

        while (match(LuaTokenType.comma))
        {
            names.Add(consume(LuaTokenType.name, "期望变量名").text);
        }

        var values = new List<LuaAstNode>();

        if (match(LuaTokenType.equal))
        {
            values.Add(parse_expression());

            while (match(LuaTokenType.comma))
            {
                values.Add(parse_expression());
            }
        }

        return new LuaLocalDecl(names, values);
    }

    /// <summary>
    ///     解析 if 语句
    /// </summary>
    private LuaAstNode parse_if()
    {
        consume(LuaTokenType.@if, "期望 'if'");
        var condition = parse_expression();
        consume(LuaTokenType.then, "期望 'then'");
        var thenBlock = parse_block();

        var elseIfClauses = new List<(LuaAstNode, LuaBlock)>();

        while (check(LuaTokenType.else_if))
        {
            advance();
            var elseIfCond = parse_expression();
            consume(LuaTokenType.then, "期望 'then'");
            var elseIfBlock = parse_block();
            elseIfClauses.Add((elseIfCond, elseIfBlock));
        }

        LuaBlock? elseBlock = null;

        if (match(LuaTokenType.@else))
        {
            elseBlock = parse_block();
        }

        consume(LuaTokenType.end, "期望 'end'");

        return new LuaIfStmt(condition, thenBlock, elseIfClauses, elseBlock);
    }

    /// <summary>
    ///     解析 while 循环
    /// </summary>
    private LuaAstNode parse_while()
    {
        consume(LuaTokenType.@while, "期望 'while'");
        var condition = parse_expression();
        consume(LuaTokenType.@do, "期望 'do'");
        var body = parse_block();
        consume(LuaTokenType.end, "期望 'end'");

        return new LuaWhileStmt(condition, body);
    }

    /// <summary>
    ///     解析 for 循环
    /// </summary>
    private LuaAstNode parse_for()
    {
        consume(LuaTokenType.@for, "期望 'for'");
        var firstName = consume(LuaTokenType.name, "期望变量名").text;

        if (check(LuaTokenType.equal))
        {
            advance();
            var start = parse_expression();
            consume(LuaTokenType.comma, "期望 ','");
            var end = parse_expression();
            LuaAstNode? step = null;

            if (match(LuaTokenType.comma))
            {
                step = parse_expression();
            }

            consume(LuaTokenType.@do, "期望 'do'");
            var body = parse_block();
            consume(LuaTokenType.end, "期望 'end'");

            return new LuaNumericForStmt(firstName, start, end, step, body);
        }

        var varNames = new List<string> { firstName };

        while (match(LuaTokenType.comma))
        {
            varNames.Add(consume(LuaTokenType.name, "期望变量名").text);
        }

        consume(LuaTokenType.@in, "期望 'in'");
        var iterExpr = parse_expression();
        consume(LuaTokenType.@do, "期望 'do'");
        var forBody = parse_block();
        consume(LuaTokenType.end, "期望 'end'");

        return new LuaGenericForStmt(varNames, iterExpr, forBody);
    }

    /// <summary>
    ///     解析 repeat..until 循环
    /// </summary>
    private LuaAstNode parse_repeat()
    {
        consume(LuaTokenType.repeat, "期望 'repeat'");
        var body = parse_block();
        consume(LuaTokenType.until, "期望 'until'");
        var condition = parse_expression();

        return new LuaRepeatStmt(body, condition);
    }

    /// <summary>
    ///     解析函数定义
    /// </summary>
    private LuaAstNode parse_function(bool isLocal)
    {
        consume(LuaTokenType.function, "期望 'function'");
        var name = consume(LuaTokenType.name, "期望函数名").text;

        while (match(LuaTokenType.dot))
        {
            name += "." + consume(LuaTokenType.name, "期望方法名").text;
        }

        if (match(LuaTokenType.colon))
        {
            name += ":" + consume(LuaTokenType.name, "期望方法名").text;
        }

        consume(LuaTokenType.left_paren, "期望 '('");
        var parameters = new List<string>();

        if (!check(LuaTokenType.right_paren))
        {
            parameters.Add(consume(LuaTokenType.name, "期望参数名").text);

            while (match(LuaTokenType.comma))
            {
                if (check(LuaTokenType.dots))
                {
                    advance();
                    break;
                }

                parameters.Add(consume(LuaTokenType.name, "期望参数名").text);
            }
        }

        consume(LuaTokenType.right_paren, "期望 ')'");
        var body = parse_block();
        consume(LuaTokenType.end, "期望 'end'");

        return new LuaFunctionDef(name, parameters, isLocal, body);
    }

    /// <summary>
    ///     解析 return 语句
    /// </summary>
    private LuaAstNode parse_return()
    {
        consume(LuaTokenType.@return, "期望 'return'");

        LuaAstNode? value = null;

        if (!is_at_end() && !check(LuaTokenType.end) && !check(LuaTokenType.else_if) &&
            !check(LuaTokenType.@else) && !check(LuaTokenType.until))
        {
            value = parse_expression();
        }

        return new LuaReturnStmt(value);
    }

    /// <summary>
    ///     解析表达式语句（赋值或函数调用）
    /// </summary>
    private LuaAstNode? parse_expr_stat()
    {
        var expr = parse_suffixed_expr();

        if (match(LuaTokenType.equal))
        {
            var targets = new List<LuaAstNode> { expr };

            while (match(LuaTokenType.comma))
            {
                targets.Add(parse_suffixed_expr());
            }

            var values = new List<LuaAstNode> { parse_expression() };

            while (match(LuaTokenType.comma))
            {
                values.Add(parse_expression());
            }

            return new LuaAssign(targets, values);
        }

        return expr;
    }

    #endregion

    #region 表达式解析

    private LuaAstNode parse_expression()
    {
        return parse_or();
    }

    private LuaAstNode parse_or()
    {
        var left = parse_and();

        while (match(LuaTokenType.or))
        {
            var right = parse_and();
            left = new LuaBinaryOp("or", left, right);
        }

        return left;
    }

    private LuaAstNode parse_and()
    {
        var left = parse_comparison();

        while (match(LuaTokenType.and))
        {
            var right = parse_comparison();
            left = new LuaBinaryOp("and", left, right);
        }

        return left;
    }

    private LuaAstNode parse_comparison()
    {
        var left = parse_concat();

        while (check(LuaTokenType.equal_equal) || check(LuaTokenType.tilde_equal) ||
               check(LuaTokenType.less) || check(LuaTokenType.less_equal) ||
               check(LuaTokenType.greater) || check(LuaTokenType.greater_equal))
        {
            var op = advance();
            var right = parse_concat();
            left = new LuaBinaryOp(op.text, left, right);
        }

        return left;
    }

    private LuaAstNode parse_concat()
    {
        var left = parse_additive();

        while (match(LuaTokenType.concat))
        {
            var right = parse_additive();
            left = new LuaBinaryOp("..", left, right);
        }

        return left;
    }

    private LuaAstNode parse_additive()
    {
        var left = parse_multiplicative();

        while (check(LuaTokenType.plus) || check(LuaTokenType.minus))
        {
            var op = advance();
            var right = parse_multiplicative();
            left = new LuaBinaryOp(op.text, left, right);
        }

        return left;
    }

    private LuaAstNode parse_multiplicative()
    {
        var left = parse_unary();

        while (check(LuaTokenType.star) || check(LuaTokenType.slash) ||
               check(LuaTokenType.percent) || check(LuaTokenType.caret))
        {
            var op = advance();
            var right = parse_unary();
            left = new LuaBinaryOp(op.text, left, right);
        }

        return left;
    }

    private LuaAstNode parse_unary()
    {
        if (check(LuaTokenType.not) || check(LuaTokenType.minus) || check(LuaTokenType.hash))
        {
            var op = advance();
            var operand = parse_unary();
            return new LuaUnaryOp(op.text, operand);
        }

        return parse_power();
    }

    private LuaAstNode parse_power()
    {
        var left = parse_suffixed_expr();

        if (match(LuaTokenType.caret))
        {
            var right = parse_unary();
            left = new LuaBinaryOp("^", left, right);
        }

        return left;
    }

    private LuaAstNode parse_suffixed_expr()
    {
        var expr = parse_primary();

        while (true)
        {
            if (check(LuaTokenType.dot))
            {
                advance();
                var field = consume(LuaTokenType.name, "期望字段名").text;
                expr = new LuaFieldAccess(expr, field);
            }
            else if (check(LuaTokenType.colon))
            {
                advance();
                var method = consume(LuaTokenType.name, "期望方法名").text;
                consume(LuaTokenType.left_paren, "期望 '('");
                var args = parse_args();
                consume(LuaTokenType.right_paren, "期望 ')'");
                expr = new LuaMethodCall(expr, method, args);
            }
            else if (check(LuaTokenType.left_bracket))
            {
                advance();
                var index = parse_expression();
                consume(LuaTokenType.right_bracket, "期望 ']'");
                expr = new LuaIndexAccess(expr, index);
            }
            else if (check(LuaTokenType.left_paren))
            {
                advance();
                var args = parse_args();
                consume(LuaTokenType.right_paren, "期望 ')'");
                expr = new LuaCall(expr, args);
            }
            else if (check(LuaTokenType.left_brace))
            {
                var table = parse_table_constructor();
                expr = new LuaCall(expr, [table]);
            }
            else if (check(LuaTokenType.@string))
            {
                var str = advance();
                expr = new LuaCall(expr, [new LuaStringLiteral(str.text)]);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private List<LuaAstNode> parse_args()
    {
        var args = new List<LuaAstNode>();

        if (!check(LuaTokenType.right_paren))
        {
            args.Add(parse_expression());

            while (match(LuaTokenType.comma))
            {
                args.Add(parse_expression());
            }
        }

        return args;
    }

    private LuaAstNode parse_primary()
    {
        if (check(LuaTokenType.nil))
        {
            advance();
            return new LuaNilLiteral();
        }

        if (check(LuaTokenType.@true))
        {
            advance();
            return new LuaBoolLiteral(true);
        }

        if (check(LuaTokenType.@false))
        {
            advance();
            return new LuaBoolLiteral(false);
        }

        if (check(LuaTokenType.integer) || check(LuaTokenType.@float))
        {
            var token = advance();
            return new LuaNumberLiteral(token.text);
        }

        if (check(LuaTokenType.@string) || check(LuaTokenType.long_string))
        {
            var token = advance();
            return new LuaStringLiteral(token.text);
        }

        if (check(LuaTokenType.name))
        {
            var token = advance();
            return new LuaIdentifier(token.text);
        }

        if (check(LuaTokenType.left_paren))
        {
            advance();
            var expr = parse_expression();
            consume(LuaTokenType.right_paren, "期望 ')'");
            return expr;
        }

        if (check(LuaTokenType.left_brace))
        {
            return parse_table_constructor();
        }

        if (check(LuaTokenType.function))
        {
            return parse_function_expr();
        }

        advance();
        return new LuaNilLiteral();
    }

    /// <summary>
    ///     解析 table 构造器
    /// </summary>
    private LuaAstNode parse_table_constructor()
    {
        consume(LuaTokenType.left_brace, "期望 '{'");
        var fields = new List<LuaTableField>();

        while (!check(LuaTokenType.right_brace) && !is_at_end())
        {
            if (match(LuaTokenType.left_bracket))
            {
                var key = parse_expression();
                consume(LuaTokenType.right_bracket, "期望 ']'");
                consume(LuaTokenType.equal, "期望 '='");
                var value = parse_expression();
                fields.Add(new LuaNamedField(to_str_key(key), value));
            }
            else if (check(LuaTokenType.name) && peek_ahead_is(LuaTokenType.equal))
            {
                var name = advance().text;
                consume(LuaTokenType.equal, "期望 '='");
                var value = parse_expression();
                fields.Add(new LuaNamedField(name, value));
            }
            else
            {
                var value = parse_expression();
                fields.Add(new LuaPositionalField(value));
            }

            if (!match(LuaTokenType.comma) && !match(LuaTokenType.semicolon))
            {
                break;
            }
        }

        consume(LuaTokenType.right_brace, "期望 '}'");

        return new LuaTableConstructor(fields);
    }

    /// <summary>
    ///     解析函数表达式
    /// </summary>
    private LuaAstNode parse_function_expr()
    {
        consume(LuaTokenType.function, "期望 'function'");
        consume(LuaTokenType.left_paren, "期望 '('");

        var parameters = new List<string>();

        if (!check(LuaTokenType.right_paren))
        {
            parameters.Add(consume(LuaTokenType.name, "期望参数名").text);

            while (match(LuaTokenType.comma))
            {
                if (check(LuaTokenType.dots))
                {
                    advance();
                    break;
                }

                parameters.Add(consume(LuaTokenType.name, "期望参数名").text);
            }
        }

        consume(LuaTokenType.right_paren, "期望 ')'");
        var body = parse_block();
        consume(LuaTokenType.end, "期望 'end'");

        return new LuaFunctionDef("", parameters, false, body);
    }

    #endregion

    #region 辅助方法

    private static string to_str_key(LuaAstNode key)
    {
        return key switch
        {
            LuaStringLiteral s => s.value,
            LuaIdentifier id => id.name,
            LuaNumberLiteral n => n.value,
            _ => "_"
        };
    }

    private bool is_at_end()
    {
        return _current >= _tokens.Count;
    }

    private bool is_block_terminator()
    {
        if (is_at_end())
        {
            return true;
        }

        var type = _tokens[_current].type;
        return type is LuaTokenType.end or LuaTokenType.else_if or LuaTokenType.@else
            or LuaTokenType.until or LuaTokenType.@return;
    }

    private bool check(LuaTokenType type)
    {
        return !is_at_end() && _tokens[_current].type == type;
    }

    private LuaToken advance()
    {
        if (!is_at_end())
        {
            _current++;
        }

        return _tokens[_current - 1];
    }

    private bool match(LuaTokenType type)
    {
        if (check(type))
        {
            advance();
            return true;
        }

        return false;
    }

    private LuaToken consume(LuaTokenType type, string message)
    {
        if (check(type))
        {
            return advance();
        }

        throw new InvalidOperationException(message);
    }

    /// <summary>
    ///     检查下一个 token 是否为指定类型（向前看 2 个位置）
    /// </summary>
    private bool peek_ahead_is(LuaTokenType type)
    {
        if (_current + 1 >= _tokens.Count)
        {
            return false;
        }

        return _tokens[_current + 1].type == type;
    }

    #endregion
}

#endregion