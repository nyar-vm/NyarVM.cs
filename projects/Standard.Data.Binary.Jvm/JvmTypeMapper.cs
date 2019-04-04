using Std.Data.Binary.Jvm.Data;

namespace Std.Data.Binary.Jvm;

/// <summary>
///     JVM 类型与描述符映射工具。
///     参考：https://docs.oracle.com/javase/specs/jvms/se17/html/jvms-4.html#jvms-4.3
/// </summary>
public static class JvmTypeMapper
{
    /// <summary>
    ///     将 JVM 类型名映射为 JVM 字段描述符。
    /// </summary>
    public static string map_to_descriptor(string jvmType)
    {
        return jvmType.ToLowerInvariant() switch
        {
            "void" => "V",
            "boolean" => "Z",
            "byte" => "B",
            "short" => "S",
            "int" => "I",
            "long" => "J",
            "float" => "F",
            "double" => "D",
            "char" => "C",
            _ when jvmType.StartsWith("java/", StringComparison.Ordinal) => $"L{jvmType};",
            _ when jvmType.EndsWith("[]") => $"[{map_to_descriptor(jvmType[..^2])}",
            _ => $"L{jvmType.Replace('.', '/')};"
        };
    }

    /// <summary>
    ///     将 JVM 字段描述符映射回 JVM 类型名。
    /// </summary>
    public static string map_from_descriptor(string descriptor)
    {
        return descriptor switch
        {
            "V" => "void",
            "Z" => "boolean",
            "B" => "byte",
            "S" => "short",
            "I" => "int",
            "J" => "long",
            "F" => "float",
            "D" => "double",
            "Ljava/lang/String;" => "java/lang/String",
            "Ljava/lang/Object;" => "java/lang/Object",
            _ when descriptor.StartsWith("[") => $"{map_from_descriptor(descriptor[1..])}[]",
            _ when descriptor.StartsWith("L") && descriptor.EndsWith(";")
                => descriptor[1..^1].Replace('/', '.'),
            _ => descriptor
        };
    }

    /// <summary>
    ///     将 JVM 方法签名映射为 JVM 方法描述符。
    /// </summary>
    public static string map_method_descriptor(string returnType, IEnumerable<string> parameterTypes)
    {
        var paramDescs = string.Join("", parameterTypes.Select(map_to_descriptor));
        var returnDesc = map_to_descriptor(returnType);
        return $"({paramDescs}){returnDesc}";
    }

    /// <summary>
    ///     获取 JVM 类型对应的加载指令。
    /// </summary>
    public static JvmOpcode get_load_opcode(string jvmType)
    {
        return jvmType.ToLowerInvariant() switch
        {
            "int" or "boolean" or "byte" or "short" or "char" => JvmOpcode.iload,
            "long" => JvmOpcode.lload,
            "float" => JvmOpcode.fload,
            "double" => JvmOpcode.dload,
            _ => JvmOpcode.aload
        };
    }

    /// <summary>
    ///     获取 JVM 类型对应的存储指令。
    /// </summary>
    public static JvmOpcode get_store_opcode(string jvmType)
    {
        return jvmType.ToLowerInvariant() switch
        {
            "int" or "boolean" or "byte" or "short" or "char" => JvmOpcode.istore,
            "long" => JvmOpcode.lstore,
            "float" => JvmOpcode.fstore,
            "double" => JvmOpcode.dstore,
            _ => JvmOpcode.astore
        };
    }

    /// <summary>
    ///     获取 JVM 类型对应的返回指令。
    /// </summary>
    public static JvmOpcode get_return_opcode(string jvmType)
    {
        return jvmType.ToLowerInvariant() switch
        {
            "void" => JvmOpcode.@return,
            "int" or "boolean" or "byte" or "short" or "char" => JvmOpcode.ireturn,
            "long" => JvmOpcode.lreturn,
            "float" => JvmOpcode.freturn,
            "double" => JvmOpcode.dreturn,
            _ => JvmOpcode.areturn
        };
    }

    /// <summary>
    ///     获取类型在 JVM 局部变量表中的槽位大小（1 或 2）。
    ///     long 和 double 占 2 个槽位，其余类型占 1 个。
    /// </summary>
    public static int get_slot_size(string jvmType)
    {
        var lower = jvmType.ToLowerInvariant();
        return lower is "long" or "double" ? 2 : 1;
    }

    /// <summary>
    ///     将强类型 JVM 类型引用映射为 JVM 字段描述符。
    /// </summary>
    /// <param name="jvmType">强类型 JVM 类型引用。</param>
    /// <returns>JVM 字段描述符。</returns>
    public static string map_to_descriptor(JvmType jvmType)
    {
        return map_to_descriptor(jvmType.to_type_name());
    }

    /// <summary>
    ///     将 JVM 方法签名映射为 JVM 方法描述符。
    /// </summary>
    /// <param name="returnType">返回值类型。</param>
    /// <param name="parameterTypes">参数类型列表。</param>
    /// <returns>JVM 方法描述符。</returns>
    public static string map_method_descriptor(JvmType returnType, IEnumerable<JvmType> parameterTypes)
    {
        var paramDescs = string.Join("", parameterTypes.Select(map_to_descriptor));
        var returnDesc = map_to_descriptor(returnType);
        return $"({paramDescs}){returnDesc}";
    }

    /// <summary>
    ///     获取强类型 JVM 类型引用对应的加载指令。
    /// </summary>
    /// <param name="jvmType">强类型 JVM 类型引用。</param>
    /// <returns>对应的加载指令。</returns>
    public static JvmOpcode get_load_opcode(JvmType jvmType)
    {
        return get_load_opcode(jvmType.to_type_name());
    }

    /// <summary>
    ///     获取强类型 JVM 类型引用对应的存储指令。
    /// </summary>
    /// <param name="jvmType">强类型 JVM 类型引用。</param>
    /// <returns>对应的存储指令。</returns>
    public static JvmOpcode get_store_opcode(JvmType jvmType)
    {
        return get_store_opcode(jvmType.to_type_name());
    }

    /// <summary>
    ///     获取强类型 JVM 类型引用对应的返回指令。
    /// </summary>
    /// <param name="jvmType">强类型 JVM 类型引用。</param>
    /// <returns>对应的返回指令。</returns>
    public static JvmOpcode get_return_opcode(JvmType jvmType)
    {
        return get_return_opcode(jvmType.to_type_name());
    }

    /// <summary>
    ///     获取强类型 JVM 类型引用在局部变量表中的槽位大小。
    /// </summary>
    /// <param name="jvmType">强类型 JVM 类型引用。</param>
    /// <returns>槽位大小（1 或 2）。</returns>
    public static int get_slot_size(JvmType jvmType)
    {
        return get_slot_size(jvmType.to_type_name());
    }
}
