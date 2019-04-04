using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Template;
using Std.Data.Text.Valkyrie.AST.Term;

namespace Nyar.Language.Valkyrie.Compiler.Meta;

/// <summary>
///     统一递归 MSP staging 求值器。
///     这是 Valkyrie multi-stage programming 的核心引擎。
///     接受任意层级的 AST，递归消除所有 <c>&lt;% %&gt;</c> 元节点，
///     直到 AST 中不再包含任何 meta 节点（到达 Level 0）。
/// </summary>
public sealed class MetaStager
{
    private const int _max_staging_depth = 64;

    /// <summary>
    ///     从最外层 CanonicalTriple 开始递归 staging。
    ///     这是外部调用的主入口。
    /// </summary>
    /// <param name="root">解析完成的编译单元（可能含任意深度 meta 节点）</param>
    /// <param name="canonicalTriple">目标 CanonicalTriple</param>
    /// <returns>完全特化的 Level 0 AST（不含任何 meta 节点）</returns>
    public CompilationUnit stage(CompilationUnit root, string canonicalTriple)
    {
        var outermostLevel = StagingLevel.from_canonical_triple(canonicalTriple);
        return stage_recursive(root, outermostLevel, _max_staging_depth);
    }

    /// <summary>
    ///     递归 staging：在给定 level 下处理当前 AST。
    ///     消除该层所有 meta 节点，若结果仍含 meta 节点则递归深度 +1。
    /// </summary>
    private CompilationUnit stage_recursive(CompilationUnit ast, StagingLevel level, int remainingDepth)
    {
        if (remainingDepth <= 0) return ast;

        if (!has_meta_nodes(ast)) return ast;

        var resultDeclarations = new List<AstNode>(ast.declarations.Count);

        foreach (var decl in ast.declarations)
        {
            var processed = stage_node(decl, level);

            switch (processed)
            {
                case List<AstNode> flattened:
                    resultDeclarations.AddRange(flattened);
                    break;

                case AstNode single:
                    resultDeclarations.Add(single);
                    break;

                case null:
                    break;
            }
        }

        var result = new CompilationUnit { declarations = resultDeclarations.AsReadOnly() };

        return stage_recursive(result, level, remainingDepth - 1);
    }

    #region 声明列表 staging

    /// <summary>
    ///     对声明列表执行递归 staging。
    /// </summary>
    private List<AstNode> stage_declarations(IReadOnlyList<AstNode> declarations, StagingLevel level)
    {
        var result = new List<AstNode>(declarations.Count);

        foreach (var decl in declarations)
        {
            var processed = stage_node(decl, level);

            switch (processed)
            {
                case List<AstNode> flattened:
                    result.AddRange(flattened);
                    break;

                case AstNode single:
                    result.Add(single);
                    break;
            }
        }

        return result;
    }

    #endregion

    #region 元节点检测

    /// <summary>
    ///     检查 AST 中是否还含有任何需要 staging 的 meta 节点（递归遍历整个树）。
    /// </summary>
    private static bool has_meta_nodes(CompilationUnit ast)
    {
        return ast.declarations.Any(has_meta_nodes_recursive);
    }

    /// <summary>
    ///     递归检查一个节点及其子树中是否含有 meta 节点。
    /// </summary>
    private static bool has_meta_nodes_recursive(AstNode node)
    {
        if (is_meta_node(node)) return true;

        return get_child_lists(node).Any(childList => childList.Any(has_meta_nodes_recursive));
    }

    /// <summary>
    ///     判断一个节点是否是 meta 节点（需要在 staging 阶段被消除）。
    /// </summary>
    private static bool is_meta_node(AstNode node)
    {
        return node is TemplateMatchNode or TemplateIfNode or TemplateLoopNode;
    }

    /// <summary>
    ///     获取一个节点的所有 <c>IReadOnlyList&lt;ValkyrieNode&gt;</c> 类型子节点列表。
    ///     用于递归树遍历，找到嵌套在容器中的 meta 节点。
    /// </summary>
    private static IEnumerable<IReadOnlyList<AstNode>> get_child_lists(AstNode node)
    {
        switch (node)
        {
            case FunctionDecl micro:
                if (micro.body is not null) yield return micro.body.statements;

                break;

            case TemplateIfNode ift:
                yield return ift.then_body;
                if (ift.else_body is not null) yield return ift.else_body;

                break;

            case TemplateLoopNode loop:
                yield return loop.body;
                break;
        }
    }

