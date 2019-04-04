using Oak.Julia.AST;
using Oak.Julia.Lexer;
using Oak.Julia.Parser;
using Nyar.VM.LegacyVM.Algebra.Core;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     Julia AST 求值器，遍历 Oak.Julia AST 节点并调用 CoreEvaluator 求值。
///     支持：函数定义、结构体、if/elseif/else、for/while、三元运算符、管道运算符、
///     范围表达式、赋值、算术/比较/逻辑运算、println/print、数组/元组/字典字面量、
///     lambda、import/using/export（跳过）、模块（展开求值）。
/// </summary>
public sealed class JuliaTreeEvaluator : UnifiedTreeEvaluator<JlAstNode>
{
    /// <summary>
    ///     创建 Julia AST 求值器
    /// </summary>
    /// <param name="config">语言运行时配置</param>
    /// <param name="env">初始变量环境</param>
    public JuliaTreeEvaluator(LanguageRuntimeConfig config, Dictionary<string, object> env) : base(config, env)
    {
    }

    /// <inheritdoc />
    public override object evaluate(JlAstNode ast)
    {
        return eval_node(ast);
    }

    #region 节点求值

    /// <summary>
    ///     根据节点类型分派求值
    /// </summary>
    private object eval_node(JlAstNode node)
    {
        return node switch
        {
            JlBlock block => eval_block(block),
            JlModule module => eval_module(module),
            JlImport => _core.unit(),
            JlExport => _core.unit(),
            JlFunctionDef func => eval_function_def(func),
            JlMacroDef => _core.unit(),
            JlStructDef structDef => eval_struct_def(structDef),
            JlTypeDef => _core.unit(),
            JlIfExpr ifExpr => eval_if(ifExpr),
            JlForExpr forExpr => eval_for(forExpr),
            JlWhileExpr whileExpr => eval_while(whileExpr),
            JlTryExpr tryExpr => eval_try(tryExpr),
            JlLetExpr letExpr => eval_let(letExpr),
            JlReturnExpr returnExpr => eval_return(returnExpr),
            JlBreakExpr => new BreakValue(),
            JlContinueExpr => new ContinueValue(),
            JlAssignment assignment => eval_assignment(assignment),
            JlCompoundAssignment compound => eval_compound_assignment(compound),
            JlBinaryOp binary => eval_binary(binary),
            JlUnaryOp unary => eval_unary(unary),
            JlTernary ternary => eval_ternary(ternary),
            JlPipe pipe => eval_pipe(pipe),
            JlRange range => eval_range(range),
            JlCall call => eval_call(call),
            JlIndex index => eval_index(index),
            JlFieldAccess fieldAccess => eval_field_access(fieldAccess),
            JlLambda lambda => eval_lambda(lambda),
            JlIdentifier identifier => eval_identifier(identifier),
            JlLiteral literal => eval_literal(literal),
            JlTuple tuple => eval_tuple(tuple),
            JlArray array => eval_array(array),
            JlDict dict => eval_dict(dict),
            JlUnit => _core.unit(),
            JlComprehension comprehension => eval_comprehension(comprehension),
            JlMacroCall => _core.unit(),
            JlCommand => _core.unit(),
            JlSymbol => _core.unit(),
            JlParameter parameter => eval_identifier(new JlIdentifier(parameter.name)),
            JlField field => eval_identifier(new JlIdentifier(field.name)),
            JlKeywordParameter kwParam => eval_identifier(new JlIdentifier(kwParam.name)),
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值代码块
    /// </summary>
    private object eval_block(JlBlock block)
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

    /// <summary>
    ///     求值模块（展开内部语句求值）
    /// </summary>
    private object eval_module(JlModule module)
    {
        object result = _core.unit();

        foreach (var stmt in module.statements)
        {
            result = eval_node(stmt);

            if (result is ReturnValue)
            {
                return result;
            }
        }

        return result;
    }

    /// <summary>
    ///     求值函数定义，注册到函数环境
    /// </summary>
    private object eval_function_def(JlFunctionDef func)
    {
        var paramNames = extract_param_names(func.parameters);
        var body = func.body;

        Func<object[], object> impl = args =>
        {
            var localEnv = new Dictionary<string, object>(_env);

            for (var i = 0; i < paramNames.Length && i < args.Length; i++)
            {
                localEnv[paramNames[i]] = args[i];
            }

            var innerEval = new JuliaTreeEvaluator(_config, localEnv);
            return innerEval.eval_node(body);
        };

        _functions[func.name] = impl;
        _env[func.name] = new BuiltinFunction(func.name, impl);

        return _core.unit();
    }

    /// <summary>
    ///     求值结构体定义，创建构造函数
    /// </summary>
    private object eval_struct_def(JlStructDef structDef)
    {
        var fieldNames = structDef.fields
            .OfType<JlField>()
            .Select(f => f.name)
            .ToArray();

        Func<object[], object> ctor = args =>
        {
            var dict = new Dictionary<string, object>();

            for (var i = 0; i < fieldNames.Length && i < args.Length; i++)
            {
                dict[fieldNames[i]] = args[i];
            }

            return dict;
        };

        _functions[structDef.name] = ctor;
        _env[structDef.name] = new BuiltinFunction(structDef.name, ctor);

        return _core.unit();
    }

    /// <summary>
    ///     求值 if 表达式
    /// </summary>
    private object eval_if(JlIfExpr ifExpr)
    {
        var condition = eval_node(ifExpr.condition);

        if (to_bool(condition))
        {
            return eval_node(ifExpr.then_branch);
        }

        foreach (var elseIf in ifExpr.else_if_branches)
        {
            var elseIfCond = eval_node(elseIf.Condition);

            if (to_bool(elseIfCond))
            {
                return eval_node(elseIf.Body);
            }
        }

        if (ifExpr.else_branch is not null)
        {
            return eval_node(ifExpr.else_branch);
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 for 循环
    /// </summary>
    private object eval_for(JlForExpr forExpr)
    {
        object result = _core.unit();
        var maxIterations = 10000;
        var iteration = 0;

        foreach (var (iterator, iterable) in forExpr.iterators)
        {
            var iterableVal = eval_node(iterable);
            var varName = extract_iterator_name(iterator);

            if (iterableVal is List<object> list)
            {
                foreach (var item in list)
                {
                    if (iteration >= maxIterations)
                    {
                        Console.WriteLine("[Julia] 警告：for 循环达到最大迭代次数，已中断");
                        return result;
                    }

                    _env[varName] = item;
                    result = eval_node(forExpr.body);

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
            else if (iterableVal is long endVal)
            {
                for (var i = 1L; i <= endVal; i++)
                {
                    if (iteration >= maxIterations)
                    {
                        Console.WriteLine("[Julia] 警告：for 循环达到最大迭代次数，已中断");
                        return result;
                    }

                    _env[varName] = i;
                    result = eval_node(forExpr.body);

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
        }

        return result;
    }

    /// <summary>
    ///     求值 while 循环
    /// </summary>
    private object eval_while(JlWhileExpr whileExpr)
    {
        var maxIterations = 10000;
        var iteration = 0;
        object result = _core.unit();

        while (iteration < maxIterations && to_bool(eval_node(whileExpr.condition)))
        {
            result = eval_node(whileExpr.body);

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
            Console.WriteLine("[Julia] 警告：while 循环达到最大迭代次数，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 try 表达式
    /// </summary>
    private object eval_try(JlTryExpr tryExpr)
    {
        try
        {
            return eval_node(tryExpr.body);
        }
        catch (Exception ex)
        {
            foreach (var (pattern, body) in tryExpr.catch_clauses)
            {
                if (pattern is JlIdentifier id)
                {
                    _env[id.name] = ex.Message;
                }

                return eval_node(body);
            }

            Console.WriteLine($"[Julia] 未捕获的异常：{ex.Message}");
            return _core.unit();
        }
    }

    /// <summary>
    ///     求值 let 表达式
    /// </summary>
    private object eval_let(JlLetExpr letExpr)
    {
        foreach (var binding in letExpr.bindings)
        {
            eval_node(binding);
        }

        return eval_node(letExpr.body);
    }

    /// <summary>
    ///     求值 return 表达式
    /// </summary>
    private object eval_return(JlReturnExpr returnExpr)
    {
        var value = returnExpr.value is not null ? eval_node(returnExpr.value) : _core.unit();
        return new ReturnValue(value);
    }

    /// <summary>
    ///     求值赋值表达式
    /// </summary>
    private object eval_assignment(JlAssignment assignment)
    {
        var value = eval_node(assignment.right);
        var varName = extract_target_name(assignment.left);
        _env[varName] = value;
        return value;
    }

    /// <summary>
    ///     求值复合赋值表达式
    /// </summary>
    private object eval_compound_assignment(JlCompoundAssignment compound)
    {
        var varName = extract_target_name(compound.left);
        var curVal = _env.TryGetValue(varName, out var existing) ? existing : 0L;
        var rhsVal = eval_node(compound.right);

        var newVal = compound.@operator switch
        {
            "+=" => _core.add(curVal, rhsVal),
            "-=" => _core.sub(curVal, rhsVal),
            "*=" => _core.mul(curVal, rhsVal),
            "/=" => _core.div(curVal, rhsVal),
            "%=" => _core.mod(curVal, rhsVal),
            "^=" => Math.Pow(to_float(curVal), to_float(rhsVal)),
            "//=" => (long)(to_float(curVal) / to_float(rhsVal)),
            _ => rhsVal
        };

        _env[varName] = newVal;
        return newVal;
    }

    /// <summary>
    ///     求值二元运算
    /// </summary>
    private object eval_binary(JlBinaryOp binary)
    {
        if (binary.@operator == "&&")
        {
            var left = eval_node(binary.left);
            return to_bool(left) ? eval_node(binary.right) : left;
        }

        if (binary.@operator == "||")
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
            "//" => (long)(to_float(leftVal) / to_float(rightVal)),
            "%" => _core.mod(leftVal, rightVal),
            "^" => Math.Pow(to_float(leftVal), to_float(rightVal)),
            "==" => _core.eq(leftVal, rightVal),
            "!=" => _core.ne(leftVal, rightVal),
            "<" => _core.lt(leftVal, rightVal),
            ">" => _core.gt(leftVal, rightVal),
            "<=" => _core.lte(leftVal, rightVal),
            ">=" => _core.gte(leftVal, rightVal),
            "===" => _core.eq(leftVal, rightVal),
            "!==" => _core.ne(leftVal, rightVal),
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值一元运算
    /// </summary>
    private object eval_unary(JlUnaryOp unary)
    {
        var operand = eval_node(unary.operand);

        return unary.@operator switch
        {
            "-" => _core.sub(0L, operand),
            "!" => _core.not(operand),
            _ => operand
        };
    }

    /// <summary>
    ///     求值三元表达式
    /// </summary>
    private object eval_ternary(JlTernary ternary)
    {
        var condition = eval_node(ternary.condition);
        return to_bool(condition) ? eval_node(ternary.then_branch) : eval_node(ternary.else_branch);
    }

    /// <summary>
    ///     求值管道运算符
    /// </summary>
    private object eval_pipe(JlPipe pipe)
    {
        if (pipe.is_reverse)
        {
            var rightVal = eval_node(pipe.right);
            return apply_function(pipe.left, rightVal);
        }

        var leftVal = eval_node(pipe.left);
        return apply_function(pipe.right, leftVal);
    }

    /// <summary>
    ///     求值范围表达式
    /// </summary>
    private object eval_range(JlRange range)
    {
        var startVal = to_int(eval_node(range.start));
        var endVal = to_int(eval_node(range.end));
        var stepVal = range.step is not null ? to_int(eval_node(range.step)) : 1L;

        var list = new List<object>();

        if (stepVal > 0)
        {
            for (var i = startVal; i <= endVal; i += stepVal)
            {
                list.Add(i);
            }
        }
        else if (stepVal < 0)
        {
            for (var i = startVal; i >= endVal; i += stepVal)
            {
                list.Add(i);
            }
        }

        return list;
    }

    /// <summary>
    ///     求值函数调用
    /// </summary>
    private object eval_call(JlCall call)
    {
        var funcName = extract_call_target(call.function);
        var argVals = call.arguments.Select(eval_node).ToArray();

        if (funcName == "println" || funcName == "print")
        {
            var output = string.Join("\t", argVals.Select(to_str));

            if (funcName == "println")
            {
                Console.WriteLine(output);
            }
            else
            {
                Console.Write(output);
            }

            return _core.unit();
        }

        if (funcName == "length" || funcName == "sizeof")
        {
            if (argVals.Length > 0)
            {
                return argVals[0] switch
                {
                    List<object> list => (long)list.Count,
                    string s => (long)s.Length,
                    Dictionary<string, object> dict => (long)dict.Count,
                    _ => 0L
                };
            }

            return 0L;
        }

        if (funcName == "string")
        {
            return string.Join("", argVals.Select(to_str));
        }

        if (funcName == "parse")
        {
            if (argVals.Length > 0 && argVals[0] is string s && long.TryParse(s, out var v))
            {
                return v;
            }

            return 0L;
        }

        if (funcName == "push!")
        {
            if (argVals.Length >= 2 && argVals[0] is List<object> list)
            {
                for (var i = 1; i < argVals.Length; i++)
                {
                    list.Add(argVals[i]);
                }

                return list;
            }

            return _core.unit();
        }

        if (funcName == "append!")
        {
            if (argVals.Length >= 2 && argVals[0] is List<object> list && argVals[1] is List<object> other)
            {
                list.AddRange(other);
                return list;
            }

            return _core.unit();
        }

        if (_env.TryGetValue(funcName, out var func))
        {
            if (func is BuiltinFunction builtin)
            {
                return builtin.invoke(argVals);
            }

            if (func is Func<object[], object> fn)
            {
                return fn(argVals);
            }
        }

        Console.WriteLine($"[Julia] 警告：未定义函数 {funcName}");
        return _core.unit();
    }

    /// <summary>
    ///     求值索引访问
    /// </summary>
    private object eval_index(JlIndex index)
    {
        var obj = eval_node(index.@object);

        if (index.indices.Count == 0)
        {
            return _core.unit();
        }

        var idxVal = eval_node(index.indices[0]);

        if (obj is List<object> list)
        {
            var idx = (int)to_int(idxVal) - 1;

            if (idx >= 0 && idx < list.Count)
            {
                return list[idx] ?? _core.unit();
            }

            return _core.unit();
        }

        if (obj is Dictionary<string, object> dict)
        {
            var key = to_str(idxVal);
            return dict.TryGetValue(key, out var val) ? val : _core.unit();
        }

        if (obj is string s)
        {
            var idx = (int)to_int(idxVal) - 1;

            if (idx >= 0 && idx < s.Length)
            {
                return s[idx].ToString();
            }

            return _core.unit();
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值字段访问
    /// </summary>
    private object eval_field_access(JlFieldAccess fieldAccess)
    {
        var obj = eval_node(fieldAccess.@object);

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(fieldAccess.field, out var val))
        {
            return val;
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 lambda 表达式
    /// </summary>
    private object eval_lambda(JlLambda lambda)
    {
        var paramNames = lambda.parameters
            .Select(p => p is JlIdentifier id ? id.name : p is JlParameter param ? param.name : "_")
            .ToArray();

        var captured = new Dictionary<string, object>(_env);

        return new BuiltinFunction("lambda", args =>
        {
            var localEnv = new Dictionary<string, object>(captured);

            for (var i = 0; i < paramNames.Length && i < args.Length; i++)
            {
                localEnv[paramNames[i]] = args[i];
            }

            var innerEval = new JuliaTreeEvaluator(_config, localEnv);
            return innerEval.eval_node(lambda.body);
        });
    }

    /// <summary>
    ///     求值标识符
    /// </summary>
    private object eval_identifier(JlIdentifier identifier)
    {
        if (_env.TryGetValue(identifier.name, out var val))
        {
            return val;
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值字面量
    /// </summary>
    private object eval_literal(JlLiteral literal)
    {
        return literal.kind switch
        {
            "number" when long.TryParse(literal.value, out var intVal) => intVal,
            "number" when double.TryParse(literal.value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var floatVal) => floatVal,
            "number" => 0L,
            "string" => literal.value,
            "char" => literal.value,
            "bool" when literal.value == "true" => true,
            "bool" when literal.value == "false" => false,
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值元组
    /// </summary>
    private object eval_tuple(JlTuple tuple)
    {
        return tuple.elements.Select(eval_node).ToList();
    }

    /// <summary>
    ///     求值数组
    /// </summary>
    private object eval_array(JlArray array)
    {
        return array.elements.Select(eval_node).ToList();
    }

    /// <summary>
    ///     求值字典
    /// </summary>
    private object eval_dict(JlDict dict)
    {
        var result = new Dictionary<string, object>();

        foreach (var (key, value) in dict.pairs)
        {
            var keyStr = to_str(eval_node(key));
            result[keyStr] = eval_node(value);
        }

        return result;
    }

    /// <summary>
    ///     求值推导式
    /// </summary>
    private object eval_comprehension(JlComprehension comprehension)
    {
        var result = new List<object>();

        if (comprehension.iterators.Count == 0)
        {
            return result;
        }

        var (iterator, iterable) = comprehension.iterators[0];
        var iterableVal = eval_node(iterable);
        var varName = extract_iterator_name(iterator);

        if (iterableVal is List<object> list)
        {
            foreach (var item in list)
            {
                _env[varName] = item;
                result.Add(eval_node(comprehension.expression));
            }
        }
        else if (iterableVal is long endVal)
        {
            for (var i = 1L; i <= endVal; i++)
            {
                _env[varName] = i;
                result.Add(eval_node(comprehension.expression));
            }
        }

        return result;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     提取参数名列表
    /// </summary>
    private static string[] extract_param_names(IReadOnlyList<JlAstNode> parameters)
    {
        var names = new List<string>();

        foreach (var param in parameters)
        {
            if (param is JlParameter p)
            {
                names.Add(p.name);
            }
            else if (param is JlIdentifier id)
            {
                names.Add(id.name);
            }
            else if (param is JlKeywordParameter kw)
            {
                names.Add(kw.name);
            }
        }

        return [.. names];
    }

    /// <summary>
    ///     提取迭代器变量名
    /// </summary>
    private string extract_iterator_name(JlAstNode iterator)
    {
        if (iterator is JlIdentifier id)
        {
            return id.name;
        }

        if (iterator is JlParameter param)
        {
            return param.name;
        }

        if (iterator is JlTuple tuple && tuple.elements.Count > 0)
        {
            return extract_iterator_name(tuple.elements[0]);
        }

        return "_";
    }

    /// <summary>
    ///     提取赋值目标变量名
    /// </summary>
    private static string extract_target_name(JlAstNode target)
    {
        if (target is JlIdentifier id)
        {
            return id.name;
        }

        return to_str_target(target);
    }

    /// <summary>
    ///     将目标节点转为字符串名称
    /// </summary>
    private static string to_str_target(JlAstNode target)
    {
        return target switch
        {
            JlIdentifier id => id.name,
            JlFieldAccess fa => $"{to_str_target(fa.@object)}.{fa.field}",
            JlIndex idx => $"{to_str_target(idx.@object)}[{idx.indices.Count}]",
            _ => "_"
        };
    }

    /// <summary>
    ///     提取函数调用目标名称
    /// </summary>
    private static string extract_call_target(JlAstNode func)
    {
        if (func is JlIdentifier id)
        {
            return id.name;
        }

        if (func is JlQualifiedAccess qa)
        {
            return $"{qa.module}.{qa.name}";
        }

        if (func is JlFieldAccess fa)
        {
            return $"{extract_call_target(fa.@object)}.{fa.field}";
        }

        return "_";
    }

    /// <summary>
    ///     将函数应用到参数
    /// </summary>
    private object apply_function(JlAstNode funcNode, object arg)
    {
        var funcName = extract_call_target(funcNode);

        if (_env.TryGetValue(funcName, out var func))
        {
            if (func is BuiltinFunction builtin)
            {
                return builtin.invoke([arg]);
            }

            if (func is Func<object[], object> fn)
            {
                return fn([arg]);
            }
        }

        Console.WriteLine($"[Julia] 警告：未定义函数 {funcName}");
        return _core.unit();
    }

    #endregion
}