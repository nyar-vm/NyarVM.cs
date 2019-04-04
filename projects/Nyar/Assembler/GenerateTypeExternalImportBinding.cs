using Nyar.Types.Externals;

namespace Nyar.Assembler;

/// <summary>
///     模块级类型外部导入绑定。
/// </summary>
public sealed record GenerateTypeExternalImportBinding(
    string type_name,
    ExternalImport external_import_link);