    #endregion

    #region 节点 staging

    /// <summary>
    ///     在给定 level 下 staging 单个节点（递归处理子节点）。
    /// </summary>
    private object? stage_node(AstNode node, StagingLevel level)
    {
        return node switch
        {
            // 元节点 — 执行 compile-time 计算
            TemplateMatchNode match => stage_match(match, level),
            TemplateIfNode ift => stage_if(ift, level),
            TemplateLoopNode loop => stage_loop(loop, level),

            // 容器节点 — 递归 staging 子节点
            FunctionDecl micro => stage_micro_declaration(micro, level),

            // 叶子节点 — 原样返回
            _ => node
        };
    }

    /// <summary>
    ///     Staging <c>DeclareMicro</c>：递归处理其 Body 中的 meta 节点。
    /// </summary>
    private AstNode stage_micro_declaration(FunctionDecl micro, StagingLevel level)
    {
        if (micro.body is null) return micro;

        var stagedBody = (BlockStmt)stage_block_stmt(micro.body, level);
        return micro with { body = stagedBody };
    }

    /// <summary>
    ///     Staging <c>FunctionBody</c>：递归处理 Statements 中的 meta 节点。
    /// </summary>
    private object stage_block_stmt(BlockStmt block, StagingLevel level)
    {
        var stagedStatements = new List<AstNode>(block.statements.Count);

        foreach (var stmt in block.statements)
        {
            var processed = stage_node(stmt, level);

            switch (processed)
            {
                case List<AstNode> flattened:
                    stagedStatements.AddRange(flattened);
                    break;

                case AstNode single:
                    stagedStatements.Add(single);
                    break;

                case null:
                    break;
            }
        }

        return block with { statements = stagedStatements.AsReadOnly() };
    }

    /// <summary>
    ///     Staging <c>&lt;% match expr %&gt; ... &lt;% end match %&gt;</c>。
    /// </summary>
    private object? stage_match(TemplateMatchNode match, StagingLevel level)
    {
        var subject = evaluate_meta_expression(match.begin.expression, level);
        FragmentArmPart? fallbackArm = null;

        foreach (var armPart in match.arm_parts)
        {
            if (!is_arm_guard_satisfied(armPart.arm, level)) continue;

            switch (armPart.arm)
            {
                case FragmentArmCaseNode caseArm:
                {
                    var matched = is_pattern_match(caseArm.pattern, subject);
                    if (matched)
                    {
                        return stage_declarations(armPart.parts, level);
                    }

                    break;
                }
                case FragmentArmWhenNode whenArm when is_when_match(whenArm, level):
                    return stage_declarations(armPart.parts, level);
                case FragmentArmElseNode:
                    fallbackArm = armPart;
                    break;
            }
        }

        return fallbackArm is not null
            ? stage_declarations(fallbackArm.parts, level)
            : null;
    }

    /// <summary>
    ///     Staging <c>&lt;% if condition %&gt; ... &lt;% end if %&gt;</c>。
    /// </summary>
    private object? stage_if(TemplateIfNode ift, StagingLevel level)
    {
        var conditionValue = evaluate_meta_expression(ift.condition, level);
        var conditionTrue = !string.IsNullOrEmpty(conditionValue)
                            && !string.Equals(conditionValue, "false", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(conditionValue, "0", StringComparison.OrdinalIgnoreCase);

        var selectedBody = conditionTrue ? ift.then_body : ift.else_body;
        return selectedBody is { Count: > 0 }
            ? stage_declarations(selectedBody, level)
            : null;
    }

    /// <summary>
    ///     Staging <c>&lt;% loop var in range %&gt; ... &lt;% end loop %&gt;</c>。
    ///     循环展开暂未完整实现，当前直接返回 body（等效于展开一次）。
    /// </summary>
    private object? stage_loop(TemplateLoopNode loop, StagingLevel level)
    {
        return stage_declarations(loop.body, level);
    }

    #endregion

    #region 表达式求值

