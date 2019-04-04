namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     Valkyrie 类型系统的核心类型表示
///     支持原始类型、泛型、函数、联合/交集、Trait、类型变量等
///     提供类型兼容性判断、类型替换、约束验证和类型窄化
/// </summary>
public sealed class ValkyrieType
{
    public ValkyrieType(TypeKind kind, string name,
        IReadOnlyList<ValkyrieType>? genericArgs = null,
        IReadOnlyList<ParameterType>? parameters = null,
        ValkyrieType? returnType = null,
        bool isMutable = false,
        IReadOnlyList<string>? constraintTypeNames = null,
        IReadOnlyList<string>? genericTypeParams = null,
        IReadOnlyList<string>? genericConstraintNames = null,
        IReadOnlyList<EffectKind>? effects = null,
        IReadOnlyList<string>? implementedTraits = null,
        ValkyrieType? narrowedFrom = null)
    {
        this.kind = kind;
        this.name = name;
        generic_args = genericArgs ?? [];
        this.parameters = parameters;
        return_type = returnType;
        is_mutable = isMutable;
        constraint_type_names = constraintTypeNames ?? [];
        generic_type_params = genericTypeParams ?? [];
        generic_constraint_names = genericConstraintNames ?? [];
        this.effects = effects ?? [];
        implemented_traits = implementedTraits ?? [];
        narrowed_from = narrowedFrom;
    }

    public TypeKind kind { get; }
    public string name { get; }
    public IReadOnlyList<ValkyrieType> generic_args { get; }
    public IReadOnlyList<ParameterType>? parameters { get; }
    public ValkyrieType? return_type { get; }
    public bool is_mutable { get; }
    public IReadOnlyList<string> constraint_type_names { get; }
    public IReadOnlyList<string> generic_type_params { get; }
    public IReadOnlyList<string> generic_constraint_names { get; }
    public IReadOnlyList<EffectKind> effects { get; }

    /// <summary>
    ///     该类型实现的 trait 名称列表（用于约束求解）
    /// </summary>
    public IReadOnlyList<string> implemented_traits { get; }

    /// <summary>
    ///     类型窄化信息：在类型测试后窄化到的目标类型
    /// </summary>
    public ValkyrieType? narrowed_from { get; }

    #region 类型替换

    /// <summary>
    ///     使用类型替换字典替换类型变量
    /// </summary>
    /// <returns>替换后的新类型，若无变化则返回自身</returns>
    public ValkyrieType substitute(IReadOnlyDictionary<string, ValkyrieType> substitutions)
    {
        if (is_type_variable && substitutions.TryGetValue(name, out var concrete)) return concrete;

        if (generic_args.Count > 0)
        {
            var substituted = generic_args.Select(a => a.substitute(substitutions)).ToList();
            if (substituted.SequenceEqual(generic_args)) return this;

            return new ValkyrieType(kind, name, substituted, parameters, return_type, is_mutable,
                constraint_type_names, generic_type_params, generic_constraint_names, effects,
                implemented_traits, narrowed_from);
        }

        if (parameters is not null)
        {
            var substParams = parameters.Select(p =>
                new ParameterType(p.name, p.type.substitute(substitutions), p.is_mutable)).ToList();
            var substReturn = return_type?.substitute(substitutions);
            return new ValkyrieType(kind, name, null, substParams, substReturn, is_mutable,
                constraint_type_names, generic_type_params, generic_constraint_names, effects,
                implemented_traits, narrowed_from);
        }

        return this;
    }

    #endregion

    #region 类型属性

    public bool is_numeric => kind == TypeKind.primitive && name is "i8" or "i16" or "i32" or "i64"
        or "u8" or "u16" or "u32" or "u64"
        or "f32" or "f64";

    public bool is_integer => kind == TypeKind.primitive && name is "i8" or "i16" or "i32" or "i64"
        or "u8" or "u16" or "u32" or "u64";

    public bool is_float => kind == TypeKind.primitive && name is "f32" or "f64";

    public bool is_bool => kind == TypeKind.primitive && name is "bool";

    public bool is_char => kind == TypeKind.primitive && name == ValkyrieTextTypeFacts.char_name;

    public bool is_owned_text => ValkyrieTextTypeFacts.is_owned_text_name(name);

    public bool is_text_like => is_owned_text;

    public bool is_unit => name is "unit" or "none";

    public bool is_error => kind == TypeKind.error;

    public bool is_auto => kind == TypeKind.unknown && ValkyrieBuiltinTypeFacts.is_auto_type_name(name);

    public bool is_trait => kind == TypeKind.trait;

    public bool is_tuple => kind == TypeKind.tuple;

