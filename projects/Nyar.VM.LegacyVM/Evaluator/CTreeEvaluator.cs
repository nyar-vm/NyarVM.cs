using Nyar.VM.LegacyVM.Algebra.Core;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     C 语言 AST 求值器，遍历 Oak.C AST 节点并调用 CoreEvaluator 求值。
/// </summary>
public sealed class CTreeEvaluator : UnifiedTreeEvaluator<CAstNode>
{
    /// <summary>
    ///     最大循环迭代次数
    /// </summary>
    private const int _max_iterations = 10000;

    /// <summary>
    ///     创建 C 语言 AST 求值器
    /// </summary>
    /// <param name="config">语言运行时配置</param>
    /// <param name="env">初始变量环境</param>
    public CTreeEvaluator(LanguageRuntimeConfig config, Dictionary<string, object> env) : base(config, env)
    {
    }

    /// <inheritdoc />
    public override object evaluate(CAstNode ast)
    {
        if (ast is CTranslationUnit unit)
        {
            return eval_translation_unit(unit);
        }

        return eval_node(ast);
    }

    #region 翻译单元

    /// <summary>
    ///     求值翻译单元
    /// </summary>
    private object eval_translation_unit(CTranslationUnit unit)
    {
        object result = _core.unit();

        foreach (var decl in unit.declarations)
        {
            if (decl is CFunctionDef funcDef && funcDef.name == "main")
            {
                eval_node(decl);
                var mainFunc = _core.var("main");
                result = _core.apply(mainFunc, []);
                result = _core.eval_with_return_unwrap(result);
            }
            else
            {
                result = eval_node(decl);
            }
        }

        return _core.eval_with_return_unwrap(result);
    }

    #endregion

    #region 节点分发

    /// <summary>
    ///     根据节点类型分发求值
    /// </summary>
    private object eval_node(CAstNode node)
    {
        return node switch
        {
            // 声明
            CVarDecl varDecl => eval_var_decl(varDecl),
            CFunctionDef funcDef => eval_function_def(funcDef),
            CStructDef structDef => eval_struct_def(structDef),
            CTypedef typedef => eval_typedef(typedef),
            CTypeNode => _core.unit(),

            // 语句
            CExprStmt exprStmt => eval_node(exprStmt.expression),
            CCompound compound => eval_compound(compound),
            CIf ifStmt => eval_if(ifStmt),
            CWhile whileStmt => eval_while(whileStmt),
            CDoWhile doWhileStmt => eval_do_while(doWhileStmt),
            CFor forStmt => eval_for(forStmt),
            CSwitch switchStmt => eval_switch(switchStmt),
            CReturn returnStmt => _core.@return(returnStmt.value is not null ? eval_node(returnStmt.value) : _core.unit()),
            CBreak => new BreakValue(),
            CContinue => new ContinueValue(),
            CGoto => _core.unit(),
            CLabel labelStmt => eval_node(labelStmt.statement),

            // 表达式
            CLiteral literal => eval_literal(literal),
            CIdentifier identifier => _core.var(identifier.name),
            CBinaryOp binaryOp => eval_binary_op(binaryOp),
            CUnaryOp unaryOp => eval_unary_op(unaryOp),
            CTernaryOp ternaryOp => eval_ternary_op(ternaryOp),
            CCall call => eval_call(call),
            CMemberAccess memberAccess => eval_member_access(memberAccess),
            CSubscript subscript => eval_subscript(subscript),
            CCast cast => eval_node(cast.expression),
            CSizeOf sizeOf => eval_sizeof(sizeOf),
            CInitList initList => eval_init_list(initList),

            _ => _core.unit()
        };
    }

    #endregion

    #region 声明求值

    /// <summary>
    ///     求值变量声明
    /// </summary>
    private object eval_var_decl(CVarDecl varDecl)
    {
        // 数组声明
        if (varDecl.initializer is CInitList initList)
        {
            var arr = eval_init_list(initList);
            return _core.set_var(varDecl.name, arr);
        }

        if (varDecl.initializer is not null)
        {
            var value = eval_node(varDecl.initializer);
            return _core.set_var(varDecl.name, value);
        }

