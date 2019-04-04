using Nyar.VM.LegacyVM.Algebra.Core;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     Rust AST 求值器，遍历 Oak.Rust AST 节点并调用 CoreEvaluator 求值。
///     支持：fn 定义、let 绑定、if/else、while/loop/for、match、return/break/continue、
///     println!/print! 宏、二元/一元运算、范围表达式、闭包、数组/元组、
///     结构体（注册）、impl 块（注册）、use 声明（跳过）、类型注解（忽略）。
/// </summary>
public sealed class RustTreeEvaluator : UnifiedTreeEvaluator<RustAstNode>
{
    /// <summary>
    ///     最大循环迭代次数
    /// </summary>
    private const int _max_iterations = 10000;

    /// <summary>
    ///     创建 Rust AST 求值器
    /// </summary>
    /// <param name="config">语言运行时配置</param>
    /// <param name="env">初始变量环境</param>
    public RustTreeEvaluator(LanguageRuntimeConfig config, Dictionary<string, object> env) : base(config, env)
    {
    }

    /// <inheritdoc />
    public override object evaluate(RustAstNode ast)
    {
        if (ast is RustCrate crate)
        {
            return eval_crate(crate);
        }

        return eval_node(ast);
    }

    #region Crate

    /// <summary>
    ///     求值 Crate 根节点
    /// </summary>
    private object eval_crate(RustCrate crate)
    {
        object result = _core.unit();

        foreach (var item in crate.items)
        {
            result = eval_node(item);

            if (result is ReturnValue)
            {
                break;
            }
        }

        return _core.eval_with_return_unwrap(result);
    }

    #endregion

    #region 节点分发

    /// <summary>
    ///     根据节点类型分发求值
    /// </summary>
    private object eval_node(RustAstNode node)
    {
        return node switch
        {
            // 跳过的节点
            RustUseDecl => _core.unit(),
            RustTypeAlias => _core.unit(),
            RustModDecl => _core.unit(),
            RustTraitDef => _core.unit(),
            RustEnumDef => _core.unit(),
            RustTypeNode => _core.unit(),

            // 注册但不执行的节点
            RustStructDef structDef => eval_struct_def(structDef),
            RustImplDef implDef => eval_impl_def(implDef),

            // 语句
            RustExprStmt exprStmt => eval_node(exprStmt.expression),
            RustLetStmt letStmt => eval_let_stmt(letStmt),
            RustReturnStmt returnStmt => _core.@return(returnStmt.value is not null ? eval_node(returnStmt.value) : _core.unit()),
            RustBreakStmt => new BreakValue(),
            RustContinueStmt => new ContinueValue(),
            RustWhileStmt whileStmt => eval_while_stmt(whileStmt),
            RustLoopStmt loopStmt => eval_loop_stmt(loopStmt),
            RustForStmt forStmt => eval_for_stmt(forStmt),
            RustFunctionDef funcDef => eval_function_def(funcDef),

            // 表达式
            RustLiteral literal => eval_literal(literal),
            RustIdentifier identifier => eval_identifier(identifier),
            RustBinaryOp binaryOp => eval_binary_op(binaryOp),
            RustUnaryOp unaryOp => eval_unary_op(unaryOp),
            RustCall call => eval_call(call),
            RustMethodCall methodCall => eval_method_call(methodCall),
            RustFieldAccess fieldAccess => eval_field_access(fieldAccess),
            RustIndex index => eval_index(index),
            RustRange range => eval_range(range),
            RustClosure closure => eval_closure(closure),
            RustIfExpr ifExpr => eval_if_expr(ifExpr),
            RustMatchExpr matchExpr => eval_match_expr(matchExpr),
            RustArrayExpr arrayExpr => eval_array_expr(arrayExpr),
            RustTupleExpr tupleExpr => eval_tuple_expr(tupleExpr),
            RustBlockExpr blockExpr => eval_block_expr(blockExpr),
            RustMacroCall macroCall => eval_macro_call(macroCall),
            RustCast cast => eval_node(cast.expression),

            _ => _core.unit()
        };
    }