    /// <summary>
    ///     是否为引用类型（class、文本、array、map、nullable、trait）
    /// </summary>
    public bool is_reference_type => kind is TypeKind.@class or TypeKind.array or TypeKind.map
                                         or TypeKind.nullable or TypeKind.trait
                                     || is_text_like;

    /// <summary>
    ///     是否为值类型（primitive（非文本）、struct、enum、tuple、fixed_array）
    /// </summary>
    public bool is_value_type => kind is TypeKind.@struct or TypeKind.@enum or TypeKind.tuple or TypeKind.fixed_array
                                 || (kind == TypeKind.primitive && !is_text_like);

    /// <summary>
    ///     是否支持默认构造（有 new() 能力）
    /// </summary>
    public bool has_default_constructor => kind is TypeKind.primitive or TypeKind.@struct or TypeKind.@enum or TypeKind.fixed_array
                                               or TypeKind.@class or TypeKind.tuple
                                           && !is_text_like;

    /// <summary>
    ///     是否满足数值约束（所有数值类型）
    /// </summary>
    public bool satisfies_numeric_constraint => is_numeric;

    /// <summary>
    ///     是否满足整数约束
    /// </summary>
    public bool satisfies_integer_constraint => is_integer;

    /// <summary>
    ///     是否满足浮点约束
    /// </summary>
    public bool satisfies_float_constraint => is_float;

    #endregion

    #region 类型兼容性

    /// <summary>
    ///     判断当前类型是否可以从 other 类型赋值
    ///     支持 Error 兼容、Nullable 兼容、名称+泛型参数匹配、隐式数值转换、Trait 实现
    /// </summary>
    public bool is_assignable_from(ValkyrieType other)
    {
        if (is_error || other.is_error) return true;

        if (is_auto) return true;

        if (kind == TypeKind.nullable && other.name == "null") return true;

        if (kind == TypeKind.nullable && generic_args.Count > 0) return generic_args[0].is_assignable_from(other);

        if (is_trait && other.implements_trait(name)) return true;

        if (name == other.name && generic_args.Count == other.generic_args.Count)
        {
            for (var i = 0; i < generic_args.Count; i++)
                if (!generic_args[i].is_assignable_from(other.generic_args[i]))
                    return false;

            return true;
        }

        if (is_numeric && other.is_numeric) return is_implicit_numeric_conversion(other, this);

        if (kind == TypeKind.union && generic_args.Count > 0)
            foreach (var member in generic_args)
                if (member.is_assignable_from(other))
                    return true;

        return false;
    }

    /// <summary>
    ///     判断当前类型是否实现了指定名称的 trait
    /// </summary>
    public bool implements_trait(string traitName)
    {
        foreach (var impl in implemented_traits)
            if (string.Equals(impl, traitName, StringComparison.Ordinal))
                return true;

        if (is_numeric && traitName is "Numeric" or "Addable" or "Comparable") return true;

        if (is_integer && traitName is "Integer" or "Numeric" or "Bitwise") return true;

        if (is_float && traitName is "Float" or "Numeric") return true;

        if (is_bool && traitName == "Comparable") return true;

        if (is_text_like && traitName is "Comparable" or "Addable" or "Iterable") return true;

        return false;
    }

    /// <summary>
    ///     验证类型是否满足指定约束
    /// </summary>
    public bool satisfies_constraint(GenericConstraintDescriptor constraint)
    {
        if (is_error || is_auto) return true;

        return constraint.kind switch
        {
            ConstraintKind.@class => is_reference_type,
            ConstraintKind.@struct => is_value_type && !is_numeric,
            ConstraintKind.@new => has_default_constructor,
            ConstraintKind.@enum => kind == TypeKind.@enum,
            ConstraintKind.numeric => satisfies_numeric_constraint,
            ConstraintKind.integer => satisfies_integer_constraint,
            ConstraintKind.@float => satisfies_float_constraint,
            ConstraintKind.nullable => kind == TypeKind.nullable,
            ConstraintKind.trait => implements_trait(constraint.target_type_name ?? ""),
            _ => true
        };
    }

    #endregion

    #region 类型窄化

