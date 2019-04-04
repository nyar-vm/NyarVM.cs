using Nyar.Assembler;
using Std.Data.Text.Python.AST;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.VM.LegacyVM.Compiler;

/// <summary>
///     Python Futamura 编译器。
///     将 Python AST 编译为 Nyar 字节码的 GenerateModule，实现第一 Futamura 投影：
///     解释器 specialized over (source, ?) → 编译器。
/// </summary>
public sealed class PythonFutamuraCompiler : ILanguageCompiler
{
    /// <inheritdoc />
    public string language => "python";

    private StackCompiler _sc = null!;

    /// <inheritdoc />
    public GenerateModule compile(string source, string moduleName)
    {
        var lexer = new Std.Data.Text.Python.Lexer.PythonLexer();
        var tokens = lexer.tokenize(source);
        var parser = new Std.Data.Text.Python.Parser.PythonParser();
        var ast = parser.parse(tokens);

        _sc = new StackCompiler(moduleName);
        compile_module((PyModule)ast);
        return _sc.module;
    }

    #region 模块编译

    /// <summary>
    ///     编译模块
    /// </summary>
    private void compile_module(PyModule module)
    {
        _sc.begin_function("main");

        foreach (var stmt in module.body)
        {
            compile_node(stmt);
        }

        _sc.emit_null();
        _sc.emit_return();
        _sc.end_function();
    }

    #endregion

    #region 节点分发

    /// <summary>
    ///     按节点类型分发编译
    /// </summary>
    private void compile_node(PyAstNode node)
    {
        switch (node)
        {
            // 语句
            case PyExprStmt exprStmt:
                compile_node(exprStmt.expression);
                _sc.emit_pop();
                break;
            case PyAssign assign:
                compile_assign(assign);
                break;
            case PyAugAssign augAssign:
                compile_aug_assign(augAssign);
                break;
            case PyIf ifStmt:
                compile_if(ifStmt);
                break;
            case PyWhile whileStmt:
                compile_while(whileStmt);
                break;
            case PyFor forStmt:
                compile_for(forStmt);
                break;
            case PyFunctionDef funcDef:
                compile_function_def(funcDef);
                break;
            case PyClassDef classDef:
                compile_class_def(classDef);
                break;
            case PyReturn returnStmt:
                if (returnStmt.value is not null)
                {
                    compile_node(returnStmt.value);
                }
                else
                {
                    _sc.emit_null();
                }

                _sc.emit_return();
                break;
            case PyYield yieldStmt:
                if (yieldStmt.value is not null)
                {
                    compile_node(yieldStmt.value);
                }
                else
                {
                    _sc.emit_null();
                }

                break;
            case PyTry tryStmt:
                compile_try(tryStmt);
                break;
            case PyRaise raiseStmt:
                if (raiseStmt.exception is not null)
                {
                    compile_node(raiseStmt.exception);
                }
                else
                {
                    _sc.emit_const_str("raise");
                }

                _sc.emit(NyarHeadCode.@throw);
                break;
            case PyBreak:
                _sc.emit_break();
                break;
            case PyContinue:
                _sc.emit_continue();
                break;
            case PyPass:
                break;
            case PyImport:
            case PyFromImport:
                break;

            // 表达式
            case PyLiteral literal:
                compile_literal(literal);
                break;
            case PyIdentifier identifier:
                compile_identifier(identifier);
                break;
            case PyBinaryOp binaryOp:
                compile_binary_op(binaryOp);
                break;
            case PyUnaryOp unaryOp:
                compile_unary_op(unaryOp);
                break;
            case PyCall call:
                compile_call(call);
                break;
            case PyAttribute attribute:
                compile_attribute(attribute);
                break;
            case PySubscript subscript:
                compile_subscript(subscript);
                break;
            case PyList list:
                compile_list(list);
                break;
            case PyTuple tuple:
                compile_tuple(tuple);
                break;
            case PyDict dict:
                compile_dict(dict);
                break;
            case PyLambda lambda:
                compile_lambda(lambda);
                break;

            default:
                break;
        }
    }

