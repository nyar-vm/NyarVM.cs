using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Nyar.Analyzer.Semantic;
using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Language.Valkyrie.TypeSystem;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Template;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using DiagnosticTextSpan = Std.Text.TextSpan;

namespace Nyar.Language.Valkyrie.Compiler.Mir;

/// <summary>
///     "HIR 降级"MIR（EGraph + Oa"/// </summary>
public sealed class HirToMirLowerer
{
    private readonly DiagnosticSink? _diagnostics;

    /// <summary>
    ///     初始"`HIR` "`MIR` "lowering 器"    /// </summary>
    /// <param name="diagnostics">可选的诊断收集器，用于报告 lowering 过程中的警告"/param>
    public HirToMirLowerer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     "`HIR` 模块 lowering "`MIR` 模块"    /// </summary>
    /// <param name="hir">输入"`HIR` 模块"/param>
    /// <param name="targetArchTag">目标架构标签"/param>
    /// <returns>lowering 后的 `MIR` 模块"/returns>
    public MirModule lower(HirModule hir, string targetArchTag, string? preferredLogicalEntry = null)
    {
        var graph = new EGraph<AlgebraNode>();
        var reachableCallables = hir.enumerate_reachable_aot_callables(preferredLogicalEntry);
        var members = new List<Id>(reachableCallables.Count);
        var module = new MirModule(hir.name, graph, targetArchTag);
        module.set_data_derive_requests(hir.enumerate_reachable_data_derive_requests(preferredLogicalEntry));

        foreach (var type in hir.types)
        {
            if (type.external_import_links.Count == 0)
            {
                continue;
            }

            module.set_type_external_import_links(type.name, type.external_import_links);
            module.set_type_external_import_links(type.namepath, type.external_import_links);

            var ownedTextName = try_get_owned_text_name_for_class(type.name);
            if (!string.IsNullOrWhiteSpace(ownedTextName))
            {
                module.set_type_external_import_links(ownedTextName!, type.external_import_links);
            }
        }

        foreach (var imply in hir.implys)
        foreach (var binding in imply.witness_bindings)
            module.add_witness_binding(new MirWitnessDispatchBinding(
                binding.trait_name.ToString(),
                binding.slot_index,
                binding.method_name,
                imply.target_type.name,
                binding.implementation.name));

        // 外部导入声明（无函数体）不依赖可达性分析：即使逻辑入口的调用链未能解析到它们，
        // 也必须保留元数据，以便后端能将其注册为导入符号"        var processedNames = new HashSet<string>(StringComparer.Ordinal);
        var externalDeclarations = hir.enumerate_aot_callables()
            .Where(callable => callable.body is null)
            .ToArray();
        Console.Error.WriteLine($"[DEBUG lower] reachableCallables={reachableCallables.Count}, externalDeclarations={externalDeclarations.Length}");
        foreach (var ed in externalDeclarations)
            Console.Error.WriteLine($"[DEBUG lower] external: {ed.name} (attrs={string.Join(",", ed.surface_attributes.Select(a => a.name))})");
        var allCallables = reachableCallables.Concat(
            externalDeclarations.Where(external => reachableCallables.All(r => !string.Equals(r.name, external.name, StringComparison.Ordinal))));

        foreach (var callable in allCallables)
        {
            if (!processedNames.Add(callable.name))
            {
                continue;
            }

            // 外部导入声明无函数体，跳"EGraph 降级，只保留元数据供后端注册为导入符号"            if (callable.body is not null)
            {
                var functionId = build_callable(hir, callable, graph, targetArchTag);
                members.Add(functionId);
            }

            var parameterTypes = callable.parameters
                .Select(parameter => parameter.type)
                .ToArray();
            module.set_parameter_types(callable.name, parameterTypes);
            module.set_return_type(callable.name, callable.return_type);
            module.set_effect_set(callable.name, callable.effect_set);

            module.set_function_attributes(callable.name, callable.surface_attributes);
            module.set_function_external_import_links(callable.name, callable.semantics.external_import_links);
            if (HirAttributeSemantics.try_get_intrinsic_name(callable.surface_attributes, out var intrinsicName))
                module.set_function_intrinsic(callable.name, intrinsicName);
            if (callable.is_logical_entry) module.add_export_function(callable.name);
        }

        var moduleId = graph.add(new AlgebraNode.Module(hir.name, [.. members]));
        graph.rebuild();
        module.root = moduleId;
        return module;
    }

    private static IReadOnlyList<HirCallable> collect_reachable_aot_callables(
        HirModule hir,
        string? preferredLogicalEntry)
    {
        var allCallables = hir.enumerate_aot_callables();
        var logicalEntries = allCallables
            .Where(callable => callable.is_logical_entry &&
                               logical_entry_matches(callable.name, preferredLogicalEntry))
            .ToArray();

        if (logicalEntries.Length == 0) return allCallables;

        var callableMap = allCallables
            .GroupBy(callable => callable.name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var reachableNames =
            new HashSet<string>(logicalEntries.Select(callable => callable.name), StringComparer.Ordinal);
        var pending = new Queue<HirCallable>(logicalEntries);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            collect_reachable_calls(current.body, hir, current, callableMap, reachableNames, pending);
        }

        return
        [
            .. allCallables
                .Where(callable => reachableNames.Contains(callable.name))
        ];
    }

    /// <summary>
    ///     判断 callable 是否匹配当前构建请求的逻辑入口"    ///     未指定时保留全部逻辑入口；指定时同时兼容简单名和限定名"    /// </summary>
    private static bool logical_entry_matches(string callableName, string? preferredLogicalEntry)
    {
        if (string.IsNullOrWhiteSpace(preferredLogicalEntry))
        {
            return true;
        }

        return string.Equals(callableName, preferredLogicalEntry, StringComparison.Ordinal) ||
               callableName.EndsWith("." + preferredLogicalEntry, StringComparison.Ordinal);
    }

    /// <summary>
    ///     "owned_text 对应的承载类名反解为文本类型名"    /// </summary>
    private static string? try_get_owned_text_name_for_class(string? typeName)
    {
        return typeName switch
        {
            "Utf8Text" => ValkyrieTextTypeFacts.utf8_name,
            "Utf16Text" => ValkyrieTextTypeFacts.utf16_name,
            "Utf32Text" => ValkyrieTextTypeFacts.utf32_name,
            "AsciiText" => "ascii",
            _ => null
        };
    }

    private static void collect_reachable_calls(
        object? node,
        HirModule hir,
        HirCallable currentCallable,
        IReadOnlyDictionary<string, HirCallable> callableMap,
        ISet<string> reachableNames,
        Queue<HirCallable> pending)
    {
        foreach (var callSite in enumerate_call_sites(node))
            if (hir.try_resolve_call(currentCallable, callSite, out var resolution) &&
                resolution is { dispatch: HirDispatchKind.@static, target_name: var targetName } &&
                callableMap.TryGetValue(targetName, out var targetCallable) &&
                reachableNames.Add(targetName))
                pending.Enqueue(targetCallable);
    }

    private static IEnumerable<AstNode> enumerate_call_sites(object? value)
    {
        if (value is null or string) yield break;

        if (value is TermCallExpression call) yield return call;

        if (value is TermDotExpression { call_body: not null } dotCall) yield return dotCall;

        if (value is IEnumerable enumerable and not ValkyrieNode)
        {
            foreach (var item in enumerable)
            foreach (var child in enumerate_call_sites(item))
                yield return child;

            yield break;
        }

        if (value is not ValkyrieNode)
        {
            yield break;
        }

        var properties = value.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            var childValue = property.GetValue(value);
            foreach (var child in enumerate_call_sites(childValue)) yield return child;
        }

