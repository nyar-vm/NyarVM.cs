using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

internal sealed class WasiP1ImportBuilder
{
    public bool collect_required_imports(GenerateModule module)
    {
        return false;
    }

    public void add_imports(
        List<WasmImport> imports,
        List<WasmFunctionType> types,
        Dictionary<(List<WasmValueType>, List<WasmValueType>), uint> typeIndexMap)
    {
    }

    public static uint get_import_function_index(string functionName, List<WasmImport> imports)
    {
        return 0;
    }
}