    #endregion

    #region 语句编译

    /// <summary>
    ///     编译赋值语句
    /// </summary>
    private void compile_assign(PyAssign assign)
    {
        compile_node(assign.value);

        // 多返回值：x, y = func()
        if (assign.target is PyTuple tupleTarget)
        {
            // 简单处理：只存储第一个
            if (tupleTarget.elements.Count > 0 && tupleTarget.elements[0] is PyIdentifier firstId)
            {
                _sc.emit_dup();
                _sc.emit_store_local(firstId.name);
            }
        }
        else if (assign.target is PyIdentifier id)
        {
            _sc.emit_dup();
            _sc.emit_store_local(id.name);
        }
        else if (assign.target is PyAttribute attr)
        {
            _sc.emit_dup();
            compile_node(attr.@object);
            _sc.emit_set_field(attr.name);
        }
        else if (assign.target is PySubscript sub)
        {
            _sc.emit_dup();
            compile_node(sub.@object);
            compile_node(sub.index);
            _sc.emit_set_index();
        }
    }

    /// <summary>
    ///     编译增量赋值
    /// </summary>
    private void compile_aug_assign(PyAugAssign augAssign)
    {
        compile_node(augAssign.value);

        if (augAssign.target is PyIdentifier id)
        {
            _sc.emit_load_local(id.name);
        }

        switch (augAssign.@operator)
        {
            case "+=":
                _sc.emit_add();
                break;
            case "-=":
                _sc.emit_sub();
                break;
            case "*=":
                _sc.emit_mul();
                break;
            case "/=":
                _sc.emit_div();
                break;
            case "%=":
                _sc.emit_rem();
                break;
            case "//=":
                _sc.emit_div();
                break;
            case "**=":
                _sc.emit_call_native("math_pow");
                break;
        }

        if (augAssign.target is PyIdentifier targetId)
        {
            _sc.emit_dup();
            _sc.emit_store_local(targetId.name);
        }
    }

