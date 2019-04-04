using Nyar.Assembler;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     字段定义，描述类型中单个字段的运行时信息
/// </summary>
/// <param name="name">字段名</param>
/// <param name="type">字段值类型</param>
/// <param name="offset">字段在对象中的字节偏移</param>
/// <param name="size">字段占用的字节数</param>
public sealed record LirFieldDef(
    string name,
    GenerateValueType type,
    int offset,
    int size
);