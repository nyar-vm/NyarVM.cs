namespace Nyar.Assembler;

/// <summary>
///     元编译阶段的结构化类型引用。
///     统一保存原始类型名、规范化类型名以及已知的类型类别，避免后端重复做字符串退化判断。
/// </summary>
public sealed record GenerateTypeReference
{
    private GenerateTypeReference(string displayName, string normalizedName, GenerateTypeKind kind)
    {
        display_name = displayName;
        normalized_name = normalizedName;
        this.kind = kind;
    }

    /// <summary>
    ///     内建 `void` 类型引用。
    /// </summary>
    public static GenerateTypeReference @void { get; } = new("void", "void", GenerateTypeKind.@void);

    /// <summary>
    ///     内建 `unit` 类型引用。
    /// </summary>
    public static GenerateTypeReference unit { get; } = new("unit", "unit", GenerateTypeKind.unit);

    /// <summary>
    ///     内建 `bool` 类型引用。
    /// </summary>
    public static GenerateTypeReference @bool { get; } = new("bool", "bool", GenerateTypeKind.@bool);

    /// <summary>
    ///     内建 `char` 类型引用。
    /// </summary>
    public static GenerateTypeReference @char { get; } = new("char", "char", GenerateTypeKind.@char);

    /// <summary>
    ///     内建 `i32` 类型引用。
    /// </summary>
    public static GenerateTypeReference i32 { get; } = new("i32", "i32", GenerateTypeKind.i32);

    private static GenerateTypeReference _i8 { get; } = new("i8", "i8", GenerateTypeKind.i8);
    private static GenerateTypeReference _i16 { get; } = new("i16", "i16", GenerateTypeKind.i16);
    private static GenerateTypeReference _i64 { get; } = new("i64", "i64", GenerateTypeKind.i64);
    private static GenerateTypeReference _i128 { get; } = new("i128", "i128", GenerateTypeKind.i128);
    private static GenerateTypeReference _u8 { get; } = new("u8", "u8", GenerateTypeKind.u8);
    private static GenerateTypeReference _u16 { get; } = new("u16", "u16", GenerateTypeKind.u16);
    private static GenerateTypeReference _u32 { get; } = new("u32", "u32", GenerateTypeKind.u32);
    private static GenerateTypeReference _u64 { get; } = new("u64", "u64", GenerateTypeKind.u64);
    private static GenerateTypeReference _f32 { get; } = new("f32", "f32", GenerateTypeKind.f32);
    private static GenerateTypeReference _f64 { get; } = new("f64", "f64", GenerateTypeKind.f64);
    private static GenerateTypeReference _utf8 { get; } = new("utf8", "utf8", GenerateTypeKind.utf8);
    private static GenerateTypeReference _uuid { get; } = new("uuid", "uuid", GenerateTypeKind.uuid);
    private static GenerateTypeReference _any { get; } = new("any", "any", GenerateTypeKind.any);
    private static GenerateTypeReference _null { get; } = new("null", "null", GenerateTypeKind.@null);
    private static GenerateTypeReference _function_ref { get; } = new("function_ref", "function_ref", GenerateTypeKind.function_ref);
    private static GenerateTypeReference _external_ref { get; } = new("external_ref", "external_ref", GenerateTypeKind.external_ref);
    private static GenerateTypeReference _v128 { get; } = new("v128", "v128", GenerateTypeKind.v128);
    private static GenerateTypeReference _unsafe_ref { get; } = new("unsafe", "unsafe", GenerateTypeKind.unsafe_ref);
    private static GenerateTypeReference _self_ref { get; } = new("self", "self", GenerateTypeKind.self_ref);
    private static GenerateTypeReference _emptylist { get; } = new("emptylist", "emptylist", GenerateTypeKind.emptylist);

    /// <summary>
    ///     内建 `utf16` 类型引用。
    /// </summary>
    public static GenerateTypeReference utf16 { get; } = new("utf16", "utf16", GenerateTypeKind.utf16);

    /// <summary>
    ///     内建 `object` 类型引用。
    /// </summary>
    public static GenerateTypeReference @object { get; } = new("object", "object", GenerateTypeKind.@object);

    /// <summary>
    ///     原始类型名称。
    /// </summary>
    public string display_name { get; }

    /// <summary>
    ///     规范化后的类型名称。
    /// </summary>
    public string normalized_name { get; }

