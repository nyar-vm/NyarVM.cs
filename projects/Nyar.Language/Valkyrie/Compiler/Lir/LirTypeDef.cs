using Nyar.Assembler;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     LIR 层类型定义，承载类型在运行时的最小表示信息
/// </summary>
/// <param name="name">类型限定名</param>
/// <param name="BaseType">基础值类型（底层表示）</param>
/// <param name="fields">字段列表</param>
/// <param name="GcPointerFieldIndices">GC 需要扫描的字段（在 Fields 中的索引）</param>
/// <param name="EnumUnderlyingType">枚举底层整数类型编码，仅 Enums/Flags 使用，其他类型为 null</param>
public sealed record LirTypeDef(
    string name,
    GenerateValueType base_type,
    IReadOnlyList<LirFieldDef> fields,
    IReadOnlyList<int> gc_pointer_field_indices,
    int? enum_underlying_type
);