    /// <summary>
    ///     创建窄化类型：在类型测试成功后，将类型窄化为更具体的类型
    ///     例如：if (x is i32) 之后 x 的类型从 auto 窄化为 i32
    /// </summary>
    public ValkyrieType narrow_to(ValkyrieType targetType)
    {
        if (kind == TypeKind.nullable && generic_args.Count > 0 && targetType.is_assignable_from(generic_args[0]))
            return new ValkyrieType(generic_args[0].kind, generic_args[0].name,
                generic_args[0].generic_args, generic_args[0].parameters, generic_args[0].return_type,
                generic_args[0].is_mutable, generic_args[0].constraint_type_names,
                generic_args[0].generic_type_params, generic_args[0].generic_constraint_names,
                generic_args[0].effects, generic_args[0].implemented_traits,
                this);

        if (kind == TypeKind.union && generic_args.Count > 0)
            foreach (var member in generic_args)
                if (targetType.is_assignable_from(member) || member.is_assignable_from(targetType))
                    return new ValkyrieType(targetType.kind, targetType.name,
                        targetType.generic_args, targetType.parameters, targetType.return_type,
                        targetType.is_mutable, targetType.constraint_type_names,
                        targetType.generic_type_params, targetType.generic_constraint_names,
                        targetType.effects, targetType.implemented_traits,
                        this);

        if (is_auto || kind == TypeKind.unknown)
            return new ValkyrieType(targetType.kind, targetType.name,
                targetType.generic_args, targetType.parameters, targetType.return_type,
                targetType.is_mutable, targetType.constraint_type_names,
                targetType.generic_type_params, targetType.generic_constraint_names,
                targetType.effects, targetType.implemented_traits,
                this);

        return this;
    }

    /// <summary>
    ///     判断类型测试是否可能成功（窄化是否有意义）
    /// </summary>
    public bool can_narrow_to(ValkyrieType targetType)
    {
        if (is_error || targetType.is_error) return false;

        if (kind == TypeKind.nullable && generic_args.Count > 0)
            return targetType.is_assignable_from(generic_args[0]) || generic_args[0].is_assignable_from(targetType);

        if (kind == TypeKind.union && generic_args.Count > 0)
        {
            foreach (var member in generic_args)
                if (targetType.is_assignable_from(member) || member.is_assignable_from(targetType))
                    return true;

            return false;
        }

        if (is_auto || kind == TypeKind.unknown) return true;

        return targetType.is_assignable_from(this) && !targetType.Equals(this);
    }

    #endregion

    #region 格式化与相等

    public override string ToString()
    {
        if (generic_args.Count > 0) return $"{name}<{string.Join(", ", generic_args)}>";

        return name;
    }

    public override bool Equals(object? obj)
    {
        return obj is ValkyrieType other && name == other.name
                                         && generic_args.SequenceEqual(other.generic_args);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(name, generic_args.Count);
    }

    #endregion

    #region 数值转换

    private static bool is_implicit_numeric_conversion(ValkyrieType from, ValkyrieType to)
    {
        var fromRank = ValkyrieBuiltinTypeFacts.get_numeric_rank(from.name);
        var toRank = ValkyrieBuiltinTypeFacts.get_numeric_rank(to.name);

        if (fromRank < 0 || toRank < 0) return false;

        if (ValkyrieBuiltinTypeFacts.is_unsigned_numeric_name(from.name) &&
            ValkyrieBuiltinTypeFacts.is_signed_numeric_target_name(to.name)) return false;

        return toRank > fromRank;
    }

    #endregion

    #region 预定义类型

    public static ValkyrieType i8 => new(TypeKind.primitive, "i8");
    public static ValkyrieType i16 => new(TypeKind.primitive, "i16");
    public static ValkyrieType i32 => new(TypeKind.primitive, "i32");
    public static ValkyrieType i64 => new(TypeKind.primitive, "i64");
    public static ValkyrieType u8 => new(TypeKind.primitive, "u8");
    public static ValkyrieType u16 => new(TypeKind.primitive, "u16");
    public static ValkyrieType u32 => new(TypeKind.primitive, "u32");
    public static ValkyrieType u64 => new(TypeKind.primitive, "u64");
    public static ValkyrieType f32 => new(TypeKind.primitive, "f32");
    public static ValkyrieType f64 => new(TypeKind.primitive, "f64");
    public static ValkyrieType @bool => new(TypeKind.primitive, "bool");
    public static ValkyrieType @char => new(TypeKind.primitive, ValkyrieTextTypeFacts.char_name);
    public static ValkyrieType utf8 => new(TypeKind.@class, ValkyrieTextTypeFacts.utf8_name);
    public static ValkyrieType utf16 => new(TypeKind.@class, ValkyrieTextTypeFacts.utf16_name);
    public static ValkyrieType utf32 => new(TypeKind.@class, ValkyrieTextTypeFacts.utf32_name);
    public static ValkyrieType c_str => new(TypeKind.@class, ValkyrieTextTypeFacts.c_str_name);
    public static ValkyrieType unit => new(TypeKind.primitive, "unit");
    public static ValkyrieType @null => new(TypeKind.primitive, "null");
    public static ValkyrieType auto => new(TypeKind.unknown, ValkyrieBuiltinTypeFacts.auto_name);
    public static ValkyrieType error => new(TypeKind.error, "<error>");

