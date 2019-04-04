using Nyar.Analyzer.Semantic;

namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     Valkyrie 运算符调用的统一类型规则。
///     这里负责承载 `operator -> method call` 解糖后，运算符成员方法的返回类型推断。
/// </summary>
public static class ValkyrieOperatorTypeFacts
{
    /// <summary>
    ///     尝试根据接收者类型、运算符成员名和参数类型推断返回类型名称。
    /// </summary>
    public static bool try_infer_operator_result_type_name(
        string receiverTypeName,
        string operatorName,
        IReadOnlyList<string> argumentTypeNames,
        out string resultTypeName)
    {
        receiverTypeName = normalize_type_name(receiverTypeName);
        var normalizedArgumentTypes = argumentTypeNames
            .Select(normalize_type_name)
            .ToArray();

        switch (operatorName)
        {
            case "infix +":
                if (normalizedArgumentTypes.Length >= 1 &&
                    try_infer_text_binary_result_type_name(receiverTypeName, normalizedArgumentTypes[0],
                        out resultTypeName))
                {
                    return true;
                }

                if (normalizedArgumentTypes.Length >= 1 &&
                    try_infer_arithmetic_result_type_name(receiverTypeName, normalizedArgumentTypes[0],
                        out resultTypeName))
                {
                    return true;
                }

                break;
            case "infix -":
            case "infix *":
            case "infix /":
            case "infix %":
            case "infix ^":
                if (normalizedArgumentTypes.Length >= 1 &&
                    try_infer_arithmetic_result_type_name(receiverTypeName, normalizedArgumentTypes[0],
                        out resultTypeName))
                {
                    return true;
                }

                break;
            case "infix ==":
            case "infix !=":
            case "infix <":
            case "infix >":
            case "infix <=":
            case "infix >=":
            case "infix &&":
            case "infix ||":
            case "prefix !":
                resultTypeName = "bool";
                return true;
            case "bit_and":
            case "bit_or":
            case "bit_xor":
            case "bit_shift_left":
            case "bit_shift_right":
            case "prefix -":
            case "bit_not":
                resultTypeName = receiverTypeName;
                return !string.IsNullOrWhiteSpace(resultTypeName) && resultTypeName != "?";
        }

        resultTypeName = string.Empty;
        return false;
    }

    /// <summary>
    ///     尝试推断运算符调用的返回类型。
    /// </summary>
    public static bool try_infer_operator_result_type(
        IType receiverType,
        string operatorName,
        IReadOnlyList<IType> argumentTypes,
        out IType resultType)
    {
        if (try_infer_operator_result_type_name(
                receiverType.name,
                operatorName,
                [.. argumentTypes.Select(argument => argument.name)],
                out var resultTypeName))
        {
            resultType = create_type(resultTypeName);
            return true;
        }

        resultType = UnknownType.instance;
        return false;
    }

    /// <summary>
    ///     尝试推断算术结果类型。
    /// </summary>
    public static bool try_infer_arithmetic_result_type_name(
        string leftTypeName,
        string rightTypeName,
        out string resultTypeName)
    {
        leftTypeName = normalize_type_name(leftTypeName);
        rightTypeName = normalize_type_name(rightTypeName);

        if (leftTypeName == "f64" || rightTypeName == "f64")
        {
            resultTypeName = "f64";
            return true;
        }

        if (leftTypeName == "f32" || rightTypeName == "f32")
        {
            resultTypeName = "f32";
            return true;
        }

        if (leftTypeName == "isize" || rightTypeName == "isize")
        {
            resultTypeName = "isize";
            return true;
        }

        if (leftTypeName == "usize" || rightTypeName == "usize")
        {
            resultTypeName = "usize";
            return true;
        }

        if (leftTypeName == "i64" || rightTypeName == "i64")
        {
            resultTypeName = "i64";
            return true;
        }

        if (leftTypeName == "i32" || rightTypeName == "i32")
        {
            resultTypeName = "i32";
            return true;
        }

        resultTypeName = string.Empty;
        return false;
    }

    /// <summary>
    ///     尝试推断文本二元运算结果类型。
    /// </summary>
    public static bool try_infer_text_binary_result_type_name(
        string leftTypeName,
        string rightTypeName,
        out string resultTypeName)
    {
        leftTypeName = normalize_type_name(leftTypeName);
        rightTypeName = normalize_type_name(rightTypeName);

        if (!ValkyrieTextTypeFacts.is_text_like_name(leftTypeName) ||
            !ValkyrieTextTypeFacts.is_text_like_name(rightTypeName))
        {
            resultTypeName = string.Empty;
            return false;
        }

        if (string.Equals(leftTypeName, rightTypeName, StringComparison.Ordinal))
        {
            resultTypeName = leftTypeName;
            return true;
        }

        if (ValkyrieTextTypeFacts.is_literal_text_name(leftTypeName) ||
            ValkyrieTextTypeFacts.is_literal_text_name(rightTypeName))
        {
            resultTypeName = ValkyrieTextTypeFacts.literal_text_name;
            return true;
        }

        resultTypeName = string.Empty;
        return false;
    }

    /// <summary>
    ///     尝试推断文本二元运算结果类型。
    ///     兼容仍以 `IType` 形式调用的旧代码路径。
    /// </summary>
    public static IType? try_infer_text_binary_result_type(IType leftType, IType rightType)
    {
        if (!try_infer_text_binary_result_type_name(leftType.name, rightType.name, out var resultTypeName))
        {
            return null;
        }

        return create_type(resultTypeName);
    }

    /// <summary>
    ///     根据类型名称构造语义类型对象。
    /// </summary>
    private static IType create_type(string typeName)
    {
        return ValkyrieBuiltinTypeFacts.try_create_annotation_type(typeName) ?? new NamedType(typeName, typeName);
    }

    /// <summary>
    ///     统一归一化运算符推断中使用的类型名称。
    /// </summary>
    private static string normalize_type_name(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return string.Empty;
        }

        return ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
    }
}