        return _core.set_var(varDecl.name, _core.int_const(0));
    }

    /// <summary>
    ///     求值函数定义
    /// </summary>
    private object eval_function_def(CFunctionDef funcDef)
    {
        var paramNames = funcDef.parameters.Select(p => p.name).ToArray();
        var bodyObj = eval_node(funcDef.body);
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
    ///     求值结构体定义
    /// </summary>
    private object eval_struct_def(CStructDef structDef)
    {
        var structDict = new Dictionary<string, object>();

        foreach (var field in structDef.fields)
        {
            structDict[field.name] = _core.int_const(0);
        }

        if (structDef.name is not null)
        {
            _core.set_var(structDef.name, structDict);
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 typedef
    /// </summary>
    private object eval_typedef(CTypedef typedef)
    {
        // typedef 简单记录名称映射
        _core.set_var(typedef.name, _core.str_const($"[typedef:{typedef.type}]"));
        return _core.unit();
    }

    #endregion

    #region 语句求值

    /// <summary>
    ///     求值复合语句
    /// </summary>
    private object eval_compound(CCompound compound)
    {
        var stmts = new List<object>();

        foreach (var stmt in compound.statements)
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

    /// <summary>
    ///     求值 if 语句
    /// </summary>
    private object eval_if(CIf ifStmt)
    {
        var condition = eval_node(ifStmt.condition);
        var thenBody = eval_node(ifStmt.then_body);
        var elseBody = ifStmt.else_body is not null ? eval_node(ifStmt.else_body) : _core.unit();
        return _core.@if(condition, thenBody, elseBody);
    }

    /// <summary>
    ///     求值 while 循环
    /// </summary>
    private object eval_while(CWhile whileStmt)
    {
        var loopCount = 0;
        object result = _core.unit();

        while (loopCount < _max_iterations && to_bool(eval_node(whileStmt.condition)))
        {
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
            Console.WriteLine($"[C] 警告：while 循环达到最大迭代次数 {_max_iterations}，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 do-while 循环
    /// </summary>
    private object eval_do_while(CDoWhile doWhileStmt)
    {
        var loopCount = 0;
        object result = _core.unit();

        do
        {
            var bodyResult = eval_node(doWhileStmt.body);

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
        } while (loopCount < _max_iterations && to_bool(eval_node(doWhileStmt.condition)));

        return result;
    }

    /// <summary>
    ///     求值 for 循环
    /// </summary>
    private object eval_for(CFor forStmt)
    {
        if (forStmt.init is not null)
        {
            eval_node(forStmt.init);
        }

        var loopCount = 0;
        object result = _core.unit();

        while (loopCount < _max_iterations)
        {
            if (forStmt.condition is not null)
            {
                var condVal = eval_node(forStmt.condition);
                if (!to_bool(condVal))
                {
                    break;
                }
            }

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

            if (forStmt.increment is not null)
            {
                eval_node(forStmt.increment);
            }

            loopCount++;
        }

        if (loopCount >= _max_iterations)
        {
            Console.WriteLine($"[C] 警告：for 循环达到最大迭代次数 {_max_iterations}，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 switch 语句
    /// </summary>
    private object eval_switch(CSwitch switchStmt)
    {
        var switchVal = eval_node(switchStmt.expression);
        var matched = false;
        object result = _core.unit();

        foreach (var caseNode in switchStmt.cases.OfType<CCase>())
        {
            if (!matched)
            {
                if (caseNode.value is null)
                {
                    // default
                    matched = true;
                }
                else
                {
                    var caseVal = eval_node(caseNode.value);
                    if (to_bool(_core.eq(switchVal, caseVal)))
                    {
                        matched = true;
                    }
                }
            }

            if (matched)
            {
                foreach (var stmt in caseNode.body)
                {
                    var stmtResult = eval_node(stmt);

                    if (stmtResult is BreakValue)
                    {
                        return result;
                    }

                    if (stmtResult is ReturnValue)
                    {
                        return stmtResult;
                    }

                    result = stmtResult;
                }
            }
        }

        return result;
    }

    #endregion

    #region 表达式求值

    /// <summary>
    ///     求值字面量
    /// </summary>
    private object eval_literal(CLiteral literal)
    {
        return literal.kind switch
        {
            "int" => long.TryParse(literal.value, out var intVal) ? intVal : 0L,
            "float" => double.TryParse(literal.value.TrimEnd('f', 'F'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var floatVal)
                ? floatVal
                : 0.0,
            "string" => _core.str_const(literal.value),
            "char" => literal.value.Length > 0 ? (long)literal.value[0] : 0L,
            "bool" => literal.value == "true" ? _core.bool_const(true) : _core.bool_const(false),
            "null" => _core.unit(),
            _ => _core.str_const(literal.value)
        };
    }

    /// <summary>
    ///     求值二元运算
    /// </summary>
    private object eval_binary_op(CBinaryOp binaryOp)
    {
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
            "&" => _core.int_const(CoreHelpers.to_i64(left) & CoreHelpers.to_i64(right)),
            "|" => _core.int_const(CoreHelpers.to_i64(left) | CoreHelpers.to_i64(right)),
            "^" => _core.int_const(CoreHelpers.to_i64(left) ^ CoreHelpers.to_i64(right)),
            "<<" => _core.int_const(CoreHelpers.to_i64(left) << (int)CoreHelpers.to_i64(right)),
            ">>" => _core.int_const(CoreHelpers.to_i64(left) >> (int)CoreHelpers.to_i64(right)),
            "=" => assign_to(binaryOp.left, right),
            "+=" => _core.set_var(get_identifier_name(binaryOp.left), _core.add(left, right)),
            "-=" => _core.set_var(get_identifier_name(binaryOp.left), _core.sub(left, right)),
            "*=" => _core.set_var(get_identifier_name(binaryOp.left), _core.mul(left, right)),
            "/=" => _core.set_var(get_identifier_name(binaryOp.left), _core.div(left, right)),
            "%=" => _core.set_var(get_identifier_name(binaryOp.left), _core.mod(left, right)),
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值一元运算
    /// </summary>
    private object eval_unary_op(CUnaryOp unaryOp)
    {
        return unaryOp.@operator switch
        {
            "!" => _core.not(eval_node(unaryOp.operand)),
            "-" => _core.sub(0L, eval_node(unaryOp.operand)),
            "+" => eval_node(unaryOp.operand),
            "*" => eval_node(unaryOp.operand),
            "&" => _core.str_const($"&{get_identifier_name(unaryOp.operand)}"),
            "++" => eval_prefix_increment(unaryOp.operand),
            "--" => eval_prefix_decrement(unaryOp.operand),
            _ => eval_node(unaryOp.operand)
        };
    }

    /// <summary>
    ///     前缀自增
    /// </summary>
    private object eval_prefix_increment(CAstNode operand)
    {
        var name = get_identifier_name(operand);
        var curVal = _core.var(name);
        var newVal = _core.add(curVal, 1L);
        _core.set_var(name, newVal);
        return newVal;
    }

    /// <summary>
    ///     前缀自减
    /// </summary>
    private object eval_prefix_decrement(CAstNode operand)
    {
        var name = get_identifier_name(operand);
        var curVal = _core.var(name);
        var newVal = _core.sub(curVal, 1L);
        _core.set_var(name, newVal);
        return newVal;
    }

    /// <summary>
    ///     求值三元运算符
    /// </summary>
    private object eval_ternary_op(CTernaryOp ternaryOp)
    {
        var condition = eval_node(ternaryOp.condition);
        return to_bool(condition) ? eval_node(ternaryOp.then_expr) : eval_node(ternaryOp.else_expr);
    }

    /// <summary>
    ///     求值函数调用
    /// </summary>
    private object eval_call(CCall call)
    {
        // 处理 printf
        if (call.function is CIdentifier { name: "printf" })
        {
            var args = call.arguments.Select(a => eval_node(a)).ToArray();
            if (args.Length == 0)
            {
                return _core.unit();
            }

            var fmt = to_str(args[0]);
            var fmtArgs = new List<string>();
            for (var k = 1; k < args.Length; k++)
            {
                fmtArgs.Add(to_str(args[k]));
            }

            var output = format_printf(fmt, fmtArgs);
            Console.WriteLine(output);
            return _core.str_const(output);
        }

        var funcVal = eval_node(call.function);
        var argVals = call.arguments.Select(a => eval_node(a)).ToArray();
        return _core.apply(funcVal, argVals);
    }

    /// <summary>
    ///     求值成员访问
    /// </summary>
    private object eval_member_access(CMemberAccess memberAccess)
    {
        var obj = eval_node(memberAccess.@object);

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(memberAccess.member, out var val))
        {
            return val;
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值下标访问
    /// </summary>
    private object eval_subscript(CSubscript subscript)
    {
        var container = eval_node(subscript.@object);
        var index = eval_node(subscript.index);

        if (container is System.Collections.IList list)
        {
            var idx = (int)CoreHelpers.to_i64(index);
            if (idx >= 0 && idx < list.Count)
            {
                return list[idx] ?? _core.unit();
            }

            return _core.unit();
        }

        if (container is string strVal)
        {
            var idx = (int)CoreHelpers.to_i64(index);
            if (idx >= 0 && idx < strVal.Length)
            {
                return _core.str_const(strVal[idx].ToString());
            }

            return _core.unit();
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 sizeof 表达式
    /// </summary>
    private object eval_sizeof(CSizeOf sizeOf)
    {
        if (sizeOf.operand is CTypeNode typeNode)
        {
            return _core.int_const(sizeof_type(typeNode.name));
        }

        return _core.int_const(4);
    }

    /// <summary>
    ///     求值初始化列表
    /// </summary>
    private object eval_init_list(CInitList initList)
    {
        var result = new List<object>();
        foreach (var elem in initList.elements)
        {
            result.Add(eval_node(elem));
        }

        return result;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     赋值到目标
    /// </summary>
    private object assign_to(CAstNode target, object value)
    {
        if (target is CIdentifier id)
        {
            return _core.set_var(id.name, value);
        }

        if (target is CSubscript subscript)
        {
            var container = eval_node(subscript.@object);
            var index = eval_node(subscript.index);

            if (container is System.Collections.IList list)
            {
                var idx = (int)CoreHelpers.to_i64(index);
                if (idx >= 0 && idx < list.Count)
                {
                    list[idx] = value;
                }

                return value;
            }
        }

        if (target is CMemberAccess memberAccess)
        {
            var obj = eval_node(memberAccess.@object);
            if (obj is Dictionary<string, object> dict)
            {
                dict[memberAccess.member] = value;
                return value;
            }
        }

        return value;
    }

    /// <summary>
    ///     获取标识符名称
    /// </summary>
    private static string get_identifier_name(CAstNode node)
    {
        if (node is CIdentifier id)
        {
            return id.name;
        }

        return "";
    }

    /// <summary>
    ///     sizeof 计算
    /// </summary>
    private static long sizeof_type(string typeName)
    {
        return typeName.TrimEnd('*') switch
        {
            "char" => 1,
            "short" => 2,
            "int" => 4,
            "long" => 8,
            "float" => 4,
            "double" => 8,
            "void" => 0,
            _ => 4
        };
    }

    /// <summary>
    ///     格式化 printf 输出
    /// </summary>
    private static string format_printf(string fmt, List<string> args)
    {
        var result = "";
        var argIdx = 0;
        var i = 0;

        while (i < fmt.Length)
        {
            if (fmt[i] == '%' && i + 1 < fmt.Length)
            {
                i++;
                var specifier = fmt[i];

                if (specifier == '%')
                {
                    result += '%';
                }
                else if (argIdx < args.Count)
                {
                    result += args[argIdx];
                    argIdx++;
                }
                else
                {
                    result += $"%{specifier}";
                }
            }
            else if (fmt[i] == '\\' && i + 1 < fmt.Length)
            {
                i++;
                result += fmt[i] switch
                {
                    'n' => Environment.NewLine,
                    't' => "\t",
                    'r' => "\r",
                    '\\' => "\\",
                    _ => $"\\{fmt[i]}"
                };
            }
            else
            {
                result += fmt[i];
            }

            i++;
        }

        return result;
    }

    #endregion
}