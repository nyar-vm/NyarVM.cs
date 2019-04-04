namespace Nyar.Assembler.Backends.Wasm;

internal static class WasmSourceMapGenerator
{
    public static string generate(string wasmFileName, GenerateModule module)
    {
        return $$"""
                 {
                   "version": 3,
                   "file": "{{wasmFileName}}",
                   "sources": ["{{module.name}}"],
                   "names": [],
                   "mappings": ""
                 }
                 """;
    }
}