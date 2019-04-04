namespace Nyar.Assembler;

/// <summary>
///     元编译导出。
/// </summary>
public sealed record GenerateModuleExport(string name, GenerateExportKind kind, int function_index);