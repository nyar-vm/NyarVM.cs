using System.Text;
using Std.Data.Binary.Clr.Data;

namespace Nyar.Assembler.Backends.Clr;

/// <summary>
///     CLR 后端 partial：MSIL 文本输出方法。
/// </summary>
public partial class ClrBackend
{
    private static readonly Dictionary<ClrOpcode, string> _s_msil_opcode_names = create_msil_opcode_names();

    private static string build_msil_text(ClrModuleData module)
    {
        var methods = collect_method_definitions(module);
        var tokenComments = build_token_comment_map(module, methods);
        var sb = new StringBuilder();
        sb.AppendLine($".module {module.module_name}");
        sb.AppendLine($"// EntryPointToken: 0x{module.clr_directory.entry_point:X8}");
        append_metadata_overview(sb, module, methods);
        sb.AppendLine();
        foreach (var type in module.types)
        {
            var fullTypeName = string.IsNullOrWhiteSpace(type.@namespace)
                ? type.name
                : $"{type.@namespace}.{type.name}";
            sb.AppendLine($".class {fullTypeName}");
            sb.AppendLine("{");
            foreach (var method in type.methods)
            {
                var methodSignature = format_method_signature(method);
                sb.AppendLine($"  .method {methodSignature}");
                sb.AppendLine("  {");
                sb.AppendLine($"    .maxstack {method.max_stack}");
                if (method.init_locals)
                {
                    sb.AppendLine("    .locals init (");
                    var localTypes = method.local_variable_types;
                    if (localTypes.Count > 0)
                        for (var i = 0; i < localTypes.Count; i++)
                        {
                            var comma = i < localTypes.Count - 1 ? "," : "";
                            sb.AppendLine($"        [{i}] {map_clr_type_name(localTypes[i])}{comma}");
                        }

                    sb.AppendLine("    )");
                }

                var localNames = method.local_variable_names.Count == 0
                    ? build_fallback_local_names(method.local_variable_types.Count)
                    : method.local_variable_names;
                foreach (var instruction in method.instructions)
                {
                    var opcode = format_msil_opcode(instruction.opcode);
                    var operandText = format_msil_operand(instruction.operand);
                    var commentText = format_instruction_comment(
                        instruction,
                        tokenComments,
                        localNames,
                        out _,
                        out _);

                    if (string.IsNullOrEmpty(operandText))
                        sb.AppendLine($"    IL_{instruction.offset:X4}: {opcode}{commentText}");
                    else
                        sb.AppendLine($"    IL_{instruction.offset:X4}: {opcode} {operandText}{commentText}");
                }

                sb.AppendLine("  }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static void append_metadata_overview(StringBuilder sb, ClrModuleData module)
    {
        append_metadata_overview(sb, module, collect_method_definitions(module));
    }

    private static void append_metadata_overview(StringBuilder sb, ClrModuleData module, IReadOnlyList<ClrMethodDef> methods)
    {
        sb.AppendLine("// Metadata:");
        append_metadata_lines(
            sb,
            "TypeDef",
            module.types.Select((type, index) =>
            {
                var fullTypeName = string.IsNullOrWhiteSpace(type.@namespace)
                    ? type.name
                    : $"{type.@namespace}.{type.name}";
                return ($"0x{0x02000001u + (uint)index:X8}", fullTypeName);
            }));
        append_metadata_lines(
            sb,
            "MethodDef",
            methods.Select((method, index) => ($"0x{0x06000001u + (uint)index:X8}", format_method_signature(method))));
        append_metadata_lines(
            sb,
            "MemberRef",
            module.external_method_refs.Select((method, index) =>
                ($"0x{0x0A000001u + (uint)index:X8}", $"{method.type_full_name}.{format_external_method_signature(method)}")));
        append_metadata_lines(
            sb,
            "TypeRef",
            module.external_type_refs.Select((typeRef, index) =>
                ($"0x{0x01000001u + (uint)index:X8}", typeRef.type_full_name)));
        append_metadata_lines(
            sb,
            "UserString",
            module.user_strings.Select((value, index) =>
                ($"0x{0x70000001u + (uint)index:X8}", $"\"{escape_comment_text(value)}\"")));
        append_metadata_lines(
            sb,
            "Field",
            module.field_token_map
                .OrderBy(pair => pair.Value)
                .Select(pair => ($"0x{pair.Value:X8}", pair.Key)));
    }

    private static void append_metadata_lines(
        StringBuilder sb,
        string sectionName,
        IEnumerable<(string Token, string Description)> entries)
    {
        foreach (var (token, description) in entries) sb.AppendLine($"//   {sectionName} {token} {description}");
    }

    private static Dictionary<uint, string> build_token_comment_map(ClrModuleData module, IReadOnlyList<ClrMethodDef> methods)
    {
        var result = new Dictionary<uint, string>();
        for (var i = 0; i < methods.Count; i++)
        {
            var token = 0x06000001u + (uint)i;
            result[token] = format_method_signature(methods[i]);
        }

        for (var i = 0; i < module.external_method_refs.Count; i++)
        {
            var token = 0x0A000001u + (uint)i;
            var method = module.external_method_refs[i];
            var signature = format_external_method_signature(method);
            result[token] = $"{method.type_full_name}.{signature}";
        }

        for (var i = 0; i < module.external_type_refs.Count; i++)
        {
            var token = 0x01000001u + (uint)i;
            result[token] = module.external_type_refs[i].type_full_name;
        }

        for (var i = 0; i < module.user_strings.Count; i++)
        {
            var token = 0x70000001u + (uint)i;
            result[token] = $"\"{escape_comment_text(module.user_strings[i])}\"";
        }

        foreach (var pair in module.field_token_map) result[pair.Value] = pair.Key;
        return result;
    }

    private static IReadOnlyList<ClrMethodDef> collect_method_definitions(ClrModuleData module)
    {
        if (module.methods.Count == 0) return [.. module.types.SelectMany(type => type.methods)];

        var methods = module.methods.ToList();
        foreach (var type in module.types)
        {
            if (string.Equals(type.name, "<Module>", StringComparison.Ordinal)) continue;

            methods.AddRange(type.methods);
        }

        return methods;
    }

    private static IReadOnlyList<string> build_fallback_local_names(int localCount)
    {
        if (localCount <= 0) return [];

        var names = new string[localCount];
        for (var i = 0; i < localCount; i++) names[i] = $"local_{i}";

        return names;
    }

    private static string format_instruction_comment(
        ClrInstruction instruction,
        IReadOnlyDictionary<uint, string> tokenComments,
        IReadOnlyList<string> localNames,
        out bool hasTokenComment,
        out bool hasLocalComment)
    {
        var parts = new List<string>();
        hasTokenComment = false;
        if (instruction.operand is ClrTokenOperand tokenOperand
            && tokenComments.TryGetValue(tokenOperand.value, out var tokenComment))
        {
            parts.Add(tokenComment);
            hasTokenComment = true;
        }

        hasLocalComment = false;
        var localIndex = get_local_index(instruction);
        if (localIndex >= 0)
        {
            var localComment = format_local_comment(localNames, localIndex);
            if (!string.IsNullOrEmpty(localComment))
            {
                parts.Add(localComment);
                hasLocalComment = true;
            }
        }

        return parts.Count == 0
            ? string.Empty
            : $" // {string.Join(" | ", parts)}";
    }

    private static string format_local_comment(IReadOnlyList<string> localNames, int index)
    {
        if (index < 0 || index >= localNames.Count) return string.Empty;
        var localName = localNames[index];
        if (string.IsNullOrWhiteSpace(localName)) return string.Empty;
        return escape_comment_text(localName);
    }

    private static int get_local_index(ClrInstruction instruction)
    {
        return instruction.opcode switch
        {
            ClrOpcode.ldloc_0 or ClrOpcode.stloc_0 => 0,
            ClrOpcode.ldloc_1 or ClrOpcode.stloc_1 => 1,
            ClrOpcode.ldloc_2 or ClrOpcode.stloc_2 => 2,
            ClrOpcode.ldloc_3 or ClrOpcode.stloc_3 => 3,
            ClrOpcode.ldloc_s or ClrOpcode.ldloc or ClrOpcode.ldloca_s or ClrOpcode.ldloca
                or ClrOpcode.stloc_s or ClrOpcode.stloc
                => instruction.operand is ClrLocalIndexOperand localOperand ? (int)localOperand.index : -1,
            _ => -1
        };
    }

    private static string format_external_method_signature(ClrExternalMethodRef method)
    {
        var pseudoMethod = new ClrMethodDef
        {
            name = method.method_name,
            signature = method.method_signature
        };
        return format_method_signature(pseudoMethod);
    }

    private static string escape_comment_text(string text)
    {
        return text
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    /// <summary>
    ///     创建 CLR 操作码到 MSIL 文本的缓存映射。
    /// </summary>
    private static Dictionary<ClrOpcode, string> create_msil_opcode_names()
    {
        var result = new Dictionary<ClrOpcode, string>();
        foreach (var opcode in Enum.GetValues<ClrOpcode>()) result[opcode] = opcode.ToString().Replace('_', '.');
        return result;
    }

    /// <summary>
    ///     将操作码格式化为 MSIL 使用的点分文本。
    /// </summary>
    private static string format_msil_opcode(ClrOpcode opcode)
    {
        return _s_msil_opcode_names.GetValueOrDefault(opcode, opcode.ToString().Replace('_', '.'));
    }

    private static string format_msil_operand(ClrOperand? operand)
    {
        return operand switch
        {
            null => string.Empty,
            ClrInt8Operand value => value.value.ToString(),
            ClrInt16Operand value => value.value.ToString(),
            ClrInt32Operand value => value.value.ToString(),
            ClrInt64Operand value => value.value.ToString(),
            ClrFloat32Operand value => value.value.ToString("R"),
            ClrFloat64Operand value => value.value.ToString("R"),
            ClrTokenOperand value => $"0x{value.value:X8}",
            ClrArgumentIndexOperand value => value.index.ToString(),
            ClrLocalIndexOperand value => value.index.ToString(),
            ClrBranchTarget8Operand value => format_il_offset(value.offset),
            ClrBranchTarget32Operand value => format_il_offset(value.offset),
            ClrSwitchTargetsOperand value => string.Join(", ", value.offsets.Select(offset => format_il_offset(offset))),
            _ => operand.ToString() ?? string.Empty
        };
    }

    private static string format_il_offset(int offset)
    {
        return $"IL_{offset:X4}";
    }

    /// <summary>
    ///     解码方法签名 blob 并格式化为 MSIL 文本签名。
    /// </summary>
    private static string format_method_signature(ClrMethodDef method)
    {
        var flags = method.flags;
        var sb = new StringBuilder();
        // 访问修饰符
        if (flags.HasFlag(ClrMethodAttributes.@public))
            sb.Append("public ");
        else if (flags.HasFlag(ClrMethodAttributes.@private)) sb.Append("private ");
        // static
        if (flags.HasFlag(ClrMethodAttributes.@static)) sb.Append("static ");
        // 解码签名 blob 获取返回类型和参数类型
        var signature = method.signature;
        if (signature.Length == 0)
        {
            sb.Append($"void {method.name}()");
            return sb.ToString();
        }

        var offset = 0;
        // 跳过调用约定字节
        offset++;
        // 读取参数数量（压缩无符号整数）
        var paramCount = read_compressed_unsigned(signature, ref offset);
        // 读取返回类型
        var returnType = read_element_type(signature, ref offset);
        // 读取参数类型
        var paramTypes = new List<string>();
        for (var i = 0; i < paramCount; i++) paramTypes.Add(read_element_type(signature, ref offset));
        sb.Append($"{returnType} {method.name}(");
        for (var i = 0; i < paramTypes.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(paramTypes[i]);
        }

        sb.Append(")");
        return sb.ToString();
    }

    /// <summary>
    ///     将 Nyar 类型名映射为 CLR 类型名。
    /// </summary>
    private static string map_clr_type_name(string typeName)
    {
        var kind = GenerateTypeReference.parse(typeName).kind;
        switch (kind)
        {
            case GenerateTypeKind.@void:
            case GenerateTypeKind.unit:
                return "void";
            case GenerateTypeKind.@bool:
                return "bool";
            case GenerateTypeKind.i8:
                return "int8";
            case GenerateTypeKind.i16:
                return "int16";
            case GenerateTypeKind.i32:
                return "int32";
            case GenerateTypeKind.i64:
                return "int64";
            case GenerateTypeKind.i128:
                return "valuetype System.Int128";
            case GenerateTypeKind.u8:
                return "uint8";
            case GenerateTypeKind.u16:
                return "uint16";
            case GenerateTypeKind.u32:
                return "uint32";
            case GenerateTypeKind.u64:
                return "uint64";
            case GenerateTypeKind.f32:
                return "float32";
            case GenerateTypeKind.f64:
                return "float64";
            case GenerateTypeKind.utf8:
            case GenerateTypeKind.utf16:
                return "string";
            case GenerateTypeKind.uuid:
                return "valuetype System.Guid";
            case GenerateTypeKind.@null:
            case GenerateTypeKind.@object:
            case GenerateTypeKind.any:
            case GenerateTypeKind.function_ref:
            case GenerateTypeKind.external_ref:
            case GenerateTypeKind.v128:
            case GenerateTypeKind.unsafe_ref:
            case GenerateTypeKind.self_ref:
            case GenerateTypeKind.emptylist:
            case GenerateTypeKind.bracket_array:
            case GenerateTypeKind.array:
            case GenerateTypeKind.list:
            case GenerateTypeKind.map:
            case GenerateTypeKind.set:
            case GenerateTypeKind.user_defined_named:
            case GenerateTypeKind.unknown_named:
            default:
                return "object";
        }
    }

    /// <summary>
    ///     从签名 blob 中读取一个压缩无符号整数。
    /// </summary>
    private static uint read_compressed_unsigned(byte[] data, ref int offset)
    {
        if (offset >= data.Length) return 0;
        var first = data[offset++];
        if ((first & 0x80) == 0) return first;
        if ((first & 0xC0) == 0x80)
        {
            if (offset >= data.Length) return 0;
            return (uint)(((first & 0x3F) << 8) | data[offset++]);
        }

        // 4字节编码
        if (offset + 3 >= data.Length) return 0;
        var result = (uint)(((first & 0x1F) << 24) | (data[offset++] << 16) | (data[offset++] << 8) | data[offset++]);
        return result;
    }

    /// <summary>
    ///     从签名 blob 中读取元素类型并返回 CLR 类型名。
    /// </summary>
    private static string read_element_type(byte[] data, ref int offset)
    {
        if (offset >= data.Length) return "void";
        var typeCode = data[offset++];
        switch (typeCode)
        {
            case 0x01: return "void"; // ELEMENT_TYPE_VOID
            case 0x02: return "bool"; // ELEMENT_TYPE_BOOLEAN
            case 0x04: return "int8"; // ELEMENT_TYPE_I1
            case 0x06: return "int16"; // ELEMENT_TYPE_I2
            case 0x08: return "int32"; // ELEMENT_TYPE_I4
            case 0x0A: return "int64"; // ELEMENT_TYPE_I8
            case 0x0C: return "float32"; // ELEMENT_TYPE_R4
            case 0x0D: return "float64"; // ELEMENT_TYPE_R8
            case 0x0E: return "string"; // ELEMENT_TYPE_STRING
            case 0x1C: return "object"; // ELEMENT_TYPE_OBJECT
            case 0x1D: // ELEMENT_TYPE_SZARRAY
            {
                var elementType = read_element_type(data, ref offset);
                return $"{elementType}[]";
            }
            default: return "object";
        }
    }
}