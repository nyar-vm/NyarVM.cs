namespace Nyar.Analyzer.Semantic;

/// <summary>
///     泛型推导结果
/// </summary>
public sealed class GenericInferenceResult
{
    private readonly Dictionary<string, IType> _substitutions;

    public GenericInferenceResult()
    {
        _substitutions = new Dictionary<string, IType>();
    }

    /// <summary>
    ///     类型参数映射
    /// </summary>
    public IReadOnlyDictionary<string, IType> substitutions => _substitutions;


    /// <summary>
    ///     是否推导成功（无冲突）
    /// </summary>
    public bool is_success { get; internal set; } = true;


    /// <summary>
    ///     注册类型参数替换
    /// </summary>
    public void add_substitution(string typeParamName, IType concreteType)
    {
        if (_substitutions.TryGetValue(typeParamName, out var existing))
        {
            if (!existing.equals(concreteType)) is_success = false;
        }
        else
        {
            _substitutions[typeParamName] = concreteType;
        }
    }


    /// <summary>
    ///     应用替换到目标类型
    /// </summary>
    public IType apply(IType type)
    {
        return type switch
        {
            TypeVariable tv when _substitutions.TryGetValue(tv.name, out var resolved) => resolved,
            GenericType gt when _substitutions.TryGetValue(gt.name, out var resolved) && gt.type_arguments.Count == 0 =>
                resolved,
            GenericType gt => apply_to_generic(gt),
            FunctionType ft => apply_to_function(ft),
            ArrayType at => apply_to_array(at),
            NullableType nt => apply_to_nullable(nt),
            NamedType named => apply_to_named(named),
            _ => type
        };
    }

    private GenericType apply_to_generic(GenericType gt)
    {
        var resolvedArgs = gt.type_arguments.Select(apply).ToList();
        return new GenericType(gt.name, resolvedArgs);
    }

    private FunctionType apply_to_function(FunctionType ft)
    {
        var resolvedParams = ft.parameter_types.Select(apply).ToList();
        var resolvedReturn = apply(ft.return_type);
        return new FunctionType(resolvedParams, resolvedReturn);
    }

    private ArrayType apply_to_array(ArrayType at)
    {
        return new ArrayType(apply(at.element_type));
    }

    private NullableType apply_to_nullable(NullableType nt)
    {
        return new NullableType(apply(nt.inner_type));
    }

    private NamedType apply_to_named(NamedType named)
    {
        var resolvedArgs = named.type_arguments.Select(apply).ToList();
        return new NamedType(named.name, named.kind_tag, named.base_type is null ? null : apply(named.base_type),
            resolvedArgs, named.members);
    }
}