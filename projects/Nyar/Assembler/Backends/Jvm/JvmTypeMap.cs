using Std.Data.Binary.Jvm;
using Nyar.Types.Externals;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     `Nyar` 规范类型名到 JVM 类型名的权威映射。
/// </summary>
internal static class JvmTypeMap
{
    /// <summary>
    ///     将结构化类型引用映射为 JVM 类型名。
    /// </summary>
    /// <param name="nyarType">结构化类型引用。</param>
    /// <returns>JVM 类型名。</returns>
    public static string to_jvm_type_name(GenerateTypeReference nyarType)
    {
        return nyarType.kind switch
        {
            GenerateTypeKind.@void => "void",
            GenerateTypeKind.unit => "int",
            GenerateTypeKind.i8 => "int",
            GenerateTypeKind.i16 => "int",
            GenerateTypeKind.i32 => "int",
            GenerateTypeKind.i64 => "long",
            GenerateTypeKind.i128 => "java/lang/Object",
            GenerateTypeKind.f32 => "float",
            GenerateTypeKind.f64 => "double",
            GenerateTypeKind.@bool => "boolean",
            GenerateTypeKind.utf8 or GenerateTypeKind.utf16 => "java/lang/String",
            GenerateTypeKind.@object or GenerateTypeKind.any or GenerateTypeKind.external_ref => "java/lang/Object",
            GenerateTypeKind.function_ref => "java/lang/Object",
            GenerateTypeKind.v128 => "java/lang/Object",
            _ => "java/lang/Object"
        };
    }

    /// <summary>
    ///     结合模块中的类型外部导入绑定，将结构化类型引用映射为 JVM 类型名。
    /// </summary>
    public static string to_jvm_type_name(GenerateModule module, GenerateTypeReference nyarType)
    {
        if (module.try_get_type_external_import_link(nyarType, CallingConvention.jvm, out var externalImportLink) &&
            externalImportLink is ExternalJvmClassImport jvmImportLink)
            return jvmImportLink.jvm_type.to_type_name();

        return to_jvm_type_name(nyarType);
    }

    /// <summary>
    ///     将 <see cref="GenerateValueType" /> 枚举值映射为 JVM 类型名。
    /// </summary>
    /// <param name="type"><see cref="GenerateValueType" /> 枚举值。</param>
    /// <returns>JVM 类型名。</returns>
    public static string to_jvm_type_name(GenerateValueType type)
    {
        return type switch
        {
            GenerateValueType.@void => "void",
            GenerateValueType.unit => "int",
            GenerateValueType.i8 => "int",
            GenerateValueType.i16 => "int",
            GenerateValueType.i32 => "int",
            GenerateValueType.i64 => "long",
            GenerateValueType.i128 => "java/lang/Object",
            GenerateValueType.f32 => "float",
            GenerateValueType.f64 => "double",
            GenerateValueType.@bool => "boolean",
            GenerateValueType.utf8 => "java/lang/String",
            GenerateValueType.utf16 => "java/lang/String",
            GenerateValueType.@object => "java/lang/Object",
            GenerateValueType.any => "java/lang/Object",
            GenerateValueType.@null => "java/lang/Object",
            GenerateValueType.function_ref => "java/lang/Object",
            GenerateValueType.external_ref => "java/lang/Object",
            GenerateValueType.v128 => "java/lang/Object",
            _ => "java/lang/Object"
        };
    }

    /// <summary>
    ///     将结构化类型引用映射为 <see cref="GenerateValueType" />。
    /// </summary>
    /// <param name="nyarType">结构化类型引用。</param>
    /// <returns>值类型枚举。</returns>
    public static GenerateValueType to_value_type(GenerateTypeReference nyarType)
    {
        return nyarType.kind switch
        {
            GenerateTypeKind.unit => GenerateValueType.unit,
            GenerateTypeKind.i8 => GenerateValueType.i32,
            GenerateTypeKind.i16 => GenerateValueType.i32,
            GenerateTypeKind.i32 => GenerateValueType.i32,
            GenerateTypeKind.i64 => GenerateValueType.i64,
            GenerateTypeKind.f32 => GenerateValueType.f32,
            GenerateTypeKind.f64 => GenerateValueType.f64,
            GenerateTypeKind.@bool => GenerateValueType.@bool,
            GenerateTypeKind.utf8 or GenerateTypeKind.utf16 => GenerateValueType.utf8,
            GenerateTypeKind.@void => GenerateValueType.@void,
            _ => GenerateValueType.@null
        };
    }

    /// <summary>
    ///     将 JVM 类型名转换为 JVM 字段描述符。
    ///     委托给 <see cref="Std.Data.Binary.Jvm.JvmTypeMapper.map_to_descriptor" />。
    /// </summary>
    /// <param name="jvmTypeName">JVM 类型名（如 "int" / "java/lang/String"）。</param>
    /// <returns>JVM 字段描述符（如 "I" / "Ljava/lang/String;"）。</returns>
    public static string to_jvm_descriptor(string jvmTypeName)
    {
        return JvmTypeMapper.map_to_descriptor(jvmTypeName);
    }

    /// <summary>
    ///     使用结构化类型引用构造 JVM 方法描述符。
    /// </summary>
    /// <param name="returnType">返回值类型引用。</param>
    /// <param name="parameterTypes">参数类型引用集合。</param>
    /// <returns>JVM 方法描述符（如 "(II)I"）。</returns>
    public static string to_jvm_method_descriptor(
        GenerateTypeReference returnType,
        IEnumerable<GenerateTypeReference> parameterTypes)
    {
        return JvmTypeMapper.map_method_descriptor(
            to_jvm_type_name(returnType),
            parameterTypes.Select(to_jvm_type_name));
    }

    /// <summary>
    ///     判断 JVM 类型是否占用 2 个局部变量表槽位（long 或 double）。
    /// </summary>
    /// <param name="jvmTypeName">JVM 类型名。</param>
    /// <returns>是否为宽类型。</returns>
    public static bool is_wide_type(string jvmTypeName)
    {
        return jvmTypeName is "long" or "double";
    }
}