    #endregion

    #region 声明求值

    /// <summary>
    ///     求值函数定义
    /// </summary>
    private object eval_function_def(RustFunctionDef funcDef)
    {
        var paramNames = funcDef.parameters.Select(p => p.name).ToArray();
        var bodyObj = eval_block_as_body(funcDef.body);
        var lambda = _core.lambda(paramNames, bodyObj);
        _core.set_var(funcDef.name, lambda);

        _functions[funcDef.name] = args =>
        {
            for (var i = 0; i < paramNames.Length && i < args.Length; i++)
            {
                _core.set_var(paramNames[i], args[i]);
            }

            return _core.eval_with_return_unwrap(eval_node(funcDef.body));
        };

        return _core.unit();
    }

    /// <summary>
    ///     求值 let 绑定
    /// </summary>
    private object eval_let_stmt(RustLetStmt letStmt)
    {
        if (letStmt.initializer is not null)
        {
            var value = eval_node(letStmt.initializer);

            if (letStmt.pattern is RustIdentifier id)
            {
                return _core.set_var(id.name, value);
            }

            if (letStmt.pattern is RustTupleExpr tuple)
            {
                if (value is System.Collections.IList list)
                {
                    for (var i = 0; i < tuple.elements.Count && i < list.Count; i++)
                    {
                        if (tuple.elements[i] is RustIdentifier elemId)
                        {
                            _core.set_var(elemId.name, list[i] ?? _core.unit());
                        }
                    }
                }

                return _core.unit();
            }
        }
        else
        {
            if (letStmt.pattern is RustIdentifier id)
            {
                return _core.set_var(id.name, _core.unit());
            }
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值结构体定义（注册为字典模板）
    /// </summary>
    private object eval_struct_def(RustStructDef structDef)
    {
        var structDict = new Dictionary<string, object>();
        foreach (var field in structDef.fields)
        {
            structDict[field.name] = _core.unit();
        }

        _core.set_var(structDef.name, structDict);
        return _core.unit();
    }

    /// <summary>
    ///     求值 impl 块（注册方法到结构体）
    /// </summary>
    private object eval_impl_def(RustImplDef implDef)
    {
        var typeName = implDef.type is RustTypeNode typeNode ? typeNode.name : "?";

        foreach (var member in implDef.members)
        {
            if (member is RustFunctionDef funcDef)
            {
                var paramNames = funcDef.parameters.Select(p => p.name).ToArray();
                var bodyObj = eval_block_as_body(funcDef.body);
                var lambda = _core.lambda(paramNames, bodyObj);

                var existingObj = _core.var(typeName);
                if (existingObj is Dictionary<string, object> dict)
                {
                    dict[funcDef.name] = lambda;
                }
            }
        }

        return _core.unit();
    }

    #endregion

    #region 语句求值

    /// <summary>
    ///     求值 while 循环
    /// </summary>
    private object eval_while_stmt(RustWhileStmt whileStmt)
    {
        var loopCount = 0;
        object result = _core.unit();

        while (loopCount < _max_iterations)
        {
            var condition = eval_node(whileStmt.condition);
            if (!to_bool(condition))
            {
                break;
            }

            var bodyResult = eval_node(whileStmt.body);

            if (bodyResult is BreakValue)
            {
                break;
            }

            if (bodyResult is ReturnValue)
            {
                return bodyResult;
            }

            result = bodyResult;
            loopCount++;
        }

        if (loopCount >= _max_iterations)
        {
            Console.WriteLine($"[Rust] 警告：while 循环达到最大迭代次数 {_max_iterations}，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 loop 循环
    /// </summary>
    private object eval_loop_stmt(RustLoopStmt loopStmt)
    {
        var loopCount = 0;
        object result = _core.unit();

        while (loopCount < _max_iterations)
        {
            var bodyResult = eval_node(loopStmt.body);

            if (bodyResult is BreakValue)
            {
                break;
            }

            if (bodyResult is ReturnValue)
            {
                return bodyResult;
            }

            result = bodyResult;
            loopCount++;
        }

        if (loopCount >= _max_iterations)
        {
            Console.WriteLine($"[Rust] 警告：loop 循环达到最大迭代次数 {_max_iterations}，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 for 循环
    /// </summary>
    private object eval_for_stmt(RustForStmt forStmt)
    {
        var varName = forStmt.pattern is RustIdentifier id ? id.name : "_";
        var iterator = eval_node(forStmt.iterator);
        object result = _core.unit();

        if (iterator is System.Collections.IList list)
        {
            foreach (var item in list)
            {
                _core.set_var(varName, item ?? _core.unit());
                var bodyResult = eval_node(forStmt.body);

                if (bodyResult is BreakValue)
                {
                    break;
                }

                if (bodyResult is ReturnValue)
                {
                    return bodyResult;
                }

                result = bodyResult;
            }
        }
        else if (iterator is string strVal)
        {
            foreach (var ch in strVal)
            {
                _core.set_var(varName, _core.str_const(ch.ToString()));
                var bodyResult = eval_node(forStmt.body);

                if (bodyResult is BreakValue)
                {
                    break;
                }

                if (bodyResult is ReturnValue)
                {
                    return bodyResult;
                }

                result = bodyResult;
            }
        }

        return result;
    }

    #endregion

    #region 表达式求值

    /// <summary>
    ///     求值字面量
    /// </summary>
    private object eval_literal(RustLiteral literal)
    {
        return literal.kind switch
        {
            "number" => parse_number(literal.value),
            "string" => _core.str_const(literal.value),
            "char" => _core.str_const(literal.value),
            "bool" => literal.value == "true" ? _core.bool_const(true) : _core.bool_const(false),
            _ => _core.str_const(literal.value)
        };
    }

    /// <summary>
    ///     解析数字字面量
    /// </summary>
    private static object parse_number(string value)
    {
        var trimmed = value;
        while (trimmed.Length > 0 && char.IsLetter(trimmed[^1]))
        {
            trimmed = trimmed[..^1];
        }

        if (string.IsNullOrEmpty(trimmed))
        {
            return 0L;
        }

        if (trimmed.StartsWith("0x") || trimmed.StartsWith("0X"))
        {
            if (long.TryParse(trimmed[2..], System.Globalization.NumberStyles.HexNumber, null, out var hexVal))
            {
                return hexVal;
            }

            return 0L;
        }

        if (long.TryParse(trimmed, out var intVal))
        {
            return intVal;
        }

        if (double.TryParse(trimmed,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var doubleVal))
        {
            return doubleVal;
        }

        return 0L;
    }

    /// <summary>
    ///     求值标识符
    /// </summary>
    private object eval_identifier(RustIdentifier identifier)
    {
        if (identifier.name == "true")
        {
            return _core.bool_const(true);
        }

        if (identifier.name == "false")
        {
            return _core.bool_const(false);
        }

        return _core.var(identifier.name);
    }

    /// <summary>
    ///     求值二元运算
    /// </summary>
    private object eval_binary_op(RustBinaryOp binaryOp)
    {
        if (binaryOp.@operator is "=" or "+=" or "-=" or "*=" or "/=" or "%=")
        {
            return eval_assignment_op(binaryOp);
        }

        var left = eval_node(binaryOp.left);
        var right = eval_node(binaryOp.right);

        return binaryOp.@operator switch
        {
            "+" => _core.add(left, right),
            "-" => _core.sub(left, right),
            "*" => _core.mul(left, right),
            "/" => _core.div(left, right),
            "%" => _core.mod(left, right),
            "==" => _core.eq(left, right),
            "!=" => _core.ne(left, right),
            "<" => _core.lt(left, right),
            ">" => _core.gt(left, right),
            "<=" => _core.lte(left, right),
            ">=" => _core.gte(left, right),
            "&&" => _core.and(left, right),
            "||" => _core.or(left, right),
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值赋值运算
    /// </summary>
    private object eval_assignment_op(RustBinaryOp binaryOp)
    {
        var rightVal = eval_node(binaryOp.right);

        if (binaryOp.@operator == "=")
        {
            return assign_to(binaryOp.left, rightVal);
        }

        var currentVal = eval_node(binaryOp.left);
        var newVal = binaryOp.@operator switch
        {
            "+=" => _core.add(currentVal, rightVal),
            "-=" => _core.sub(currentVal, rightVal),
            "*=" => _core.mul(currentVal, rightVal),
            "/=" => _core.div(currentVal, rightVal),
            "%=" => _core.mod(currentVal, rightVal),
            _ => rightVal
        };

        return assign_to(binaryOp.left, newVal);
    }

    /// <summary>
    ///     赋值到目标
    /// </summary>
    private object assign_to(RustAstNode target, object value)
    {
        if (target is RustIdentifier id)
        {
            return _core.set_var(id.name, value);
        }

        if (target is RustFieldAccess fieldAccess)
        {
            var obj = eval_node(fieldAccess.@object);
            if (obj is Dictionary<string, object> dict)
            {
                dict[fieldAccess.field] = value;
            }

            return value;
        }

        if (target is RustIndex index)
        {
            var container = eval_node(index.@object);
            var idx = eval_node(index.index);

            if (container is System.Collections.IList list)
            {
                var i = (int)to_int(idx);
                if (i >= 0 && i < list.Count)
                {
                    list[i] = value;
                }
            }

            return value;
        }

        return value;
    }

    /// <summary>
    ///     求值一元运算
    /// </summary>
    private object eval_unary_op(RustUnaryOp unaryOp)
    {
        return unaryOp.@operator switch
        {
            "!" => _core.not(eval_node(unaryOp.operand)),
            "-" => _core.sub(0L, eval_node(unaryOp.operand)),
            "*" => eval_node(unaryOp.operand),
            "&" => eval_node(unaryOp.operand),
            _ => eval_node(unaryOp.operand)
        };
    }

    /// <summary>
    ///     求值函数调用
    /// </summary>
    private object eval_call(RustCall call)
    {
        var calleeVal = eval_node(call.function);
        var args = call.arguments.Select(a => eval_node(a)).ToArray();
        return _core.apply(calleeVal, args);
    }

    /// <summary>
    ///     求值方法调用
    /// </summary>
    private object eval_method_call(RustMethodCall methodCall)
    {
        var receiver = eval_node(methodCall.receiver);
        var args = methodCall.arguments.Select(a => eval_node(a)).ToArray();

        return methodCall.method switch
        {
            "push" => eval_array_push(receiver, args),
            "pop" => eval_array_pop(receiver),
            "len" => eval_len(receiver),
            "to_string" => _core.str_const(to_str(receiver)),
            "contains" => eval_contains(receiver, args),
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值字段访问
    /// </summary>
    private object eval_field_access(RustFieldAccess fieldAccess)
    {
        var obj = eval_node(fieldAccess.@object);

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(fieldAccess.field, out var val))
        {
            return val;
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值下标访问
    /// </summary>
    private object eval_index(RustIndex index)
    {
        var container = eval_node(index.@object);
        var idx = eval_node(index.index);

        if (container is System.Collections.IList list)
        {
            var i = (int)to_int(idx);
            if (i >= 0 && i < list.Count)
            {
                return list[i] ?? _core.unit();
            }

            return _core.unit();
        }

        if (container is string strVal)
        {
            var i = (int)to_int(idx);
            if (i >= 0 && i < strVal.Length)
            {
                return _core.str_const(strVal[i].ToString());
            }

            return _core.unit();
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值范围表达式
    /// </summary>
    private object eval_range(RustRange range)
    {
        var start = range.start is not null ? to_int(eval_node(range.start)) : 0;
        var end = range.end is not null ? to_int(eval_node(range.end)) : 0;
        var inclusiveEnd = range.inclusive ? 1 : 0;

        var list = new List<object>();
        for (var i = start; i < end + inclusiveEnd; i++)
        {
            list.Add(_core.int_const(i));
        }

        return list;
    }

    /// <summary>
    ///     求值闭包
    /// </summary>
    private object eval_closure(RustClosure closure)
    {
        var bodyObj = eval_node(closure.body);
        return _core.lambda([.. closure.parameters], bodyObj);
    }

    /// <summary>
    ///     求值 if 表达式
    /// </summary>
    private object eval_if_expr(RustIfExpr ifExpr)
    {
        var condition = eval_node(ifExpr.condition);
        var thenBody = eval_block_as_body(ifExpr.then_branch);
        var elseBody = ifExpr.else_branch is not null ? eval_block_as_body(ifExpr.else_branch) : _core.unit();
        return _core.@if(condition, thenBody, elseBody);
    }

    /// <summary>
    ///     求值 match 表达式
    /// </summary>
    private object eval_match_expr(RustMatchExpr matchExpr)
    {
        var scrutinee = eval_node(matchExpr.scrutinee);

        foreach (var arm in matchExpr.arms)
        {
            if (arm.pattern is RustIdentifier { name: "_" })
            {
                return eval_node(arm.body);
            }

            var patternVal = eval_node(arm.pattern);
            if (to_bool(_core.eq(scrutinee, patternVal)))
            {
                return eval_node(arm.body);
            }
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值数组表达式
    /// </summary>
    private object eval_array_expr(RustArrayExpr arrayExpr)
    {
        var list = new List<object>();
        foreach (var elem in arrayExpr.elements)
        {
            list.Add(eval_node(elem));
        }

        return list;
    }

    /// <summary>
    ///     求值元组表达式
    /// </summary>
    private object eval_tuple_expr(RustTupleExpr tupleExpr)
    {
        var list = new List<object>();
        foreach (var elem in tupleExpr.elements)
        {
            list.Add(eval_node(elem));
        }

        return list;
    }

    /// <summary>
    ///     求值块表达式
    /// </summary>
    private object eval_block_expr(RustBlockExpr blockExpr)
    {
        var stmts = new List<object>();

        foreach (var stmt in blockExpr.statements)
        {
            var result = eval_node(stmt);
            stmts.Add(result);

            if (result is ReturnValue)
            {
                break;
            }
        }

        if (stmts.Count > 0)
        {
            var last = stmts[^1];
            if (last is not BreakValue and not ContinueValue)
            {
                return last;
            }
        }

        return _core.block([.. stmts]);
    }

    /// <summary>
    ///     将块求值为函数体
    /// </summary>
    private object eval_block_as_body(RustAstNode body)
    {
        if (body is RustBlockExpr block)
        {
            var stmts = new List<object>();

            foreach (var stmt in block.statements)
            {
                var result = eval_node(stmt);
                stmts.Add(result);

                if (result is ReturnValue)
                {
                    break;
                }
            }

            return _core.block([.. stmts]);
        }

        return eval_node(body);
    }

    /// <summary>
    ///     求值宏调用
    /// </summary>
    private object eval_macro_call(RustMacroCall macroCall)
    {
        var name = macroCall.name;

        if (name == "println" || name == "print")
        {
            return eval_println_macro(macroCall.body, name == "println");
        }

        if (name == "format")
        {
            return eval_format_macro(macroCall.body);
        }

        if (name == "vec")
        {
            return eval_node(macroCall.body);
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 println!/print! 宏
    /// </summary>
    private object eval_println_macro(RustAstNode body, bool withNewline)
    {
        if (body is RustLiteral { kind: "string" } fmtLiteral)
        {
            var output = format_rust_string(fmtLiteral.value, []);
            if (withNewline)
            {
                Console.WriteLine(output);
            }
            else
            {
                Console.Write(output);
            }

            return _core.unit();
        }

        var val = eval_node(body);
        var strVal = to_str(val);
        if (withNewline)
        {
            Console.WriteLine(strVal);
        }
        else
        {
            Console.Write(strVal);
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 format! 宏
    /// </summary>
    private object eval_format_macro(RustAstNode body)
    {
        if (body is RustLiteral { kind: "string" } fmtLiteral)
        {
            var output = format_rust_string(fmtLiteral.value, []);
            return _core.str_const(output);
        }

        var val = eval_node(body);
        return _core.str_const(to_str(val));
    }

    /// <summary>
    ///     格式化 Rust 格式化字符串
    /// </summary>
    private static string format_rust_string(string fmt, object[] args)
    {
        var result = "";
        var argIdx = 0;
        var i = 0;

        while (i < fmt.Length)
        {
            if (fmt[i] == '{' && i + 1 < fmt.Length)
            {
                if (fmt[i + 1] == '{')
                {
                    result += '{';
                    i += 2;
                    continue;
                }

                var j = i + 1;
                while (j < fmt.Length && fmt[j] != '}')
                {
                    j++;
                }

                if (j < fmt.Length)
                {
                    var spec = fmt[(i + 1)..j].Trim();
                    object argVal;
                    if (string.IsNullOrEmpty(spec) && argIdx < args.Length)
                    {
                        argVal = args[argIdx++];
                    }
                    else if (int.TryParse(spec, out var specIdx) && specIdx < args.Length)
                    {
                        argVal = args[specIdx];
                    }
                    else
                    {
                        argVal = "";
                    }

                    result += to_str(argVal);
                    i = j + 1;
                }
                else
                {
                    result += fmt[i];
                    i++;
                }
            }
            else if (fmt[i] == '}' && i + 1 < fmt.Length && fmt[i + 1] == '}')
            {
                result += '}';
                i += 2;
            }
            else
            {
                result += fmt[i];
                i++;
            }
        }

        return result;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     数组 push 方法
    /// </summary>
    private object eval_array_push(object receiver, object[] args)
    {
        if (receiver is not System.Collections.IList list)
        {
            return _core.unit();
        }

        foreach (var arg in args)
        {
            list.Add(arg);
        }

        return _core.unit();
    }

    /// <summary>
    ///     数组 pop 方法
    /// </summary>
    private object eval_array_pop(object receiver)
    {
        if (receiver is not System.Collections.IList list || list.Count == 0)
        {
            return _core.unit();
        }

        var last = list[^1];
        list.RemoveAt(list.Count - 1);
        return last ?? _core.unit();
    }

    /// <summary>
    ///     len 方法
    /// </summary>
    private object eval_len(object receiver)
    {
        if (receiver is string strVal)
        {
            return _core.int_const(strVal.Length);
        }

        if (receiver is System.Collections.IList list)
        {
            return _core.int_const(list.Count);
        }

        return _core.int_const(0);
    }

    /// <summary>
    ///     contains 方法
    /// </summary>
    private object eval_contains(object receiver, object[] args)
    {
        if (args.Length == 0)
        {
            return _core.bool_const(false);
        }

        if (receiver is string strVal)
        {
            return _core.bool_const(strVal.Contains(to_str(args[0])));
        }

        if (receiver is System.Collections.IList list)
        {
            foreach (var item in list)
            {
                if (to_bool(_core.eq(item, args[0])))
                {
                    return _core.bool_const(true);
                }
            }

            return _core.bool_const(false);
        }

        return _core.bool_const(false);
    }

    #endregion
}