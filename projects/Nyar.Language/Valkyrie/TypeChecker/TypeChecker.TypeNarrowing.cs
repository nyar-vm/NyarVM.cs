using Oak.Valkyrie.AST.Term;

namespace Nyar.Language.Valkyrie.TypeChecker;

/// <summary>
/// 类型窄化与类型测试支持
/// 在 if/match 条件中的类型测试后，窄化变量类型
/// 支持 is 表达式推断、as 表达式推断、显式转型检查
/// </summary>
public sealed partial class TypeChecker
{
    #region 类型窄化

    /// <summary>
    /// 类型窄化上下文：记录在条件分支中窄化的变量
    /// </summary>
    private readonly Dictionary<string, ValkyrieType> _narrowed_types = new(StringComparer.Ordinal);

    /// <summary>
    /// 在 if 语句的条件分支中应用类型窄化
    /// 如果条件包含类型测试（如 x is i32），则在 then 块中窄化 x 的类型
    /// </summary>
    private void apply_type_narrowing_in_if(IfStatement ifStmt)
    {
        var savedNarrowed = new Dictionary<string, ValkyrieType>(_narrowed_types, StringComparer.Ordinal);

        var narrowingInfo = analyze_condition_for_narrowing(ifStmt.Condition);
        foreach (var (varName, narrowedType) in narrowingInfo.positive_narrowing)
        {
            _narrowed_types[varName] = narrowedType;
        }

        CheckBlockStmt(ifStmt.ThenBlock);

        _narrowed_types.Clear();
        foreach (var kv in savedNarrowed)
        {
            _narrowed_types[kv.Key] = kv.Value;
        }

        if (ifStmt.ElseBlock is not null)
        {
            var savedNarrowed2 = new Dictionary<string, ValkyrieType>(_narrowed_types, StringComparer.Ordinal);

            foreach (var (varName, narrowedType) in narrowingInfo.negative_narrowing)
            {
                _narrowed_types[varName] = narrowedType;
            }

            if (ifStmt.ElseBlock is BlockStmt elseBlock)
            {
                CheckBlockStmt(elseBlock);
            }
            else if (ifStmt.ElseBlock is IfStatement elseIf)
            {
                CheckIfStmt(elseIf);
            }

            _narrowed_types.Clear();
            foreach (var kv in savedNarrowed2)
            {
                _narrowed_types[kv.Key] = kv.Value;
            }
        }
    }

    /// <summary>
    /// 条件窄化分析结果
    /// </summary>
    private sealed class NarrowingInfo
    {
        /// <summary>条件为真时的窄化映射</summary>
        public Dictionary<string, ValkyrieType> positive_narrowing { get; } = new(StringComparer.Ordinal);

