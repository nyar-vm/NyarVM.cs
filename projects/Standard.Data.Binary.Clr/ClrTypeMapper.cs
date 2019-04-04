namespace Std.Data.Binary.Clr;

/// <summary>
///     CLR 类型映射器，提供 Nyar 类型的CLR 类型之间的双向映的
/// </summary>
public static class ClrTypeMapper
{
    private static readonly Dictionary<string, ClrPrimitiveType> _nyar_to_clr_map = new()
    {
        { "void", ClrPrimitiveType.@void },
        { "bool", ClrPrimitiveType.boolean },
        { "i32", ClrPrimitiveType.int32 },
        { "i64", ClrPrimitiveType.int64 },
        { "f32", ClrPrimitiveType.float32 },
        { "f64", ClrPrimitiveType.float64 },
        { "string", ClrPrimitiveType.@string },
        { "object", ClrPrimitiveType.@object }
    };

    private static readonly Dictionary<PrimitiveKind, string> _clr_to_nyar_map = new()
    {
        { PrimitiveKind.@void, "void" },
        { PrimitiveKind.boolean, "bool" },
        { PrimitiveKind.int32, "i32" },
        { PrimitiveKind.int64, "i64" },
        { PrimitiveKind.float32, "f32" },
        { PrimitiveKind.float64, "f64" },
        { PrimitiveKind.@string, "string" },
        { PrimitiveKind.@object, "object" }
    };

    /// <summary>
    ///     的Nyar 类型名称映射的CLR 类型引用
    /// </summary>
    /// <param name="nyarType">Nyar 类型名称。</param>
    /// <returns>对应的CLR 类型引用。</returns>
    /// <exception cref="ArgumentException">未知的Nyar 类型名称</exception>
    public static ClrType map_to_clr(string nyarType)
    {
        if (_nyar_to_clr_map.TryGetValue(nyarType, out var primitiveType)) return primitiveType;

        if (nyarType.EndsWith("[]"))
        {
            var elementTypeName = nyarType[..^2];
            var elementType = map_to_clr(elementTypeName);
            return new ClrArrayType(elementType);
        }

        var dotIndex = nyarType.LastIndexOf('.');
        if (dotIndex >= 0)
        {
            var ns = nyarType[..dotIndex];
            var name = nyarType[(dotIndex + 1)..];
            return new ClrNamedType(name, ns);
        }

        return new ClrNamedType(nyarType, string.Empty);
    }

    /// <summary>
    ///     的CLR 类型引用映射的Nyar 类型名称
    /// </summary>
    /// <param name="clrType">CLR 类型引用。</param>
    /// <returns>对应的Nyar 类型名称。</returns>
    /// <exception cref="ArgumentException">无法映射的CLR 类型</exception>
    public static string map_from_clr(ClrType clrType)
    {
        if (clrType is ClrPrimitiveType primitiveType)
        {
            if (_clr_to_nyar_map.TryGetValue(primitiveType.kind, out var nyarName)) return nyarName;

            throw new ArgumentException($"Unknown CLR primitive type: {primitiveType.kind}", nameof(clrType));
        }

        if (clrType is ClrArrayType arrayType)
        {
            var elementTypeName = map_from_clr(arrayType.element_type);
            return $"{elementTypeName}[]";
        }

        if (clrType is ClrNamedType namedType)
        {
            if (string.IsNullOrEmpty(namedType.@namespace)) return namedType.name;

            return $"{namedType.@namespace}.{namedType.name}";
        }

        if (clrType is ClrGenericType genericType)
        {
            var args = string.Join(",", genericType.type_arguments.ConvertAll(map_from_clr));
            return $"{genericType.name}<{args}>";
        }

        throw new ArgumentException($"Unknown CLR type reference: {clrType.GetType().Name}", nameof(clrType));
    }
}