    /// <summary>
    ///     不可达符号（底类型），表示永远不会正常返回的表达式的类型
    /// </summary>
    /// <param name="name">类型名称，默认为 <c>!</c></param>
    /// <returns>底类型实例</returns>
    public static ValkyrieType unimplemented_symbol(string name = "!")
    {
        return new ValkyrieType(TypeKind.primitive, name);
    }

    /// <summary>
    ///     创建带 trait 实现列表的类型
    /// </summary>
    public static ValkyrieType with_traits(TypeKind kind, string name,
        IReadOnlyList<string> implementedTraits,
        IReadOnlyList<ValkyrieType>? genericArgs = null)
    {
        return new ValkyrieType(kind, name, genericArgs, implementedTraits: implementedTraits);
    }

    /// <summary>
    ///     创建 Trait 类型
    /// </summary>
    public static ValkyrieType trait_type(string name,
        IReadOnlyList<ValkyrieType>? genericArgs = null,
        IReadOnlyList<ParameterType>? parameters = null,
        ValkyrieType? returnType = null)
    {
        return new ValkyrieType(TypeKind.trait, name, genericArgs, parameters, returnType);
    }

    /// <summary>
    ///     创建元组类型
    /// </summary>
    public static ValkyrieType tuple(IReadOnlyList<ValkyrieType> elements)
    {
        var name = $"({string.Join(", ", elements)})";
        return new ValkyrieType(TypeKind.tuple, name, elements);
    }

    /// <summary>
    ///     创建数组类型 <c>Array&lt;T&gt;</c>（编译器原语）
    /// </summary>
    public static ValkyrieType array(ValkyrieType element)
    {
        return new ValkyrieType(TypeKind.array, "Array", [element]);
    }

    /// <summary>
    ///     创建定长数组类型 <c>FixedArray&lt;T, N&gt;</c>（编译器原语）
    /// </summary>
    public static ValkyrieType fixed_array(ValkyrieType element, int size)
    {
        return new ValkyrieType(TypeKind.fixed_array, "FixedArray", [element, ValkyrieType.@int(size)]);
    }

    /// <summary>
    ///     创建编译期整数常量类型（用于定长数组大小参数）
    /// </summary>
    private static ValkyrieType @int(int value)
    {
        return new ValkyrieType(TypeKind.primitive, "i32");
    }

    public static ValkyrieType map(ValkyrieType key, ValkyrieType value)
    {
        return new ValkyrieType(TypeKind.map, "map", [key, value]);
    }

    public static ValkyrieType nullable(ValkyrieType inner)
    {
        return new ValkyrieType(TypeKind.nullable, $"{inner.name}?", [inner]);
    }

    public static ValkyrieType type_variable(string name,
        IReadOnlyList<string>? constraintTypeNames = null)
    {
        return new ValkyrieType(TypeKind.type_variable, name, constraintTypeNames: constraintTypeNames);
    }

    public static ValkyrieType vec2 => new(TypeKind.primitive, "vec2");
    public static ValkyrieType vec3 => new(TypeKind.primitive, "vec3");
    public static ValkyrieType vec4 => new(TypeKind.primitive, "vec4");
    public static ValkyrieType i_vec2 => new(TypeKind.primitive, "ivec2");
    public static ValkyrieType i_vec3 => new(TypeKind.primitive, "ivec3");
    public static ValkyrieType i_vec4 => new(TypeKind.primitive, "ivec4");
    public static ValkyrieType u_vec2 => new(TypeKind.primitive, "uvec2");
    public static ValkyrieType u_vec3 => new(TypeKind.primitive, "uvec3");
    public static ValkyrieType u_vec4 => new(TypeKind.primitive, "uvec4");
    public static ValkyrieType mat3 => new(TypeKind.primitive, "mat3");
    public static ValkyrieType mat4 => new(TypeKind.primitive, "mat4");

    public bool is_vector => name is "vec2" or "vec3" or "vec4"
        or "ivec2" or "ivec3" or "ivec4"
        or "uvec2" or "uvec3" or "uvec4";

    public bool is_matrix => name is "mat3" or "mat4";

    public bool is_shader_type => is_vector || is_matrix || is_numeric;

    public bool is_type_variable => kind == TypeKind.type_variable;

    /// <summary>
    ///     是否已确定具体类型（非类型变量）
    /// </summary>
    public bool is_concrete => !is_type_variable && kind != TypeKind.unknown && kind != TypeKind.error;

    public bool is_pure => effects.Contains(EffectKind.pure);

    public bool @is => effects.Contains(EffectKind.async);

    public bool is_io => effects.Contains(EffectKind.io);

    #endregion
}
