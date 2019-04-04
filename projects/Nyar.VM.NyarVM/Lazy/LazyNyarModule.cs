using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.Bytecode;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;
using Std.Data.Binary.NyarIR.Scanner;
using NyarFunction = Nyar.Types.NyarFunction;

namespace Nyar.VM.NyarVM.Lazy;

/// <summary>
///     惰性加载的 Nyar 模块。
///     它持有原始 `.nyar` 模块二进制，仅在首次访问某个段时才解码对应模块数据。
/// </summary>
public sealed class LazyNyarModule : IModule
{
    private readonly byte[] _module_bytes;
    private readonly object _lock = new();
    private readonly NyarModuleData? _pre_decoded_module_data;
    private readonly NyarScanHeader _scan_header;

    private List<Value>? _constants;
    private List<ModuleExport>? _exports;
    private List<NyarFunction>? _functions;
    private List<ModuleImport>? _imports;

    /// <summary>
    ///     初始化惰性模块。
    /// </summary>
    /// <param name="moduleBytes">原始 `.nyar` 模块二进制。</param>
    /// <param name="scanHeader">预扫描的头部信息。</param>
    public LazyNyarModule(byte[] moduleBytes, NyarScanHeader scanHeader)
    {
        _module_bytes = moduleBytes;
        _scan_header = scanHeader;
        name = scanHeader.module_name;
        version = scanHeader.version;
    }

    /// <summary>
    ///     初始化惰性模块，并复用预解码的模块数据。
    /// </summary>
    /// <param name="moduleBytes">原始 `.nyar` 模块二进制。</param>
    /// <param name="moduleData">预解码的模块数据。</param>
    public LazyNyarModule(byte[] moduleBytes, NyarModuleData moduleData)
    {
        _module_bytes = moduleBytes;
        _pre_decoded_module_data = moduleData;
        name = moduleData.name ?? string.Empty;
        version = moduleData.version;
    }

    /// <summary>
    ///     是否已完全加载所有段
    /// </summary>
    public bool is_fully_loaded => _constants is not null && _functions is not null
                                                          && _imports is not null && _exports is not null;

    /// <summary>
    ///     模块名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     模块版本的
    /// </summary>
    public uint version { get; }

    /// <summary>
    ///     常量池（惰性加载，首次访问时解码）
    /// </summary>
    public IReadOnlyList<Value> constants
    {
        get
        {
            if (_constants is null)
                lock (_lock)
                {
                    _constants ??= decode_constants();
                }

            return _constants;
        }
    }

    /// <summary>
    ///     函数表（惰性加载，首次访问时解码）
    /// </summary>
    public IReadOnlyList<IFunction> functions
    {
        get
        {
            if (_functions is null)
                lock (_lock)
                {
                    _functions ??= decode_functions();
                }

            return _functions;
        }
    }

    /// <summary>
    ///     导入表（惰性加载，首次访问时解码）
    /// </summary>
    public IReadOnlyList<IModuleImport> imports
    {
        get
        {
            if (_imports is null)
                lock (_lock)
                {
                    _imports ??= decode_imports();
                }

            return _imports;
        }
    }

    /// <summary>
    ///     导出表（惰性加载，首次访问时解码）
    /// </summary>
    public IReadOnlyList<IModuleExport> exports
    {
        get
        {
            if (_exports is null)
                lock (_lock)
                {
                    _exports ??= decode_exports();
                }

            return _exports;
        }
    }

    /// <summary>
    ///     原始模块二进制。
    ///     该属性沿用 `raw_bytecode` 命名，但实际承载的是完整 `.nyar` 模块字节。
    /// </summary>
    public byte[]? raw_bytecode
    {
        get => _module_bytes;
        set { }
    }

    /// <summary>
    ///     根据名称查找函数
    /// </summary>
    public IFunction? find_function(string functionName)
    {
        foreach (var func in functions)
            if (func.name == functionName)
                return func;

        return null;
    }

    /// <summary>
    ///     根据名称查找导出
    /// </summary>
    public IModuleExport? find_export(string exportName)
    {
        foreach (var export in exports)
            if (export.name == exportName)
                return export;

        return null;
    }

    /// <summary>
    ///     强制预加载所有段
    /// </summary>
    public void preload_all()
    {
        _ = constants;
        _ = functions;
        _ = imports;
        _ = exports;
    }

    /// <summary>
    ///     预加载函数表和导出表（调用前的最小加载集的
    /// </summary>
    public void preload_for_execution()
    {
        _ = functions;
        _ = exports;
    }

    #region 惰性解码方的

    private NyarModuleData get_or_decode_module_data()
    {
        if (_pre_decoded_module_data is not null) return _pre_decoded_module_data;

        var decoder = new NyarDecoder();
        return decoder.decode(_module_bytes);
    }

    private List<Value> decode_constants()
    {
        var moduleData = get_or_decode_module_data();
        var constants = new List<Value>(moduleData.constants.Count);

        foreach (var constant in moduleData.constants)
            constants.Add(NyarModuleConverter.convert_constant_back(constant));

        return constants;
    }

    private List<NyarFunction> decode_functions()
    {
        var moduleData = get_or_decode_module_data();
        var functions = new List<NyarFunction>(moduleData.functions.Count);

        foreach (var funcData in moduleData.functions)
            functions.Add(new NyarFunction(
                funcData.name,
                funcData.arity,
                funcData.local_count,
                funcData.code_offset,
                funcData.code_length
            ));

        return functions;
    }

    private List<ModuleImport> decode_imports()
    {
        var moduleData = get_or_decode_module_data();
        var imports = new List<ModuleImport>(moduleData.imports.Count);

        foreach (var importData in moduleData.imports)
        {
            var kind = importData.kind switch
            {
                NyarImportKind.function => ImportKind.function,
                NyarImportKind.global => ImportKind.global,
                NyarImportKind.module => ImportKind.module,
                _ => ImportKind.function
            };

            imports.Add(new ModuleImport(importData.module_name, importData.symbol_name, kind));
        }

        return imports;
    }

    private List<ModuleExport> decode_exports()
    {
        var moduleData = get_or_decode_module_data();
        var exports = new List<ModuleExport>(moduleData.exports.Count);

        foreach (var exportData in moduleData.exports)
        {
            var kind = exportData.kind switch
            {
                NyarExportKind.function => ExportKind.function,
                NyarExportKind.global => ExportKind.global,
                _ => ExportKind.function
            };

            exports.Add(new ModuleExport(exportData.symbol_name, kind, exportData.function_index));
        }

        return exports;
    }

    #endregion
}
