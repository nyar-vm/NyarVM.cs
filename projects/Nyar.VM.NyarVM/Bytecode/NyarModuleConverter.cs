using System.Numerics;
using Nyar.Types;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;
using Std.Data.Binary.NyarIR.Encode;
using AcornNyarFunction = Std.Data.Binary.NyarIR.Data.NyarFunction;
using NyarNyarFunction = Nyar.Types.NyarFunction;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM.Bytecode;

/// <summary>
///     `NyarModule` 与 `NyarModuleData` 之间的双向转换器。
///     它只负责模块二进制与代码字节流的编解码，不直接构造解码后的指令对象。
/// </summary>
public static class NyarModuleConverter
{
    /// <summary>
    ///     将运行时模块编码为 `.nyar` 模块二进制。
    /// </summary>
    public static byte[] encode(NyarModule module)
    {
        var moduleData = to_module_data(module);
        var encoder = new NyarEncoder();
        return encoder.encode(moduleData);
    }

    /// <summary>
    ///     将 `.nyar` 模块二进制解码为运行时模块。
    ///     其中函数仍只持有代码区范围，具体指令需在执行阶段再从代码字节流解码。
    /// </summary>
    public static NyarModule decode(byte[] moduleBytes)
    {
        var decoder = new NyarDecoder();
        var moduleData = decoder.decode(moduleBytes);
        return from_module_data(moduleData);
    }

    /// <summary>
    ///     将运行时模块转换为二进制数据模型。
    /// </summary>
    public static NyarModuleData to_module_data(NyarModule module)
    {
        var constants = new List<NyarConstant>();

        foreach (var value in module.constants) constants.Add(convert_constant(value));

        var functions = new List<AcornNyarFunction>();

        foreach (var func in module.functions)
            functions.Add(new AcornNyarFunction(func.name, func.arity, func.local_count,
                func.code_offset, func.code_length));

        var imports = new List<NyarImport>();

        foreach (var import in module.imports)
            imports.Add(new NyarImport(convert_import_kind(import.kind), import.module_name,
                import.symbol_name));

        var exports = new List<NyarExport>();

        foreach (var export in module.exports)
            exports.Add(new NyarExport(convert_export_kind(export.kind), export.name,
                export.function_index));

        var witnessEntries = new List<NyarWitnessDispatchEntry>();

        foreach (var entry in module.witness_entries)
            witnessEntries.Add(new NyarWitnessDispatchEntry
            {
                method_id = entry.method_id,
                type_id = entry.type_id,
                method_name = entry.method_name,
                function_index = entry.function_index,
                interface_id = entry.interface_id,
                interface_method_index = entry.interface_method_index
            });

        return new NyarModuleData
        {
            version = module.version,
            name = module.name,
            constants = constants,
            functions = functions,
            imports = imports,
            exports = exports,
            witness_entries = witnessEntries,
            code_bytes = module.raw_bytecode
        };
    }

    /// <summary>
    ///     将二进制数据模型转换为运行时模块。
    /// </summary>
    public static NyarModule from_module_data(NyarModuleData moduleData)
    {
        var module = new NyarModule(moduleData.name)
        {
            version = moduleData.version
        };

        foreach (var constant in moduleData.constants) module.constants.Add(convert_constant_back(constant));

        foreach (var func in moduleData.functions)
        {
            var nyarFunc =
                new NyarNyarFunction(func.name, func.arity, func.local_count, func.code_offset, func.code_length)
                {
                    module = module
                };
            module.functions.Add(nyarFunc);
        }

        foreach (var import in moduleData.imports)
            module.imports.Add(new ModuleImport(import.module_name, import.symbol_name,
                convert_import_kind_back(import.kind)));

        foreach (var export in moduleData.exports)
            module.exports.Add(new ModuleExport(export.symbol_name, convert_export_kind_back(export.kind),
                export.function_index));

        foreach (var entry in moduleData.witness_entries)
            module.witness_entries.Add(new WitnessDispatchEntry(
                entry.method_id,
                entry.type_id,
                entry.method_name,
                entry.function_index,
                entry.interface_id,
                entry.interface_method_index));

        module.raw_bytecode = moduleData.code_bytes;

        return module;
    }

