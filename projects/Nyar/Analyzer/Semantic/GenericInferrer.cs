namespace Nyar.Analyzer.Semantic;

/// <summary>
///     泛型推导引擎
///     从函数调用的实际参数类型推导泛型类型参数
/// </summary>
public static class GenericInferrer
{
    /// <summary>
    ///     从参数类型推导泛型类型参数
    /// </summary>
    /// <param name="genericParams">泛型类型参数名列表。</param>
    /// <param name="paramTypes">函数形参类型列表（可包含 TypeVariable）。</param>
    /// <param name="argTypes">调用实参类型列表。</param>
    /// <returns>推导结果。</returns>
    public static GenericInferenceResult infer(
        IReadOnlyList<string> genericParams,
        IReadOnlyList<IType> paramTypes,
        IReadOnlyList<IType> argTypes)
    {
        var result = new GenericInferenceResult();
        var typeVars = create_type_variables(genericParams);

        if (paramTypes.Count != argTypes.Count)
        {
            result.is_success = false;
            return result;
        }

        for (var i = 0; i < paramTypes.Count; i++) unify(paramTypes[i], argTypes[i], typeVars, result);

        foreach (var tv in typeVars.Values)
            if (tv.is_resolved)
                result.add_substitution(tv.name, tv.inferred_type!);

        return result;
    }


    /// <summary>
    ///     从范型函数签名和调用参数类型推导类型参数
    /// </summary>
    /// <param name="genericParams">泛型类型参数名列表。</param>
    /// <param name="functionType">函数类型（参数类型可能包含 TypeVariable 占位）。</param>
    /// <param name="argTypes">调用实参类型列表。</param>
    /// <returns>推导结果。</returns>
    public static GenericInferenceResult infer_from_function_type(
        IReadOnlyList<string> genericParams,
        FunctionType functionType,
        IReadOnlyList<IType> argTypes)
    {
        return infer(genericParams, functionType.parameter_types, argTypes);
    }

    private static Dictionary<string, TypeVariable> create_type_variables(IReadOnlyList<string> names)
    {
        var vars = new Dictionary<string, TypeVariable>();
        foreach (var name in names) vars[name] = new TypeVariable(name);

        return vars;
    }

    private static void unify(IType paramType, IType argType,
        Dictionary<string, TypeVariable> typeVars, GenericInferenceResult result)
    {
        if (paramType is TypeVariable tv)
        {
            if (tv.is_resolved)
            {
                if (!tv.inferred_type!.equals(argType)) result.is_success = false;
            }
            else
            {
                tv.bind(argType);
            }

            return;
        }

        if (paramType is GenericType { type_arguments.Count: 0 } paramGeneric
            && is_type_variable_name(paramGeneric.name))
        {
            if (typeVars.TryGetValue(paramGeneric.name, out var existing))
            {
                if (existing.is_resolved)
                {
                    if (!existing.inferred_type!.equals(argType)) result.is_success = false;
                }
                else
                {
                    existing.bind(argType);
                }
            }
            else
            {
                var newTv = new TypeVariable(paramGeneric.name);
                newTv.bind(argType);
                typeVars[paramGeneric.name] = newTv;
            }

            return;
        }

        if (paramType is GenericType paramGen && argType is GenericType argGeneric
                                              && paramGen.name == argGeneric.name
                                              && paramGen.type_arguments.Count == argGeneric.type_arguments.Count)
        {
            for (var i = 0; i < paramGen.type_arguments.Count; i++)
                unify(paramGen.type_arguments[i], argGeneric.type_arguments[i], typeVars, result);

            return;
        }

        if (paramType is ArrayType paramArray && argType is ArrayType argArray)
        {
            unify(paramArray.element_type, argArray.element_type, typeVars, result);
            return;
        }

        if (paramType is NullableType paramNullable && argType is NullableType argNullable)
            unify(paramNullable.inner_type, argNullable.inner_type, typeVars, result);
    }


    /// <summary>
    ///     判断类型名是否为类型变量名（以大写字母开头，仅含字母数字和下划线）
    /// </summary>
    internal static bool is_type_variable_name(string name)
    {
        if (name.Length == 0) return false;

        if (!char.IsUpper(name[0])) return false;

        for (var i = 1; i < name.Length; i++)
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                return false;

        return true;
    }
}