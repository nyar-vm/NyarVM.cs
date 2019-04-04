using EffectKind = Nyar.IR.Intent.EffectKind;
using EffectSet = Nyar.IR.Intent.EffectSet;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.TypeChecker;

/// <summary>
///     Valkyrie 类型检查器最小实现。
///     当前仅维持运行时编译链路可用，完整类型系统后续恢复。
/// </summary>
public sealed class TypeChecker
{
    private readonly List<TypeDiagnostic> _diagnostics = [];
    private readonly Dictionary<string, ValkyrieType> _typeRegistry = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ValkyrieType> _typeSubstitutions = new(StringComparer.Ordinal);
    private Scope _currentScope;

    /// <summary>
    ///     检查编译单元并返回诊断结果。
    ///     现阶段为桩实现，直接返回空诊断，避免无关目标阻塞构建。
    /// </summary>
    public TypeCheckResult check(CompilationUnit compilationUnit, string? filePath = null, string? sourceText = null)
    {
        _diagnostics.Clear();
        _currentScope = new Scope();
        return new TypeCheckResult(_diagnostics);
    }

    /// <summary>
    ///     添加错误诊断
    /// </summary>
    private void AddError(string code, string message, TextSpan span, string hint)
    {
        _diagnostics.Add(new TypeDiagnostic(code, message, DiagnosticSeverity.error)
        {
            suggestion = hint
        });
    }

    /// <summary>
    ///     添加警告诊断
    /// </summary>
    private void AddWarning(string code, string message, TextSpan span, string hint)
    {
        _diagnostics.Add(new TypeDiagnostic(code, message, DiagnosticSeverity.warning)
        {
            suggestion = hint
        });
    }

    /// <summary>
    ///     类型推断（桩实现，返回 Auto）
    /// </summary>
    private ValkyrieType InferType(AstNode node)
    {
        return ValkyrieType.auto;
    }

    /// <summary>解析类型注解（桩实现，接受任意 AST 类型节点作为参数）</summary>
    private ValkyrieType ResolveTypeAnnotation(object? annotation)
    {
        if (annotation is null) return ValkyrieType.auto;

        return new ValkyrieType(TypeKind.primitive, annotation.ToString() ?? "auto");
    }

    /// <summary>
    ///     检查声明分发（桩实现，暂不执行具体类型检查）
    /// </summary>
    private void CheckDeclaration(AstNode node)
    {
        // 桩实现：被排除的分部文件（Declarations/Expressions/Analysis 等）中定义了具体检查方法，
        // 当前编译不需要完整类型检查，仅维持链路可用。
    }

    /// <summary>
    ///     查找符号（桩实现）
    /// </summary>
    private Symbol? GetSymbol(string name)
    {
        return _currentScope.Resolve(name);
    }

    /// <summary>
    ///     判断类型是否为子类型（桩实现）
    /// </summary>
    private bool IsSubtypeOf(ValkyrieType a, ValkyrieType b)
    {
        return b.is_assignable_from(a);
    }

    /// <summary>
    ///     解析当前作用域中的变量类型
    /// </summary>
    private ValkyrieType resolve_variable_type(string name)
    {
        var symbol = _currentScope.Resolve(name);
        return symbol?.type ?? ValkyrieType.error;
    }

    /// <summary>
    ///     推断表达式的效应类型
    /// </summary>
    private Typed InferEffectType(AstNode node)
    {
        if (node is RaiseStatement rs)
        {
            return check_raise(rs);
        }

        if (node is CatchStatementNode cs)
        {
            return check_catch(cs);
        }

        if (node is TryStatement ts)
        {
            return check_try(ts);
        }

        return new Typed(InferType(node), EffectSet.pure);
    }

    #region 效应类型推断辅助方法

    /// <summary>
    ///     检查 raise 语句的效应类型
    /// </summary>
    /// <remarks>
    ///     raise 表达式正常类型为不可达符号 <c>!</c>，效应集合包含被抛出的效应操作。
    ///     若被抛出的操作实现了 Effectful trait 且 Resume 不为 <c>!</c>，则为可恢复效应；
    ///     否则为不可恢复效应，表达式类型为 <c>!</c>。
    /// </remarks>
    private Typed check_raise(RaiseStatement raiseExpr)
    {
        var op = infer_effect_from_raise_value(raiseExpr.value);
        return new Typed(ValkyrieType.unimplemented_symbol("!"), EffectSet.from(op));
    }

    /// <summary>
    ///     检查 catch 表达式的效应消去
    /// </summary>
    /// <remarks>
    ///     推断 handler 覆盖的效应子集 E_h，剩余效应为 E_body \ E_h。
    ///     类型规则：
    ///     <list type="bullet">
    ///         <item>若所有分支均以 resume 结束，整体类型为 T / (E \ E_h)</item>
    ///         <item>若某些分支未调用 resume，整体类型为 B / (E \ E_h)</item>
    ///     </list>
    /// </remarks>
    private Typed check_catch(CatchStatementNode catchExpr)
    {
        var bodyTyped = InferEffectType(catchExpr.expression);
        var handled = infer_handled_effects_from_arms(catchExpr.arms);
        return new Typed(bodyTyped.normal, bodyTyped.effects.difference(handled));
    }