    /// <summary>
    ///     在 staging 环境中求值一个表达式节点。
    /// </summary>
    private static string evaluate_meta_expression(AstNode? node, StagingLevel level)
    {
        return node switch
        {
            null => string.Empty,

            IdentifierNode identifier => evaluate_identifier(identifier, level),
            TermLiteralNamePathNode namePath => evaluate_qualified_path(namePath.path, level),

            TermDotExpression dotAccess => evaluate_dot_access(dotAccess, level),

            TermLiteralNumberNode literal => evaluate_literal(literal),
            TermLiteralTextNode literal => evaluate_literal(literal),
            TermLiteralBooleanNode literal => evaluate_literal(literal),
            LiteralNullNode literal => evaluate_literal(literal),

            TermBinaryExpression binary => evaluate_binary(binary, level),

            QualifiedPathNode path => path.full_name,

            _ => string.Empty
        };
    }

    private static string evaluate_identifier(IdentifierNode node, StagingLevel level)
    {
        return node.name switch
        {
            "arch" => level.arch,
            "impl" => level.impl,
            "vendor" => level.vendor,
            "spec" => level.spec,
            "os" => level.os,
            "abi" => level.abi,
            "target" => level.target,
            _ => node.name
        };
    }

    private static string evaluate_qualified_path(QualifiedPathNode path, StagingLevel level)
    {
        return path.full_name switch
        {
            "arch" => level.arch,
            "impl" => level.impl,
            "vendor" => level.vendor,
            "spec" => level.spec,
            "os" => level.os,
            "abi" => level.abi,
            "target" => level.target,
            _ => path.full_name
        };
    }

    private static string strip_arm_condition(string conditionName)
    {
        if (conditionName.Length >= 2
            && ((conditionName[0] == '"' && conditionName[^1] == '"')
                || (conditionName[0] == '\'' && conditionName[^1] == '\'')))
            return conditionName[1..^1];

        return conditionName;
    }

    private static string evaluate_dot_access(TermDotExpression node, StagingLevel level)
    {
        if (node.caller is TermLiteralNamePathNode { path.name: "target" })
            return node.callee.name switch
            {
                "arch" => level.arch,
                "impl" => level.impl,
                "vendor" => level.vendor,
                "spec" => level.spec,
                "os" => level.os,
                "abi" => level.abi,
                _ => node.callee.name
            };

        var objectValue = evaluate_meta_expression(node.caller, level);
        if (string.Equals(objectValue, "target", StringComparison.OrdinalIgnoreCase))
            return node.callee.name switch
            {
                "arch" => level.arch,
                "impl" => level.impl,
                "vendor" => level.vendor,
                "spec" => level.spec,
                "os" => level.os,
                "abi" => level.abi,
                _ => node.callee.name
            };

        return node.callee.name;
    }

    private static string evaluate_literal(TermNode node)
    {
        return node switch
        {
            TermLiteralNumberNode number => number.value,
            TermLiteralTextNode text => extract_text_literal(text),
            TermLiteralBooleanNode boolean => boolean.value ? "true" : "false",
            LiteralNullNode => string.Empty,
            _ => string.Empty
        };
    }

    private static string evaluate_binary(TermBinaryExpression node, StagingLevel level)
    {
        var left = evaluate_meta_expression(node.left, level);
        var right = evaluate_meta_expression(node.right, level);

        return node.@operator switch
        {
            TermBinaryOperator.equal => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
                ? "true"
                : "false",
            TermBinaryOperator.not_equal => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
                ? "false"
                : "true",
            _ => "false"
        };
    }

    private static bool is_arm_guard_satisfied(FragmentArmNode arm, StagingLevel level)
    {
        var guard = arm switch
        {
            FragmentArmCaseNode caseArm => caseArm.guard,
            FragmentArmTypeNode typeArm => typeArm.guard,
            FragmentArmWhenNode whenArm => whenArm.guard,
            _ => null
        };

        if (guard is null) return true;

        var guardValue = evaluate_meta_expression(guard, level);
        return string.Equals(guardValue, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool is_pattern_match(PatternNode pattern, string subject)
    {
        return pattern switch
        {
            PatternLiteralTextNode textPattern => string.Equals(
                textPattern.value,
                strip_arm_condition(subject),
                StringComparison.OrdinalIgnoreCase),
            PatternLiteralWildcardNode => true,
            _ => false
        };
    }

    private static bool is_when_match(FragmentArmWhenNode arm, StagingLevel level)
    {
        var value = evaluate_meta_expression(arm.term, level);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string extract_text_literal(TermLiteralTextNode node)
    {
        return node.value;
    }

    #endregion
}