    /// <summary>
    ///     类型类别。
    /// </summary>
    public GenerateTypeKind kind { get; }

    /// <summary>
    ///     是否为空类型名。
    /// </summary>
    public bool is_empty => string.IsNullOrEmpty(display_name);

    /// <summary>
    ///     是否为无返回值类型。
    /// </summary>
    public bool is_void_like => kind is GenerateTypeKind.@void or GenerateTypeKind.unit;

    /// <summary>
    ///     是否为文本类型。
    /// </summary>
    public bool is_text_like => kind is GenerateTypeKind.utf8 or GenerateTypeKind.utf16;

    /// <summary>
    ///     是否为对象擦除类型。
    /// </summary>
    public bool is_erased_object_type => kind is GenerateTypeKind.@object or GenerateTypeKind.any or GenerateTypeKind.external_ref;

    /// <summary>
    ///     是否为内建类型别名。
    /// </summary>
    public bool is_builtin_alias => kind is not (GenerateTypeKind.unknown_named or GenerateTypeKind.bracket_array
        or GenerateTypeKind.array or GenerateTypeKind.list or GenerateTypeKind.map
        or GenerateTypeKind.set or GenerateTypeKind.user_defined_named);

    /// <summary>
    ///     是否为数组或集合族类型。
    /// </summary>
    public bool is_array_family => kind is GenerateTypeKind.bracket_array or GenerateTypeKind.array
        or GenerateTypeKind.list or GenerateTypeKind.map or GenerateTypeKind.set;

    /// <summary>
    ///     是否为用户定义的引用类型。
    /// </summary>
    public bool is_user_defined_reference => kind == GenerateTypeKind.user_defined_named;

    /// <summary>
    ///     是否默认以引用零值表示。
    /// </summary>
    public bool defaults_to_null_reference => is_text_like || is_erased_object_type
                                                           || is_array_family || is_user_defined_reference;

    /// <summary>
    ///     是否会映射为 CLR 的 `object` 元素类型。
    /// </summary>
    public bool is_clr_object_element_type => kind is not (GenerateTypeKind.@void or GenerateTypeKind.unit
        or GenerateTypeKind.@bool or GenerateTypeKind.@char or GenerateTypeKind.i8 or GenerateTypeKind.i16
        or GenerateTypeKind.i32 or GenerateTypeKind.i64 or GenerateTypeKind.f32
        or GenerateTypeKind.f64 or GenerateTypeKind.utf8 or GenerateTypeKind.utf16);

    /// <summary>
    ///     尝试映射为统一值类型枚举。
    /// </summary>
    public GenerateValueType? value_type => kind switch
    {
        GenerateTypeKind.@void => GenerateValueType.@void,
        GenerateTypeKind.unit => GenerateValueType.unit,
        GenerateTypeKind.@bool => GenerateValueType.@bool,
        GenerateTypeKind.@char => GenerateValueType.@char,
        GenerateTypeKind.i8 => GenerateValueType.i8,
        GenerateTypeKind.i16 => GenerateValueType.i16,
        GenerateTypeKind.i32 => GenerateValueType.i32,
        GenerateTypeKind.i64 => GenerateValueType.i64,
        GenerateTypeKind.i128 => GenerateValueType.i128,
        GenerateTypeKind.f32 => GenerateValueType.f32,
        GenerateTypeKind.f64 => GenerateValueType.f64,
        GenerateTypeKind.utf8 => GenerateValueType.utf8,
        GenerateTypeKind.utf16 => GenerateValueType.utf16,
        GenerateTypeKind.@object => GenerateValueType.@object,
        GenerateTypeKind.any => GenerateValueType.any,
        GenerateTypeKind.@null => GenerateValueType.@null,
        GenerateTypeKind.function_ref => GenerateValueType.function_ref,
        GenerateTypeKind.external_ref => GenerateValueType.external_ref,
        GenerateTypeKind.v128 => GenerateValueType.v128,
        _ => null
    };

