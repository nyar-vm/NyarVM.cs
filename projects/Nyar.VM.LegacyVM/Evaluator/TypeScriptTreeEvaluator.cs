using Nyar.VM.LegacyVM.Algebra;
using Std.Data.Text.Typescript.AST;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     TypeScript / JavaScript AST 求值器，遍历 Oak.Typescript AST 节点并调用 CoreEvaluator 求值。
///     JavaScript 与 TypeScript 共享相同的 AST 结构，因此统一使用此求值器。
/// </summary>
public sealed class TypeScriptTreeEvaluator : UnifiedTreeEvaluator<TsAstNode>
{
    /// <summary>
    ///     最大循环迭代次数
    /// </summary>
    private const int _max_iterations = 10000;

    /// <summary>
    ///     创建 TypeScript AST 求值器
    /// </summary>
    /// <param name="config">语言运行时配置</param>
    /// <param name="env">初始变量环境</param>
    public TypeScriptTreeEvaluator(LanguageRuntimeConfig config, Dictionary<string, object> env) : base(config, env)
    {
    }

    /// <inheritdoc />
    public override object evaluate(TsAstNode ast)
    {
        if (ast is TsCompilationUnit unit)
        {
            return eval_compilation_unit(unit);
        }

        return eval_node(ast);
    }

    #region 编译单元

    /// <summary>
    ///     求值编译单元
    /// </summary>
    private object eval_compilation_unit(TsCompilationUnit unit)
    {
        object result = _core.unit();

