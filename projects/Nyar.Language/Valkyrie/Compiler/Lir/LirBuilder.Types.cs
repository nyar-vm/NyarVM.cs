using System.Collections.Generic;
using System.Collections.Immutable;
using Nyar.Assembler;
using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Rules;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Mir;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     承载类型映射和辅助方法的 partial 分部类。
///     包含值类型兼容性判断、类型映射、变量声明降级、寄存器管理、常量发射、效应降级等辅助方法。
/// </summary>
public sealed partial class LirBuilder
{
    #region 推断与辅助

    /// <summary>
    ///     判断源值类型是否与目标返回类型兼容。
    /// </summary>
    private static bool is_value_type_compatible(GenerateValueType source, GenerateValueType target)
    {
        if (source == target) return true;

        // 引用类型之间互相兼容（object/any/function_ref/external_ref/utf8）
        if (is_reference_type(source) && is_reference_type(target)) return true;

        // 整数类型之间兼容（i8/i16/i32/i64/u8/u16/u32/u64/i128/u128）
        if (is_integer_type(source) && is_integer_type(target)) return true;

        // 浮点类型之间兼容（f32/f64/f128）
        if (is_float_type(source) && is_float_type(target)) return true;

        return false;
    }

    /// <summary>
    ///     判断值类型是否为引用类型。
    /// </summary>
    private static bool is_reference_type(GenerateValueType t)
    {
        return t is GenerateValueType.@object or GenerateValueType.any
            or GenerateValueType.function_ref or GenerateValueType.external_ref
            or GenerateValueType.@null or GenerateValueType.v128
            or GenerateValueType.i128 or GenerateValueType.utf8;
    }

    /// <summary>
    ///     判断值类型是否为整数类型。
    /// </summary>
    private static bool is_integer_type(GenerateValueType t)
    {
        return t is GenerateValueType.i8 or GenerateValueType.i16 or GenerateValueType.i32
            or GenerateValueType.i64 or GenerateValueType.i128;
    }

    /// <summary>
    ///     判断值类型是否为浮点类型。
    /// </summary>
    private static bool is_float_type(GenerateValueType t)
    {
        return t is GenerateValueType.f32 or GenerateValueType.f64;
    }

    /// <summary>
    ///     判断值类型是否为文本类型（utf8/utf16）。
    ///     注意：utf32/c_str 在 LIR 中映射为 external_ref，无法与 class/trait/union 区分，
    ///     因此不在此处判断；如需识别，需通过 HIR 类型名补充判断。
    /// </summary>
    private static bool is_text_type(GenerateValueType t)
    {
        return t is GenerateValueType.utf8 or GenerateValueType.utf16;
    }

    /// <summary>
    ///     判断值类型是否为需要拆箱才能在 i32 比较中使用的对象引用类型。
    ///     包括 any / object / external_ref / function_ref / null，
    ///     它们在 JVM 中映射为 Object，必须先 any_to_i32 拆箱才能参与 i32 比较。
    ///     排除 utf8（String）和 v128 / i128 等特殊类型。
    /// </summary>
    private static bool is_object_ref_for_i32_cmp(GenerateValueType t)
    {
        return t is GenerateValueType.any
            or GenerateValueType.@object
            or GenerateValueType.external_ref
            or GenerateValueType.function_ref
            or GenerateValueType.@null;
    }

