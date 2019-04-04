namespace Nyar.Assembler.Backends.Wasm;

internal static class WasmTypeScriptDeclGenerator
{
    public static string generate(GenerateModule module, string moduleName)
    {
        return
            """
            export function instantiate(moduleUrl?: URL | string): Promise<WebAssembly.Instance>;
            export default instantiate;
            """;
    }
}