        foreach (var decl in unit.declarations)
        {
            result = eval_node(decl);

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
    private object eval_node(TsAstNode node)
    {
        return node switch
        {
            // 跳过类型-only 声明
            TsImportDecl => _core.unit(),
            TsInterfaceDecl => _core.unit(),
            TsTypeAliasDecl => _core.unit(),
            TsNamespaceDecl => _core.unit(),

            // 声明
            TsVariableDecl varDecl => eval_variable_decl(varDecl),
            TsFunctionDecl funcDecl => eval_function_decl(funcDecl),
            TsEnumDecl enumDecl => eval_enum_decl(enumDecl),
            TsClassDecl classDecl => eval_class_decl(classDecl),
            TsExportDecl exportDecl => eval_node(exportDecl.value),

            // 语句
            TsExprStmt exprStmt => eval_node(exprStmt.expression),
            TsBlockStmt blockStmt => eval_block(blockStmt),
            TsIfStmt ifStmt => eval_if_stmt(ifStmt),
            TsForStmt forStmt => eval_for_stmt(forStmt),
            TsForOfStmt forOfStmt => eval_for_of_stmt(forOfStmt),
            TsForInStmt forInStmt => eval_for_in_stmt(forInStmt),
            TsWhileStmt whileStmt => eval_while_stmt(whileStmt),
            TsDoWhileStmt doWhileStmt => eval_do_while_stmt(doWhileStmt),
            TsReturnStmt returnStmt => _core.@return(returnStmt.value is not null ? eval_node(returnStmt.value) : _core.unit()),
            TsBreakStmt => new BreakValue(),
            TsContinueStmt => new ContinueValue(),
            TsSwitchStmt switchStmt => eval_switch_stmt(switchStmt),
            TsTryStmt tryStmt => eval_try_stmt(tryStmt),
            TsThrowStmt throwStmt => eval_throw_stmt(throwStmt),
            TsEmptyStmt => _core.unit(),
            TsDebuggerStmt => _core.unit(),
            TsLabeledStmt labeledStmt => eval_node(labeledStmt.statement),

            // 表达式
            TsLiteral literal => eval_literal(literal),
            TsIdentifier identifier => eval_identifier(identifier),
            TsBinaryExpr binaryExpr => eval_binary_expr(binaryExpr),
            TsUnaryExpr unaryExpr => eval_unary_expr(unaryExpr),
            TsAssignmentExpr assignExpr => eval_assignment_expr(assignExpr),
            TsConditionalExpr condExpr => eval_conditional_expr(condExpr),
            TsCallExpr callExpr => eval_call_expr(callExpr),
            TsPropertyAccess propAccess => eval_property_access(propAccess),
            TsElementAccess elemAccess => eval_element_access(elemAccess),
            TsArrayLiteral arrayLit => eval_array_literal(arrayLit),
            TsObjectLiteral objLit => eval_object_literal(objLit),
            TsArrowFunctionExpr arrowFunc => eval_arrow_function(arrowFunc),
            TsFunctionExpr funcExpr => eval_function_expr(funcExpr),
            TsNewExpr newExpr => eval_new_expr(newExpr),
            TsSpreadElement spreadEl => eval_node(spreadEl.argument),
            TsTypeofExpr typeofExpr => eval_typeof_expr(typeofExpr),
            TsThisExpr => _core.unit(),
            TsSuperExpr => _core.unit(),
            TsInstanceofExpr => _core.bool_const(false),
            TsYieldExpr => _core.unit(),

            // 参数和属性（不会作为独立节点求值，但提供兜底）
            TsParameter => _core.unit(),
            TsProperty => _core.unit(),

            _ => _core.unit()
        };
    }

    #endregion

    #region 声明求值

    /// <summary>
    ///     求值变量声明
    /// </summary>
    private object eval_variable_decl(TsVariableDecl decl)
    {
        if (decl.initializer is not null)
        {
            var value = eval_node(decl.initializer);
            return _core.set_var(decl.name, value);
        }

        return _core.set_var(decl.name, _core.unit());
    }

    /// <summary>
    ///     求值函数声明
    /// </summary>
    private object eval_function_decl(TsFunctionDecl decl)
    {
        var paramNames = decl.parameters.Select(p => p.name.TrimStart('.')).ToArray();
        var bodyObj = eval_block_as_body(decl.body);
        var lambda = _core.lambda(paramNames, bodyObj);
        _core.set_var(decl.name, lambda);

        _functions[decl.name] = args =>
        {
            for (var i = 0; i < paramNames.Length && i < args.Length; i++)
            {
                _core.set_var(paramNames[i], args[i]);
            }

            return _core.eval_with_return_unwrap(eval_node(decl.body));
        };

        return _core.unit();
    }

    /// <summary>
    ///     求值枚举声明
    /// </summary>
    private object eval_enum_decl(TsEnumDecl decl)
    {
        var enumDict = new Dictionary<string, object>();
        long nextValue = 0;

        foreach (var member in decl.members)
        {
            if (member.initializer is not null)
            {
                var val = eval_node(member.initializer);
                nextValue = to_int(val) + 1;
                enumDict[member.name] = val;
            }
            else
            {
                enumDict[member.name] = _core.int_const(nextValue);
                nextValue++;
            }
        }

        _core.set_var(decl.name, enumDict);

        foreach (var kvp in enumDict)
        {
            _core.set_var($"{decl.name}.{kvp.Key}", kvp.Value);
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值类声明
    /// </summary>
    private object eval_class_decl(TsClassDecl decl)
    {
        var classDict = new Dictionary<string, object>();

        foreach (var member in decl.members)
        {
            if (member is TsFunctionDecl method)
            {
                var paramNames = method.parameters.Select(p => p.name.TrimStart('.')).ToArray();
                var bodyObj = eval_block_as_body(method.body);
                var lambda = _core.lambda(paramNames, bodyObj);
                classDict[method.name] = lambda;
            }
            else if (member is TsVariableDecl field)
            {
                var value = field.initializer is not null ? eval_node(field.initializer) : _core.unit();
                classDict[field.name] = value;
            }
        }

        _core.set_var(decl.name, classDict);
        return _core.unit();
    }

    #endregion

    #region 语句求值

    /// <summary>
    ///     求值块语句
    /// </summary>
    private object eval_block(TsBlockStmt block)
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

    /// <summary>
    ///     将块语句求值为函数体
    /// </summary>
    private object eval_block_as_body(TsAstNode body)
    {
        if (body is TsBlockStmt block)
        {
            return eval_block(block);
        }

        return eval_node(body);
    }

    /// <summary>
    ///     求值 if 语句
    /// </summary>
    private object eval_if_stmt(TsIfStmt ifStmt)
    {
        var condition = eval_node(ifStmt.condition);
        var thenBody = eval_block_as_body(ifStmt.then_block);
        var elseBody = ifStmt.else_block is not null ? eval_block_as_body(ifStmt.else_block) : _core.unit();
        return _core.@if(condition, thenBody, elseBody);
    }

    /// <summary>
    ///     求值 for 语句
    /// </summary>
    private object eval_for_stmt(TsForStmt forStmt)
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

            var bodyResult = eval_block_as_body(forStmt.body);

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
            Console.WriteLine($"[JavaScript] 警告：for 循环达到最大迭代次数 {_max_iterations}，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 for-of 语句
    /// </summary>
    private object eval_for_of_stmt(TsForOfStmt forOfStmt)
    {
        var varName = forOfStmt.left is TsVariableDecl varDecl ? varDecl.name : "?";
        var iterable = eval_node(forOfStmt.right);
        object result = _core.unit();

        if (iterable is System.Collections.IList list)
        {
            foreach (var item in list)
            {
                _core.set_var(varName, item ?? _core.unit());
                var bodyResult = eval_block_as_body(forOfStmt.body);

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
        else if (iterable is string strVal)
        {
            foreach (var ch in strVal)
            {
                _core.set_var(varName, _core.str_const(ch.ToString()));
                var bodyResult = eval_block_as_body(forOfStmt.body);

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

    /// <summary>
    ///     求值 for-in 语句
    /// </summary>
    private object eval_for_in_stmt(TsForInStmt forInStmt)
    {
        var varName = forInStmt.left is TsVariableDecl varDecl ? varDecl.name : "?";
        var obj = eval_node(forInStmt.right);
        object result = _core.unit();

        if (obj is Dictionary<string, object> dict)
        {
            foreach (var key in dict.Keys)
            {
                _core.set_var(varName, _core.str_const(key));
                var bodyResult = eval_block_as_body(forInStmt.body);

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

    /// <summary>
    ///     求值 while 语句
    /// </summary>
    private object eval_while_stmt(TsWhileStmt whileStmt)
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

            var bodyResult = eval_block_as_body(whileStmt.body);

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
            Console.WriteLine($"[JavaScript] 警告：while 循环达到最大迭代次数 {_max_iterations}，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 do-while 语句
    /// </summary>
    private object eval_do_while_stmt(TsDoWhileStmt doWhileStmt)
    {
        var loopCount = 0;
        object result = _core.unit();

        do
        {
            var bodyResult = eval_block_as_body(doWhileStmt.body);

            if (bodyResult is BreakValue)
            {
                break;
            }

            if (bodyResult is ReturnValue)
            {
                return bodyResult;
            }

            result = bodyResult;

            var condition = eval_node(doWhileStmt.condition);
            if (!to_bool(condition))
            {
                break;
            }

            loopCount++;
        } while (loopCount < _max_iterations);

        return result;
    }

    /// <summary>
    ///     求值 switch 语句
    /// </summary>
    private object eval_switch_stmt(TsSwitchStmt switchStmt)
    {
        var switchVal = eval_node(switchStmt.expression);
        var matched = false;
        object result = _core.unit();

        foreach (var switchCase in switchStmt.cases)
        {
            if (!matched)
            {
                if (switchCase.test is null)
                {
                    matched = true;
                }
                else
                {
                    var caseVal = eval_node(switchCase.test);
                    if (to_bool(_core.eq(switchVal, caseVal)))
                    {
                        matched = true;
                    }
                }
            }

            if (matched)
            {
                foreach (var stmt in switchCase.statements)
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

    /// <summary>
    ///     求值 try 语句
    /// </summary>
    private object eval_try_stmt(TsTryStmt tryStmt)
    {
        try
        {
            return eval_node(tryStmt.block);
        }
        catch (Exception ex)
        {
            if (tryStmt.catch_clause is not null)
            {
                if (tryStmt.catch_clause.parameter_name is not null)
                {
                    _core.set_var(tryStmt.catch_clause.parameter_name, ex.Message);
                }

                return eval_node(tryStmt.catch_clause.block);
            }

            return _core.unit();
        }
        finally
        {
            if (tryStmt.finally_block is not null)
            {
                eval_node(tryStmt.finally_block);
            }
        }
    }

    /// <summary>
    ///     求值 throw 语句
    /// </summary>
    private object eval_throw_stmt(TsThrowStmt throwStmt)
    {
        var value = eval_node(throwStmt.value);
        throw new Exception(to_str(value));
    }

    #endregion

    #region 表达式求值

    /// <summary>
    ///     求值字面量
    /// </summary>
    private object eval_literal(TsLiteral literal)
    {
        return literal.kind switch
        {
            "number" => parse_number(literal.value),
            "bigint" => parse_number(literal.value),
            "string" => _core.str_const(literal.value),
            "template" => _core.str_const(literal.value),
            "true" => _core.bool_const(true),
            "false" => _core.bool_const(false),
            "null" => _core.unit(),
            _ => _core.str_const(literal.value)
        };
    }

    /// <summary>
    ///     解析数字字面量
    /// </summary>
    private static object parse_number(string value)
    {
        if (long.TryParse(value, out var intVal))
        {
            return intVal;
        }

        if (double.TryParse(value,
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
    private object eval_identifier(TsIdentifier identifier)
    {
        if (identifier.name == "undefined" || identifier.name == "null")
        {
            return _core.unit();
        }

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
    ///     求值二元表达式
    /// </summary>
    private object eval_binary_expr(TsBinaryExpr expr)
    {
        var left = eval_node(expr.left);
        var right = eval_node(expr.right);

        return expr.@operator switch
        {
            "+" => _core.add(left, right),
            "-" => _core.sub(left, right),
            "*" => _core.mul(left, right),
            "/" => _core.div(left, right),
            "%" => _core.mod(left, right),
            "==" => _core.eq(left, right),
            "===" => _core.eq(left, right),
            "!=" => _core.ne(left, right),
            "!==" => _core.ne(left, right),
            "<" => _core.lt(left, right),
            ">" => _core.gt(left, right),
            "<=" => _core.lte(left, right),
            ">=" => _core.gte(left, right),
            "&&" => _core.and(left, right),
            "||" => _core.or(left, right),
            "**" => _core.int_const((long)Math.Pow(to_float(left), to_float(right))),
            "in" => eval_in_op(left, right),
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值 in 运算符
    /// </summary>
    private object eval_in_op(object left, object right)
    {
        var key = to_str(left);

        if (right is Dictionary<string, object> dict)
        {
            return _core.bool_const(dict.ContainsKey(key));
        }

        return _core.bool_const(false);
    }

    /// <summary>
    ///     求值一元表达式
    /// </summary>
    private object eval_unary_expr(TsUnaryExpr expr)
    {
        if (expr.is_prefix)
        {
            return expr.@operator switch
            {
                "!" => _core.not(eval_node(expr.operand)),
                "-" => _core.sub(0L, eval_node(expr.operand)),
                "+" => eval_node(expr.operand),
                "typeof" => eval_typeof_value(eval_node(expr.operand)),
                "void" => _core.unit(),
                "delete" => _core.unit(),
                "++" => eval_prefix_increment(expr.operand),
                "--" => eval_prefix_decrement(expr.operand),
                _ => eval_node(expr.operand)
            };
        }

        // 后缀运算符
        return expr.@operator switch
        {
            "++" => eval_postfix_increment(expr.operand),
            "--" => eval_postfix_decrement(expr.operand),
            _ => eval_node(expr.operand)
        };
    }

    /// <summary>
    ///     前缀自增
    /// </summary>
    private object eval_prefix_increment(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            var curVal = _core.var(id.name);
            var newVal = _core.add(curVal, 1L);
            _core.set_var(id.name, newVal);
            return newVal;
        }

        return _core.unit();
    }

    /// <summary>
    ///     前缀自减
    /// </summary>
    private object eval_prefix_decrement(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            var curVal = _core.var(id.name);
            var newVal = _core.sub(curVal, 1L);
            _core.set_var(id.name, newVal);
            return newVal;
        }

        return _core.unit();
    }

    /// <summary>
    ///     后缀自增
    /// </summary>
    private object eval_postfix_increment(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            var curVal = _core.var(id.name);
            var newVal = _core.add(curVal, 1L);
            _core.set_var(id.name, newVal);
            return curVal;
        }

        return _core.unit();
    }

    /// <summary>
    ///     后缀自减
    /// </summary>
    private object eval_postfix_decrement(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            var curVal = _core.var(id.name);
            var newVal = _core.sub(curVal, 1L);
            _core.set_var(id.name, newVal);
            return curVal;
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值赋值表达式
    /// </summary>
    private object eval_assignment_expr(TsAssignmentExpr expr)
    {
        var rightVal = eval_node(expr.right);

        if (expr.@operator == "=")
        {
            return assign_to(expr.left, rightVal);
        }

        // 增强赋值：+= -= *= /= %=
        var currentVal = eval_node(expr.left);
        var newVal = expr.@operator switch
        {
            "+=" => _core.add(currentVal, rightVal),
            "-=" => _core.sub(currentVal, rightVal),
            "*=" => _core.mul(currentVal, rightVal),
            "/=" => _core.div(currentVal, rightVal),
            "%=" => _core.mod(currentVal, rightVal),
            _ => rightVal
        };

        return assign_to(expr.left, newVal);
    }

    /// <summary>
    ///     赋值到目标
    /// </summary>
    private object assign_to(TsAstNode target, object value)
    {
        if (target is TsIdentifier id)
        {
            return _core.set_var(id.name, value);
        }

        if (target is TsPropertyAccess propAccess)
        {
            var obj = eval_node(propAccess.@object);
            if (obj is Dictionary<string, object> dict)
            {
                dict[propAccess.property] = value;
            }

            return value;
        }

        if (target is TsElementAccess elemAccess)
        {
            var container = eval_node(elemAccess.@object);
            var index = eval_node(elemAccess.index);

            if (container is System.Collections.IList list)
            {
                var idx = (int)to_int(index);
                if (idx >= 0 && idx < list.Count)
                {
                    list[idx] = value;
                }
            }

            return value;
        }

        return value;
    }

    /// <summary>
    ///     求值条件表达式（三元运算符）
    /// </summary>
    private object eval_conditional_expr(TsConditionalExpr expr)
    {
        var condition = eval_node(expr.condition);
        if (to_bool(condition))
        {
            return eval_node(expr.then_branch);
        }

        return eval_node(expr.else_branch);
    }

    /// <summary>
    ///     求值调用表达式
    /// </summary>
    private object eval_call_expr(TsCallExpr callExpr)
    {
        // 处理 console.log
        if (callExpr.callee is TsPropertyAccess propAccess)
        {
            if (propAccess is { @object: TsIdentifier { name: "console" }, property: "log" })
            {
                var args = callExpr.arguments.Select(a => eval_node(a)).ToArray();
                foreach (var arg in args)
                {
                    print_output(arg);
                }

                return _core.unit();
            }

            if (propAccess is { @object: TsIdentifier { name: "console" }, property: "error" })
            {
                var args = callExpr.arguments.Select(a => eval_node(a)).ToArray();
                foreach (var arg in args)
                {
                    Console.Error.WriteLine(to_str(arg));
                }

                return _core.unit();
            }
        }

        // 处理方法调用
        if (callExpr.callee is TsPropertyAccess methodAccess)
        {
            var receiver = eval_node(methodAccess.@object);
            var args = callExpr.arguments.Select(a => eval_node(a)).ToArray();

            return methodAccess.property switch
            {
                "push" => eval_array_push(receiver, args),
                "pop" => eval_array_pop(receiver),
                "map" => eval_array_map(receiver, args),
                "filter" => eval_array_filter(receiver, args),
                "forEach" => eval_array_for_each(receiver, args),
                "length" => receiver is System.Collections.IList l ? _core.int_const(l.Count) : _core.int_const(0),
                _ => eval_method_call(receiver, methodAccess.property, args)
            };
        }

        // 普通函数调用
        var calleeVal = eval_node(callExpr.callee);
        var callArgs = callExpr.arguments.Select(a => eval_node(a)).ToArray();
        return _core.apply(calleeVal, callArgs);
    }

    /// <summary>
    ///     求值属性访问
    /// </summary>
    private object eval_property_access(TsPropertyAccess propAccess)
    {
        // console 的属性
        if (propAccess.@object is TsIdentifier { name: "console" })
        {
            return _core.unit();
        }

        var obj = eval_node(propAccess.@object);

        if (obj is string strVal && propAccess.property == "length")
        {
            return _core.int_const(strVal.Length);
        }

        if (obj is System.Collections.IList list && propAccess.property == "length")
        {
            return _core.int_const(list.Count);
        }

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(propAccess.property, out var val))
        {
            return val;
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值下标访问
    /// </summary>
    private object eval_element_access(TsElementAccess elemAccess)
    {
        var container = eval_node(elemAccess.@object);
        var index = eval_node(elemAccess.index);

        if (container is System.Collections.IList list)
        {
            var idx = (int)to_int(index);
            if (idx >= 0 && idx < list.Count)
            {
                return list[idx] ?? _core.unit();
            }

            return _core.unit();
        }

        if (container is string strVal)
        {
            var idx = (int)to_int(index);
            if (idx >= 0 && idx < strVal.Length)
            {
                return _core.str_const(strVal[idx].ToString());
            }

            return _core.unit();
        }

        if (container is Dictionary<string, object> dict)
        {
            var key = to_str(index);
            if (dict.TryGetValue(key, out var val))
            {
                return val;
            }

            return _core.unit();
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值数组字面量
    /// </summary>
    private object eval_array_literal(TsArrayLiteral arrayLit)
    {
        var list = new List<object>();

        foreach (var elem in arrayLit.elements)
        {
            if (elem is TsSpreadElement spread)
            {
                var spreadVal = eval_node(spread.argument);
                if (spreadVal is System.Collections.IList spreadList)
                {
                    foreach (var item in spreadList)
                    {
                        list.Add(item ?? _core.unit());
                    }
                }
            }
            else
            {
                list.Add(eval_node(elem));
            }
        }

        return list;
    }

    /// <summary>
    ///     求值对象字面量
    /// </summary>
    private object eval_object_literal(TsObjectLiteral objLit)
    {
        var dict = new Dictionary<string, object>();

        foreach (var prop in objLit.properties)
        {
            var val = eval_node(prop.value);
            dict[prop.key] = val;
        }

        return dict;
    }

    /// <summary>
    ///     求值箭头函数
    /// </summary>
    private object eval_arrow_function(TsArrowFunctionExpr arrowFunc)
    {
        var paramNames = arrowFunc.parameters.Select(p => p.name.TrimStart('.')).ToArray();
        var bodyObj = eval_block_as_body(arrowFunc.body);
        return _core.lambda(paramNames, bodyObj);
    }

    /// <summary>
    ///     求值函数表达式
    /// </summary>
    private object eval_function_expr(TsFunctionExpr funcExpr)
    {
        var paramNames = funcExpr.parameters.Select(p => p.name.TrimStart('.')).ToArray();
        var bodyObj = eval_block_as_body(funcExpr.body);
        var lambda = _core.lambda(paramNames, bodyObj);

        if (funcExpr.name is not null)
        {
            _core.set_var(funcExpr.name, lambda);
        }

        return lambda;
    }

    /// <summary>
    ///     求值 new 表达式
    /// </summary>
    private object eval_new_expr(TsNewExpr newExpr)
    {
        var args = newExpr.arguments.Select(a => eval_node(a)).ToArray();

        if (newExpr.callee is TsIdentifier { name: "Array" })
        {
            if (args.Length == 1)
            {
                var size = to_int(args[0]);
                var list = new List<object>();
                for (var i = 0; i < size; i++)
                {
                    list.Add(_core.unit());
                }

                return list;
            }

            return new List<object>(args);
        }

        if (newExpr.callee is TsIdentifier { name: "Map" or "Set" })
        {
            return new Dictionary<string, object>();
        }

        // 通用：尝试调用构造函数
        var calleeVal = eval_node(newExpr.callee);
        return _core.apply(calleeVal, args);
    }

    /// <summary>
    ///     求值 typeof 表达式
    /// </summary>
    private object eval_typeof_expr(TsTypeofExpr typeofExpr)
    {
        var val = eval_node(typeofExpr.operand);
        return eval_typeof_value(val);
    }

    /// <summary>
    ///     获取值的 typeof 结果
    /// </summary>
    private static object eval_typeof_value(object val)
    {
        return val switch
        {
            null => "undefined",
            long => "number",
            int => "number",
            double => "number",
            bool => "boolean",
            string => "string",
            System.Collections.IList => "object",
            Dictionary<string, object> => "object",
            LambdaClosure => "function",
            BuiltinFunction => "function",
            _ => val.GetType().Name.ToLower()
        };
    }

    #endregion

    #region 数组方法

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

        return _core.int_const(list.Count);
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
    ///     数组 map 方法
    /// </summary>
    private object eval_array_map(object receiver, object[] args)
    {
        if (receiver is not System.Collections.IList list || args.Length == 0)
        {
            return _core.unit();
        }

        var callback = args[0];
        var result = new List<object>();

        foreach (var item in list)
        {
            var mapped = _core.apply(callback, [item ?? _core.unit()]);
            result.Add(mapped);
        }

        return result;
    }

    /// <summary>
    ///     数组 filter 方法
    /// </summary>
    private object eval_array_filter(object receiver, object[] args)
    {
        if (receiver is not System.Collections.IList list || args.Length == 0)
        {
            return _core.unit();
        }

        var callback = args[0];
        var result = new List<object>();

        foreach (var item in list)
        {
            var pred = _core.apply(callback, [item ?? _core.unit()]);
            if (to_bool(pred))
            {
                result.Add(item ?? _core.unit());
            }
        }

        return result;
    }

    /// <summary>
    ///     数组 forEach 方法
    /// </summary>
    private object eval_array_for_each(object receiver, object[] args)
    {
        if (receiver is not System.Collections.IList list || args.Length == 0)
        {
            return _core.unit();
        }

        var callback = args[0];
        foreach (var item in list)
        {
            _core.apply(callback, [item ?? _core.unit()]);
        }

        return _core.unit();
    }

    /// <summary>
    ///     通用方法调用
    /// </summary>
    private object eval_method_call(object receiver, string method, object[] args)
    {
        if (receiver is string strVal)
        {
            return method switch
            {
                "toUpperCase" => _core.str_const(strVal.ToUpper()),
                "toLowerCase" => _core.str_const(strVal.ToLower()),
                "trim" => _core.str_const(strVal.Trim()),
                "split" => args.Length > 0 ? eval_string_split(strVal, args[0]) : new List<object> { _core.str_const(strVal) },
                "indexOf" => args.Length > 0 ? _core.int_const(strVal.IndexOf(to_str(args[0]), StringComparison.Ordinal)) : _core.int_const(-1),
                "includes" => args.Length > 0 ? _core.bool_const(strVal.Contains(to_str(args[0]))) : _core.bool_const(false),
                "charAt" => args.Length > 0 ? eval_string_char_at(strVal, args[0]) : _core.str_const(""),
                "slice" => eval_string_slice(strVal, args),
                _ => _core.unit()
            };
        }

        return _core.unit();
    }

    /// <summary>
    ///     字符串 split 方法
    /// </summary>
    private object eval_string_split(string strVal, object separator)
    {
        var sep = to_str(separator);
        var parts = strVal.Split(sep);
        var result = new List<object>();
        foreach (var p in parts)
        {
            result.Add(_core.str_const(p));
        }

        return result;
    }

    /// <summary>
    ///     字符串 charAt 方法
    /// </summary>
    private object eval_string_char_at(string strVal, object indexObj)
    {
        var idx = (int)to_int(indexObj);
        if (idx >= 0 && idx < strVal.Length)
        {
            return _core.str_const(strVal[idx].ToString());
        }

        return _core.str_const("");
    }

    /// <summary>
    ///     字符串 slice 方法
    /// </summary>
    private object eval_string_slice(string strVal, object[] args)
    {
        var start = args.Length > 0 ? (int)to_int(args[0]) : 0;
        var end = args.Length > 1 ? (int)to_int(args[1]) : strVal.Length;

        if (start < 0)
        {
            start = strVal.Length + start;
        }

        if (end < 0)
        {
            end = strVal.Length + end;
        }

        if (start < 0)
        {
            start = 0;
        }

        if (end > strVal.Length)
        {
            end = strVal.Length;
        }

        if (start >= end)
        {
            return _core.str_const("");
        }

        return _core.str_const(strVal[start..end]);
    }

    #endregion
}