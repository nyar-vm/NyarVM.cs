using Nyar.Assembler;
using Std.Data.Text.Typescript.AST;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.VM.LegacyVM.Compiler;

/// <summary>
///     JavaScript/TypeScript Futamura 编译器。
///     将 JS/TS AST 编译为 Nyar 字节码的 GenerateModule，实现第一 Futamura 投影：
///     解释器 specialized over (source, ?) → 编译器。
/// </summary>
public sealed class JavaScriptFutamuraCompiler : ILanguageCompiler
{
    /// <inheritdoc />
    public string language => "javascript";

    private StackCompiler _sc = null!;

    /// <inheritdoc />
    public GenerateModule compile(string source, string moduleName)
    {
        var lexer = new Std.Data.Text.JavaScript.Lexer.JsLexer();
        var tokens = lexer.tokenize(source);
        var parser = new Std.Data.Text.JavaScript.Parser.JsParser();
        var ast = parser.parse(tokens);

        _sc = new StackCompiler(moduleName);
        compile_module((TsCompilationUnit)ast);
        return _sc.module;
    }

    #region 模块编译

    /// <summary>
    ///     编译顶层编译单元
    /// </summary>
    private void compile_module(TsCompilationUnit unit)
    {
        // 将顶层语句编译为 main 函数
        _sc.begin_function("main");

        foreach (var decl in unit.declarations)
        {
            compile_node(decl);
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
    private void compile_node(TsAstNode node)
    {
        switch (node)
        {
            // 跳过类型-only 声明
            case TsImportDecl or TsInterfaceDecl or TsTypeAliasDecl or TsNamespaceDecl:
                break;

            // 声明
            case TsVariableDecl varDecl:
                compile_variable_decl(varDecl);
                break;
            case TsFunctionDecl funcDecl:
                compile_function_decl(funcDecl);
                break;
            case TsEnumDecl enumDecl:
                compile_enum_decl(enumDecl);
                break;
            case TsClassDecl classDecl:
                compile_class_decl(classDecl);
                break;
            case TsExportDecl exportDecl:
                compile_node(exportDecl.value);
                break;

            // 语句
            case TsExprStmt exprStmt:
                compile_node(exprStmt.expression);
                _sc.emit_pop();
                break;
            case TsBlockStmt blockStmt:
                compile_block(blockStmt);
                break;
            case TsIfStmt ifStmt:
                compile_if(ifStmt);
                break;
            case TsSwitchStmt switchStmt:
                compile_switch(switchStmt);
                break;
            case TsWhileStmt whileStmt:
                compile_while(whileStmt);
                break;
            case TsDoWhileStmt doWhileStmt:
                compile_do_while(doWhileStmt);
                break;
            case TsForStmt forStmt:
                compile_for(forStmt);
                break;
            case TsForOfStmt forOfStmt:
                compile_for_of(forOfStmt);
                break;
            case TsForInStmt forInStmt:
                compile_for_in(forInStmt);
                break;
            case TsReturnStmt returnStmt:
                compile_return(returnStmt);
                break;
            case TsThrowStmt throwStmt:
                compile_node(throwStmt.value);
                _sc.emit(NyarHeadCode.@throw);
                break;
            case TsTryStmt tryStmt:
                compile_try(tryStmt);
                break;
            case TsBreakStmt:
                _sc.emit_break();
                break;
            case TsContinueStmt:
                _sc.emit_continue();
                break;
            case TsLabeledStmt labeledStmt:
                compile_node(labeledStmt.statement);
                break;
            case TsEmptyStmt or TsDebuggerStmt:
                break;

            // 表达式
            case TsLiteral literal:
                compile_literal(literal);
                break;
            case TsIdentifier identifier:
                compile_identifier(identifier);
                break;
            case TsBinaryExpr binaryExpr:
                compile_binary(binaryExpr);
                break;
            case TsUnaryExpr unaryExpr:
                compile_unary(unaryExpr);
                break;
            case TsAssignmentExpr assignExpr:
                compile_assignment(assignExpr);
                break;
            case TsConditionalExpr condExpr:
                compile_conditional(condExpr);
                break;
            case TsCallExpr callExpr:
                compile_call(callExpr);
                break;
            case TsPropertyAccess propAccess:
                compile_property_access(propAccess);
                break;
            case TsElementAccess elemAccess:
                compile_element_access(elemAccess);
                break;
            case TsArrayLiteral arrayLit:
                compile_array_literal(arrayLit);
                break;
            case TsObjectLiteral objLit:
                compile_object_literal(objLit);
                break;
            case TsArrowFunctionExpr arrowFunc:
                compile_arrow_function(arrowFunc);
                break;
            case TsFunctionExpr funcExpr:
                compile_function_expr(funcExpr);
                break;
            case TsNewExpr newExpr:
                compile_new_expr(newExpr);
                break;
            case TsSpreadElement spreadEl:
                compile_node(spreadEl.argument);
                break;
            case TsTypeofExpr typeofExpr:
                compile_node(typeofExpr.operand);
                break;
            case TsThisExpr:
                _sc.emit_null();
                break;
            case TsSuperExpr:
                _sc.emit_null();
                break;
            case TsInstanceofExpr:
                _sc.emit_const_i64(0);
                break;
            case TsYieldExpr:
                _sc.emit_null();
                break;

            // 其他节点暂不处理
            default:
                break;
        }
    }

    #endregion

    #region 声明编译

    /// <summary>
    ///     编译变量声明
    /// </summary>
    private void compile_variable_decl(TsVariableDecl decl)
    {
        if (decl.initializer is not null)
        {
            compile_node(decl.initializer);
        }
        else
        {
            _sc.emit_null();
        }

        _sc.emit_store_local(decl.name);
    }

    /// <summary>
    ///     编译函数声明（将函数体编译为独立函数并注册到常量池）
    /// </summary>
    private void compile_function_decl(TsFunctionDecl decl)
    {
        // 保存当前函数引用
        var savedFunc = _sc.current_function;
        var prevLocals = new Dictionary<string, int>();

        // 编译函数体为独立函数
        _sc.begin_function(decl.name);

        foreach (var param in decl.parameters)
        {
            _sc.add_parameter(param.name.TrimStart('.'));
        }

        if (decl.body is TsBlockStmt block)
        {
            foreach (var stmt in block.statements)
            {
                compile_node(stmt);
            }
        }
        else
        {
            compile_node(decl.body);
        }

        _sc.emit_null();
        _sc.emit_return();
        _sc.end_function();

        // 恢复之前的函数上下文
        // 注意：StackCompiler 的 current_function 会切换到新函数
        // 编译完成后，我们需要回到原来的函数
        // 但这个设计上有点问题，先简化处理：不在顶层函数中引用子函数
        // 后续可以通过函数表加载

        // 在 main 函数中存储函数引用占位
        // 实际这需要更复杂的设计，暂用 null 占位
    }

    /// <summary>
    ///     编译枚举声明
    /// </summary>
    private void compile_enum_decl(TsEnumDecl decl)
    {
        long value = 0;

        foreach (var member in decl.members)
        {
            if (member.initializer is not null)
            {
                compile_node(member.initializer);
            }
            else
            {
                _sc.emit_const_i64(value);
            }

            _sc.emit_store_local($"{decl.name}_{member.name}");
            _sc.emit_store_local(member.name);
            value++;
        }
    }

    /// <summary>
    ///     编译类声明（将成员存储为对象）
    /// </summary>
    private void compile_class_decl(TsClassDecl decl)
    {
        _sc.emit_new_object();

        foreach (var member in decl.members)
        {
            if (member is TsFunctionDecl method)
            {
                // 方法暂时用 null 占位
                _sc.emit_const_str(method.name);
                _sc.emit_null();
                _sc.emit_set_index();
            }
            else if (member is TsVariableDecl field)
            {
                _sc.emit_const_str(field.name);
                if (field.initializer is not null)
                {
                    compile_node(field.initializer);
                }
                else
                {
                    _sc.emit_null();
                }

                _sc.emit_set_index();
            }
        }

        _sc.emit_store_local(decl.name);
    }

    #endregion

    #region 语句编译

    /// <summary>
    ///     编译块语句
    /// </summary>
    private void compile_block(TsBlockStmt block)
    {
        foreach (var stmt in block.statements)
        {
            compile_node(stmt);
        }
    }

    /// <summary>
    ///     编译 if 语句
    /// </summary>
    private void compile_if(TsIfStmt ifStmt)
    {
        var elseLabel = _sc.generate_label("else");
        var endLabel = _sc.generate_label("endif");

        compile_node(ifStmt.condition);
        _sc.emit_jump_if_false(elseLabel);

        // then 分支
        compile_node(ifStmt.then_block);
        _sc.emit_jump(endLabel);

        // else 分支
        _sc.mark_label(elseLabel);
        if (ifStmt.else_block is not null)
        {
            compile_node(ifStmt.else_block);
        }

        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译 switch 语句
    /// </summary>
    private void compile_switch(TsSwitchStmt switchStmt)
    {
        var endLabel = _sc.generate_label("swend");
        var defaultLabel = _sc.generate_label("swdefault");
        var labels = new List<(TsAstNode? Test, string Label)>();

        foreach (var switchCase in switchStmt.cases)
        {
            var caseLabel = _sc.generate_label("swcase");
            labels.Add((switchCase.test, caseLabel));
            if (switchCase.test is null)
            {
                defaultLabel = caseLabel;
            }
        }

        // 求值 switch 表达式
        compile_node(switchStmt.expression);

        // 对每个 case 生成比较和跳转
        foreach (var (test, caseLabel) in labels)
        {
            if (test is null)
            {
                continue; // default 最后处理
            }

            _sc.emit_dup(); // 复制栈顶供后续 case 比较
            compile_node(test);
            _sc.emit_eq();
            _sc.emit_jump_if_true(caseLabel);
            _sc.emit_pop(); // 弹出复制值
        }

        // 默认跳转到 default
        _sc.emit_pop(); // 弹出最后的比较值
        _sc.emit_jump(defaultLabel);

        // 编译每个 case 的语句
        for (var i = 0; i < switchStmt.cases.Count; i++)
        {
            var (_, caseLabel) = labels[i];
            _sc.mark_label(caseLabel);

            foreach (var stmt in switchStmt.cases[i].statements)
            {
                compile_node(stmt);
            }
        }

        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译 while 循环
    /// </summary>
    private void compile_while(TsWhileStmt whileStmt)
    {
        var loopLabel = _sc.generate_label("loop");
        var endLabel = _sc.generate_label("endloop");
        var continueLabel = _sc.generate_label("loopcont");

        _sc.enter_loop(endLabel, continueLabel);

        _sc.mark_label(loopLabel);
        compile_node(whileStmt.condition);
        _sc.emit_jump_if_false(endLabel);

        compile_node(whileStmt.body);

        _sc.mark_label(continueLabel);
        _sc.emit_jump(loopLabel);

        _sc.mark_label(endLabel);
        _sc.leave_loop();
    }

    /// <summary>
    ///     编译 do-while 循环
    /// </summary>
    private void compile_do_while(TsDoWhileStmt doWhileStmt)
    {
        var bodyLabel = _sc.generate_label("dobody");
        var endLabel = _sc.generate_label("doend");
        var continueLabel = _sc.generate_label("docont");

        _sc.enter_loop(endLabel, continueLabel);

        _sc.mark_label(bodyLabel);
        compile_node(doWhileStmt.body);

        _sc.mark_label(continueLabel);
        compile_node(doWhileStmt.condition);
        _sc.emit_jump_if_true(bodyLabel);

        _sc.mark_label(endLabel);
        _sc.leave_loop();
    }

    /// <summary>
    ///     编译 for 循环
    /// </summary>
    private void compile_for(TsForStmt forStmt)
    {
        if (forStmt.init is not null)
        {
            compile_node(forStmt.init);
        }

        var loopLabel = _sc.generate_label("forloop");
        var endLabel = _sc.generate_label("forend");
        var continueLabel = _sc.generate_label("forcont");

        _sc.enter_loop(endLabel, continueLabel);

        _sc.mark_label(loopLabel);
        if (forStmt.condition is not null)
        {
            compile_node(forStmt.condition);
            _sc.emit_jump_if_false(endLabel);
        }

        compile_node(forStmt.body);

        _sc.mark_label(continueLabel);
        if (forStmt.increment is not null)
        {
            compile_node(forStmt.increment);
            _sc.emit_pop();
        }

        _sc.emit_jump(loopLabel);
        _sc.mark_label(endLabel);
        _sc.leave_loop();
    }

    /// <summary>
    ///     编译 for-of 循环
    /// </summary>
    private void compile_for_of(TsForOfStmt forOfStmt)
    {
        // for-of 编译为 FFI 调用，运行时由 VM 处理迭代
        var varName = forOfStmt.left is TsVariableDecl varDecl ? varDecl.name : "_iter";

        // 将迭代对象加载到栈
        compile_node(forOfStmt.right);

        var endLabel = _sc.generate_label("foend");
        var continueLabel = _sc.generate_label("focont");

        _sc.enter_loop(endLabel, continueLabel);

        // 调用迭代原生函数
        _sc.emit_call_native("for_of_iter");
        // 将迭代值存到局部变量
        _sc.emit_store_local(varName);

        compile_node(forOfStmt.body);

        _sc.mark_label(continueLabel);
        _sc.emit_call_native("for_of_next");
        _sc.emit_jump_if_false(endLabel);
        _sc.emit_jump(continueLabel);

        _sc.mark_label(endLabel);
        _sc.leave_loop();
    }

    /// <summary>
    ///     编译 for-in 循环
    /// </summary>
    private void compile_for_in(TsForInStmt forInStmt)
    {
        var varName = forInStmt.left is TsVariableDecl varDecl ? varDecl.name : "_key";

        compile_node(forInStmt.right);

        var endLabel = _sc.generate_label("fiend");
        var continueLabel = _sc.generate_label("ficont");

        _sc.enter_loop(endLabel, continueLabel);

        _sc.emit_call_native("for_in_iter");
        _sc.emit_store_local(varName);

        compile_node(forInStmt.body);

        _sc.mark_label(continueLabel);
        _sc.emit_call_native("for_in_next");
        _sc.emit_jump_if_false(endLabel);
        _sc.emit_jump(continueLabel);

        _sc.mark_label(endLabel);
        _sc.leave_loop();
    }

    /// <summary>
    ///     编译 return 语句
    /// </summary>
    private void compile_return(TsReturnStmt returnStmt)
    {
        if (returnStmt.value is not null)
        {
            compile_node(returnStmt.value);
        }
        else
        {
            _sc.emit_null();
        }

        _sc.emit_return();
    }

    /// <summary>
    ///     编译 try-catch-finally 语句
    /// </summary>
    private void compile_try(TsTryStmt tryStmt)
    {
        // try-catch-finally 编译为 try_block / catch_block / finally_block 标签模式
        // catch 的参数存在局部变量中，异常由 VM 运行时推送
        var tryLabel = _sc.generate_label("try");
        var catchLabel = _sc.generate_label("catch");
        var finallyLabel = _sc.generate_label("finally");
        var endLabel = _sc.generate_label("tryend");

        _sc.mark_label(tryLabel);
        compile_node(tryStmt.block);
        _sc.emit_jump(finallyLabel);

        _sc.mark_label(catchLabel);
        if (tryStmt.catch_clause is not null)
        {
            if (tryStmt.catch_clause.parameter_name is not null)
            {
                _sc.emit_store_local(tryStmt.catch_clause.parameter_name);
            }
            else
            {
                _sc.emit_pop();
            }

            compile_node(tryStmt.catch_clause.block);
        }
        else
        {
            _sc.emit_pop();
        }

        _sc.mark_label(finallyLabel);
        if (tryStmt.finally_block is not null)
        {
            compile_node(tryStmt.finally_block);
        }

        _sc.mark_label(endLabel);
    }

    #endregion

    #region 表达式编译

    /// <summary>
    ///     编译字面量
    /// </summary>
    private void compile_literal(TsLiteral literal)
    {
        switch (literal.kind)
        {
            case "number" or "bigint":
                if (long.TryParse(literal.value, out var intVal))
                {
                    _sc.emit_const_i64(intVal);
                }
                else if (double.TryParse(literal.value,
                             System.Globalization.NumberStyles.Float,
                             System.Globalization.CultureInfo.InvariantCulture,
                             out var floatVal))
                {
                    _sc.emit_const_f64(floatVal);
                }
                else
                {
                    _sc.emit_const_i64(0);
                }

                break;
            case "string" or "template":
                _sc.emit_const_str(literal.value);
                break;
            case "true":
                _sc.emit_const_i64(1);
                break;
            case "false":
                _sc.emit_const_i64(0);
                break;
            case "null":
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
    private void compile_identifier(TsIdentifier identifier)
    {
        if (identifier.name == "undefined" || identifier.name == "null")
        {
            _sc.emit_null();
            return;
        }

        if (identifier.name == "true")
        {
            _sc.emit_const_i64(1);
            return;
        }

        if (identifier.name == "false")
        {
            _sc.emit_const_i64(0);
            return;
        }

        _sc.emit_load_local(identifier.name);
    }

    /// <summary>
    ///     编译二元表达式
    /// </summary>
    private void compile_binary(TsBinaryExpr expr)
    {
        // 短路求值特殊处理
        if (expr.@operator == "&&")
        {
            compile_logical_and(expr);
            return;
        }

        if (expr.@operator == "||")
        {
            compile_logical_or(expr);
            return;
        }

        compile_node(expr.left);
        compile_node(expr.right);

        switch (expr.@operator)
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
            case "%":
                _sc.emit_rem();
                break;
            case "==":
            case "===":
                _sc.emit_eq();
                break;
            case "!=":
            case "!==":
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
            case "**":
                _sc.emit_call_native("math_pow");
                break;
            case "in":
                _sc.emit_call_native("op_in");
                break;
            default:
                _sc.emit_null();
                break;
        }
    }

    /// <summary>
    ///     编译逻辑与（短路求值）
    /// </summary>
    private void compile_logical_and(TsBinaryExpr expr)
    {
        var falseLabel = _sc.generate_label("and_false");
        var endLabel = _sc.generate_label("and_end");

        compile_node(expr.left);
        _sc.emit_dup();
        _sc.emit_jump_if_false(falseLabel);
        _sc.emit_pop();

        compile_node(expr.right);
        _sc.emit_jump(endLabel);

        _sc.mark_label(falseLabel);
        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译逻辑或（短路求值）
    /// </summary>
    private void compile_logical_or(TsBinaryExpr expr)
    {
        var trueLabel = _sc.generate_label("or_true");
        var endLabel = _sc.generate_label("or_end");

        compile_node(expr.left);
        _sc.emit_dup();
        _sc.emit_jump_if_true(trueLabel);
        _sc.emit_pop();

        compile_node(expr.right);
        _sc.emit_jump(endLabel);

        _sc.mark_label(trueLabel);
        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译一元表达式
    /// </summary>
    private void compile_unary(TsUnaryExpr expr)
    {
        if (expr.is_prefix)
        {
            switch (expr.@operator)
            {
                case "!":
                    compile_node(expr.operand);
                    _sc.emit_not();
                    break;
                case "-":
                    _sc.emit_const_i64(0);
                    compile_node(expr.operand);
                    _sc.emit_sub();
                    break;
                case "+":
                    compile_node(expr.operand);
                    break;
                case "typeof":
                    compile_node(expr.operand);
                    _sc.emit_call_native("typeof");
                    break;
                case "void":
                    compile_node(expr.operand);
                    _sc.emit_pop();
                    _sc.emit_null();
                    break;
                case "delete":
                    compile_node(expr.operand);
                    _sc.emit_pop();
                    _sc.emit_null();
                    break;
                case "++":
                    compile_prefix_increment(expr.operand);
                    break;
                case "--":
                    compile_prefix_decrement(expr.operand);
                    break;
                default:
                    compile_node(expr.operand);
                    break;
            }

            return;
        }

        // 后缀运算符
        switch (expr.@operator)
        {
            case "++":
                compile_postfix_increment(expr.operand);
                break;
            case "--":
                compile_postfix_decrement(expr.operand);
                break;
            default:
                compile_node(expr.operand);
                break;
        }
    }

    /// <summary>
    ///     前缀自增
    /// </summary>
    private void compile_prefix_increment(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            _sc.emit_load_local(id.name);
            _sc.emit_const_i64(1);
            _sc.emit_add();
            _sc.emit_dup();
            _sc.emit_store_local(id.name);
        }
    }

    /// <summary>
    ///     前缀自减
    /// </summary>
    private void compile_prefix_decrement(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            _sc.emit_load_local(id.name);
            _sc.emit_const_i64(1);
            _sc.emit_sub();
            _sc.emit_dup();
            _sc.emit_store_local(id.name);
        }
    }

    /// <summary>
    ///     后缀自增
    /// </summary>
    private void compile_postfix_increment(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            _sc.emit_load_local(id.name);
            _sc.emit_dup();
            _sc.emit_const_i64(1);
            _sc.emit_add();
            _sc.emit_store_local(id.name);
        }
    }

    /// <summary>
    ///     后缀自减
    /// </summary>
    private void compile_postfix_decrement(TsAstNode operand)
    {
        if (operand is TsIdentifier id)
        {
            _sc.emit_load_local(id.name);
            _sc.emit_dup();
            _sc.emit_const_i64(1);
            _sc.emit_sub();
            _sc.emit_store_local(id.name);
        }
    }

    /// <summary>
    ///     编译赋值表达式
    /// </summary>
    private void compile_assignment(TsAssignmentExpr expr)
    {
        compile_node(expr.right);

        if (expr.@operator != "=")
        {
            // 增强赋值：先加载左值，做运算，再存回
            compile_node(expr.left);

            switch (expr.@operator)
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
            }
        }

        // 将结果存入目标
        if (expr.left is TsIdentifier targetId)
        {
            _sc.emit_dup();
            _sc.emit_store_local(targetId.name);
        }
        else if (expr.left is TsPropertyAccess propAccess)
        {
            // 对象属性赋值：值, 对象, 属性 → set_field
            _sc.emit_dup();
            compile_node(propAccess.@object);
            _sc.emit_set_field(propAccess.property);
        }
        else if (expr.left is TsElementAccess elemAccess)
        {
            _sc.emit_dup();
            compile_node(elemAccess.@object);
            compile_node(elemAccess.index);
            _sc.emit_set_index();
        }
    }

    /// <summary>
    ///     编译条件表达式（三元运算符）
    /// </summary>
    private void compile_conditional(TsConditionalExpr expr)
    {
        var elseLabel = _sc.generate_label("ternelse");
        var endLabel = _sc.generate_label("ternend");

        compile_node(expr.condition);
        _sc.emit_jump_if_false(elseLabel);

        compile_node(expr.then_branch);
        _sc.emit_jump(endLabel);

        _sc.mark_label(elseLabel);
        compile_node(expr.else_branch);

        _sc.mark_label(endLabel);
    }

    /// <summary>
    ///     编译函数调用
    /// </summary>
    private void compile_call(TsCallExpr callExpr)
    {
        // console.log / console.error → 调用打印内置
        if (callExpr.callee is TsPropertyAccess { @object: TsIdentifier { name: "console" } } propAccess)
        {
            foreach (var arg in callExpr.arguments)
            {
                compile_node(arg);
            }

            if (propAccess.property == "error")
            {
                _sc.emit_call_native("console_error");
            }
            else
            {
                _sc.emit_call_native("console_log");
            }

            return;
        }

        // 方法调用（含内置方法）
        if (callExpr.callee is TsPropertyAccess methodAccess)
        {
            var receiver = methodAccess.@object;

            // 内置数组/字符串方法
            switch (methodAccess.property)
            {
                case "push":
                    compile_node(receiver);
                    foreach (var arg in callExpr.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("array_push_call");
                    return;
                case "pop":
                    compile_node(receiver);
                    _sc.emit_call_native("array_pop_call");
                    return;
                case "length":
                    compile_node(receiver);
                    _sc.emit_length();
                    return;
                case "toUpperCase":
                    compile_node(receiver);
                    _sc.emit_call_native("string_to_upper");
                    return;
                case "toLowerCase":
                    compile_node(receiver);
                    _sc.emit_call_native("string_to_lower");
                    return;
                case "trim":
                    compile_node(receiver);
                    _sc.emit_call_native("string_trim");
                    return;
                case "includes":
                    compile_node(receiver);
                    foreach (var arg in callExpr.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("string_includes");
                    return;
                case "indexOf":
                    compile_node(receiver);
                    foreach (var arg in callExpr.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("string_index_of");
                    return;
                case "split":
                    compile_node(receiver);
                    foreach (var arg in callExpr.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("string_split");
                    return;
                default:
                    // 通用方法调用：receiver → 参数 → call
                    compile_node(receiver);
                    foreach (var arg in callExpr.arguments)
                    {
                        compile_node(arg);
                    }

                    _sc.emit_call_native("method_call");
                    return;
            }
        }

        // 普通函数调用
        if (callExpr.callee is TsIdentifier funcId)
        {
            foreach (var arg in callExpr.arguments)
            {
                compile_node(arg);
            }

            _sc.emit_call(funcId.name, callExpr.arguments.Count);
            return;
        }

        // 匿名函数调用
        compile_node(callExpr.callee);
        foreach (var arg in callExpr.arguments)
        {
            compile_node(arg);
        }

        _sc.emit_call_native("apply_call");
    }

    /// <summary>
    ///     编译属性访问
    /// </summary>
    private void compile_property_access(TsPropertyAccess propAccess)
    {
        // console 对象
        if (propAccess.@object is TsIdentifier { name: "console" })
        {
            _sc.emit_null();
            return;
        }

        // length 属性
        if (propAccess.property == "length")
        {
            compile_node(propAccess.@object);
            _sc.emit_length();
            return;
        }

        // 通用属性访问
        compile_node(propAccess.@object);
        _sc.emit_get_field(propAccess.property);
    }

    /// <summary>
    ///     编译下标访问
    /// </summary>
    private void compile_element_access(TsElementAccess elemAccess)
    {
        compile_node(elemAccess.@object);
        compile_node(elemAccess.index);
        _sc.emit_get_index();
    }

    /// <summary>
    ///     编译数组字面量
    /// </summary>
    private void compile_array_literal(TsArrayLiteral arrayLit)
    {
        _sc.emit_new_object();

        foreach (var elem in arrayLit.elements)
        {
            if (elem is TsSpreadElement spread)
            {
                compile_node(spread.argument);
                _sc.emit_call_native("array_spread");
            }
            else
            {
                compile_node(elem);
                _sc.emit_array_push();
            }
        }
    }

    /// <summary>
    ///     编译对象字面量
    /// </summary>
    private void compile_object_literal(TsObjectLiteral objLit)
    {
        _sc.emit_new_object();

        foreach (var prop in objLit.properties)
        {
            _sc.emit_const_str(prop.key);
            compile_node(prop.value);
            _sc.emit_set_index();
        }
    }

    /// <summary>
    ///     编译箭头函数
    /// </summary>
    private void compile_arrow_function(TsArrowFunctionExpr arrowFunc)
    {
        // 箭头函数编译为独立函数并加载其引用
        var funcName = _sc.generate_label("lambda");
        var savedFunc = _sc.current_function;

        _sc.begin_function(funcName);
        foreach (var param in arrowFunc.parameters)
        {
            _sc.add_parameter(param.name.TrimStart('.'));
        }

        if (arrowFunc.body is TsBlockStmt block)
        {
            foreach (var stmt in block.statements)
            {
                compile_node(stmt);
            }
        }
        else
        {
            compile_node(arrowFunc.body);
            _sc.emit_return();
        }

        // 如果没有 return，补 null + return
        _sc.emit_null();
        _sc.emit_return();
        _sc.end_function();

        // 在调用处（原函数）加载函数引用
        // 注意：由于 StackCompiler 当前设计限制，我们先简化处理
        _sc.emit_call_native("create_lambda");
    }

    /// <summary>
    ///     编译函数表达式
    /// </summary>
    private void compile_function_expr(TsFunctionExpr funcExpr)
    {
        var funcName = funcExpr.name ?? _sc.generate_label("func");
        var savedFunc = _sc.current_function;

        _sc.begin_function(funcName);
        foreach (var param in funcExpr.parameters)
        {
            _sc.add_parameter(param.name.TrimStart('.'));
        }

        compile_node(funcExpr.body);

        _sc.emit_null();
        _sc.emit_return();
        _sc.end_function();

        _sc.emit_call_native("create_lambda");
    }

    /// <summary>
    ///     编译 new 表达式
    /// </summary>
    private void compile_new_expr(TsNewExpr newExpr)
    {
        // new Array(n) → emit_call_native("new_array")
        if (newExpr.callee is TsIdentifier { name: "Array" })
        {
            foreach (var arg in newExpr.arguments)
            {
                compile_node(arg);
            }

            _sc.emit_call_native("new_array");
            return;
        }

        // new Map / new Set → 创建空对象
        if (newExpr.callee is TsIdentifier { name: "Map" or "Set" })
        {
            _sc.emit_new_object();
            return;
        }

        // 通用构造
        compile_node(newExpr.callee);
        foreach (var arg in newExpr.arguments)
        {
            compile_node(arg);
        }

        _sc.emit_call_native("new_call");
    }

    #endregion
}