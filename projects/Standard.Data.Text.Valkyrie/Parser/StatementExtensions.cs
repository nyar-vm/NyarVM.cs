using System;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.Lexer;
using DiagnosticTextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Valkyrie.Parser;

/// <summary>
///     语句解析扩展入口。
/// </summary>
internal static class StatementExtensions
{
    extension(TokenStream tokens)
    {
        internal ValkyrieNode parse_statement_node(ValkyrieLanguage language)
        {
            while (tokens.check(ValkyrieTokenKind.comment_start))
            {
                tokens.advance();
                if (tokens.check(ValkyrieTokenKind.comment_content)) tokens.advance();
            }

            if (tokens.check(ValkyrieTokenKind.@return))
            {
                tokens.advance();
                ValkyrieNode? value = null;
                if (!tokens.check(ValkyrieTokenKind.semicolon) && !tokens.check(ValkyrieTokenKind.brace_r))
                    value = tokens.parse_term_node();

                expect_or_warn_semicolon(tokens);
                return new ReturnStatement { value = value };
            }

            if (tokens.check(ValkyrieTokenKind.@break)) return parse_break_stmt(tokens);

            if (tokens.check(ValkyrieTokenKind.@continue)) return parse_continue_stmt(tokens);

            if (tokens.check(ValkyrieTokenKind.resume)) return parse_resume_stmt(tokens);

            if (tokens.check(ValkyrieTokenKind.raise)) return parse_raise_stmt(tokens);

            if (tokens.check(ValkyrieTokenKind.yield)) return parse_yield_stmt(tokens);

            if (tokens.check(ValkyrieTokenKind.@try)) return parse_try_stmt(tokens, language);

            if (tokens.check(ValkyrieTokenKind.let))
            {
                tokens.advance();
                return tokens.parse_variable_decl_node(language);
            }

            if (tokens.check(ValkyrieTokenKind.@if)) return parse_if_stmt(tokens, language);

            if (tokens.check(ValkyrieTokenKind.match)) return parse_match_stmt(tokens, language);

            if (tokens.check(ValkyrieTokenKind.@catch)) return parse_catch_stmt(tokens, language);

            if (tokens.check(ValkyrieTokenKind.@while)) return parse_while_stmt(tokens, language);

            if (tokens.check(ValkyrieTokenKind.until)) return parse_until_stmt(tokens, language);

            if (tokens.check(ValkyrieTokenKind.loop)) return parse_loop_stmt(tokens, language);

            if (tokens.check(ValkyrieTokenKind.discard))
            {
                tokens.advance();
                expect_or_warn_semicolon(tokens);
                return new LetStatement();
            }

            if (tokens.check(ValkyrieTokenKind.template_l))
            {
                var metaNode = tokens.parse_meta_template_node();
                return metaNode ?? new LetStatement();
            }

            if (tokens.check(ValkyrieTokenKind.brace_l))
            {
                return tokens.parse_block_node(language);
            }

            if (tokens.is_declaration_start_node(language)) return tokens.parse_declaration_node(language);

            var expr = tokens.parse_term_node();
            expect_or_warn_semicolon(tokens);
            return try_wrap_assignment_statement(expr);
        }
    }

    private static IfStatement parse_if_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.advance();
        ValkyrieNode cond;
        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
        {
            tokens.advance();
            cond = tokens.parse_term_node();
            tokens.expect(ValkyrieTokenKind.parenthesis_r);
        }
        else
        {
            cond = tokens.parse_block_introducer_term_node();
        }

        var thenBlock = tokens.parse_block_node(language);
        ValkyrieNode? elseBlock = null;

        if (tokens.check(ValkyrieTokenKind.@else))
        {
            tokens.advance();
            if (tokens.check(ValkyrieTokenKind.@if))
                elseBlock = parse_if_stmt(tokens, language);
            else
                elseBlock = coerce_else_branch(tokens, language);
        }