        /// <summary>条件为假时的窄化映射</summary>
        public Dictionary<string, ValkyrieType> negative_narrowing { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// 分析条件表达式中的类型窄化机会
    /// 识别 x is T、x != null、x == null 等模式
    /// </summary>
    private NarrowingInfo analyze_condition_for_narrowing(AstNode condition)
    {
        var info = new NarrowingInfo();

        switch (condition)
        {
            case BinaryExpr binary:
                analyze_binary_condition_for_narrowing(binary, info);
                break;

            case TermUnaryExpression { Operator: TermUnaryOperator.LogicalNot, IsPrefix: true } unary:
                var innerInfo = analyze_condition_for_narrowing(unary.Operand);
                foreach (var kv in innerInfo.positive_narrowing)
                {
                    info.negative_narrowing[kv.Key] = kv.Value;
                }

                foreach (var kv in innerInfo.negative_narrowing)
                {
                    info.positive_narrowing[kv.Key] = kv.Value;
                }
                break;

            default:
                break;
        }

        return info;
    }

    /// <summary>
    /// 分析二元条件表达式中的类型窄化
    /// </summary>
    private void analyze_binary_condition_for_narrowing(BinaryExpr binary, NarrowingInfo info)
    {
        if (binary.Operator == "&&")
        {
            var leftInfo = analyze_condition_for_narrowing(binary.Left);
            var rightInfo = analyze_condition_for_narrowing(binary.Right);

            foreach (var kv in leftInfo.positive_narrowing)
            {
                info.positive_narrowing[kv.Key] = kv.Value;
            }

            foreach (var kv in rightInfo.positive_narrowing)
            {
                info.positive_narrowing[kv.Key] = kv.Value;
            }
        }
        else if (binary.Operator == "||")
        {
            var leftInfo = analyze_condition_for_narrowing(binary.Left);
            var rightInfo = analyze_condition_for_narrowing(binary.Right);

            foreach (var kv in leftInfo.negative_narrowing)
            {
                info.negative_narrowing[kv.Key] = kv.Value;
            }

            foreach (var kv in rightInfo.negative_narrowing)
            {
                info.negative_narrowing[kv.Key] = kv.Value;
            }
        }
        else if (binary.Operator == "!=")
        {
            analyze_null_comparison_for_narrowing(binary, isNullCheck: false, info);
        }
        else if (binary.Operator == "==")
        {
            analyze_null_comparison_for_narrowing(binary, isNullCheck: true, info);
        }
    }

    /// <summary>
    /// 分析 null 比较中的类型窄化
    /// x != null → 正向窄化：去掉 nullable
    /// x == null → 反向窄化：确认 nullable
    /// </summary>
    private void analyze_null_comparison_for_narrowing(BinaryExpr binary, bool isNullCheck, NarrowingInfo info)
    {
        if (binary.Right is LiteralExpr { LiteralKind: LiteralType.Null })
        {
            var varName = get_variable_name(binary.Left);
            if (varName is not null)
            {
                var varType = resolve_variable_type(varName);
                if (varType.Kind == TypeKind.Nullable && varType.GenericArgs.Count > 0)
                {
                    var innerType = varType.GenericArgs[0];
                    if (isNullCheck)
                    {
                        info.negative_narrowing[varName] = innerType;
                    }
                    else
                    {
                        info.positive_narrowing[varName] = innerType;
                    }
                }
            }
        }
        else if (binary.Left is LiteralExpr { LiteralKind: LiteralType.Null })
        {
            var varName = get_variable_name(binary.Right);
            if (varName is not null)
            {
                var varType = resolve_variable_type(varName);
                if (varType.Kind == TypeKind.Nullable && varType.GenericArgs.Count > 0)
                {
                    var innerType = varType.GenericArgs[0];
                    if (isNullCheck)
                    {
                        info.negative_narrowing[varName] = innerType;
                    }
                    else
                    {
                        info.positive_narrowing[varName] = innerType;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取表达式中的变量名（仅支持简单标识符）
    /// </summary>
    private static string? get_variable_name(AstNode expr)
    {
        return expr switch
        {
            IdentifierNode ident => ident.Name,
            _ => null
        };
    }

    /// <summary>
    /// 解析变量的当前类型（考虑窄化）
    /// </summary>
    private ValkyrieType resolve_variable_type(string varName)
    {
        if (_narrowed_types.TryGetValue(varName, out var narrowedType))
        {
            return narrowedType;
        }

        var symbol = _currentScope.Resolve(varName);
        return symbol?.type ?? ValkyrieType.Error;
    }

    #endregion

    #region 类型测试表达式推断

    /// <summary>
    /// 推断 is 表达式的类型
    /// x is T 返回 bool，同时在条件上下文中窄化 x 的类型为 T
    /// </summary>
    private ValkyrieType infer_is_type(BinaryExpr binary)
    {
        if (binary.Operator != "is")
        {
            return ValkyrieType.Error;
        }

        var leftType = InferType(binary.Left);
        var rightType = resolve_type_from_is_operand(binary.Right);

        if (leftType.IsError || rightType.IsError)
        {
            return ValkyrieType.Bool;
        }

        if (!leftType.CanNarrowTo(rightType))
        {
            AddWarning("VALK2088",
                $"类型测试 '{leftType} is {rightType}' 永远为 false，因为 '{leftType}' 不可能为 '{rightType}'",
                binary.Span, "检查类型测试是否正确");
        }

        return ValkyrieType.Bool;
    }

    /// <summary>
    /// 推断 as 表达式的类型
    /// x as T 返回 T?（可空类型），如果转换失败返回 null
    /// </summary>
    private ValkyrieType infer_as_type(BinaryExpr binary)
    {
        if (binary.Operator != "as")
        {
            return ValkyrieType.Error;
        }

        var leftType = InferType(binary.Left);
        var rightType = resolve_type_from_is_operand(binary.Right);

        if (leftType.IsError || rightType.IsError)
        {
            return rightType;
        }

        if (!leftType.CanNarrowTo(rightType) && !rightType.IsAssignableFrom(leftType))
        {
            AddWarning("VALK2089",
                $"类型转换 '{leftType} as {rightType}' 永远返回 null",
                binary.Span, "检查类型转换是否正确，或使用显式类型转换");
        }

        return MakeNullable(rightType);
    }

    /// <summary>
    /// 从 is/as 操作的右侧操作数解析类型
    /// 支持标识符和类型注解两种形式
    /// </summary>
    private ValkyrieType resolve_type_from_is_operand(AstNode operand)
    {
        if (operand is IdentifierNode ident)
        {
            if (IsPrimitiveTypeName(ident.Name))
            {
                return ResolvePrimitiveType(ident.Name);
            }

            if (_typeRegistry.TryGetValue(ident.Name, out var registeredType))
            {
                return registeredType;
            }

            AddError("VALK2090",
                $"类型测试中引用了未定义的类型 '{ident.Name}'",
                ident.Span, "确保类型已定义");
            return ValkyrieType.Error;
        }

        return InferType(operand);
    }

    /// <summary>
    /// 检查显式类型转换的合法性
    /// 支持数值类型之间的转换、nullable 解包、引用类型向下转型
    /// </summary>
    private ValkyrieType check_explicit_cast(ValkyrieType sourceType, ValkyrieType targetType, TextSpan span)
    {
        if (sourceType.IsError || targetType.IsError)
        {
            return targetType;
        }

        if (targetType.IsAssignableFrom(sourceType))
        {
            return targetType;
        }

        if (sourceType.IsNumeric && targetType.IsNumeric)
        {
            return targetType;
        }

        if (sourceType.Kind == TypeKind.Nullable && targetType.IsAssignableFrom(sourceType.GenericArgs[0]))
        {
            return targetType;
        }

        if (sourceType.Kind == TypeKind.Union)
        {
            foreach (var member in sourceType.GenericArgs)
            {
                if (targetType.IsAssignableFrom(member) || member.IsAssignableFrom(targetType))
                {
                    return targetType;
                }
            }
        }

        AddError("VALK2091",
            $"不允许从 '{sourceType}' 到 '{targetType}' 的显式类型转换",
            span, "确保类型之间有合法的转换路径");

        return targetType;
    }

    #endregion
}
