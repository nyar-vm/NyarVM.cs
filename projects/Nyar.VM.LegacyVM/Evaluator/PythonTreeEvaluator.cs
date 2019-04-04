using Nyar.VM.LegacyVM.Algebra;
using Std.Data.Text.Python.AST;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     Python AST 求值器，遍历 Oak.Python AST 节点并调用 CoreEvaluator 求值。
/// </summary>
public sealed class PythonTreeEvaluator : UnifiedTreeEvaluator<PyAstNode>
{
    /// <summary>
    ///     最大循环迭代次数
    /// </summary>
    private const int _max_iterations = 10000;

    /// <summary>
    ///     创建 Python AST 求值器
    /// </summary>
    /// <param name="config">语言运行时配置</param>
    /// <param name="env">初始变量环境</param>
    public PythonTreeEvaluator(LanguageRuntimeConfig config, Dictionary<string, object> env) : base(config, env)
    {
    }

    /// <inheritdoc />
    public override object evaluate(PyAstNode ast)
    {
        if (ast is PyModule module)
        {
            return eval_module(module);
        }

        return eval_node(ast);
    }

    #region 模块

    /// <summary>
    ///     求值模块
    /// </summary>
    private object eval_module(PyModule module)
    {
        object result = _core.unit();

        foreach (var stmt in module.body)
        {
            result = eval_node(stmt);

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
    private object eval_node(PyAstNode node)
    {
        return node switch
        {
            // 语句
            PyExprStmt exprStmt => eval_node(exprStmt.expression),
            PyAssign assign => eval_assign(assign),
            PyAugAssign augAssign => eval_aug_assign(augAssign),
            PyIf ifStmt => eval_if(ifStmt),
            PyWhile whileStmt => eval_while(whileStmt),
            PyFor forStmt => eval_for(forStmt),
            PyFunctionDef funcDef => eval_function_def(funcDef),
            PyClassDef classDef => eval_class_def(classDef),
            PyReturn returnStmt => _core.@return(returnStmt.value is not null ? eval_node(returnStmt.value) : _core.unit()),
            PyYield yieldStmt => eval_yield(yieldStmt),
            PyBreak => new BreakValue(),
            PyContinue => new ContinueValue(),
            PyPass => _core.unit(),
            PyTry tryStmt => eval_try(tryStmt),
            PyRaise raiseStmt => eval_raise(raiseStmt),
            PyImport => _core.unit(),
            PyFromImport => _core.unit(),

            // 表达式
            PyLiteral literal => eval_literal(literal),
            PyIdentifier identifier => eval_identifier(identifier),
            PyBinaryOp binaryOp => eval_binary_op(binaryOp),
            PyUnaryOp unaryOp => eval_unary_op(unaryOp),
            PyCall call => eval_call(call),
            PyAttribute attribute => eval_attribute(attribute),
            PySubscript subscript => eval_subscript(subscript),
            PyList list => eval_list(list),
            PyTuple tuple => eval_tuple(tuple),
            PyDict dict => eval_dict(dict),
            PyLambda lambda => eval_lambda(lambda),

            _ => _core.unit()
        };
    }

    #endregion

    #region 语句求值

    /// <summary>
    ///     求值赋值语句
    /// </summary>
    private object eval_assign(PyAssign assign)
    {
        var value = eval_node(assign.value);

        // 多返回值：x, y = func()
        if (assign.target is PyTuple tupleTarget && value is System.Collections.IList list)
        {
            for (var i = 0; i < tupleTarget.elements.Count && i < list.Count; i++)
            {
                if (tupleTarget.elements[i] is PyIdentifier id)
                {
                    _core.set_var(id.name, list[i] ?? _core.unit());
                }
            }

            return value;
        }

        return assign_to(assign.target, value);
    }

    /// <summary>
    ///     求值增量赋值语句
    /// </summary>
    private object eval_aug_assign(PyAugAssign augAssign)
    {
        var currentVal = eval_node(augAssign.target);
        var rhsVal = eval_node(augAssign.value);
        var newVal = augAssign.@operator switch
        {
            "+=" => _core.add(currentVal, rhsVal),
            "-=" => _core.sub(currentVal, rhsVal),
            "*=" => _core.mul(currentVal, rhsVal),
            "/=" => _core.div(currentVal, rhsVal),
            "%=" => _core.mod(currentVal, rhsVal),
            "//=" => _core.int_const(CoreHelpers.to_i64(_core.div(currentVal, rhsVal))),
            "**=" => _core.int_const((long)Math.Pow(CoreHelpers.to_i64(currentVal), CoreHelpers.to_i64(rhsVal))),
            _ => rhsVal
        };

        return assign_to(augAssign.target, newVal);
    }

    /// <summary>
    ///     赋值到目标
    /// </summary>
    private object assign_to(PyAstNode target, object value)
    {
        if (target is PyIdentifier id)
        {
            return _core.set_var(id.name, value);
        }

        if (target is PySubscript subscript)
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

            if (container is Dictionary<string, object> dict)
            {
                var key = CoreHelpers.to_str(index);
                dict[key] = value;
                return value;
            }
        }

        if (target is PyAttribute attribute)
        {
            var obj = eval_node(attribute.@object);
            if (obj is Dictionary<string, object> dict)
            {
                dict[attribute.name] = value;
                return value;
            }
        }

        return value;
    }