        var fields = value.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (var field in fields)
        {
            var childValue = field.GetValue(value);
            foreach (var child in enumerate_call_sites(childValue)) yield return child;
        }
    }

    private Id build_callable(HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var bodyBlock = callable.body;
        var bodyId = build_block_implicit_return(bodyBlock, hir, callable, graph, targetArchTag);

        var parameterNames = callable.parameters.Select(parameter => parameter.name).ToImmutableArray();
        var lambdaId = graph.add(new AlgebraNode.Lambda(parameterNames, bodyId));
        return graph.add(new AlgebraNode.Export(callable.name, lambdaId));
    }

    /// <summary>
    ///     "`HirTypeRef` 的类型名映射"`LIR GenerateFunction` 所需的类型名字串"    /// </summary>
    private static string map_hir_type_to_lir_type_name(HirTypeRef hirType)
    {
        ValkyrieTextTypeFacts.ensure_no_pre_hir_literal_type(hirType.name, "MIR");
        ensure_no_legacy_text_type_alias(hirType.name, "MIR");
        ensure_no_legacy_scalar_type_alias(hirType.name, "MIR");
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(hirType.name);
        return hirType.special_kind switch
        {
            HirSpecialTypeKind.unit => "unit",
            HirSpecialTypeKind.@void => "void",
            HirSpecialTypeKind.auto => "i32",
            HirSpecialTypeKind.exit_code => "i32",
            _ => normalizedTypeName switch
            {
                "i32" => "i32",
                "i64" => "i64",
                "f32" => "f32",
                "f64" => "f64",
                "bool" => "bool",
                "utf8" => "utf8",
                "utf16" => "utf16",
                "utf32" => "utf32",
                "char" => "char",
                "Unit" or "unit" => "unit",
                "Void" or "void" or "Never" or "never" => "void",
                _ => "i32"
            }
        };
    }

    private Id build_block(BlockStmt? block, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        if (block is null) return graph.add(AlgebraNodeBuilder.None());

        var statementIds = block.statements
            .Select(statement => build_statement(statement, hir, callable, graph, targetArchTag))
            .ToArray();

        return build_sequence(statementIds, graph);
    }

    private Id build_block_implicit_return(BlockStmt? block, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        if (block is null) return graph.add(AlgebraNodeBuilder.None());

        var statements = block.statements;
        if (statements.Count == 0) return graph.add(AlgebraNodeBuilder.None());

        // 临时调试
        // Console.Error.WriteLine($"[MIR DEBUG] build_block_implicit_return callable={callable.name}, statementCount={statements.Count}");
        for (var i = 0; i < statements.Count; i++)
        {
            // Console.Error.WriteLine($"  stmt[{i}] type={statements[i].GetType().Name}");
        }

        var lastStmt = statements[^1];
        if (lastStmt is ReturnStatement) return build_block(block, hir, callable, graph, targetArchTag);

        var allButLastIds = statements.Take(statements.Count - 1)
            .Select(statement => build_statement(statement, hir, callable, graph, targetArchTag))
            .ToList();

        Id lastId;
        if (lastStmt is TermNode term)
        {
            var exprId = build_expression_or_none(term, hir, callable, graph, targetArchTag);
            lastId = graph.add(AlgebraNodeBuilder.Return(exprId));
        }
        else
        {
            lastId = build_statement(lastStmt, hir, callable, graph, targetArchTag);
        }

        allButLastIds.Add(lastId);

        return build_sequence(allButLastIds, graph);
    }

    private Id build_sequence(IReadOnlyList<Id> ids, EGraph<AlgebraNode> graph)
    {
        if (ids.Count == 0) return graph.add(AlgebraNodeBuilder.None());

        if (ids.Count == 1) return ids[0];

        return graph.add(AlgebraNodeBuilder.Seq([.. ids]));
    }

    private Id build_statement(AstNode statement, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        // 临时调试：打印语句类型（生产环境已关闭）
        // Console.Error.WriteLine($"[MIR DEBUG] build_statement type={statement.GetType().Name}, callable={callable.name}");

        return statement switch
        {
            AssignmentStatement assignment => build_assignment_statement(assignment, hir, callable, graph, targetArchTag),
            ReturnStatement ret =>
                graph.add(AlgebraNodeBuilder.Return(build_expression_or_none(ret.value, hir, callable, graph, targetArchTag))),
            BreakStatement => graph.add(AlgebraNodeBuilder.Break()),
            ContinueStatement => graph.add(AlgebraNodeBuilder.Continue()),
            TermNode term => build_expression(term, hir, callable, graph, targetArchTag),
            LetDeclaration let => build_let_declaration(let, hir, callable, graph, targetArchTag),
            TemplateMatchNode templateMatch => report_unhandled_statement(templateMatch, graph),
            IfStatement ifStmt => build_if_statement(ifStmt, hir, callable, graph, targetArchTag),
            LoopStatement loop => build_loop_statement(loop, hir, callable, graph, targetArchTag),
            LoopInStatement loopIn => build_loop_in_statement(loopIn, hir, callable, graph, targetArchTag),
            WhileStatement whileStmt => build_while_statement(whileStmt, hir, callable, graph, targetArchTag),
            UntilStatement untilStmt => build_until_statement(untilStmt, hir, callable, graph, targetArchTag),
            MatchStatementNode matchStmt => build_match_statement(matchStmt, hir, callable, graph, targetArchTag),
            CatchStatementNode catchStmt => build_catch_statement(catchStmt, hir, callable, graph, targetArchTag),
            ResumeStatement resume => build_resume_statement(resume, hir, callable, graph, targetArchTag),
            RaiseStatement raiseStmt => build_raise_statement(raiseStmt, hir, callable, graph, targetArchTag),
            TryStatement tryStmt => build_try_statement(tryStmt, hir, callable, graph, targetArchTag),
            _ => report_unhandled_statement(statement, graph)
        };
    }

    private Id report_unhandled_statement(AstNode statement, EGraph<AlgebraNode> graph)
    {
        _diagnostics?.report(new DiagnosticTextSpan(statement.span.start, statement.span.length),
            $"MIR 降级：未处理的语句类"'{statement.GetType().Name}'，将被跳过",
            DiagnosticSeverity.warning);
        return graph.add(AlgebraNodeBuilder.None());
    }

    private Id build_expression_or_none(AstNode? node, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        if (node is null) return graph.add(AlgebraNodeBuilder.None());

        return build_expression(node, hir, callable, graph, targetArchTag);
    }

    private Id build_expression(AstNode expression, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        return expression switch
        {
            TermLiteralNumberNode literal => build_literal(literal, graph),
            TermLiteralTextNode literal => build_literal(literal, graph),
            TermLiteralBooleanNode literal => build_literal(literal, graph),
            LiteralNullNode literal => build_literal(literal, graph),
            IdentifierNode ident => try_build_top_level_constant_reference(hir, callable, ident.name, graph,
                targetArchTag, out var identifierValueId)
                ? identifierValueId
                : graph.add(AlgebraNodeBuilder.Symbol(ident.name)),
            TermLiteralNamePathNode termPath => try_build_top_level_constant_reference(hir, callable,
                termPath.path.full_name, graph, targetArchTag, out var pathValueId)
                ? pathValueId
                : try_build_unite_variant_ctor(termPath.path.full_name, hir, graph, out var pathVariantId)
                    ? pathVariantId
                    : graph.add(AlgebraNodeBuilder.Symbol(termPath.path.full_name)),
            TermAsExpression cast => build_cast_expression(cast, hir, callable, graph, targetArchTag),
            TermIsExpression isExpression => build_is_expression(isExpression, hir, callable, graph),
            TermInExpression inExpression => build_in_expression(inExpression, hir, callable, graph, targetArchTag),
            TermBinaryExpression binary => build_binary_expression(binary, hir, callable, graph, targetArchTag),
            TermUnaryExpression unary => build_unary_expression(unary, hir, callable, graph, targetArchTag),
            TermCallExpression call => build_call_expression(call, hir, callable, graph, targetArchTag),
            TermDotExpression { call_body: not null } dot => build_dot_call_expression(dot, hir, callable, graph,
                targetArchTag),
            TermDotExpression { call_body: null } dot => build_dot_field_expression(dot, hir, callable, graph,
                targetArchTag),
            TermOrdinalExpression ordinal => build_ordinal_expression(ordinal, hir, callable, graph, targetArchTag),
            TermOffsetExpression offset => build_offset_expression(offset, hir, callable, graph, targetArchTag),
            TermLiteralObjectNode objectLiteral => build_object_literal(objectLiteral, hir, callable, graph,
                targetArchTag),
            TermLiteralArrayNode array => graph.add(AlgebraNodeBuilder.ArrayLiteral(
                [.. array.elements.Select(element => build_expression(element, hir, callable, graph, targetArchTag))])),
            TermLiteralTupleNode tuple => build_tuple_literal(tuple, hir, callable, graph, targetArchTag),
            TermCatchExpression catchExpr => build_catch_expression(catchExpr, hir, callable, graph, targetArchTag),
            TermMatchExpression matchExpr => build_match_expression(matchExpr, hir, callable, graph, targetArchTag),
            TermNode term => report_unhandled_expression(term, graph),
            LetDeclaration let => build_let_declaration(let, hir, callable, graph, targetArchTag),
            ReturnStatement ret =>
                graph.add(AlgebraNodeBuilder.Return(build_expression_or_none(ret.value, hir, callable, graph, targetArchTag))),
            BreakStatement => graph.add(AlgebraNodeBuilder.Break()),
            ContinueStatement => graph.add(AlgebraNodeBuilder.Continue()),
            _ => report_unhandled_expression(expression, graph)
        };
    }

    private Id report_unhandled_expression(AstNode expression, EGraph<AlgebraNode> graph)
    {
        _diagnostics?.report(new DiagnosticTextSpan(expression.span.start, expression.span.length),
            $"MIR 降级：未处理的表达式类型 '{expression.GetType().Name}'，将被跳过",
            DiagnosticSeverity.warning);
        return graph.add(AlgebraNodeBuilder.None());
    }

    /// <summary>
    ///     构建对象字面量表达式，生"NewObject + SetField 节点"    ///     对于 unite/union 类型的变体构造，自动追加 <c>SetField("__tag", Const(discriminant))</c> 以写入判别值"    /// </summary>
    private Id build_object_literal(TermLiteralObjectNode literal, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var typeName = try_resolve_constructor_type_name(literal.constructor, out var name)
            ? qualify_constructor_type_name(name, callable.namespace_path)
            : string.Empty;

        if (literal.fields.Count == 0 && !literal.has_spread)
        {
            var emptyObjectId = graph.add(new AlgebraNode.NewObject(typeName, 0));

            if (try_resolve_unite_variant_discriminant(hir, typeName, out var emptyTag))
            {
                var emptyTagId = graph.add(AlgebraNodeBuilder.Constant(emptyTag ?? 0));
                var emptyTagSetId = graph.add(new AlgebraNode.SetField(emptyObjectId, "__tag", emptyTagId));
                return build_sequence([emptyObjectId, emptyTagSetId], graph);
            }

            return emptyObjectId;
        }

        var newObjectId = graph.add(new AlgebraNode.NewObject(typeName, literal.fields.Count));
        var setFieldIds = new List<Id> { newObjectId };

        foreach (var field in literal.fields)
        {
            var fieldValueId = field.value is not null
                ? build_expression(field.value, hir, callable, graph, targetArchTag)
                : graph.add(AlgebraNodeBuilder.Symbol(field.name));
            var setFieldId = graph.add(new AlgebraNode.SetField(newObjectId, field.name, fieldValueId));
            setFieldIds.Add(setFieldId);
        }

        if (try_resolve_unite_variant_discriminant(hir, typeName, out var tag))
        {
            var tagValueId = graph.add(AlgebraNodeBuilder.Constant(tag ?? 0));
            var tagSetId = graph.add(new AlgebraNode.SetField(newObjectId, "__tag", tagValueId));
            setFieldIds.Add(tagSetId);
        }

        return build_sequence(setFieldIds, graph);
    }

    /// <summary>
    ///     构建 tuple 字面量表达式，生成匿名值对象构造序列"    ///     tuple 在语义上是前端原生值类型，这里仅注册后端可用的匿名结构形状"    ///     避免误降级为数组/宿主 tuple 对象"    /// </summary>
    private Id build_tuple_literal(TermLiteralTupleNode literal, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var fieldCount = literal.elements.Count;
        var tupleObjectId = graph.add(new AlgebraNode.NewObject($"__tuple_arity{fieldCount}", fieldCount));
        var setFieldIds = new List<Id>(fieldCount + 1) { tupleObjectId };

        for (var i = 0; i < fieldCount; i++)
        {
            var elementId = build_expression(literal.elements[i], hir, callable, graph, targetArchTag);
            var setFieldId = graph.add(new AlgebraNode.SetField(tupleObjectId, $"_{i}", elementId));
            setFieldIds.Add(setFieldId);
        }

        return build_sequence(setFieldIds, graph);
    }

    /// <summary>
    ///     尝试"HIR 类型定义中查找构造器名称对应"unite/union 变体判别值"    ///     遍历所有类型，找到包含匹配变体名的 unite/union 类型，返回其判别值"    /// </summary>
    /// <param name="hir">HIR 模块"/param>
    /// <param name="constructorName">构造器名称（即变体名称）"/param>
    /// <param name="discriminant">找到的判别值"/param>
    /// <returns>是否成功找到判别值"/returns>
    private static bool try_resolve_unite_variant_discriminant(HirModule hir, string constructorName,
        out long? discriminant)
    {
        discriminant = null;

        if (string.IsNullOrWhiteSpace(constructorName)) return false;

        foreach (var type in hir.types.OfType<HirUniteDef>())
        {
            foreach (var variant in type.variants)
            {
                if (!string.Equals(variant.name, constructorName, StringComparison.Ordinal)) continue;

                discriminant = variant.discriminant;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     尝试"unite/union 变体构造器名（"`EndOfFile`）构建为 MIR "NewObject + SetField("__tag", discriminant) 序列"    ///     当该名称确实对应一"unite/union 变体时返"true，否则返"false"    /// </summary>
    private static bool try_build_unite_variant_ctor(string constructorName, HirModule hir,
        EGraph<AlgebraNode> graph, out Id valueId)
    {
        if (string.IsNullOrWhiteSpace(constructorName))
        {
            valueId = default;
            return false;
        }

        if (!try_resolve_unite_variant_discriminant(hir, constructorName, out var discriminant))
        {
            valueId = default;
            return false;
        }

        // 变体名本身就是构造器名，但不一定包含类型信息"        // 我们需要找到包含该变体"unite/union 类型名"        string? typeName = null;
        foreach (var type in hir.types.OfType<HirUniteDef>())
        {
            foreach (var variant in type.variants)
            {
                if (string.Equals(variant.name, constructorName, StringComparison.Ordinal))
                {
                    typeName = type.name;
                    break;
                }
            }
            if (typeName is not null) break;
        }

        if (typeName is null)
        {
            valueId = default;
            return false;
        }

        var newObjectId = graph.add(new AlgebraNode.NewObject(typeName, 0));
        var tagId = graph.add(AlgebraNodeBuilder.Constant(discriminant.Value));
        var tagSetId = graph.add(new AlgebraNode.SetField(newObjectId, "__tag", tagId));
        valueId = graph.add(AlgebraNodeBuilder.Seq([newObjectId, tagSetId]));
        return true;
    }

    private Id build_is_expression(
        TermIsExpression isExpression,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph)
    {
        if (!is_static_is_operand_supported(isExpression.operand))
            throw new NotSupportedException(
                "`is` 当前仅支持标识符与纯字面量操作数；需要运行时求值协议信息的场景，请先补齐共享类型测试语义");

        if (!hir.try_resolve_expression_type(callable, isExpression.operand, out var sourceType))
            throw new NotSupportedException(
                $"`is` 左侧表达式 `{isExpression.operand.GetType().Name}` 的静态类型尚无法稳定解析，暂时不能无债接入三后端主链");

        if (!try_extract_is_target_type_name(isExpression.target_pattern_node, out var targetTypeName))
            throw new NotSupportedException(
                $"`is` 目标模式 `{isExpression.target_pattern_node.GetType().Name}` 尚未接入当前主链；请先补齐共享类型模式语义");

        var normalizedSourceTypeName = normalize_is_type_name(sourceType.name);
        var normalizedTargetTypeName = normalize_is_type_name(targetTypeName);
        var result = normalizedSourceTypeName == normalizedTargetTypeName
                     || hir.is_type_compatible(sourceType, targetTypeName, callable.namespace_name);

        if (result) return graph.add(AlgebraNodeBuilder.BooleanConstant(true));

        if (is_scalar_is_type_name(normalizedSourceTypeName) && is_scalar_is_type_name(normalizedTargetTypeName))
            return graph.add(AlgebraNodeBuilder.BooleanConstant(false));

        var operandId = build_expression(isExpression.operand, hir, callable, graph, string.Empty);
        var typeRefId = graph.add(AlgebraNodeBuilder.Symbol(targetTypeName));
        var typeCheckFuncId = graph.add(AlgebraNodeBuilder.Symbol("__nyar_typecheck"));
        return graph.add(new Call(typeCheckFuncId, [operandId, typeRefId]));
    }

    private Id build_cast_expression(
        TermAsExpression cast,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var valueId = build_expression(cast.operand, hir, callable, graph, targetArchTag);
        var typeRefId = graph.add(AlgebraNodeBuilder.TypeRef(get_type_name(cast.target_type), ImmutableArray<Id>.Empty));

        if (cast.is_nullable)
        {
            var asConvertFuncId = graph.add(AlgebraNodeBuilder.Symbol("__nyar_asconvert"));
            return graph.add(new Call(asConvertFuncId, [valueId, typeRefId]));
        }

        return graph.add(AlgebraNodeBuilder.Cast(valueId, typeRefId));
    }

    private Id build_in_expression(
        TermInExpression inExpression,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var valueId = build_expression(inExpression.operand, hir, callable, graph, targetArchTag);
        var targetId = build_expression(inExpression.target, hir, callable, graph, targetArchTag);

        var containsFuncId = graph.add(AlgebraNodeBuilder.Symbol("contains"));
        return graph.add(new Call(containsFuncId, [targetId, valueId]));
    }

    private Id build_binary_expression(
        TermBinaryExpression binary,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        // logical_and / logical_or 保留"HIR 二元节点，此处降级为短路求值：
        // a || b -> if a then true else b
        // a && b -> if a then b else false
        if (binary.@operator is TermBinaryOperator.logical_or or TermBinaryOperator.logical_and)
        {
            var leftId = build_expression(binary.left, hir, callable, graph, targetArchTag);
            var rightId = build_expression(binary.right, hir, callable, graph, targetArchTag);
            var trueId = graph.add(AlgebraNodeBuilder.BooleanConstant(true));
            var falseId = graph.add(AlgebraNodeBuilder.BooleanConstant(false));
            return binary.@operator == TermBinaryOperator.logical_or
                ? graph.add(new Choice(leftId, trueId, rightId))
                : graph.add(new Choice(leftId, rightId, falseId));
        }

        throw new NotSupportedException(
            $"HIR 中不应再保留二元运算 `{binary.@operator}`；请先完成 `operator -> method call` 解糖");
    }

    private Id build_unary_expression(
        TermUnaryExpression unary,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        throw new NotSupportedException(
            $"HIR 中不应再保留一元运算 `{unary.@operator}`；请先完成 `operator -> method call` 解糖");
    }

    private Id build_assignment_statement(
        AssignmentStatement assignment,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var rightValue = build_expression(assignment.value, hir, callable, graph, targetArchTag);

        return assignment.target switch
        {
            TermLiteralNamePathNode namePath => build_simple_assign(graph, namePath.path.full_name, rightValue),
            IdentifierNode identifier => build_simple_assign(graph, identifier.name, rightValue),
            TermDotExpression dotExpr => build_field_assign(hir, callable, graph, targetArchTag, dotExpr, rightValue),
            TermIndexExpression indexExpr => build_index_assign(hir, callable, graph, targetArchTag, indexExpr, rightValue),
            TermOrdinalExpression ordinalExpr => build_ordinal_assign(hir, callable, graph, targetArchTag, ordinalExpr, rightValue),
            TermOffsetExpression offsetExpr => build_offset_assign(hir, callable, graph, targetArchTag, offsetExpr, rightValue),
            _ => throw new NotSupportedException(
                $"当前仅支持对符号、字段和索引的赋值，实际左值节点为 `{assignment.target.GetType().Name}`")
        };
    }

    private Id build_simple_assign(EGraph<AlgebraNode> graph, string symbolName, Id rightValue)
    {
        var targetSymbol = graph.add(AlgebraNodeBuilder.Symbol(symbolName));
        return graph.add(AlgebraNodeBuilder.StateUpdate(targetSymbol, rightValue));
    }

    private Id build_field_assign(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        TermDotExpression dotExpr,
        Id rightValue)
    {
        var targetId = build_expression(dotExpr.caller, hir, callable, graph, targetArchTag);
        if (try_build_tuple_ordinal_index_id(hir, callable, dotExpr.caller, dotExpr.callee.name, graph, out var ordinalIndexId))
        {
            return graph.add(new SetOrdinalIdx(targetId, ordinalIndexId, rightValue));
        }

        var fieldName = dotExpr.callee.name;
        return graph.add(new AlgebraNode.SetField(targetId, fieldName, rightValue));
    }

    private Id build_index_assign(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        TermIndexExpression indexExpr,
        Id rightValue)
    {
        throw new NotSupportedException(
            $"MIR 不应再接收到遗留统一索引赋值 `{indexExpr.GetType().Name}`；请先在前端明确区分序数索引和偏移索引");
    }

    private Id build_ordinal_assign(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        TermOrdinalExpression ordinalExpr,
        Id rightValue)
    {
        return build_multi_ordinal_assign(
            hir,
            callable,
            graph,
            targetArchTag,
            ordinalExpr.target,
            ordinalExpr.indices,
            rightValue);
    }

    private Id build_offset_assign(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        TermOffsetExpression offsetExpr,
        Id rightValue)
    {
        return build_multi_offset_assign(
            hir,
            callable,
            graph,
            targetArchTag,
            offsetExpr.target,
            offsetExpr.indices,
            rightValue);
    }

    private Id build_multi_ordinal_assign(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        ValkyrieNode target,
        IReadOnlyList<ValkyrieNode> indices,
        Id rightValue)
    {
        if (indices.Count == 0)
        {
            throw new NotSupportedException("索引赋值至少需要一个索引参数");
        }

        var targetId = build_expression(target, hir, callable, graph, targetArchTag);

        for (var i = 0; i < indices.Count - 1; i++)
        {
            var nestedIndexId = build_expression(indices[i], hir, callable, graph, targetArchTag);
            targetId = graph.add(new GetOrdinalIdx(targetId, nestedIndexId));
        }

        var lastIndexId = build_expression(indices[^1], hir, callable, graph, targetArchTag);

        return graph.add(new SetOrdinalIdx(targetId, lastIndexId, rightValue));
    }

    private Id build_multi_offset_assign(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        ValkyrieNode target,
        IReadOnlyList<ValkyrieNode> indices,
        Id rightValue)
    {
        if (indices.Count == 0)
        {
            throw new NotSupportedException("索引赋值至少需要一个索引参数");
        }

        var targetId = build_expression(target, hir, callable, graph, targetArchTag);

        for (var i = 0; i < indices.Count - 1; i++)
        {
            var nestedIndexId = build_expression(indices[i], hir, callable, graph, targetArchTag);
            targetId = graph.add(new GetOffsetIdx(targetId, nestedIndexId));
        }

        var lastIndexId = build_expression(indices[^1], hir, callable, graph, targetArchTag);

        return graph.add(new SetOffsetIdx(targetId, lastIndexId, rightValue));
    }

    private static bool is_static_is_operand_supported(AstNode operand)
    {
        return operand is IdentifierNode
            or TermLiteralNamePathNode
            or TermLiteralNumberNode
            or TermLiteralTextNode
            or TermLiteralBooleanNode
            or LiteralNullNode;
    }

    private static bool try_extract_is_target_type_name(PatternNode pattern, out string typeName)
    {
        switch (pattern)
        {
            case PatternLiteralObjectNode { path.full_name: { Length: > 0 } qualifiedName }:
                typeName = qualifiedName;
                return true;
            case PatternLiteralObjectNode { path.name: { Length: > 0 } simpleName }:
                typeName = simpleName;
                return true;
            default:
                typeName = string.Empty;
                return false;
        }
    }

    private static bool is_scalar_is_type_name(string typeName)
    {
        return typeName is "bool"
            or "utf8"
            or "utf16"
            or "utf32"
            or "c_str"
            or "char"
            or "i8"
            or "i16"
            or "i32"
            or "i64"
            or "isize"
            or "u8"
            or "u16"
            or "u32"
            or "u64"
            or "usize"
            or "f32"
            or "f64"
            or "unit"
            or "void"
            or "null";
    }

    private static string normalize_is_type_name(string typeName)
    {
        ValkyrieTextTypeFacts.ensure_no_pre_hir_literal_type(typeName, "MIR");
        ValkyrieTextTypeFacts.ensure_no_legacy_text_ref_type(typeName, "MIR");
        ensure_no_legacy_text_type_alias(typeName, "MIR");
        ensure_no_legacy_scalar_type_alias(typeName, "MIR");
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return normalizedTypeName switch
        {
            "Unit" => "unit",
            "Void" or "Never" => "void",
            _ => normalizedTypeName
        };
    }

    /// <summary>
    ///     阻止历史遗留的宽 `string` 文本别名继续流入 `MIR`。
    /// </summary>
    private static void ensure_no_legacy_text_type_alias(string? typeName, string stageName)
    {
        if (typeName is not "string" and not "String")
        {
            return;
        }

        throw new InvalidOperationException(
            $"Valkyrie {stageName} 阶段禁止使用宽泛的 `string` 类型；请显式改用 `utf8`、`utf16`、`utf32` 或 `c_str`");
    }

    /// <summary>
    ///     阻止历史遗留的宿主语言标量别名继续流入 `MIR`。
    /// </summary>
    private static void ensure_no_legacy_scalar_type_alias(string? typeName, string stageName)
    {
        if (typeName is not "int" and not "long" and not "float" and not "double" and not "boolean")
        {
            return;
        }

        throw new InvalidOperationException(
            $"Valkyrie {stageName} 阶段禁止继续使用历史遗留标量别名 `{typeName}`，请先归一化为 `i32`、`i64`、`f32`、`f64` 或 `bool`");
    }

    #region 循环语句

    private Id build_if_statement(IfStatement ifStmt, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        // 临时调试（生产环境已关闭"        // Console.Error.WriteLine($"[MIR DEBUG] build_if_statement condition type={ifStmt.condition.GetType().Name}, callable={callable.name}");
        // if (ifStmt.condition is TermBinaryExpression binaryExpr)
        // {
        //     Console.Error.WriteLine($"[MIR DEBUG]   binary op={binaryExpr.@operator}, left={binaryExpr.left.GetType().Name}, right={binaryExpr.right.GetType().Name}");
        // }
        // else if (ifStmt.condition is TermDotExpression dotExpr)
        // {
        //     Console.Error.WriteLine($"[MIR DEBUG]   dot caller={dotExpr.caller.GetType().Name}, callee={dotExpr.callee.name}, hasCallBody={dotExpr.call_body != null}");
        // }
        // else if (ifStmt.condition is TermCallExpression callExpr)
        // {
        //     Console.Error.WriteLine($"[MIR DEBUG]   call caller={callExpr.caller.GetType().Name}");
        // }
        var conditionId = build_expression(ifStmt.condition, hir, callable, graph, targetArchTag);
        var thenBodyId = build_block(ifStmt.then_block, hir, callable, graph, targetArchTag);
        var elseBodyId = ifStmt.else_block switch
        {
            null => graph.add(AlgebraNodeBuilder.None()),
            FunctionBody elseBlock => build_block(elseBlock, hir, callable, graph, targetArchTag),
            IfStatement elseIf => build_if_statement(elseIf, hir, callable, graph, targetArchTag),
            _ => graph.add(AlgebraNodeBuilder.None())
        };

        return graph.add(new Choice(conditionId, thenBodyId, elseBodyId));
    }

    /// <summary>
    ///     构建 `for` 循环或无限循环语句"    ///     `LoopStatement` 同时覆盖"`Initializer/Condition/Update` "`for` 循环和三者均为空的无限循环"    /// </summary>
    private Id build_loop_statement(LoopStatement loop, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        if (loop.initializer is not null || loop.condition is not null || loop.update is not null)
        {
            var initId = loop.initializer is not null
                ? build_statement(loop.initializer, hir, callable, graph, targetArchTag)
                : graph.add(AlgebraNodeBuilder.None());
            var condId = loop.condition is not null
                ? build_expression(loop.condition, hir, callable, graph, targetArchTag)
                : graph.add(AlgebraNodeBuilder.BooleanConstant(true));
            var bodyId = build_block(loop.body, hir, callable, graph, targetArchTag);
            var stepId = loop.update is not null
                ? build_statement(loop.update, hir, callable, graph, targetArchTag)
                : graph.add(AlgebraNodeBuilder.None());

            var iterBody = graph.add(AlgebraNodeBuilder.Seq([bodyId, stepId]));
            var repeatId = graph.add(AlgebraNodeBuilder.Repeat(condId, iterBody));
            return graph.add(AlgebraNodeBuilder.Seq([initId, repeatId]));
        }

        var bodyResultId = build_block(loop.body, hir, callable, graph, targetArchTag);
        return graph.add(AlgebraNodeBuilder.Repeat(graph.add(AlgebraNodeBuilder.BooleanConstant(true)), bodyResultId));
    }

    private Id build_loop_in_statement(LoopInStatement loopIn, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        if (loopIn.iterable is not AstNode iterable ||
            !hir.try_resolve_loop_iteration(callable, iterable, out var iteration) ||
            iteration is null)
        {
            return report_unhandled_statement(loopIn, graph);
        }

        var iteratorName = $"__loop_iter_{_loopCounter++}";
        var itemName = resolve_loop_item_storage_name(loopIn);

        var iterableId = build_expression(iterable, hir, callable, graph, targetArchTag);
        var iteratorValueId = iteration.iterator_factory is not null
            ? build_resolved_call(iteration.iterator_factory, iterableId, [], graph)
            : iterableId;

        var iteratorDeclId = graph.add(AlgebraNodeBuilder.VarDecl(
            ImmutableArray<Id>.Empty,
            ImmutableArray<string>.Empty,
            iteratorName,
            null,
            iteratorValueId));

        var iteratorSymbolId = graph.add(AlgebraNodeBuilder.Symbol(iteratorName));
        var hasNextId = build_resolved_call(iteration.has_next_call, iteratorSymbolId, [], graph);
        var nextValueId = build_resolved_call(iteration.next_call, iteratorSymbolId, [], graph);
        var itemValueId = iteration.unwrap_call is not null
            ? build_resolved_call(iteration.unwrap_call, nextValueId, [], graph)
            : nextValueId;

        var itemDeclId = graph.add(AlgebraNodeBuilder.VarDecl(
            ImmutableArray<Id>.Empty,
            ImmutableArray<string>.Empty,
            itemName,
            iteration.item_type.is_unknown_type
                ? null
                : graph.add(AlgebraNodeBuilder.TypeRef(iteration.item_type.name, ImmutableArray<Id>.Empty)),
            itemValueId));

        var bodyId = build_block(loopIn.body, hir, callable, graph, targetArchTag);
        var loopBodyItems = new List<Id> { itemDeclId };
        if (loopIn.iterator_pattern is not null &&
            requires_loop_pattern_rebinding(loopIn.iterator_pattern, itemName))
        {
            loopBodyItems.AddRange(build_loop_pattern_bindings(loopIn.iterator_pattern, itemValueId, graph));
        }

        loopBodyItems.Add(bodyId);
        var repeatId = graph.add(AlgebraNodeBuilder.Repeat(hasNextId, build_sequence([.. loopBodyItems], graph)));
        return build_sequence([iteratorDeclId, repeatId], graph);
    }

    /// <summary>
    ///     "`loop ... in ...` 头部选择循环项的存储变量名"    ///     变量模式沿用原变量名，解构模式则先落到临时值再展开绑定"    /// </summary>
    private string resolve_loop_item_storage_name(LoopInStatement loopIn)
    {
        return loopIn.iterator_pattern switch
        {
            PatternLiteralVariableNode variablePattern when
                !string.Equals(variablePattern.name, "_", StringComparison.Ordinal) => variablePattern.name,
            PatternLiteralTupleNode => $"__loop_item_{_loopCounter++}",
            _ when !string.IsNullOrWhiteSpace(loopIn.iterator_name) => loopIn.iterator_name!,
            _ => "_"
        };
    }

    /// <summary>
    ///     判断循环模式是否需要在临时循环项之上追加解构绑定"    /// </summary>
    private static bool requires_loop_pattern_rebinding(PatternNode pattern, string itemName)
    {
        return pattern switch
        {
            PatternLiteralVariableNode variablePattern =>
                !string.Equals(variablePattern.name, itemName, StringComparison.Ordinal),
            _ => true
        };
    }

    /// <summary>
    ///     "`loop (a, b) in values` 这样的头部模式绑定降到循环体前置声明"    /// </summary>
    private IEnumerable<Id> build_loop_pattern_bindings(PatternNode pattern, Id sourceValueId, EGraph<AlgebraNode> graph)
    {
        switch (pattern)
        {
            case PatternLiteralVariableNode variablePattern when
                !string.Equals(variablePattern.name, "_", StringComparison.Ordinal):
                yield return graph.add(AlgebraNodeBuilder.VarDecl(
                    ImmutableArray<Id>.Empty,
                    ImmutableArray<string>.Empty,
                    variablePattern.name,
                    null,
                    sourceValueId));
                yield break;
            case PatternLiteralTupleNode tuplePattern:
                for (var i = 0; i < tuplePattern.elements.Count; i++)
                {
                    var ordinalId = graph.add(AlgebraNodeBuilder.Constant(i + 1));
                    var elementValueId = graph.add(new GetOrdinalIdx(sourceValueId, ordinalId));
                    foreach (var binding in build_loop_pattern_bindings(tuplePattern.elements[i], elementValueId, graph))
                    {
                        yield return binding;
                    }
                }

                yield break;
            default:
                yield break;
        }
    }

    /// <summary>
    ///     构建 `while` 循环语句"    /// </summary>
    private Id build_while_statement(WhileStatement whileStmt, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var condId = build_expression(whileStmt.condition, hir, callable, graph, targetArchTag);
        var bodyId = build_block(whileStmt.body, hir, callable, graph, targetArchTag);
        return graph.add(AlgebraNodeBuilder.Repeat(condId, bodyId));
    }

    /// <summary>
    ///     构建 until 循环语句
    /// </summary>
    private Id build_until_statement(UntilStatement untilStmt, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var condId = build_until_condition_expression(untilStmt.condition, hir, callable, graph, targetArchTag);
        var bodyId = build_block(untilStmt.body, hir, callable, graph, targetArchTag);
        return graph.add(AlgebraNodeBuilder.Repeat(condId, bodyId));
    }

    /// <summary>
    ///     "`until (cond)` 归一化为 `while (!cond)` 使用的循环条件"    ///     优先直接翻转比较运算，绕开当前 `Not` 在部分后端链路中的错误折叠"    /// </summary>
    private Id build_until_condition_expression(AstNode condition, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        if (condition is TermBinaryExpression binary)
        {
            var flipped = binary.@operator switch
            {
                TermBinaryOperator.equal => TermBinaryOperator.not_equal,
                TermBinaryOperator.not_equal => TermBinaryOperator.equal,
                TermBinaryOperator.less_than => TermBinaryOperator.greater_than_or_equal,
                TermBinaryOperator.less_than_or_equal => TermBinaryOperator.greater_than,
                TermBinaryOperator.greater_than => TermBinaryOperator.less_than_or_equal,
                TermBinaryOperator.greater_than_or_equal => TermBinaryOperator.less_than,
                _ => (TermBinaryOperator?)null
            };

            if (flipped is { } flippedOperator)
            {
                var inverted = new TermBinaryExpression(flippedOperator, binary.left, binary.right);
                return build_expression(inverted, hir, callable, graph, targetArchTag);
            }
        }

        var condId = build_expression(condition, hir, callable, graph, targetArchTag);
        return graph.add(new Not(condId));
    }

    #endregion

    #region Match 模式匹配

    /// <summary>
    ///     构建 `match` 模式匹配语句，直接生"Choice 链"    /// </summary>
    private Id build_match_statement(MatchStatementNode matchStmt, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var valueId = build_expression(matchStmt.expression, hir, callable, graph, targetArchTag);
        var arms = matchStmt.arms
            .SelectMany(expand_or_pattern_arm)
            .ToList();

        return build_match_choice_chain(valueId, arms, hir, callable, graph, targetArchTag);
    }

    /// <summary>
    ///     构建后缀 match 表达式，直接生成 Choice "+ 临时变量"    /// </summary>
    private Id build_match_expression(TermMatchExpression matchExpr, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var valueId = build_expression(matchExpr.operand, hir, callable, graph, targetArchTag);
        var arms = matchExpr.arms
            .SelectMany(expand_or_pattern_arm)
            .ToList();
        var tempName = $"__match_result_{_matchCounter++}";
        var tempSymbolId = graph.add(AlgebraNodeBuilder.Symbol(tempName));

        // 声明临时变量
        var varDeclId = graph.add(AlgebraNodeBuilder.VarDecl(
            ImmutableArray<Id>.Empty,
            ImmutableArray<string>.Empty,
            tempName,
            null,
            null));

        // 构建 Choice 链，每个分支体末尾将结果写入临时变量
        var chainId = build_match_expression_choice_chain(valueId, arms, tempName, hir, callable, graph, targetArchTag);

        // Seq(VarDecl, Choice" Symbol(tempName)) "表达式返回临时变量引"        return graph.add(AlgebraNodeBuilder.Seq([varDeclId, chainId, tempSymbolId]));
    }

    /// <summary>
    ///     "OR-pattern 分支"c>case A | B: body</c>）展开为多个独立分支（<c>case A: body; case B: body</c>）"    ///     "OR-pattern 分支原样返回"    /// </summary>
    private static IEnumerable<ArmNode> expand_or_pattern_arm(ArmNode arm)
    {
        if (arm is ArmCaseNode { pattern: PatternExpressionBinaryNode { @operator: PatternBinaryOperator.fake_or } orPat } caseArm)
        {
            foreach (var alternative in flatten_or_pattern(orPat))
                yield return caseArm with { pattern = alternative };
        }
        else
        {
            yield return arm;
        }
    }

    /// <summary>
    ///     "OR-pattern 树（<c>A | B | C</c>）展开为叶子模式列表（<c>[A, B, C]</c>）"    /// </summary>
    private static IEnumerable<PatternNode> flatten_or_pattern(PatternNode pattern)
    {
        if (pattern is PatternExpressionBinaryNode { @operator: PatternBinaryOperator.fake_or } orPat)
        {
            foreach (var left in flatten_or_pattern(orPat.lhs))
                yield return left;
            foreach (var right in flatten_or_pattern(orPat.rhs))
                yield return right;
        }
        else
        {
            yield return pattern;
        }
    }

    /// <summary>
    ///     构建 match 语句"Choice 链"    ///     对于字面量模式和通配符模式，生成比较表达"+ Choice 节点"    ///     对于变量模式和构造器模式，暂未实现"    /// </summary>
    private Id build_match_choice_chain(Id valueId, IReadOnlyList<ArmNode> arms, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        if (arms.Count == 0)
        {
            return graph.add(AlgebraNodeBuilder.None());
        }

        var arm = arms[0];
        var remainingArms = arms.Skip(1).ToList();

        return arm switch
        {
            ArmCaseNode caseArm => build_case_choice(valueId, caseArm, remainingArms, hir, callable, graph, targetArchTag),
            ArmElseNode elseArm => build_arm_body(elseArm, hir, callable, graph, targetArchTag, valueId),
            _ => throw new NotSupportedException($"`match` 分支类型 `{arm.GetType().Name}` 未接入主链")
        };
    }

    /// <summary>
    ///     "case 分支构建 Choice 节点"    ///     根据模式类型生成条件表达式，然后将条件、分支体和剩余分支组合为 Choice"    /// </summary>
    private Id build_case_choice(Id valueId, ArmCaseNode caseArm, IReadOnlyList<ArmNode> remainingArms,
        HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var bodyId = build_arm_body(caseArm, hir, callable, graph, targetArchTag, valueId);

        // 构建模式匹配条件
        var conditionId = build_pattern_match_condition(valueId, caseArm.pattern, hir, callable, graph);

        // 如果"guard 条件，与模式条件取逻辑"        if (caseArm.guard is not null)
        {
            var guardId = build_expression(caseArm.guard, hir, callable, graph, targetArchTag);
            conditionId = graph.add(new And(conditionId, guardId));
        }

        // 通配符模式：条件恒为真，直接返回分支"        if (caseArm.pattern is PatternLiteralWildcardNode || caseArm.pattern is PatternLiteralVariableNode)
        {
            return bodyId;
        }

        if (remainingArms.Count == 0)
        {
            // 最后一个分支，else 部分为空
            return graph.add(new Choice(conditionId, bodyId, graph.add(AlgebraNodeBuilder.None())));
        }

        var restId = build_match_choice_chain(valueId, remainingArms, hir, callable, graph, targetArchTag);

        return graph.add(new Choice(conditionId, bodyId, restId));
    }

    /// <summary>
    ///     构建 match 表达式的 Choice 链"    ///     "match 语句类似，但每个分支体末尾会通过 StateUpdate 将结果写入临时变量"    /// </summary>
    private Id build_match_expression_choice_chain(Id valueId, IReadOnlyList<ArmNode> arms, string tempName,
        HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph, string targetArchTag)
    {
        if (arms.Count == 0)
        {
            return graph.add(AlgebraNodeBuilder.None());
        }

        var arm = arms[0];
        var remainingArms = arms.Skip(1).ToList();

        return arm switch
        {
            ArmCaseNode caseArm => build_case_expression_choice(valueId, caseArm, remainingArms, tempName,
                hir, callable, graph, targetArchTag),
            ArmElseNode elseArm => build_else_expression_arm(elseArm, tempName, hir, callable, graph, targetArchTag),
            _ => throw new NotSupportedException($"`match` 分支类型 `{arm.GetType().Name}` 未接入主链")
        };
    }

    /// <summary>
    ///     "match 表达式的 else 分支构建结果节点"    ///     将分支体结果通过 StateUpdate 写入临时变量"    /// </summary>
    private Id build_else_expression_arm(ArmElseNode elseArm, string tempName, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var bodyBlockId = elseArm.body is null
            ? graph.add(AlgebraNodeBuilder.None())
            : build_block(elseArm.body, hir, callable, graph, targetArchTag);
        var updateId = graph.add(new AlgebraNode.StateUpdate(
            graph.add(AlgebraNodeBuilder.Symbol(tempName)), bodyBlockId));

        return updateId;
    }

    /// <summary>
    ///     "match 表达式的 case 分支构建 Choice 节点"    ///     分支体末尾将结果写入临时变量"    /// </summary>
    private Id build_case_expression_choice(Id valueId, ArmCaseNode caseArm, IReadOnlyList<ArmNode> remainingArms,
        string tempName, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var bodyBlockId = caseArm.body is null
            ? graph.add(AlgebraNodeBuilder.None())
            : build_block(caseArm.body, hir, callable, graph, targetArchTag);

        // 将分支体结果写入临时变量
        var updateId = graph.add(new AlgebraNode.StateUpdate(
            graph.add(AlgebraNodeBuilder.Symbol(tempName)), bodyBlockId));

        // 构建模式匹配条件
        var conditionId = build_pattern_match_condition(valueId, caseArm.pattern, hir, callable, graph);

        if (caseArm.guard is not null)
        {
            var guardId = build_expression(caseArm.guard, hir, callable, graph, targetArchTag);
            conditionId = graph.add(new And(conditionId, guardId));
        }

        // 通配"变量模式：条件恒为真
        if (caseArm.pattern is PatternLiteralWildcardNode || caseArm.pattern is PatternLiteralVariableNode)
        {
            return updateId;
        }

        if (remainingArms.Count == 0)
        {
            return graph.add(new Choice(conditionId, updateId, graph.add(AlgebraNodeBuilder.None())));
        }

        var restId = build_match_expression_choice_chain(valueId, remainingArms, tempName,
            hir, callable, graph, targetArchTag);

        return graph.add(new Choice(conditionId, updateId, restId));
    }

    /// <summary>
    ///     根据模式类型生成匹配条件表达式"    /// </summary>
    private Id build_pattern_match_condition(Id valueId, PatternNode pattern, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph)
    {
        return pattern switch
        {
            PatternLiteralNumberNode lit => build_equality_check(valueId,
                build_number_literal_node(lit.value, graph), graph),
            PatternLiteralTextNode lit => build_equality_check(valueId,
                graph.add(AlgebraNodeBuilder.StringConstant(lit.value)), graph),
            PatternLiteralBooleanNode lit => build_equality_check(valueId,
                graph.add(AlgebraNodeBuilder.BooleanConstant(lit.value)), graph),
            PatternLiteralNullNode => build_equality_check(valueId,
                graph.add(AlgebraNodeBuilder.None()), graph),
            PatternLiteralWildcardNode => graph.add(AlgebraNodeBuilder.BooleanConstant(true)),
            PatternLiteralVariableNode =>
                graph.add(AlgebraNodeBuilder.BooleanConstant(true)),
            PatternLiteralRangeNode rangePat => build_range_pattern_condition(valueId, rangePat, graph),
            PatternLiteralObjectNode objPat =>
                build_object_pattern_condition(valueId, objPat, hir, graph),
            PatternLiteralTupleNode tuplePat =>
                build_tuple_pattern_condition(valueId, tuplePat, hir, callable, graph),
            PatternExpressionBinaryNode { @operator: PatternBinaryOperator.fake_or } =>
                throw new NotSupportedException(
                    "OR-pattern 应在进入 match_choice_chain 之前完成展开，不应出现在此处"),
            _ => throw new NotSupportedException(
                $"match 模式 `{pattern.GetType().Name}` 未接入 MIR 主链")
        };
    }

    /// <summary>
    ///     构建相等性检查表达式：`value == literal`。
    /// </summary>
    private static Id build_equality_check(Id valueId, Id literalId, EGraph<AlgebraNode> graph)
    {
        return graph.add(new Cmp(CompareOp.eq, valueId, literalId));
    }

    /// <summary>
    ///     构建范围模式的条件表达式：value >= lower && value <= upper"    /// </summary>
    private Id build_range_pattern_condition(Id valueId, PatternLiteralRangeNode rangePat, EGraph<AlgebraNode> graph)
    {
        var lowerId = build_pattern_literal(rangePat.lower ?? throw new NotSupportedException("范围模式缺少下界"), graph);
        var upperId = build_pattern_literal(rangePat.upper ?? throw new NotSupportedException("范围模式缺少上界"), graph);

        var geExpr = graph.add(new Cmp(CompareOp.ge, valueId, lowerId));
        var leExpr = graph.add(new Cmp(CompareOp.le, valueId, upperId));

        return graph.add(new And(geExpr, leExpr));
    }

    /// <summary>
    ///     构建对象模式的条件表达式：检"__tag 并解构每个字段与对应子模式匹配"    /// </summary>
    private Id build_object_pattern_condition(Id valueId, PatternLiteralObjectNode objPat,
        HirModule hir, EGraph<AlgebraNode> graph)
    {
        var conditions = new List<Id>();

        // 检"__tag 判别值（仅对 unite/union 变体有效"        var typeName = resolve_pattern_path_name_for_object(objPat);
        if (!string.IsNullOrWhiteSpace(typeName) &&
            try_resolve_unite_variant_discriminant(hir, typeName, out var discriminant) &&
            discriminant.HasValue)
        {
            var tagFieldId = graph.add(AlgebraNodeBuilder.GetField(valueId, "__tag"));
            var tagValueId = graph.add(AlgebraNodeBuilder.Constant(discriminant.Value));
            conditions.Add(graph.add(new Cmp(CompareOp.eq, tagFieldId, tagValueId)));
        }

        // 检查每个字"        foreach (var field in objPat.fields)
        {
            var fieldId = graph.add(AlgebraNodeBuilder.GetField(valueId, field.name));
            var subConditionId = build_pattern_match_condition(fieldId, field.pattern, hir, default!, graph);
            conditions.Add(subConditionId);
        }

        if (conditions.Count == 0)
        {
            return graph.add(AlgebraNodeBuilder.BooleanConstant(true));
        }

        Id result = conditions[0];
        for (var i = 1; i < conditions.Count; i++)
        {
            result = graph.add(new And(result, conditions[i]));
        }

        return result;
    }

    /// <summary>
    ///     解析对象模式的构造函数名"    /// </summary>
    private static string resolve_pattern_path_name_for_object(PatternLiteralObjectNode objPat)
    {
        return objPat.path is { } path
            ? resolve_pattern_path_name(path)
            : string.Empty;
    }

    /// <summary>
    ///     构建元组模式的条件表达式：依次解构每个元组字段并与对应子模式匹配"    ///     同时检"__tag 判别值以支持 unite/union 变体匹配"    /// </summary>
    private Id build_tuple_pattern_condition(Id valueId, PatternLiteralTupleNode tuplePat,
        HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph)
    {
        var conditions = new List<Id>();

        // 检"__tag 判别值（仅对 unite/union 变体有效"        var typeName = resolve_pattern_path_name_for_tuple(tuplePat);
        if (!string.IsNullOrWhiteSpace(typeName) &&
            try_resolve_unite_variant_discriminant(hir, typeName, out var discriminant) &&
            discriminant.HasValue)
        {
            var tagFieldId = graph.add(AlgebraNodeBuilder.GetField(valueId, "__tag"));
            var tagValueId = graph.add(AlgebraNodeBuilder.Constant(discriminant.Value));
            conditions.Add(graph.add(new Cmp(CompareOp.eq, tagFieldId, tagValueId)));
        }

        var elements = tuplePat.elements;
        for (var i = 0; i < elements.Count; i++)
        {
            // 元组模式"1 基序数索引解构，保持"`pair.1` / `pair.2` 一致"            var ordinalId = graph.add(AlgebraNodeBuilder.Constant(i + 1));
            var fieldId = graph.add(new GetOrdinalIdx(valueId, ordinalId));

            // 递归构建子模式匹配条"            var subConditionId = build_pattern_match_condition(fieldId, elements[i], hir, callable, graph);
            conditions.Add(subConditionId);
        }

        if (conditions.Count == 0)
        {
            return graph.add(AlgebraNodeBuilder.BooleanConstant(true));
        }

        Id result = conditions[0];
        for (var i = 1; i < conditions.Count; i++)
        {
            result = graph.add(new And(result, conditions[i]));
        }

        return result;
    }

    /// <summary>
    ///     解析元组模式的构造函数名"    /// </summary>
    private static string resolve_pattern_path_name_for_tuple(PatternLiteralTupleNode tuplePat)
    {
        return tuplePat.path is { } path
            ? resolve_pattern_path_name(path)
            : string.Empty;
    }

    /// <summary>
    ///     构建分支体"    ///     对于变量模式，在分支体前插入变量绑定（VarDecl）"    /// </summary>
    private Id build_arm_body(ArmNode arm, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag, Id matchValueId)
    {
        var bodyBlockId = arm switch
        {
            ArmCaseNode caseArm => caseArm.body is null
                ? graph.add(AlgebraNodeBuilder.None())
                : build_block(caseArm.body, hir, callable, graph, targetArchTag),
            ArmElseNode elseArm => elseArm.body is null
                ? graph.add(AlgebraNodeBuilder.None())
                : build_block(elseArm.body, hir, callable, graph, targetArchTag),
            _ => graph.add(AlgebraNodeBuilder.None())
        };

        // 处理变量绑定：对于变量模式，"body 前插"VarDecl
        if (arm is ArmCaseNode { pattern: PatternLiteralVariableNode varPat } &&
            !string.Equals(varPat.name, "_", StringComparison.Ordinal))
        {
            var varDeclId = graph.add(AlgebraNodeBuilder.VarDecl(
                ImmutableArray<Id>.Empty,
                ImmutableArray<string>.Empty,
                varPat.name,
                null,
                matchValueId));

            return graph.add(AlgebraNodeBuilder.Seq([varDeclId, bodyBlockId]));
        }

        // 处理元组模式变量绑定：按 1 基序数索引提取元素，保持"`pair.1` / `pair.2` 一致"        if (arm is ArmCaseNode { pattern: PatternLiteralTupleNode tuplePat })
        {
            var bindings = new List<Id>();
            for (var i = 0; i < tuplePat.elements.Count; i++)
            {
                var element = tuplePat.elements[i];
                if (element is PatternLiteralVariableNode elementVarPat &&
                    !string.Equals(elementVarPat.name, "_", StringComparison.Ordinal))
                {
                    var ordinalId = graph.add(AlgebraNodeBuilder.Constant(i + 1));
                    var fieldValueId = graph.add(new GetOrdinalIdx(matchValueId, ordinalId));
                    var varDeclId = graph.add(AlgebraNodeBuilder.VarDecl(
                        ImmutableArray<Id>.Empty,
                        ImmutableArray<string>.Empty,
                        elementVarPat.name,
                        null,
                        fieldValueId));
                    bindings.Add(varDeclId);
                }
            }

            if (bindings.Count > 0)
            {
                bindings.Add(bodyBlockId);
                return graph.add(AlgebraNodeBuilder.Seq([.. bindings]));
            }
        }

        // 处理对象模式变量绑定：对"Object{fields} 这样的对象模式，"body 前插"VarDecl
        if (arm is ArmCaseNode { pattern: PatternLiteralObjectNode objPat })
        {
            var bindings = new List<Id>();
            foreach (var field in objPat.fields)
            {
                if (field.pattern is PatternLiteralVariableNode fieldVarPat &&
                    !string.Equals(fieldVarPat.name, "_", StringComparison.Ordinal))
                {
                    var fieldValueId = graph.add(AlgebraNodeBuilder.GetField(matchValueId, field.name));
                    var varDeclId = graph.add(AlgebraNodeBuilder.VarDecl(
                        ImmutableArray<Id>.Empty,
                        ImmutableArray<string>.Empty,
                        fieldVarPat.name,
                        null,
                        fieldValueId));
                    bindings.Add(varDeclId);
                }
            }

            if (bindings.Count > 0)
            {
                bindings.Add(bodyBlockId);
                return graph.add(AlgebraNodeBuilder.Seq([.. bindings]));
            }
        }

        return bodyBlockId;
    }

    /// <summary>
    ///     构建模式字面量节点（用于范围模式"catch 模式中的字面量比较）"    /// </summary>
    private static Id build_pattern_literal(PatternNode literal, EGraph<AlgebraNode> graph)
    {
        return literal switch
        {
            PatternLiteralNumberNode numberLiteral => graph.add(
                resolve_number_literal_node(numberLiteral.value)),
            PatternLiteralTextNode textLiteral => graph.add(AlgebraNodeBuilder.StringConstant(textLiteral.value)),
            PatternLiteralBooleanNode booleanLiteral => graph.add(AlgebraNodeBuilder.BooleanConstant(booleanLiteral.value)),
            PatternLiteralNullNode => graph.add(AlgebraNodeBuilder.None()),
            _ => graph.add(AlgebraNodeBuilder.None())
        };
    }

    /// <summary>
    ///     match 表达式临时变量计数器，用于生成唯一名称"    /// </summary>
    private static int _matchCounter;
    private static int _loopCounter;

    #endregion

    #region Catch / Resume 效应处理

    /// <summary>
    ///     构建 catch 效应捕获语句，将效应操作降级"Core Catch 节点
    /// </summary>
    private Id build_catch_statement(CatchStatementNode catchStmt, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var expressionId = build_expression(catchStmt.expression, hir, callable, graph, targetArchTag);
        var armIds = new List<Id>();

        foreach (var arm in catchStmt.arms)
        {
            armIds.Add(build_catch_arm(arm, hir, callable, graph, targetArchTag));
        }

        return graph.add(new Catch(expressionId, [.. armIds]));
    }

    /// <summary>
    ///     构建后缀 catch 表达式："expr.catch { ... } 降级"Core Catch 节点
    /// </summary>
    private Id build_catch_expression(TermCatchExpression catchExpr, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var expressionId = build_expression(catchExpr.operand, hir, callable, graph, targetArchTag);
        var armIds = new List<Id>();

        foreach (var arm in catchExpr.arms)
        {
            armIds.Add(build_catch_arm(arm, hir, callable, graph, targetArchTag));
        }

        return graph.add(new Catch(expressionId, [.. armIds]));
    }

    /// <summary>
    ///     构建 catch 分支：将模式匹配 arm 转为 Core CatchArm 节点
    /// </summary>
    private Id build_catch_arm(ArmNode arm, HirModule hir, HirCallable callable, EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        return arm switch
        {
            ArmCaseNode caseArm => graph.add(new CatchArm(
                build_catch_pattern(caseArm.pattern, graph),
                caseArm.body is null
                    ? graph.add(AlgebraNodeBuilder.None())
                    : build_block(caseArm.body, hir, callable, graph, targetArchTag))),
            ArmElseNode elseArm => graph.add(new CatchArm(
                graph.add(new WildPattern()),
                elseArm.body is null
                    ? graph.add(AlgebraNodeBuilder.None())
                    : build_block(elseArm.body, hir, callable, graph, targetArchTag))),
            _ => throw new NotSupportedException(
                $"catch 分支节点 `{arm.GetType().Name}` 未接入主链；请补齐或改写为 `else` 分支")
        };
    }

    /// <summary>
    ///     构建 `catch` 模式节点，使其符合 `Core` 方言模式类型。
    /// </summary>
    private Id build_catch_pattern(AstNode pattern, EGraph<AlgebraNode> graph)
    {
        if (pattern is PatternLiteralNullNode)
        {
            return graph.add(new LitPattern(graph.add(AlgebraNodeBuilder.None())));
        }

        return pattern switch
        {
            PatternLiteralNumberNode lit =>
                graph.add(new LitPattern(build_pattern_literal(lit, graph))),
            PatternLiteralTextNode lit =>
                graph.add(new LitPattern(build_pattern_literal(lit, graph))),
            PatternLiteralBooleanNode lit =>
                graph.add(new LitPattern(build_pattern_literal(lit, graph))),
            PatternLiteralVariableNode varPat =>
                graph.add(new VarPattern(varPat.name, default)),
            PatternLiteralObjectNode { path: { } objectPath } objPat =>
                graph.add(new ObjPattern(
                    resolve_pattern_path_name(objectPath),
                    [
                        .. objPat.fields.Select(field => graph.add(new ObjPatField(
                            field.name,
                            build_catch_pattern(field.pattern, graph))))
                    ])),
            PatternLiteralTupleNode { path: { } tuplePath } tuplePat =>
                graph.add(new CtorPattern(
                    resolve_pattern_path_name(tuplePath),
                    [.. tuplePat.elements.Select(e => build_catch_pattern(e, graph))])),
            PatternLiteralTupleNode tuplePat =>
                graph.add(new CtorPattern(
                    "tuple",
                    [.. tuplePat.elements.Select(e => build_catch_pattern(e, graph))])),
            PatternLiteralWildcardNode =>
                graph.add(new WildPattern()),
            _ => throw new NotSupportedException(
                $"catch 模式节点 `{pattern.GetType().Name}` 未接入主链")
        };
    }

    private static string resolve_pattern_path_name(QualifiedPathNode path)
    {
        return string.IsNullOrWhiteSpace(path.full_name)
            ? path.name
            : path.full_name;
    }

    /// <summary>
    ///     构建 resume 恢复语句，生"Core Resume 节点
    /// </summary>
    private Id build_resume_statement(ResumeStatement resume, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var valueId = resume.value is not null
            ? build_expression(resume.value, hir, callable, graph, targetArchTag)
            : graph.add(AlgebraNodeBuilder.None());

        return graph.add(new Resume(valueId));
    }

    /// <summary>
    ///     构建 raise 效应抛出语句，生"Core Perform 节点
    /// </summary>
    private Id build_raise_statement(RaiseStatement raiseStmt, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var opId = build_expression(raiseStmt.value, hir, callable, graph, targetArchTag);
        var effectName = try_extract_effect_name(raiseStmt.value, out var name) ? name : "effect";
        return graph.add(new Perform(effectName, [opId]));
    }

    /// <summary>
    ///     构建 try Result 语句，生"Core Handle 节点
    /// </summary>
    private Id build_try_statement(TryStatement tryStmt, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        var bodyId = build_block(tryStmt.body, hir, callable, graph, targetArchTag);

        var effectName = tryStmt.capture_types.Count > 0
            ? get_type_name(tryStmt.capture_types[0])
            : "effect";

        var handlerParamName = "__effect_op";
        var failConstructorId = graph.add(AlgebraNodeBuilder.Symbol("Fail"));
        var handlerParamId = graph.add(AlgebraNodeBuilder.Symbol(handlerParamName));
        var failValueId = graph.add(new Apply(failConstructorId, [handlerParamId]));
        var handlerBodyId = graph.add(AlgebraNodeBuilder.Return(failValueId));
        var handlerId = graph.add(new AlgebraNode.Lambda([handlerParamName], handlerBodyId));

        return graph.add(new Handle(effectName, handlerId, bodyId));
    }

    /// <summary>
    ///     尝试"raise 表达式中提取效应名称"    ///     如果表达式是名称路径或对象构造器，直接使用其名称；否则返"false"    /// </summary>
    private static bool try_extract_effect_name(TermNode value, out string name)
    {
        switch (value)
        {
            case TermLiteralNamePathNode { path.full_name: { Length: > 0 } qualifiedName }:
                name = qualifiedName;
                return true;
            case TermLiteralObjectNode { constructor: IdentifierNode { name: { Length: > 0 } ctorName } }:
                name = ctorName;
                return true;
            case TermLiteralObjectNode { constructor: TypeLiteralNamePathNode { path.full_name: { Length: > 0 } ctorTypeName } }:
                name = ctorTypeName;
                return true;
            case TermLiteralObjectNode { constructor: TermLiteralNamePathNode { path.full_name: { Length: > 0 } ctorPath } }:
                name = ctorPath;
                return true;
            default:
                name = string.Empty;
                return false;
        }
    }

    #endregion

    #region 璋冪敤琛ㄨ揪"

    private Id build_call_expression(
        TermCallExpression call,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var argumentIds = (call.call_body.term_arguments?.items ?? [])
            .Select(argument => build_expression(argument.value, hir, callable, graph, targetArchTag))
            .ToList();

        if (try_lower_builtin_call(call.caller, argumentIds, graph, out var builtinId)) return builtinId;

        if (hir.try_resolve_call(callable, call, out var resolvedCall) &&
            resolvedCall is { dispatch: HirDispatchKind.@static } resolution)
        {
            if (resolution.inject_receiver && call.caller is TermDotExpression dot)
                argumentIds.Insert(0, build_expression(dot.caller, hir, callable, graph, targetArchTag));

            var targetId = graph.add(AlgebraNodeBuilder.Symbol(resolution.target_name));
            return graph.add(new Apply(targetId, [.. argumentIds]));
        }

        if (hir.try_resolve_call(callable, call, out resolvedCall) &&
            resolvedCall is { dispatch: HirDispatchKind.witness, method_index: { } slotIndex, witness_trait_name: { } witnessTraitName } witnessResolution &&
            call.caller is TermDotExpression witnessDot)
        {
            var receiverId = build_expression(witnessDot.caller, hir, callable, graph, targetArchTag);
            argumentIds.Insert(0, receiverId);
            var targetId = graph.add(AlgebraNodeBuilder.Symbol(witnessResolution.target_name));
            return graph.add(new Apply(targetId, [.. argumentIds]));
        }

        return graph.add(new Apply(
            build_call_callee_expression(call.caller, hir, callable, graph, targetArchTag),
            [.. argumentIds]));
    }

    private Id build_dot_call_expression(
        TermDotExpression call,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var argumentIds = (call.call_body?.term_arguments?.items ?? [])
            .Select(argument => build_expression(argument.value, hir, callable, graph, targetArchTag))
            .ToList();

        var resolved = hir.try_resolve_call(callable, call, out var resolvedCall);

        if (resolved && resolvedCall is { dispatch: HirDispatchKind.@static } resolution)
        {
            if (resolution.inject_receiver)
                argumentIds.Insert(0, build_expression(call.caller, hir, callable, graph, targetArchTag));

            var targetId = graph.add(AlgebraNodeBuilder.Symbol(resolution.target_name));
            return graph.add(new Apply(targetId, [.. argumentIds]));
        }

        if (hir.try_resolve_call(callable, call, out resolvedCall) &&
            resolvedCall is { dispatch: HirDispatchKind.witness, method_index: { } slotIndex, witness_trait_name: { } witnessTraitName } witnessResolution)
        {
            var receiverId = build_expression(call.caller, hir, callable, graph, targetArchTag);
            argumentIds.Insert(0, receiverId);
            var targetId = graph.add(AlgebraNodeBuilder.Symbol(witnessResolution.target_name));
            return graph.add(new Apply(targetId, [.. argumentIds]));
        }

        // 回退路径：直接构造限定名，避"build_call_callee_expression 在无 caller 名解析时递归"build_expression
        // 同时"receiver（call.caller）作为第一个参数传入，否则方法调用丢失 self 参数"        // 导致 JVM 字节码中 invokestatic 的参数栈为空，触"VerifyError"        if (try_resolve_qualified_name(call.caller, out var receiverName))
        {
            var qualifiedCallName = $"{receiverName}.{call.callee.name}";
            if (hir.has_function_name(qualifiedCallName, argumentIds.Count))
            {
                return graph.add(new Apply(
                    graph.add(AlgebraNodeBuilder.Symbol(qualifiedCallName)),
                    [.. argumentIds]));
            }

            var qualifiedArguments = new List<Id>(argumentIds.Count + 1)
            {
                build_expression(call.caller, hir, callable, graph, targetArchTag)
            };
            qualifiedArguments.AddRange(argumentIds);
            return graph.add(new Apply(
                graph.add(AlgebraNodeBuilder.Symbol(qualifiedCallName)),
                [.. qualifiedArguments]));
        }

        // caller 名无法解析时的终极兜底（如复杂表达式作为 caller），仅使"callee "        // 但仍需"receiver（call.caller）作为第一个参数传入，否则方法调用丢失 self 参数
        var fallbackArguments = new List<Id>(argumentIds.Count + 1)
        {
            build_expression(call.caller, hir, callable, graph, targetArchTag)
        };
        fallbackArguments.AddRange(argumentIds);
        return graph.add(new Apply(
            graph.add(AlgebraNodeBuilder.Symbol(call.callee.name)),
            [.. fallbackArguments]));
    }

    private static Id build_resolved_call(HirCallResolution resolution, Id receiverId, IReadOnlyList<Id> argumentIds,
        EGraph<AlgebraNode> graph)
    {
        var args = new List<Id>(argumentIds.Count + (resolution.inject_receiver ? 1 : 0));
        if (resolution.inject_receiver)
            args.Add(receiverId);
        args.AddRange(argumentIds);

        var targetId = graph.add(AlgebraNodeBuilder.Symbol(resolution.target_name));
        return graph.add(new Apply(targetId, [.. args]));
    }

    /// <summary>
    ///     构建成员字段访问表达式，生成 GetField 节点
    /// </summary>
    private Id build_dot_field_expression(
        TermDotExpression dot,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var receiverId = build_expression(dot.caller, hir, callable, graph, targetArchTag);
        if (try_build_tuple_ordinal_index_id(hir, callable, dot.caller, dot.callee.name, graph, out var ordinalIndexId))
        {
            return graph.add(new GetOrdinalIdx(receiverId, ordinalIndexId));
        }

        var fieldName = dot.callee.name;
        return graph.add(new AlgebraNode.GetField(receiverId, fieldName));
    }

    private static bool try_build_tuple_ordinal_index_id(HirModule hir, HirCallable callable, AstNode receiver,
        string memberName, EGraph<AlgebraNode> graph, out Id ordinalIndexId)
    {
        if (hir.try_resolve_expression_type(callable, receiver, out var receiverType) &&
            receiverType.is_tuple_type &&
            receiverType.tuple_elements is not null &&
            int.TryParse(memberName, out var ordinal) &&
            ordinal > 0 &&
            ordinal <= receiverType.tuple_elements.Count)
        {
            ordinalIndexId = graph.add(AlgebraNodeBuilder.Constant(ordinal));
            return true;
        }

        ordinalIndexId = default;
        return false;
    }

    private Id build_ordinal_expression(
        TermOrdinalExpression ordinal,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        return build_multi_ordinal_expression(hir, callable, graph, targetArchTag, ordinal.target, ordinal.indices);
    }

    private Id build_offset_expression(
        TermOffsetExpression offset,
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        return build_multi_offset_expression(hir, callable, graph, targetArchTag, offset.target, offset.indices);
    }

    private Id build_multi_ordinal_expression(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        ValkyrieNode target,
        IReadOnlyList<ValkyrieNode> indices)
    {
        if (indices.Count == 0)
            return build_expression(target, hir, callable, graph, targetArchTag);

        var currentId = build_expression(target, hir, callable, graph, targetArchTag);

        foreach (var index in indices)
        {
            var indexId = build_expression(index, hir, callable, graph, targetArchTag);
            currentId = graph.add(new GetOrdinalIdx(currentId, indexId));
        }

        return currentId;
    }

    private Id build_multi_offset_expression(
        HirModule hir,
        HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        ValkyrieNode target,
        IReadOnlyList<ValkyrieNode> indices)
    {
        if (indices.Count == 0)
            return build_expression(target, hir, callable, graph, targetArchTag);

        var currentId = build_expression(target, hir, callable, graph, targetArchTag);

        foreach (var index in indices)
        {
            var indexId = build_expression(index, hir, callable, graph, targetArchTag);
            currentId = graph.add(new GetOffsetIdx(currentId, indexId));
        }

        return currentId;
    }

    /// <summary>
    ///     尝试将少量历史内建调用降级为 intrinsic Apply 节点"    ///     当前仅保留仍未完成显式属性绑定迁移的入口"    /// </summary>
    private bool try_lower_builtin_call(
        ValkyrieNode callee,
        List<Id> argumentIds,
        EGraph<AlgebraNode> graph,
        out Id loweredId)
    {
        loweredId = default;

        if (!try_resolve_qualified_name(callee, out var calleeName)) return false;

        switch (calleeName)
        {
            case "push" when argumentIds.Count == 2:
            {
                var targetId = graph.add(AlgebraNodeBuilder.Symbol("__nyar_array_push"));
                loweredId = graph.add(new Apply(targetId, [.. argumentIds]));
                return true;
            }
            default:
                return false;
        }
    }

    private Id build_call_callee_expression(ValkyrieNode callee, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph, string targetArchTag)
    {
        if (try_resolve_qualified_name(callee, out var qualifiedName))
            return graph.add(AlgebraNodeBuilder.Symbol(qualifiedName));

        return build_expression(callee, hir, callable, graph, targetArchTag);
    }

    private bool try_resolve_qualified_name(ValkyrieNode node, out string qualifiedName)
    {
        switch (node)
        {
            case IdentifierNode identifier when !string.IsNullOrWhiteSpace(identifier.name):
                qualifiedName = identifier.name;
                return true;
            case QualifiedPathNode path when !string.IsNullOrWhiteSpace(path.full_name):
                qualifiedName = path.full_name;
                return true;
            case TermLiteralNamePathNode termPath when !string.IsNullOrWhiteSpace(termPath.path.full_name):
                qualifiedName = termPath.path.full_name;
                return true;
            case TermDotExpression dot:
                if (try_resolve_qualified_name(dot.caller, out var targetName))
                {
                    qualifiedName = $"{targetName}.{dot.callee.name}";
                    return true;
                }

                break;
        }

        qualifiedName = string.Empty;
        return false;
    }

    #endregion

    #region 赋值与声明

    private Id build_let_declaration(LetDeclaration declaration, HirModule hir, HirCallable callable,
        EGraph<AlgebraNode> graph,
        string targetArchTag)
    {
        var typeId = declaration.var_type is null
            ? try_infer_initializer_type(declaration.initializer, graph)
            : graph.add(AlgebraNodeBuilder.TypeRef(get_type_name(declaration.var_type), ImmutableArray<Id>.Empty));
        Id? valueId = declaration.initializer is null
            ? null
            : build_expression(declaration.initializer, hir, callable, graph, targetArchTag);

        if (declaration.pattern is not null)
        {
            var rootValueId = valueId ?? graph.add(AlgebraNodeBuilder.None());
            var destructureIds = new List<Id>();

            // 如果初始化表达式不是简单的符号引用，则先绑定到临时变量以避免重复求"            if (declaration.initializer is not (IdentifierNode or TermLiteralNamePathNode))
            {
                var tmpName = $"__destructure_tmp_{Guid.NewGuid():N}";
                var tmpDeclId = graph.add(AlgebraNodeBuilder.VarDecl(
                    ImmutableArray<Id>.Empty,
                    ImmutableArray<string>.Empty,
                    tmpName,
                    typeId,
                    rootValueId));
                destructureIds.Add(tmpDeclId);
                rootValueId = graph.add(AlgebraNodeBuilder.Symbol(tmpName));
            }

            build_pattern_destructuring(declaration.pattern, rootValueId, destructureIds, graph);

            return destructureIds.Count == 1 ? destructureIds[0] : graph.add(AlgebraNodeBuilder.Seq([.. destructureIds]));
        }

        return graph.add(AlgebraNodeBuilder.VarDecl(
            ImmutableArray<Id>.Empty,
            [
                .. declaration.annotations.modifiers
                    .Select(modifier => modifier.name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
            ],
            declaration.name?.name ?? string.Empty,
            typeId,
            valueId));
    }

    private void build_pattern_destructuring(PatternNode pattern, Id valueId, List<Id> result, EGraph<AlgebraNode> graph)
    {
        switch (pattern)
        {
            case PatternLiteralVariableNode varPat:
                if (!string.Equals(varPat.name, "_", StringComparison.Ordinal))
                {
                    result.Add(graph.add(AlgebraNodeBuilder.VarDecl(
                        ImmutableArray<Id>.Empty,
                        ImmutableArray<string>.Empty,
                        varPat.name,
                        null,
                        valueId)));
                }

                break;
            case PatternLiteralTupleNode tuplePat:
                for (var i = 0; i < tuplePat.elements.Count; i++)
                {
                    var ordinalId = graph.add(AlgebraNodeBuilder.Constant(i + 1));
                    var elementValueId = graph.add(new GetOrdinalIdx(valueId, ordinalId));
                    build_pattern_destructuring(tuplePat.elements[i], elementValueId, result, graph);
                }

                break;
            case PatternLiteralObjectNode objPat:
                foreach (var field in objPat.fields)
                {
                    var fieldName = field.name ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(fieldName)) continue;

                    var fieldValueId = graph.add(AlgebraNodeBuilder.GetField(valueId, fieldName));
                    if (field.pattern is not null)
                    {
                        build_pattern_destructuring(field.pattern, fieldValueId, result, graph);
                    }
                    else
                    {
                        result.Add(graph.add(AlgebraNodeBuilder.VarDecl(
                            ImmutableArray<Id>.Empty,
                            ImmutableArray<string>.Empty,
                            fieldName,
                            null,
                            fieldValueId)));
                    }
                }

                break;
        }
    }

    private static Id? try_infer_initializer_type(ValkyrieNode? initializer, EGraph<AlgebraNode> graph)
    {
        if (initializer is TermLiteralObjectNode objectLiteral &&
            try_resolve_constructor_type_name(objectLiteral.constructor, out var constructorTypeName))
            return graph.add(AlgebraNodeBuilder.TypeRef(constructorTypeName, ImmutableArray<Id>.Empty));

        return null;
    }

    private static bool try_resolve_constructor_type_name(ValkyrieNode constructor, out string typeName)
    {
        switch (constructor)
        {
            case IdentifierNode { name: { Length: > 0 } identifierName }:
                typeName = identifierName;
                return true;
            case QualifiedPathNode { full_name: { Length: > 0 } qualifiedName }:
                typeName = qualifiedName;
                return true;
            case TypeLiteralNamePathNode { path.full_name: { Length: > 0 } typePathName }:
                typeName = typePathName;
                return true;
            case TermLiteralNamePathNode { path.full_name: { Length: > 0 } literalName }:
                typeName = literalName;
                return true;
            default:
                typeName = string.Empty;
                return false;
        }
    }

    private static string qualify_constructor_type_name(string typeName, SemanticNameSpace? currentNamespace)
    {
        var typeNamePath = ValkyrieNamePath.parse(typeName);
        if (typeNamePath.is_empty)
        {
            return string.Empty;
        }

        if (currentNamespace is null or { is_empty: true } || !typeNamePath.@namespace.is_empty)
        {
            return typeNamePath.ToString();
        }

        return currentNamespace.qualify(typeNamePath).ToString();
    }

    #endregion

    #region 顶层常量

    private bool try_build_top_level_constant_reference(
        HirModule hir,
        HirCallable callable,
        string name,
        EGraph<AlgebraNode> graph,
        string targetArchTag,
        out Id valueId)
    {
        if (try_resolve_top_level_constant_initializer(hir, callable, name, out var initializer) &&
            initializer is not null)
        {
            valueId = build_expression(initializer, hir, callable, graph, targetArchTag);
            return true;
        }

        valueId = default;
        return false;
    }

    private static bool try_resolve_top_level_constant_initializer(
        HirModule hir,
        HirCallable callable,
        string name,
        out ValkyrieNode? initializer)
    {
        var currentNamespace = callable.namespace_path;
        var letName = ValkyrieNamePath.parse(name);
        var resolved = hir.resolve_let(letName, currentNamespace);
        if (resolved is null && !name.Contains('.', StringComparison.Ordinal))
        {
            resolved = hir.lets.FirstOrDefault(candidate =>
                string.Equals(candidate.name, name, StringComparison.Ordinal));
        }

        initializer = resolved is { is_mutable: false } ? resolved.initializer : null;
        return initializer is not null;
    }

    private static string build_qualified_name(string? currentNamespace, string name)
    {
        return string.IsNullOrWhiteSpace(currentNamespace)
            ? name
            : $"{currentNamespace}.{name}";
    }

    #endregion

    #region 瀛楅潰閲?

    private Id build_literal(TermNode literal, EGraph<AlgebraNode> graph)
    {
        switch (literal)
        {
            case TermLiteralNumberNode number:
                return graph.add(resolve_number_literal_node(number.value));
            case TermLiteralTextNode text:
                return graph.add(AlgebraNodeBuilder.StringConstant(text.value));
            case TermLiteralBooleanNode boolean:
                return graph.add(AlgebraNodeBuilder.BooleanConstant(boolean.value));
            case LiteralNullNode:
                return graph.add(AlgebraNodeBuilder.None());
            default:
                return graph.add(AlgebraNodeBuilder.None());
        }
    }

    private static Id build_number_literal_node(string rawText, EGraph<AlgebraNode> graph)
    {
        return graph.add(resolve_number_literal_node(rawText));
    }

    private static AlgebraNode resolve_number_literal_node(string rawText)
    {
        if (ValkyrieNumberLiteralFacts.is_floating_literal(rawText))
        {
            return new AlgebraNode.FloatConstant(ValkyrieNumberLiteralFacts.try_parse_f64(rawText, out var floatValue)
                ? floatValue
                : 0d);
        }

        return AlgebraNodeBuilder.Constant(ValkyrieNumberLiteralFacts.try_parse_i64(rawText, out var integerValue)
            ? integerValue
            : 0);
    }

    private static string get_type_name(TypeNode typeNode)
    {
        return typeNode switch
        {
            TypeLiteralNamePathNode literal => literal.path.full_name,
            _ => typeNode.ToString() ?? "unknown"
        };
    }

    #endregion
}