    /// <summary>
    ///     检查 try Result 表达式的效应
    /// </summary>
    /// <remarks>
    ///     try Result&lt;T, [E_c]&gt; { body } 将效应操作转为 Result：
    ///     <list type="bullet">
    ///         <item>正常返回 v : T 则整体为 Fine(v)</item>
    ///         <item>若内部 raise 了属于捕获列表的效应操作，计算立即终止，整体为 Fail(op)</item>
    ///     </list>
    ///     剩余效应为 E_body \ E_c。
    /// </remarks>
    private Typed check_try(TryStatement tryExpr)
    {
        var bodyTyped = infer_effect_type_from_body(tryExpr.body);
        var captured = resolve_effects_from_type_nodes(tryExpr.capture_types);
        var resultType = ResolveTypeAnnotation(tryExpr.result_type);
        return new Typed(resultType, bodyTyped.effects.difference(captured));
    }

    /// <summary>
    ///     验证 resume(v) 的类型匹配效应操作的 Effectful::Resume 类型
    /// </summary>
    /// <remarks>
    ///     resume 仅可在 catch 分支内使用，恢复被挂起的 continuation，
    ///     将 value 作为原 raise 表达式的返回值。
    ///     value 的类型必须严格等于被匹配效应的 Effectful::Resume。
    /// </remarks>
    private void check_resume(ResumeStatement resumeExpr)
    {
        if (resumeExpr.value is not null)
        {
            var valueType = InferType(resumeExpr.value);
            if (valueType.is_error)
            {
                return;
            }

            // 验证 resume(v) 的类型匹配当前效应上下文的 Effectful::Resume
            // 完整实现需要跟踪效应上下文的恢复类型，桩实现暂跳过精确验证
        }
    }

    /// <summary>
    ///     从 raise 表达式中推断被抛出的效应操作类型
    /// </summary>
    private EffectKind infer_effect_from_raise_value(TermNode value)
    {
        // 根据表达式的结构推断效应类型
        // 桩实现：使用 perform 作为通用的效应操作类型
        // 完整实现需要解析 value 的类型并映射到对应的 EffectKind
        return EffectKind.perform;
    }

    /// <summary>
    ///     从 catch 分支列表中推断被处理的效应集合
    /// </summary>
    private EffectSet infer_handled_effects_from_arms(IReadOnlyList<ArmNode> arms)
    {
        var handled = new List<EffectKind>();

        foreach (var arm in arms)
        {
            // 每个 arm 通过模式匹配处理特定效应操作
            // 桩实现：无法精确推断每个 arm 覆盖的效应，返回空集
            // 完整实现需要分析 arm 的 pattern 来确定覆盖的效应类型
        }

        return handled.Count > 0 ? new EffectSet(handled) : EffectSet.pure;
    }

    /// <summary>
    ///     从类型节点列表中解析效应集合
    /// </summary>
    private EffectSet resolve_effects_from_type_nodes(IReadOnlyList<TypeNode> typeNodes)
    {
        var effects = new List<EffectKind>();

        foreach (var typeNode in typeNodes)
        {
            var effect = resolve_effect_from_type_node(typeNode);
            if (effect.HasValue)
            {
                effects.Add(effect.Value);
            }
        }

        return effects.Count > 0 ? new EffectSet(effects) : EffectSet.pure;
    }

    /// <summary>
    ///     从单个类型节点解析效应类型
    /// </summary>
    private EffectKind? resolve_effect_from_type_node(TypeNode typeNode)
    {
        var resolved = ResolveTypeAnnotation(typeNode);
        if (resolved.is_error)
        {
            return null;
        }

        if (resolved.effects.Count > 0)
        {
            return map_effect_from_type_system(resolved.effects[0]);
        }

        return null;
    }

    /// <summary>
    ///     将 TypeSystem.EffectKind 映射到 IR.EffectKind
    /// </summary>
    private static EffectKind map_effect_from_type_system(Nyar.Language.Valkyrie.TypeSystem.EffectKind source)
    {
        return source switch
        {
            Nyar.Language.Valkyrie.TypeSystem.EffectKind.io => EffectKind.io,
            _ => EffectKind.perform
        };
    }

    /// <summary>
    ///     推断函数体中所有语句的效应并集
    /// </summary>
    private Typed infer_effect_type_from_body(FunctionBody body)
    {
        var bodyEffects = EffectSet.pure;
        ValkyrieType? lastExprType = null;

        foreach (var stmt in body.statements)
        {
            var stmtTyped = InferEffectType(stmt);
            bodyEffects = bodyEffects.union(stmtTyped.effects);
            lastExprType = stmtTyped.normal;
        }

        return new Typed(lastExprType ?? ValkyrieType.unit, bodyEffects);
    }

    #endregion

    #region 效应签名验证

    /// <summary>
    ///     验证函数效应签名：函数体的效应集合必须为声明效应集合的子集
    /// </summary>
    /// <param name="declared">函数声明中标注的效应集合</param>
    /// <param name="inferred">函数体中推断出的效应集合</param>
    private void validate_effect_signature(EffectSet declared, EffectSet inferred)
    {
        if (!inferred.is_subset_of(declared))
        {
            AddError("VALK3001", "函数效应签名不匹配：函数体效应超出声明范围", default, "将声明的效应集合扩大，或移除多余的 raise");
        }
    }

    #endregion
}