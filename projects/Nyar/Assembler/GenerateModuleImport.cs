namespace Nyar.Assembler;

/// <summary>
///     元编译导入。
/// </summary>
public sealed record GenerateModuleImport(string module_name, string symbol_name, GenerateImportKind kind);