    /// <summary>
    ///     编译 if 语句
    /// </summary>
    private void compile_if(PyIf ifStmt)
    {
        var elseLabel = _sc.generate_label("else");
        var endLabel = _sc.generate_label("endif");

        compile_node(ifStmt.condition);
        _sc.emit_jump_if_false(elseLabel);

        // then 分支
        compile_body(ifStmt.then_body);
        _sc.emit_jump(endLabel);

        // else 分支
        _sc.mark_label(elseLabel);
        if (ifStmt.else_body is { Count: > 0 })
        {
            compile_body(ifStmt.else_body);
        }

        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译 while 循环
    /// </summary>
    private void compile_while(PyWhile whileStmt)
    {
        var loopLabel = _sc.generate_label("loop");
        var endLabel = _sc.generate_label("endloop");
        var continueLabel = _sc.generate_label("loopcont");

        _sc.enter_loop(endLabel, continueLabel);

        _sc.mark_label(loopLabel);
        compile_node(whileStmt.condition);
        _sc.emit_jump_if_false(endLabel);

        compile_body(whileStmt.body);

        _sc.mark_label(continueLabel);
        _sc.emit_jump(loopLabel);

        _sc.mark_label(endLabel);
        _sc.leave_loop();
    }

    /// <summary>
    ///     编译 for 循环
    /// </summary>
    private void compile_for(PyFor forStmt)
    {
        // 将迭代器变量加载到栈
        compile_node(forStmt.iterable);

        var loopLabel = _sc.generate_label("forloop");
        var endLabel = _sc.generate_label("forend");
        var continueLabel = _sc.generate_label("forcont");

        _sc.enter_loop(endLabel, continueLabel);

        // 调用迭代原生函数获取下一个元素
        _sc.emit_call_native("for_iter_next");
        _sc.emit_store_local(forStmt.iterator);

        _sc.mark_label(loopLabel);
        compile_body(forStmt.body);

        _sc.mark_label(continueLabel);
        // 检查是否还有更多元素
        _sc.emit_load_local(forStmt.iterator);
        _sc.emit_call_native("for_iter_has_next");
        _sc.emit_jump_if_false(endLabel);

        // 获取下一个元素
        _sc.emit_call_native("for_iter_next");
        _sc.emit_store_local(forStmt.iterator);
        _sc.emit_jump(loopLabel);

        _sc.mark_label(endLabel);
        _sc.leave_loop();
    }

    /// <summary>
    ///     编译函数定义
    /// </summary>
    private void compile_function_def(PyFunctionDef funcDef)
    {
        _sc.begin_function(funcDef.name);

        foreach (var param in funcDef.parameters)
        {
            _sc.add_parameter(param);
        }

        compile_body(funcDef.body);

        _sc.emit_null();
        _sc.emit_return();
        _sc.end_function();
    }

    /// <summary>
    ///     编译类定义
    /// </summary>
    private void compile_class_def(PyClassDef classDef)
    {
        _sc.emit_new_object();

        foreach (var member in classDef.body)
        {
            if (member is PyFunctionDef method)
            {
                _sc.emit_const_str(method.name);
                // 方法暂时用 null 占位
                _sc.emit_null();
                _sc.emit_set_index();
            }
            else if (member is PyAssign { target: PyIdentifier fieldId } assign)
            {
                _sc.emit_const_str(fieldId.name);
                compile_node(assign.value);
                _sc.emit_set_index();
            }
            else if (member is PyExprStmt exprStmt)
            {
                compile_node(exprStmt.expression);
                _sc.emit_pop();
            }
        }

        _sc.emit_store_local(classDef.name);
    }

    /// <summary>
    ///     编译 try/except/finally 语句
    /// </summary>
    private void compile_try(PyTry tryStmt)
    {
        var tryLabel = _sc.generate_label("try");
        var catchLabel = _sc.generate_label("catch");
        var finallyLabel = _sc.generate_label("finally");
        var endLabel = _sc.generate_label("tryend");

        _sc.mark_label(tryLabel);
        compile_body(tryStmt.body);

        if (tryStmt.else_body is { Count: > 0 })
        {
            compile_body(tryStmt.else_body);
        }

        _sc.emit_jump(finallyLabel);

        // except 处理
        _sc.mark_label(catchLabel);
        if (tryStmt.handlers.Count > 0)
        {
            var handler = tryStmt.handlers[0];
            if (handler.name is not null)
            {
                _sc.emit_store_local(handler.name);
            }
            else
            {
                _sc.emit_pop();
            }

            compile_body(handler.body);
        }
        else
        {
            _sc.emit_pop();
        }

        // finally
        _sc.mark_label(finallyLabel);
        if (tryStmt.finally_body is { Count: > 0 })
        {
            compile_body(tryStmt.finally_body);
        }

        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译语句体
    /// </summary>
    private void compile_body(IReadOnlyList<PyAstNode> body)
    {
        foreach (var stmt in body)
        {
            compile_node(stmt);
        }
    }

    #endregion

    #region 表达式编译

    /// <summary>
    ///     编译字面量
    /// </summary>
    private void compile_literal(PyLiteral literal)
    {
        switch (literal.kind)
        {
            case "int":
                if (long.TryParse(literal.value, out var intVal))
                {
                    _sc.emit_const_i64(intVal);
                }
                else
                {
                    _sc.emit_const_i64(0);
                }

                break;
            case "float":
                if (double.TryParse(literal.value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var floatVal))
                {
                    _sc.emit_const_f64(floatVal);
                }
                else
                {
                    _sc.emit_const_f64(0.0);
                }

                break;
            case "string":
                _sc.emit_const_str(literal.value);
                break;
            case "bool":
                if (literal.value == "True")
                {
                    _sc.emit_const_i64(1);
                }
                else
                {
                    _sc.emit_const_i64(0);
                }

                break;
            case "none":
                _sc.emit_null();
                break;
            default:
                _sc.emit_const_str(literal.value);
                break;
        }
    }

    /// <summary>
    ///     编译标识符
    /// </summary>
    private void compile_identifier(PyIdentifier identifier)
    {
        if (identifier.name == "True")
        {
            _sc.emit_const_i64(1);
            return;
        }

        if (identifier.name == "False")
        {
            _sc.emit_const_i64(0);
            return;
        }

        if (identifier.name == "None")
        {
            _sc.emit_null();
            return;
        }

        _sc.emit_load_local(identifier.name);
    }

    /// <summary>
    ///     编译二元运算
    /// </summary>
    private void compile_binary_op(PyBinaryOp binaryOp)
    {
        // 短路求值特殊处理
        if (binaryOp.@operator == "and")
        {
            compile_logical_and(binaryOp);
            return;
        }

        if (binaryOp.@operator == "or")
        {
            compile_logical_or(binaryOp);
            return;
        }

        compile_node(binaryOp.left);
        compile_node(binaryOp.right);

        switch (binaryOp.@operator)
        {
            case "+":
                _sc.emit_add();
                break;
            case "-":
                _sc.emit_sub();
                break;
            case "*":
                _sc.emit_mul();
                break;
            case "/":
                _sc.emit_div();
                break;
            case "//":
                _sc.emit_div();
                break;
            case "%":
                _sc.emit_rem();
                break;
            case "**":
                _sc.emit_call_native("math_pow");
                break;
            case "==":
                _sc.emit_eq();
                break;
            case "!=":
                _sc.emit_ne();
                break;
            case "<":
                _sc.emit_lt();
                break;
            case ">":
                _sc.emit_gt();
                break;
            case "<=":
                _sc.emit_le();
                break;
            case ">=":
                _sc.emit_ge();
                break;
            case "in":
                _sc.emit_call_native("op_in");
                break;
            default:
                break;
        }
    }

    /// <summary>
    ///     编译逻辑与（短路求值）
    /// </summary>
    private void compile_logical_and(PyBinaryOp binaryOp)
    {
        var falseLabel = _sc.generate_label("and_false");
        var endLabel = _sc.generate_label("and_end");

        compile_node(binaryOp.left);
        _sc.emit_dup();
        _sc.emit_jump_if_false(falseLabel);
        _sc.emit_pop();

        compile_node(binaryOp.right);
        _sc.emit_jump(endLabel);

        _sc.mark_label(falseLabel);
        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译逻辑或（短路求值）
    /// </summary>
    private void compile_logical_or(PyBinaryOp binaryOp)
    {
        var trueLabel = _sc.generate_label("or_true");
        var endLabel = _sc.generate_label("or_end");

        compile_node(binaryOp.left);
        _sc.emit_dup();
        _sc.emit_jump_if_true(trueLabel);
        _sc.emit_pop();

        compile_node(binaryOp.right);
        _sc.emit_jump(endLabel);

        _sc.mark_label(trueLabel);
        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译一元运算
    /// </summary>
    private void compile_unary_op(PyUnaryOp unaryOp)
    {
        compile_node(unaryOp.operand);

        switch (unaryOp.@operator)
        {
            case "not":
                _sc.emit_not();
                break;
            case "-":
                _sc.emit_neg();
                break;
            case "+":
                break;
            default:
                break;
        }
    }

    /// <summary>
    ///     编译函数调用
    /// </summary>
    private void compile_call(PyCall call)
    {
        // print 函数 → 调用内置打印
        if (call.function is PyIdentifier { name: "print" })
        {
            foreach (var arg in call.arguments)
            {
                compile_node(arg);
            }

            _sc.emit_call_native("console_log");
            return;
        }

        // len 函数
        if (call.function is PyIdentifier { name: "len" })
        {
            foreach (var arg in call.arguments)
            {
                compile_node(arg);
            }

            _sc.emit_length();
            return;
        }

        // range 函数
        if (call.function is PyIdentifier { name: "range" })
        {
            foreach (var arg in call.arguments)
            {
                compile_node(arg);
            }

            _sc.emit_call_native("builtin_range");
            return;
        }

        // type 函数
        if (call.function is PyIdentifier { name: "type" or "int" or "float" or "str" or "bool" })
        {
            foreach (var arg in call.arguments)
            {
                compile_node(arg);
            }

            _sc.emit_call_native("builtin_cast");
            return;
        }

        // 方法调用
        if (call.function is PyAttribute attrCall)
        {
            var receiver = attrCall.@object;

            switch (attrCall.name)
            {
                case "append":
                    compile_node(receiver);
                    foreach (var arg in call.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("array_push_call");
                    return;
                case "pop":
                    compile_node(receiver);
                    _sc.emit_call_native("array_pop_call");
                    return;
                case "upper":
                    compile_node(receiver);
                    _sc.emit_call_native("string_to_upper");
                    return;
                case "lower":
                    compile_node(receiver);
                    _sc.emit_call_native("string_to_lower");
                    return;
                case "strip":
                    compile_node(receiver);
                    _sc.emit_call_native("string_trim");
                    return;
                case "split":
                    compile_node(receiver);
                    foreach (var arg in call.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("string_split");
                    return;
                default:
                    compile_node(receiver);
                    foreach (var arg in call.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("method_call");
                    return;
            }
        }

        // 简单函数调用
        if (call.function is PyIdentifier funcId)
        {
            foreach (var arg in call.arguments)
            {
                compile_node(arg);
            }

            _sc.emit_call(funcId.name, call.arguments.Count);
            return;
        }

        // 匿名函数调用
        compile_node(call.function);
        foreach (var arg in call.arguments)
        {
            compile_node(arg);
        }

        _sc.emit_call_native("apply_call");
    }

    /// <summary>
    ///     编译属性访问
    /// </summary>
    private void compile_attribute(PyAttribute attribute)
    {
        if (attribute.name == "length")
        {
            compile_node(attribute.@object);
            _sc.emit_length();
            return;
        }

        compile_node(attribute.@object);
        _sc.emit_get_field(attribute.name);
    }

    /// <summary>
    ///     编译下标访问
    /// </summary>
    private void compile_subscript(PySubscript subscript)
    {
        compile_node(subscript.@object);
        compile_node(subscript.index);
        _sc.emit_get_index();
    }

    /// <summary>
    ///     编译列表字面量
    /// </summary>
    private void compile_list(PyList list)
    {
        _sc.emit_new_object();

        foreach (var elem in list.elements)
        {
            compile_node(elem);
            _sc.emit_array_push();
        }
    }

    /// <summary>
    ///     编译元组字面量
    /// </summary>
    private void compile_tuple(PyTuple tuple)
    {
        _sc.emit_new_object();

        foreach (var elem in tuple.elements)
        {
            compile_node(elem);
            _sc.emit_array_push();
        }
    }

    /// <summary>
    ///     编译字典字面量
    /// </summary>
    private void compile_dict(PyDict dict)
    {
        _sc.emit_new_object();

        foreach (var (keyNode, valueNode) in dict.items)
        {
            // 字符串键
            if (keyNode is PyLiteral { kind: "string" } strLit)
            {
                _sc.emit_const_str(strLit.value);
                compile_node(valueNode);
                _sc.emit_set_index();
            }
            else if (keyNode is PyIdentifier idKey)
            {
                _sc.emit_const_str(idKey.name);
                compile_node(valueNode);
                _sc.emit_set_index();
            }
            else
            {
                compile_node(keyNode);
                compile_node(valueNode);
                _sc.emit_set_index();
            }
        }
    }

    /// <summary>
    ///     编译 lambda 表达式
    /// </summary>
    private void compile_lambda(PyLambda lambda)
    {
        var funcName = _sc.generate_label("lambda");

        _sc.begin_function(funcName);
        foreach (var param in lambda.parameters)
        {
            _sc.add_parameter(param);
        }

        compile_node(lambda.body);
        _sc.emit_return();
        _sc.end_function();

        _sc.emit_call_native("create_lambda");
    }

    #endregion
}