    #region 常量转换

    private static NyarConstant convert_constant(Value value)
    {
        switch (value.type)
        {
            case ValueType.i32:
                return new NyarConstant(NyarConstantKind.integer32, value.i32);

            case ValueType.i64:
                return new NyarConstant(NyarConstantKind.integer32, (int)value.i64);

            case ValueType.f64:
                return new NyarConstant(NyarConstantKind.float64, value.f64);

            case ValueType.@bool:
                return new NyarConstant(NyarConstantKind.boolean, value.@bool);

            case ValueType.@null:
                return new NyarConstant(NyarConstantKind.@null, null);

            case ValueType.utf8:
                return new NyarConstant(NyarConstantKind.@string, value.utf8 as string ?? "");

            case ValueType.big_int:
            {
                var bigIntObj = value.big_int;
                if (bigIntObj is BigInteger bigInt)
                {
                    var isNegative = bigInt.Sign < 0;
                    var absValue = BigInteger.Abs(bigInt);
                    var magnitude = absValue.ToByteArray();
                    var result = new byte[magnitude.Length + 1];
                    result[0] = (byte)(isNegative ? 1 : 0);
                    Array.Copy(magnitude, 0, result, 1, magnitude.Length);
                    return new NyarConstant(NyarConstantKind.big_int, result);
                }

                return new NyarConstant(NyarConstantKind.@null, null);
            }

            default:
                return new NyarConstant(NyarConstantKind.@null, null);
        }
    }

    public static Value convert_constant_back(NyarConstant constant)
    {
        switch (constant.kind)
        {
            case NyarConstantKind.integer32:
                return Value.from_int((int)(constant.payload ?? 0));

            case NyarConstantKind.float64:
                return Value.from_double((double)(constant.payload ?? 0.0));

            case NyarConstantKind.boolean:
                return Value.from_bool((bool)(constant.payload ?? false));

            case NyarConstantKind.@null:
                return Value.@null;

            case NyarConstantKind.@string:
                return Value.from_string((string)(constant.payload ?? ""));

            case NyarConstantKind.big_int:
                return convert_big_int_back(constant.payload as byte[]);

            default:
                return Value.@null;
        }
    }

    private static Value convert_big_int_back(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0) return Value.from_big_int(BigInteger.Zero);

        var isNegative = bytes[0] != 0;
        var magnitude = new byte[bytes.Length - 1];
        Array.Copy(bytes, 1, magnitude, 0, magnitude.Length);
        var bigInt = new BigInteger(magnitude);
        if (isNegative) bigInt = -bigInt;

        return Value.from_big_int(bigInt);
    }

    #endregion

    #region 导入/导出类型转换

    private static NyarImportKind convert_import_kind(ImportKind kind)
    {
        return kind switch
        {
            ImportKind.function => NyarImportKind.function,
            ImportKind.global => NyarImportKind.global,
            ImportKind.module => NyarImportKind.module,
            _ => NyarImportKind.function
        };
    }

    private static ImportKind convert_import_kind_back(NyarImportKind kind)
    {
        return kind switch
        {
            NyarImportKind.function => ImportKind.function,
            NyarImportKind.global => ImportKind.global,
            NyarImportKind.module => ImportKind.module,
            _ => ImportKind.function
        };
    }

    private static NyarExportKind convert_export_kind(ExportKind kind)
    {
        return kind switch
        {
            ExportKind.function => NyarExportKind.function,
            ExportKind.global => NyarExportKind.global,
            _ => NyarExportKind.function
        };
    }

    private static ExportKind convert_export_kind_back(NyarExportKind kind)
    {
        return kind switch
        {
            NyarExportKind.function => ExportKind.function,
            NyarExportKind.global => ExportKind.global,
            _ => ExportKind.function
        };
    }

    #endregion
}