    /// <summary>
    ///     求值 if 语句
    /// </summary>
    private object eval_if(PyIf ifStmt)
    {
        var condition = eval_node(ifStmt.condition);
        if (to_bool(condition))
        {
            return eval_body(ifStmt.then_body);
        }

        if (ifStmt.else_body is not null && ifStmt.else_body.Count > 0)
        {
            return eval_body(ifStmt.else_body);
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 while 循环
    /// </summary>
    private object eval_while(PyWhile whileStmt)
    {
        var loopCount = 0;
        object result = _core.unit();

        while (loopCount < _max_iterations && to_bool(eval_node(whileStmt.condition)))
        {
            var bodyResult = eval_body(whileStmt.body);

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
            Console.WriteLine($"[Python] 警告：while 循环达到最大迭代次数 {_max_iterations}，已中断");
        }

        return result;
    }

    /// <summary>
    ///     求值 for 循环
    /// </summary>
    private object eval_for(PyFor forStmt)
    {
        var iterable = eval_node(forStmt.iterable);
        object result = _core.unit();

        if (iterable is System.Collections.IList list)
        {
            foreach (var item in list)
            {
                _core.set_var(forStmt.iterator, item ?? _core.unit());
                var bodyResult = eval_body(forStmt.body);

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
                _core.set_var(forStmt.iterator, _core.str_const(ch.ToString()));
                var bodyResult = eval_body(forStmt.body);

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
    ///     求值函数定义
    /// </summary>
    private object eval_function_def(PyFunctionDef funcDef)
    {
        var paramNames = funcDef.parameters.ToArray();
        var bodyObj = eval_body_as_lambda_body(funcDef.body);
        var lambda = _core.lambda(paramNames, bodyObj);
        _core.set_var(funcDef.name, lambda);

        _functions[funcDef.name] = args =>
        {
            for (var i = 0; i < paramNames.Length && i < args.Length; i++)
            {
                _core.set_var(paramNames[i], args[i]);
            }

            return _core.eval_with_return_unwrap(eval_body(funcDef.body));
        };

        return _core.unit();
    }

    /// <summary>
    ///     求值类定义
    /// </summary>
    private object eval_class_def(PyClassDef classDef)
    {
        var classDict = new Dictionary<string, object>();

        foreach (var member in classDef.body)
        {
            if (member is PyFunctionDef method)
            {
                var paramNames = method.parameters.ToArray();
                var bodyObj = eval_body_as_lambda_body(method.body);
                var lambda = _core.lambda(paramNames, bodyObj);
                classDict[method.name] = lambda;
            }
            else if (member is PyAssign assign)
            {
                if (assign.target is PyIdentifier id)
                {
                    var value = eval_node(assign.value);
                    classDict[id.name] = value;
                }
            }
            else if (member is PyExprStmt exprStmt)
            {
                eval_node(exprStmt);
            }
        }

        _core.set_var(classDef.name, classDict);
        return _core.unit();
    }

    /// <summary>
    ///     求值 yield 语句（基础支持：返回值但不实现生成器协议）
    /// </summary>
    private object eval_yield(PyYield yieldStmt)
    {
        if (yieldStmt.value is not null)
        {
            return eval_node(yieldStmt.value);
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值 try/except/finally 语句
    /// </summary>
    private object eval_try(PyTry tryStmt)
    {
        try
        {
            var result = eval_body(tryStmt.body);

            if (tryStmt.else_body is not null && tryStmt.else_body.Count > 0)
            {
                result = eval_body(tryStmt.else_body);
            }

            return result;
        }
        catch (Exception ex)
        {
            foreach (var handler in tryStmt.handlers)
            {
                if (handler.exception_type is not null)
                {
                    // 简单匹配：不检查异常类型
                }

                if (handler.name is not null)
                {
                    _core.set_var(handler.name, ex.Message);
                }

                return eval_body(handler.body);
            }

            Console.WriteLine($"[Python] 未捕获的异常：{ex.Message}");
            return _core.unit();
        }
        finally
        {
            if (tryStmt.finally_body is not null && tryStmt.finally_body.Count > 0)
            {
                eval_body(tryStmt.finally_body);
            }
        }
    }

    /// <summary>
    ///     求值 raise 语句
    /// </summary>
    private object eval_raise(PyRaise raiseStmt)
    {
        if (raiseStmt.exception is not null)
        {
            var value = eval_node(raiseStmt.exception);
            throw new Exception(to_str(value));
        }

        throw new Exception("raise");
    }

    /// <summary>
    ///     求值语句列表
    /// </summary>
    private object eval_body(IReadOnlyList<PyAstNode> body)
    {
        var stmts = new List<object>();

        foreach (var stmt in body)
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
    ///     将语句列表求值为 lambda 体
    /// </summary>
    private object eval_body_as_lambda_body(IReadOnlyList<PyAstNode> body)
    {
        return eval_body(body);
    }

    #endregion

    #region 表达式求值

    /// <summary>
    ///     求值字面量
    /// </summary>
    private object eval_literal(PyLiteral literal)
    {
        return literal.kind switch
        {
            "int" => long.TryParse(literal.value, out var intVal) ? intVal : 0L,
            "float" => double.TryParse(literal.value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var floatVal)
                ? floatVal
                : 0.0,
            "string" => _core.str_const(literal.value),
            "bool" => literal.value == "True" ? _core.bool_const(true) : _core.bool_const(false),
            "none" => _core.unit(),
            _ => _core.str_const(literal.value)
        };
    }

    /// <summary>
    ///     求值标识符
    /// </summary>
    private object eval_identifier(PyIdentifier identifier)
    {
        if (identifier.name == "True")
        {
            return _core.bool_const(true);
        }

        if (identifier.name == "False")
        {
            return _core.bool_const(false);
        }

        if (identifier.name == "None")
        {
            return _core.unit();
        }

        return _core.var(identifier.name);
    }

    /// <summary>
    ///     求值二元运算
    /// </summary>
    private object eval_binary_op(PyBinaryOp binaryOp)
    {
        var left = eval_node(binaryOp.left);
        var right = eval_node(binaryOp.right);

        return binaryOp.@operator switch
        {
            "+" => _core.add(left, right),
            "-" => _core.sub(left, right),
            "*" => _core.mul(left, right),
            "/" => _core.div(left, right),
            "//" => _core.int_const(CoreHelpers.to_i64(_core.div(left, right))),
            "%" => _core.mod(left, right),
            "**" => _core.int_const((long)Math.Pow(CoreHelpers.to_i64(left), CoreHelpers.to_i64(right))),
            "==" => _core.eq(left, right),
            "!=" => _core.ne(left, right),
            "<" => _core.lt(left, right),
            ">" => _core.gt(left, right),
            "<=" => _core.lte(left, right),
            ">=" => _core.gte(left, right),
            "and" => _core.and(left, right),
            "or" => _core.or(left, right),
            "in" => eval_in_op(left, right),
            _ => _core.unit()
        };
    }

    /// <summary>
    ///     求值 in 运算符
    /// </summary>
    private object eval_in_op(object left, object right)
    {
        if (right is System.Collections.IList rList)
        {
            foreach (var item in rList)
            {
                if (to_bool(_core.eq(left, item ?? _core.unit())))
                {
                    return _core.bool_const(true);
                }
            }

            return _core.bool_const(false);
        }

        if (right is string rStr)
        {
            var lStr = CoreHelpers.to_str(left);
            return _core.bool_const(rStr.Contains(lStr));
        }

        if (right is Dictionary<string, object> rDict)
        {
            var key = CoreHelpers.to_str(left);
            return _core.bool_const(rDict.ContainsKey(key));
        }

        return _core.bool_const(false);
    }

    /// <summary>
    ///     求值一元运算
    /// </summary>
    private object eval_unary_op(PyUnaryOp unaryOp)
    {
        var operand = eval_node(unaryOp.operand);

        return unaryOp.@operator switch
        {
            "not" => _core.not(operand),
            "-" => _core.sub(0L, operand),
            "+" => operand,
            _ => operand
        };
    }

    /// <summary>
    ///     求值函数调用
    /// </summary>
    private object eval_call(PyCall call)
    {
        // 处理 print 函数
        if (call.function is PyIdentifier { name: "print" })
        {
            var args = call.arguments.Select(a => eval_node(a)).ToArray();
            foreach (var arg in args)
            {
                print_output(arg);
            }

            return _core.unit();
        }

        var funcVal = eval_node(call.function);
        var argVals = call.arguments.Select(a => eval_node(a)).ToArray();
        return _core.apply(funcVal, argVals);
    }

    /// <summary>
    ///     求值属性访问
    /// </summary>
    private object eval_attribute(PyAttribute attribute)
    {
        var obj = eval_node(attribute.@object);

        if (obj is string strVal && attribute.name == "length")
        {
            return _core.int_const(strVal.Length);
        }

        if (obj is System.Collections.IList list && attribute.name == "length")
        {
            return _core.int_const(list.Count);
        }

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(attribute.name, out var val))
        {
            return val;
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值下标访问
    /// </summary>
    private object eval_subscript(PySubscript subscript)
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

        if (container is Dictionary<string, object> dict)
        {
            var key = CoreHelpers.to_str(index);
            if (dict.TryGetValue(key, out var val))
            {
                return val;
            }

            return _core.unit();
        }

        return _core.unit();
    }

    /// <summary>
    ///     求值列表字面量
    /// </summary>
    private object eval_list(PyList list)
    {
        var result = new List<object>();
        foreach (var elem in list.elements)
        {
            result.Add(eval_node(elem));
        }

        return result;
    }

    /// <summary>
    ///     求值元组字面量
    /// </summary>
    private object eval_tuple(PyTuple tuple)
    {
        var result = new List<object>();
        foreach (var elem in tuple.elements)
        {
            result.Add(eval_node(elem));
        }

        return result;
    }

    /// <summary>
    ///     求值字典字面量
    /// </summary>
    private object eval_dict(PyDict dict)
    {
        var result = new Dictionary<string, object>();
        foreach (var (keyNode, valueNode) in dict.items)
        {
            var key = eval_dict_key(keyNode);
            var val = eval_node(valueNode);
            result[key] = val;
        }

        return result;
    }

    /// <summary>
    ///     求值字典键
    /// </summary>
    private string eval_dict_key(PyAstNode keyNode)
    {
        if (keyNode is PyLiteral { kind: "string" } strLit)
        {
            return strLit.value;
        }

        if (keyNode is PyIdentifier id)
        {
            return id.name;
        }

        return CoreHelpers.to_str(eval_node(keyNode));
    }

    /// <summary>
    ///     求值 lambda 表达式
    /// </summary>
    private object eval_lambda(PyLambda lambda)
    {
        var paramNames = lambda.parameters.ToArray();
        var bodyVal = eval_node(lambda.body);
        return _core.lambda(paramNames, bodyVal);
    }

    #endregion
}