    /// <summary>
    ///     解析类型引用。
    /// </summary>
    /// <param name="typeName">类型名称。</param>
    /// <returns>结构化类型引用。</returns>
    public static GenerateTypeReference parse(string? typeName)
    {
        var displayName = typeName ?? string.Empty;
        if (displayName.Length == 0) return new GenerateTypeReference(string.Empty, string.Empty, GenerateTypeKind.unknown_named);

        switch (displayName)
        {
            case "void": return @void;
            case "unit": return unit;
            case "bool": return @bool;
            case "char": return @char;
            case "i8": return _i8;
            case "i16": return _i16;
            case "i32": return i32;
            case "i64": return _i64;
            case "i128": return _i128;
            case "u8": return _u8;
            case "u16": return _u16;
            case "u32": return _u32;
            case "u64": return _u64;
            case "f32": return _f32;
            case "f64": return _f64;
            case "utf8": return _utf8;
            case "utf16": return utf16;
            case "uuid": return _uuid;
            case "object": return @object;
            case "any": return _any;
            case "null": return _null;
            case "function_ref": return _function_ref;
            case "external_ref": return _external_ref;
            case "v128": return _v128;
            case "unsafe": return _unsafe_ref;
            case "self": return _self_ref;
            case "emptylist": return _emptylist;
        }

        var normalizedName = displayName.ToLowerInvariant();
        var kind = normalizedName switch
        {
            "void" => GenerateTypeKind.@void,
            "unit" => GenerateTypeKind.unit,
            "bool" => GenerateTypeKind.@bool,
            "char" => GenerateTypeKind.@char,
            "i8" => GenerateTypeKind.i8,
            "i16" => GenerateTypeKind.i16,
            "i32" => GenerateTypeKind.i32,
            "i64" => GenerateTypeKind.i64,
            "i128" => GenerateTypeKind.i128,
            "u8" => GenerateTypeKind.u8,
            "u16" => GenerateTypeKind.u16,
            "u32" => GenerateTypeKind.u32,
            "u64" => GenerateTypeKind.u64,
            "f32" => GenerateTypeKind.f32,
            "f64" => GenerateTypeKind.f64,
            "utf8" => GenerateTypeKind.utf8,
            "utf16" => GenerateTypeKind.utf16,
            "uuid" => GenerateTypeKind.uuid,
            "object" => GenerateTypeKind.@object,
            "any" => GenerateTypeKind.any,
            "null" => GenerateTypeKind.@null,
            "function_ref" => GenerateTypeKind.function_ref,
            "external_ref" => GenerateTypeKind.external_ref,
            "v128" => GenerateTypeKind.v128,
            "unsafe" => GenerateTypeKind.unsafe_ref,
            "self" => GenerateTypeKind.self_ref,
            "emptylist" => GenerateTypeKind.emptylist,
            _ when displayName.StartsWith('[') => GenerateTypeKind.bracket_array,
            _ when normalizedName.StartsWith("array", StringComparison.Ordinal) => GenerateTypeKind.array,
            _ when normalizedName.StartsWith("list", StringComparison.Ordinal) => GenerateTypeKind.list,
            _ when normalizedName.StartsWith("map", StringComparison.Ordinal) => GenerateTypeKind.map,
            _ when normalizedName.StartsWith("set", StringComparison.Ordinal) => GenerateTypeKind.set,
            _ when displayName.StartsWith('_') => GenerateTypeKind.user_defined_named,
            _ => GenerateTypeKind.unknown_named
        };

        return new GenerateTypeReference(displayName, normalizedName, kind);
    }

    /// <summary>
    ///     从统一值类型枚举构造类型引用。
    /// </summary>
    /// <param name="valueType">值类型枚举。</param>
    /// <returns>结构化类型引用。</returns>
    public static GenerateTypeReference from_value_type(GenerateValueType valueType)
    {
        return valueType switch
        {
            GenerateValueType.@void => @void,
            GenerateValueType.unit => unit,
            GenerateValueType.@bool => @bool,
            GenerateValueType.@char => @char,
            GenerateValueType.i8 => _i8,
            GenerateValueType.i16 => _i16,
            GenerateValueType.i32 => i32,
            GenerateValueType.i64 => _i64,
            GenerateValueType.i128 => _i128,
            GenerateValueType.f32 => _f32,
            GenerateValueType.f64 => _f64,
            GenerateValueType.utf8 => _utf8,
            GenerateValueType.utf16 => utf16,
            GenerateValueType.@object => @object,
            GenerateValueType.any => _any,
            GenerateValueType.@null => _null,
            GenerateValueType.function_ref => _function_ref,
            GenerateValueType.external_ref => _external_ref,
            GenerateValueType.v128 => _v128
        };
    }

    /// <summary>
    ///     返回原始类型名。
    /// </summary>
    public override string ToString()
    {
        return display_name;
    }
}