    /// <summary>
    ///     当源类型和目标类型不兼容时，插入隐式类型转换指令。
    ///     例如：any（Object）→ i32（int）时插入 any_to_i32 拆箱。
    /// </summary>
    private static void insert_implicit_cast(GenerateValueType source, GenerateValueType target, FunctionLoweringContext context)
    {
        if (source == target) return;

        // any（Object）→ utf8（String）转换（checkcast）
        if (source == GenerateValueType.any && target == GenerateValueType.utf8)
        {
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.any_to_utf8));
            return;
        }

        if (is_reference_type(source) && is_reference_type(target)) return;
        if (is_integer_type(source) && is_integer_type(target)) return;
        if (is_float_type(source) && is_float_type(target)) return;

        // any（Object）→ i32 拆箱
        if (source == GenerateValueType.any && target == GenerateValueType.i32)
        {
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.any_to_i32));
        }
    }

    private static GenerateValueType emit_const(IList<GenerateInstruction> instructions, GenerateOperand operand,
        GenerateValueType valueType)
    {
        instructions.Add(new GenerateInstruction(NyarHeadCode.@const, operand));
        return valueType;
    }

    private static GenerateValueType emit_integer_constant(
        List<GenerateInstruction> instructions,
        long value,
        GenerateValueType? expectedType)
    {
        var integerType = normalize_integer_value_type(expectedType ?? GenerateValueType.i32);
        return integerType == GenerateValueType.i64
            ? emit_const(instructions, new GenerateOperand.I64(value), GenerateValueType.i64)
            : emit_const(instructions, new GenerateOperand.I32((int)value), integerType);
    }

    private static void emit_default_value(List<GenerateInstruction> instructions, GenerateValueType returnType)
    {
        instructions.Add(new GenerateInstruction(NyarHeadCode.@const, returnType switch
        {
            GenerateValueType.unit => new GenerateOperand.I32(0),
            GenerateValueType.i8 or GenerateValueType.i16 or GenerateValueType.i32 => new GenerateOperand.I32(0),
            GenerateValueType.i64 => new GenerateOperand.I64(0),
            GenerateValueType.f32 => new GenerateOperand.F32(0f),
            GenerateValueType.f64 => new GenerateOperand.F64(0d),
            GenerateValueType.utf8 => new GenerateOperand.Str(string.Empty),
            GenerateValueType.utf16 => new GenerateOperand.Str(string.Empty),
            GenerateValueType.@bool => new GenerateOperand.I32(0),
            GenerateValueType.@void => new GenerateOperand.Null(GenerateValueType.@void),
            _ => new GenerateOperand.Null(returnType)
        }));
    }

    private static string to_cg_type_name(GenerateValueType valueType)
    {
        return valueType switch
        {
            GenerateValueType.@void => "void",
            GenerateValueType.unit => "unit",
            GenerateValueType.@bool => "bool",
            GenerateValueType.i8 => "i8",
            GenerateValueType.i16 => "i16",
            GenerateValueType.i32 => "i32",
            GenerateValueType.i64 => "i64",
            GenerateValueType.f32 => "f32",
            GenerateValueType.f64 => "f64",
            GenerateValueType.utf8 => "utf8",
            GenerateValueType.utf16 => "utf16",
            GenerateValueType.@object => "object",
            GenerateValueType.any => "any",
            GenerateValueType.function_ref => "function_ref",
            GenerateValueType.external_ref => "external_ref",
            GenerateValueType.v128 => "v128",
            _ => "i32"
        };
    }

    /// <summary>
    ///     根据类型名和 HIR 类型种类映射到 Nyar 值类型。
    /// </summary>
    private static GenerateValueType map_type_name_to_value_type(string typeName, HirTypeKind? kind)
    {
        if (kind.HasValue)
            return kind.Value switch
            {
                HirTypeKind.enums or HirTypeKind.flags => map_enum_underlying_type(typeName),
                HirTypeKind.structure or HirTypeKind.unite => GenerateValueType.@object,
                HirTypeKind.@class or HirTypeKind.trait => GenerateValueType.external_ref,
                _ => map_type_name_to_value_type(typeName)
            };

        return map_type_name_to_value_type(typeName);
    }

    /// <summary>
    ///     获取枚举的底层整数类型，默认为 i32。
    /// </summary>
    private static GenerateValueType map_enum_underlying_type(string typeName)
    {
        return typeName switch
        {
            "i8" => GenerateValueType.i8,
            "i16" => GenerateValueType.i16,
            "i32" => GenerateValueType.i32,
            "i64" => GenerateValueType.i64,
            "u8" => GenerateValueType.i8,
            "u16" => GenerateValueType.i16,
            "u32" => GenerateValueType.i32,
            "u64" => GenerateValueType.i64,
            _ => GenerateValueType.i32
        };
    }

    /// <summary>
    ///     将类型名字符串映射为 GenerateValueType 枚举值。
    /// </summary>
    private static GenerateValueType map_type_name_to_value_type(string typeName)
    {
        ValkyrieTextTypeFacts.ensure_no_pre_hir_literal_type(typeName, "LIR");
        ensure_no_legacy_string_type_name(typeName);
        ensure_no_legacy_scalar_type_name(typeName);
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);

        return normalizedTypeName switch
        {
            "void" => GenerateValueType.@void,
            "unit" or "Unit" => GenerateValueType.unit,
            "bool" => GenerateValueType.@bool,
            "i8" => GenerateValueType.i8,
            "i16" => GenerateValueType.i16,
            "i32" => GenerateValueType.i32,
            "i64" => GenerateValueType.i64,
            "isize" or "usize" => GenerateValueType.i32,
            "f32" => GenerateValueType.f32,
            "f64" => GenerateValueType.f64,
            "any" => GenerateValueType.any,
            "utf8" => GenerateValueType.utf8,
            "utf16" => GenerateValueType.utf16,
            "utf32" or "c_str" => GenerateValueType.external_ref,
            _ => GenerateValueType.external_ref
        };
    }

    /// <summary>
    ///     判断类型名是否为内建值类型（非结构体/类）。
    ///     用于在 lower_function 中决定是否保留原始类型名：
    ///     内建类型名应映射为 GenerateValueType，结构体类型名应保留原名。
    /// </summary>
    private static bool is_builtin_value_type_name(string typeName)
    {
        ensure_no_legacy_string_type_name(typeName);
        ensure_no_legacy_scalar_type_name(typeName);
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);

        return normalizedTypeName switch
        {
            "void" or "unit" or "Unit" or "bool"
                or "i8" or "i16" or "i32" or "i64"
                or "isize" or "usize"
                or "f32" or "f64"
                or "any" or "utf8" or "utf16" or "utf32" or "c_str" => true,
            _ => false
        };
    }

    /// <summary>
    ///     从 `LIR` 开始禁止使用历史遗留的宿主语言标量别名。
    /// </summary>
    private static void ensure_no_legacy_scalar_type_name(string typeName)
    {
        if (typeName is not "int" and not "long" and not "float" and not "double" and not "boolean")
        {
            return;
        }

        throw new InvalidOperationException(
            "Valkyrie 从 LIR 开始禁止使用宿主语言标量别名；请在进入 LIR 前显式归一化为 `i32`、`i64`、`f32`、`f64` 或 `bool`。");
    }

    /// <summary>
    ///     从 `LIR` 开始禁止使用历史遗留的宽泛 `string` 类型名。
    /// </summary>
    private static void ensure_no_legacy_string_type_name(string typeName)
    {
        if (typeName is not "string" and not "String")
        {
            return;
        }

        throw new InvalidOperationException(
            "Valkyrie 从 LIR 开始禁止使用宽泛的 `string` 类型；请在进入 LIR 前显式确定为 `utf8`、`utf16`、`utf32` 或 `c_str`。");
    }

    private static NyarHeadCode? try_get_cast_opcode(GenerateValueType sourceType, GenerateValueType targetType)
    {
        return (sourceType, targetType) switch
        {
            (GenerateValueType.i32, GenerateValueType.i64) => NyarHeadCode.i32_extend_i64_s,
            (GenerateValueType.i64, GenerateValueType.i32) => NyarHeadCode.i64_trunc_i32_s,
            (GenerateValueType.i32, GenerateValueType.f32) => NyarHeadCode.i32_to_f32_s,
            (GenerateValueType.i32, GenerateValueType.f64) => NyarHeadCode.i32_to_f64_s,
            (GenerateValueType.i64, GenerateValueType.f64) => NyarHeadCode.i64_to_f64,
            (GenerateValueType.f64, GenerateValueType.i32) => NyarHeadCode.f64_to_i32,
            (GenerateValueType.f64, GenerateValueType.i64) => NyarHeadCode.f64_to_i64,
            (GenerateValueType.any, GenerateValueType.i32) => NyarHeadCode.any_to_i32,
            (GenerateValueType.any, GenerateValueType.utf8) => NyarHeadCode.any_to_utf8,
            _ => null
        };
    }

    private static void lower_variable_declaration(VarDecl declaration, FunctionLoweringContext context)
    {
        if (string.IsNullOrWhiteSpace(declaration.name)) return;

        var declaredType = resolve_variable_type(declaration, context);
        var initValue = declaration.value;

        var initializerType = initValue != default
            ? lower_expression(initValue, context, declaredType)
            : GenerateValueType.@void;
        var localType = declaredType ?? normalize_storage_type(initializerType);
        var localIndex = register_local(declaration.name, localType, context);

        // 当初始化器不存在或初始化器返回 void 时，需要推入默认值以确保 store_local 栈平衡
        if (declaration.value == default || initializerType == GenerateValueType.@void)
            emit_default_value(context.instructions, localType);

        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.store_local,
            new GenerateOperand.Local(localIndex, localType)));
    }

    /// <summary>
    ///     Oa.VarDecl 版本的变量声明降级（与 Dialect.Core.Nodes.VarDecl 结构相同但类型不同）
    /// </summary>
    private static void lower_variable_declaration_ikun(AlgebraNode.VarDecl declaration, FunctionLoweringContext context)
    {
        if (string.IsNullOrWhiteSpace(declaration.name)) return;

        var declaredType = resolve_variable_type_ikun(declaration, context);
        var initValue = declaration.value;

        var initializerType = initValue is { } v
            ? lower_expression(v, context, declaredType)
            : GenerateValueType.@void;
        var localType = declaredType ?? normalize_storage_type(initializerType);
        var localIndex = register_local(declaration.name, localType, context);

        // 当初始化器不存在或初始化器返回 void 时，需要推入默认值以确保 store_local 栈平衡
        if (declaration.value is null || initializerType == GenerateValueType.@void)
            emit_default_value(context.instructions, localType);

        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.store_local,
            new GenerateOperand.Local(localIndex, localType)));
    }

    private static GenerateValueType? resolve_variable_type(VarDecl declaration, FunctionLoweringContext context)
    {
        if (declaration.type == default) return null;

        return resolve_node(context.graph, declaration.type) is TypeRef typeRef
            ? map_type_name_to_value_type(typeRef.name)
            : null;
    }

    /// <summary>
    ///     Oa.VarDecl 版本的变量类型解析
    /// </summary>
    private static GenerateValueType? resolve_variable_type_ikun(AlgebraNode.VarDecl declaration, FunctionLoweringContext context)
    {
        if (declaration.type is not { } typeId) return null;

        return resolve_node(context.graph, typeId) is TypeRef typeRef
            ? map_type_name_to_value_type(typeRef.name)
            : null;
    }

    private static GenerateValueType map_hir_type_ref_to_value_type(HirTypeRef typeRef)
    {
        return typeRef.special_kind switch
        {
            HirSpecialTypeKind.unit => GenerateValueType.unit,
            HirSpecialTypeKind.@void => GenerateValueType.@void,
            HirSpecialTypeKind.auto => GenerateValueType.i32,
            HirSpecialTypeKind.exit_code => GenerateValueType.i32,
            _ => map_type_name_to_value_type(typeRef.name)
        };
    }

    private static int register_local(string name, GenerateValueType valueType, FunctionLoweringContext context)
    {
        if (context.locals.TryGetValue(name, out var existingIndex))
        {
            context.local_types[name] = valueType;
            return existingIndex;
        }

        var localIndex = context.local_variables.Count;
        context.locals[name] = localIndex;
        context.local_types[name] = valueType;
        context.local_variables.Add(new GenerateLocalVariable(name, GenerateTypeReference.from_value_type(valueType), localIndex));
        return localIndex;
    }

    private static int register_scoped_local(string name, GenerateValueType valueType, FunctionLoweringContext context)
    {
        var localIndex = context.local_variables.Count;
        context.locals[name] = localIndex;
        context.local_types[name] = valueType;
        context.local_variables.Add(new GenerateLocalVariable(name, GenerateTypeReference.from_value_type(valueType), localIndex));
        return localIndex;
    }

    private static GenerateValueType normalize_storage_type(GenerateValueType valueType)
    {
        return valueType == GenerateValueType.@void ? GenerateValueType.i32 : normalize_integer_value_type(valueType);
    }

    private static GenerateValueType normalize_integer_value_type(GenerateValueType valueType)
    {
        return valueType switch
        {
            GenerateValueType.i64 => GenerateValueType.i64,
            GenerateValueType.f32 => GenerateValueType.f32,
            GenerateValueType.f64 => GenerateValueType.f64,
            GenerateValueType.utf8 => GenerateValueType.utf8,
            GenerateValueType.utf16 => GenerateValueType.utf16,
            GenerateValueType.@object => GenerateValueType.@object,
            GenerateValueType.any => GenerateValueType.any,
            GenerateValueType.function_ref => GenerateValueType.function_ref,
            GenerateValueType.external_ref => GenerateValueType.external_ref,
            GenerateValueType.v128 => GenerateValueType.v128,
            _ => GenerateValueType.i32
        };
    }

    private static GenerateValueType select_integer_binary_type(
        GenerateValueType leftType,
        GenerateValueType rightType,
        GenerateValueType? expectedType)
    {
        if (normalize_integer_value_type(expectedType ?? GenerateValueType.i32) == GenerateValueType.i64)
            return GenerateValueType.i64;

        if (normalize_integer_value_type(leftType) == GenerateValueType.i64 ||
            normalize_integer_value_type(rightType) == GenerateValueType.i64)
            return GenerateValueType.i64;

        return GenerateValueType.i32;
    }

    #endregion

    #region 效应降级

    /// <summary>
    ///     判断 long 值是否为 CoreBuiltin 中的效应内置函数（VmPerform 或 VmHandle）
    /// </summary>
    private static bool is_standard_effect_builtin(long value)
    {
        return value == (long)CoreBuiltin.vm_perform || value == (long)CoreBuiltin.vm_handle;
    }

    /// <summary>
    ///     将 CoreBuiltin 效应内置函数调用降级为对应的效应操作码序列。
    ///     VmPerform → perform_effect；VmHandle → enter_effect_handler / exit_effect_handler。
    /// </summary>
    private static GenerateValueType emit_standard_effect_builtin(
        CoreBuiltin builtin,
        IReadOnlyList<Id> arguments,
        FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        return builtin switch
        {
            CoreBuiltin.vm_perform => emit_vm_perform(arguments, context, expectedType),
            CoreBuiltin.vm_handle => emit_vm_handle(arguments, context, expectedType),
            _ => throw new NotSupportedException($"CoreBuiltin `{builtin}` 不是效应内置函数，无法降级。")
        };
    }

    /// <summary>
    ///     将 VmPerform 调用降级为 perform_effect 指令。
    ///     参数布局：[effectNameHash, argsTuple]
    /// </summary>
    private static GenerateValueType emit_vm_perform(
        IReadOnlyList<Id> arguments,
        FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        if (arguments.Count != 2)
            throw new InvalidOperationException(
                $"VmPerform 期望 2 个参数（effectNameHash, argsTuple），实际收到 {arguments.Count}。");

        // 求值 effectNameHash → 栈
        lower_expression(arguments[0], context, GenerateValueType.i32);
        // 求值 argsTuple → 栈
        lower_expression(arguments[1], context, null);
        // 发射 perform_effect 指令
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.perform_effect));
        return expectedType ?? GenerateValueType.any;
    }

    /// <summary>
    ///     将 VmHandle 调用降级为 enter_effect_handler / exit_effect_handler 指令序列。
    ///     参数布局：[effectNameHash, handler, body]
    /// </summary>
    private static GenerateValueType emit_vm_handle(
        IReadOnlyList<Id> arguments,
        FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        if (arguments.Count != 3)
            throw new InvalidOperationException(
                $"VmHandle 期望 3 个参数（effectNameHash, handler, body），实际收到 {arguments.Count}。");

        // 求值 effectNameHash → 栈
        lower_expression(arguments[0], context, GenerateValueType.i32);
        // 求值 handler → 栈
        lower_expression(arguments[1], context, null);
        // 注册效应处理器
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.enter_effect_handler));
        // 执行 body（可能触发效应）
        var resultType = lower_expression(arguments[2], context, expectedType);
        // 卸载效应处理器
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.exit_effect_handler));
        return resultType;
    }

    #endregion

    #region 调试辅助

    /// <summary>
    ///     格式化操作数为可读字符串，供调试输出使用
    /// </summary>
    private static string format_operand(GenerateOperand operand)
    {
        return operand switch
        {
            GenerateOperand.FuncRef funcRef => $"FuncRef({funcRef.name}, params=[{string.Join(",", funcRef.signature.parameters)}], results=[{string.Join(",", funcRef.signature.results)}])",
            GenerateOperand.I32 i32 => $"I32({i32.value})",
            GenerateOperand.I64 i64 => $"I64({i64.value})",
            GenerateOperand.F64 f64 => $"F64({f64.value})",
            GenerateOperand.Str str => $"Str(\"{str.value}\")",
            GenerateOperand.Label label => $"Label({label.name})",
            GenerateOperand.Local local => $"Local({local.index}, {local.type})",
            GenerateOperand.Param param => $"Param({param.index}, {param.type})",
            GenerateOperand.Const c => $"Const(pool={c.pool_index})",
            GenerateOperand.Null => "Null",
            _ => operand.ToString() ?? operand.GetType().Name
        };
    }

    #endregion
}