        return new IfStatement(cond, thenBlock, elseBlock, default);
    }

    private static MatchStatementNode parse_match_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.advance();

        // Support both "match expr { ... }" and "match (expr) { ... }" syntax
        TermNode expr;
        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
        {
            tokens.advance();
            expr = tokens.parse_term_node();
            tokens.expect(ValkyrieTokenKind.parenthesis_r);
        }
        else
        {
            expr = tokens.parse_block_introducer_term_value_node();
        }

        tokens.expect(ValkyrieTokenKind.brace_l);
        var arms = new List<ArmNode>();
        while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            arms.Add(parse_match_arm(tokens, language));

        tokens.expect(ValkyrieTokenKind.brace_r);
        return new MatchStatementNode { expression = expr, arms = arms };
    }

    internal static ArmNode parse_match_arm(TokenStream tokens, ValkyrieLanguage language)
    {
        if (tokens.check(ValkyrieTokenKind.@else))
        {
            tokens.advance();
            // 支持 `else =>` 和 `else:` 两种语法
            if (tokens.check(ValkyrieTokenKind.fat_arrow))
            {
                tokens.advance();
            }
            else
            {
                tokens.expect(ValkyrieTokenKind.colon);
            }

            var arm = new ArmElseNode
            {
                body = parse_arm_body(tokens, language)
            };
            return arm;
        }

        if (tokens.check(ValkyrieTokenKind.@case)) tokens.advance();

        // 支持仅包含守卫的匹配臂（when <condition> => ... 或 when <condition>: ...）
        if (tokens.check(ValkyrieTokenKind.when))
        {
            tokens.advance();
            var whenGuard = tokens.parse_term_value_node();

            // 支持 `=>` 和 `:` 两种语法
            if (tokens.check(ValkyrieTokenKind.fat_arrow))
            {
                tokens.advance();
            }
            else
            {
                tokens.expect(ValkyrieTokenKind.colon);
            }

            var whenBody = parse_arm_body(tokens, language);

            return new ArmCaseNode
            {
                pattern = PatternNodeExtensions.create_wildcard_pattern(),
                guard = whenGuard,
                body = whenBody
            };
        }

        var (pattern, guard) = parse_case_arm_pattern(tokens);

        // Support both ':' and '=>' syntax for match arms
        if (tokens.check(ValkyrieTokenKind.fat_arrow))
            tokens.advance();
        else
            tokens.expect(ValkyrieTokenKind.colon);

        var armBody = parse_arm_body(tokens, language);
        var armNode = new ArmCaseNode
        {
            pattern = pattern,
            guard = guard,
            body = armBody
        };
        return armNode;
    }

    private static CatchStatementNode parse_catch_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.advance();
        var expr = tokens.parse_block_introducer_term_value_node();

        tokens.expect(ValkyrieTokenKind.brace_l);
        var arms = new List<ArmNode>();
        while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            arms.Add(parse_catch_arm(tokens, language));

        tokens.expect(ValkyrieTokenKind.brace_r);
        return new CatchStatementNode { expression = expr, arms = arms };
    }

    internal static ArmNode parse_catch_arm(TokenStream tokens, ValkyrieLanguage language)
    {
        if (tokens.check(ValkyrieTokenKind.@case))
        {
            tokens.advance();
            var (pattern, guard) = parse_case_arm_pattern(tokens);

            tokens.expect(ValkyrieTokenKind.colon);
            var body = parse_arm_body(tokens, language);
            return new ArmCaseNode
            {
                pattern = pattern,
                guard = guard,
                body = body
            };
        }

        if (tokens.check(ValkyrieTokenKind.@else))
        {
            tokens.advance();
            tokens.expect(ValkyrieTokenKind.colon);
            var body = parse_arm_body(tokens, language);
            return new ArmElseNode { body = body };
        }

        // 兜底：尝试解析模式
        var (fallbackPattern, fallbackGuard) = parse_case_arm_pattern(tokens);
        tokens.expect(ValkyrieTokenKind.colon);
        var fallbackBody = parse_arm_body(tokens, language);
        return new ArmCaseNode
        {
            pattern = fallbackPattern,
            guard = fallbackGuard,
            body = fallbackBody
        };
    }

    private static FunctionBody parse_arm_body(TokenStream tokens, ValkyrieLanguage language)
    {
        if (tokens.check(ValkyrieTokenKind.brace_l)) return tokens.parse_block_node(language);

        var stmts = new List<ValkyrieNode> { tokens.parse_statement_node(language) };
        while (!tokens.is_at_end()
               && !tokens.check(ValkyrieTokenKind.brace_r)
               && !tokens.check(ValkyrieTokenKind.@case)
               && !tokens.check(ValkyrieTokenKind.@else)
               && !tokens.check(ValkyrieTokenKind.fat_arrow)) // For single-line arms with '=>'
            stmts.Add(tokens.parse_statement_node(language));

        return new FunctionBody { statements = stmts };
    }

    private static PatternNode parse_arm_pattern(TokenStream tokens)
    {
        return tokens.parse_pattern_expression_node();
    }

    private static (PatternNode Pattern, TermNode? Guard) parse_case_arm_pattern(TokenStream tokens)
    {
        var pattern = parse_arm_pattern(tokens);
        TermNode? guard = null;

        // Support both 'if' and 'when' keywords for guard expressions
        if (tokens.check(ValkyrieTokenKind.@if) || tokens.check(ValkyrieTokenKind.when))
        {
            tokens.advance();
            guard = tokens.parse_term_value_node();
        }

        return (pattern, guard);
    }

    private static PatternNode report_unsupported_case_pattern(TokenStream tokens)
    {
        tokens.diagnostics?.report_error(new DiagnosticTextSpan(tokens.position, 1),
            "当前语句 `match case` 只能映射到现有 AST 的 `PatternNode`。");
        return PatternNodeExtensions.create_wildcard_pattern();
    }

    private static ValkyrieNode coerce_else_branch(TokenStream tokens, ValkyrieLanguage language)
    {
        var block = tokens.parse_block_node(language);
        if (block.statements.Count == 1)
        {
            return block.statements[0];
        }

        return block;
    }

    private static ResumeStatement parse_resume_stmt(TokenStream tokens)
    {
        tokens.advance();
        ValkyrieNode? value = null;
        if (!tokens.check(ValkyrieTokenKind.semicolon) && !tokens.check(ValkyrieTokenKind.brace_r))
            value = tokens.parse_term_node();

        expect_or_warn_semicolon(tokens);
        return new ResumeStatement { value = value };
    }

    private static WhileStatement parse_while_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.advance();
        ValkyrieNode cond;
        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
        {
            tokens.advance();
            cond = tokens.parse_term_node();
            tokens.expect(ValkyrieTokenKind.parenthesis_r);
        }
        else
        {
            cond = tokens.parse_block_introducer_term_node();
        }

        var body = tokens.parse_block_node(language);
        return new WhileStatement { condition = cond, body = body };
    }

    private static UntilStatement parse_until_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.advance();
        ValkyrieNode cond;
        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
        {
            tokens.advance();
            cond = tokens.parse_term_node();
            tokens.expect(ValkyrieTokenKind.parenthesis_r);
        }
        else
        {
            cond = tokens.parse_block_introducer_term_node();
        }

        var body = tokens.parse_block_node(language);
        return new UntilStatement { condition = cond, body = body };
    }

    private static ValkyrieNode parse_loop_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.advance();

        if (tokens.check(ValkyrieTokenKind.brace_l))
        {
            var infiniteBody = tokens.parse_block_node(language);
            return new LoopStatement(null, null, null, infiniteBody, default);
        }

        if (has_loop_header_semicolon(tokens)) return parse_counted_loop_stmt(tokens, language);

        return parse_loop_in_stmt(tokens, language);
    }

    private static LoopStatement parse_counted_loop_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        ValkyrieNode? initializer = null;
        ValkyrieNode? condition = null;
        ValkyrieNode? update = null;

        if (!tokens.check(ValkyrieTokenKind.semicolon))
            initializer = parse_loop_initializer(tokens, language);
        else
            tokens.advance();

        if (!tokens.check(ValkyrieTokenKind.semicolon)) condition = tokens.parse_term_node();

        tokens.expect(ValkyrieTokenKind.semicolon);

        if (!tokens.check(ValkyrieTokenKind.brace_l)) update = tokens.parse_block_introducer_term_node();

        var body = tokens.parse_block_node(language);
        return new LoopStatement(initializer, condition, update, body, default);
    }

    private static ValkyrieNode parse_loop_initializer(TokenStream tokens, ValkyrieLanguage language)
    {
        if (tokens.check(ValkyrieTokenKind.let))
        {
            tokens.advance();
            return tokens.parse_variable_decl_node(language);
        }

        var initializer = tokens.parse_term_node();
        tokens.expect(ValkyrieTokenKind.semicolon);
        return try_wrap_assignment_statement(initializer);
    }

    private static ValkyrieNode try_wrap_assignment_statement(TermNode term)
    {
        if (term is TermBinaryExpression binary && is_assignment_operator(binary.@operator))
        {
            return new AssignmentStatement
            {
                @operator = binary.@operator,
                target = binary.left,
                value = binary.right,
                span = binary.span
            };
        }

        return term;
    }

    private static bool is_assignment_operator(TermBinaryOperator op)
    {
        return op switch
        {
            TermBinaryOperator.assign or
                TermBinaryOperator.plus_assign or
                TermBinaryOperator.minus_assign or
                TermBinaryOperator.multiply_assign or
                TermBinaryOperator.divide_assign or
                TermBinaryOperator.modulus_assign or
                TermBinaryOperator.and_assign or
                TermBinaryOperator.or_assign or
                TermBinaryOperator.xor_assign or
                TermBinaryOperator.left_shift_assign or
                TermBinaryOperator.right_shift_assign => true,
            _ => false
        };
    }

    private static LoopInStatement parse_loop_in_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        string? iterName = null;
        PatternNode? iteratorPattern = null;
        ValkyrieNode? iterable = null;

        if (tokens.check(ValkyrieTokenKind.@in))
        {
            tokens.advance();
            iterable = tokens.parse_block_introducer_term_node();
        }
        else
        {
            iteratorPattern = tokens.parse_pattern_expression_node();
            if (iteratorPattern is PatternLiteralVariableNode variablePattern &&
                !string.Equals(variablePattern.name, "_", StringComparison.Ordinal))
            {
                iterName = variablePattern.name;
            }

            tokens.expect(ValkyrieTokenKind.@in);

            iterable = tokens.parse_block_introducer_term_node();
        }

        var body = tokens.parse_block_node(language);
        return new LoopInStatement(iterName, iteratorPattern, iterable, body, default);
    }

    private static bool has_loop_header_semicolon(TokenStream tokens)
    {
        var parenDepth = 0;
        var bracketDepth = 0;
        for (var offset = 0; !tokens.is_at_end(); offset++)
        {
            var kind = tokens.peek_valkyrie_kind(offset);
            switch (kind)
            {
                case ValkyrieTokenKind.parenthesis_l:
                    parenDepth++;
                    break;
                case ValkyrieTokenKind.parenthesis_r:
                    parenDepth--;
                    break;
                case ValkyrieTokenKind.bracket_l:
                    bracketDepth++;
                    break;
                case ValkyrieTokenKind.bracket_r:
                    bracketDepth--;
                    break;
                case ValkyrieTokenKind.semicolon when parenDepth == 0 && bracketDepth == 0:
                    return true;
                case ValkyrieTokenKind.brace_l when parenDepth == 0 && bracketDepth == 0:
                    return false;
                case ValkyrieTokenKind.eos:
                    return false;
            }
        }

        return false;
    }

    private static void expect_or_warn_semicolon(TokenStream tokens)
    {
        tokens.match(ValkyrieTokenKind.semicolon);
    }

    private static RaiseStatement parse_raise_stmt(TokenStream tokens)
    {
        tokens.advance();
        var value = tokens.parse_term_node();
        expect_or_warn_semicolon(tokens);
        return new RaiseStatement { value = value };
    }

    private static BreakStatement parse_break_stmt(TokenStream tokens)
    {
        tokens.advance();
        expect_or_warn_semicolon(tokens);
        return new BreakStatement();
    }

    private static ContinueStatement parse_continue_stmt(TokenStream tokens)
    {
        tokens.advance();
        expect_or_warn_semicolon(tokens);
        return new ContinueStatement();
    }

    private static YieldStatement parse_yield_stmt(TokenStream tokens)
    {
        tokens.advance();
        if (tokens.check(ValkyrieTokenKind.@break))
        {
            tokens.advance();
            expect_or_warn_semicolon(tokens);
            return new YieldStatement { keyword = YieldKeyword.YieldBreak, value = null };
        }

        if (tokens.check(ValkyrieTokenKind.@return))
        {
            tokens.advance();
            TermNode? value = null;
            if (!tokens.check(ValkyrieTokenKind.semicolon) && !tokens.check(ValkyrieTokenKind.brace_r))
            {
                value = tokens.parse_term_node();
            }

            expect_or_warn_semicolon(tokens);
            return new YieldStatement { keyword = YieldKeyword.YieldReturn, value = value };
        }

        var yieldValue = tokens.parse_term_node();
        expect_or_warn_semicolon(tokens);
        return new YieldStatement { keyword = YieldKeyword.Yield, value = yieldValue };
    }

    private static TryStatement parse_try_stmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.advance();
        var resultType = tokens.parse_type_expression_node();
        var body = tokens.parse_block_node(language);
        return new TryStatement { result_type = resultType, body = body };
    }
}
