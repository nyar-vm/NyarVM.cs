using System.Collections.Frozen;
using Nyar.Assembler;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     intrinsic 注册表。
///     这里登记的是 `JVM/WASM/CLR` 共享的最小基础能力。
/// </summary>
internal static class IntrinsicRegistry
{
    private static readonly FrozenDictionary<string, IntrinsicDescriptor> _descriptors =
        new Dictionary<string, IntrinsicDescriptor>(StringComparer.Ordinal)
        {
            ["i32.add"] = new("i32.add", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_add),
            ["i32.sub"] = new("i32.sub", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_sub),
            ["i32.mul"] = new("i32.mul", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_mul),
            ["i32.div"] = new("i32.div", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_div_s),
            ["i32.rem"] = new("i32.rem", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_rem_s),
            ["i32.and"] = new("i32.and", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_and),
            ["i32.or"] = new("i32.or", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_or),
            ["i32.xor"] = new("i32.xor", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_xor),
            ["i32.shl"] = new("i32.shl", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_shl),
            ["i32.shr"] = new("i32.shr", 2, GenerateValueType.i32, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_shr_s),
            ["i32.eq"] = new("i32.eq", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_eq),
            ["i32.lt"] = new("i32.lt", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.i32_lt_s),

            ["i64.add"] = new("i64.add", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_add),
            ["i64.sub"] = new("i64.sub", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_sub),
            ["i64.mul"] = new("i64.mul", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_mul),
            ["i64.div"] = new("i64.div", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_div_s),
            ["i64.rem"] = new("i64.rem", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_rem_s),
            ["i64.and"] = new("i64.and", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_and),
            ["i64.or"] = new("i64.or", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_or),
            ["i64.xor"] = new("i64.xor", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_xor),
            ["i64.shl"] = new("i64.shl", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_shl),
            ["i64.shr"] = new("i64.shr", 2, GenerateValueType.i64, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_shr_s),
            ["i64.eq"] = new("i64.eq", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_eq),
            ["i64.lt"] = new("i64.lt", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.i64_lt_s),

            ["f64.add"] = new("f64.add", 2, GenerateValueType.f64, IntrinsicLoweringKind.opcode, NyarHeadCode.f64_add),
            ["f64.sub"] = new("f64.sub", 2, GenerateValueType.f64, IntrinsicLoweringKind.opcode, NyarHeadCode.f64_sub),
            ["f64.mul"] = new("f64.mul", 2, GenerateValueType.f64, IntrinsicLoweringKind.opcode, NyarHeadCode.f64_mul),
            ["f64.div"] = new("f64.div", 2, GenerateValueType.f64, IntrinsicLoweringKind.opcode, NyarHeadCode.f64_div),
            ["f64.eq"] = new("f64.eq", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.f64_eq),
            ["f64.lt"] = new("f64.lt", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.f64_lt),

            ["__nyar_typecheck"] = new("__nyar_typecheck", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode,
                NyarHeadCode.call_intrinsic),
            ["__nyar_asconvert"] = new("__nyar_asconvert", 2, GenerateValueType.any, IntrinsicLoweringKind.opcode,
                NyarHeadCode.call_intrinsic),
            ["__nyar_array_push"] = new("__nyar_array_push", 2, GenerateValueType.@void, IntrinsicLoweringKind.opcode,
                NyarHeadCode.array_push),
            ["arrget"] = new("arrget", 2, GenerateValueType.any, IntrinsicLoweringKind.opcode, NyarHeadCode.array_get),
            ["arrset"] = new("arrset", 3, GenerateValueType.@void, IntrinsicLoweringKind.opcode, NyarHeadCode.array_set),
            ["__nyar_length"] = new("__nyar_length", 1, GenerateValueType.i32, IntrinsicLoweringKind.opcode,
                NyarHeadCode.length),

            ["utf8_eq"] = new("utf8_eq", 2, GenerateValueType.@bool, IntrinsicLoweringKind.static_call,
                static_call_target: "std.text.Utf8Text.equals"),
            ["utf8_ne"] = new("utf8_ne", 2, GenerateValueType.@bool, IntrinsicLoweringKind.static_call,
                static_call_target: "std.text.Utf8Text.not_equals"),
            ["utf8_concat"] = new("utf8_concat", 2, GenerateValueType.utf8, IntrinsicLoweringKind.static_call,
                static_call_target: "std.text.Utf8Text.concat"),
            ["utf8_len_bytes"] = new("utf8_len_bytes", 1, GenerateValueType.i32, IntrinsicLoweringKind.static_call,
                static_call_target: "std.text.Utf8Text.byte_length"),
            ["utf8_len_chars"] = new("utf8_len_chars", 1, GenerateValueType.i32, IntrinsicLoweringKind.static_call,
                static_call_target: "std.text.Utf8Text.length"),
            ["utf8_substr"] = new("utf8_substr", 3, GenerateValueType.utf8, IntrinsicLoweringKind.static_call,
                static_call_target: "std.text.Utf8Text.slice"),

            // NyarVM 宿主 intrinsic：通过 call_intrinsic 指令委托给运行时注册的宿主函数
            ["println"] = new("io.println", 1, GenerateValueType.@void, IntrinsicLoweringKind.opcode, NyarHeadCode.call_intrinsic),
            ["print"] = new("io.print", 1, GenerateValueType.@void, IntrinsicLoweringKind.opcode, NyarHeadCode.call_intrinsic),
            ["eprintln"] = new("io.eprintln", 1, GenerateValueType.@void, IntrinsicLoweringKind.opcode, NyarHeadCode.call_intrinsic),
            ["file_exists"] = new("fs.file_exists", 1, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.call_intrinsic),
            ["read_file"] = new("fs.read_file", 1, GenerateValueType.utf8, IntrinsicLoweringKind.opcode, NyarHeadCode.call_intrinsic),
            ["write_file"] = new("fs.write_file", 2, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.call_intrinsic),
            ["host.build_project"] = new("host.build_project", 3, GenerateValueType.@bool, IntrinsicLoweringKind.opcode, NyarHeadCode.call_intrinsic)
        }.ToFrozenDictionary(StringComparer.Ordinal);

    public static bool try_get(string intrinsicName, out IntrinsicDescriptor descriptor)
    {
        return _descriptors.TryGetValue(intrinsicName, out descriptor!);
    }
}
