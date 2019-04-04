using System.Text;
using Nyar.Types.Externals;
using Std.Data.Binary.Wasm;

namespace Nyar.Assembler.Backends.Wasm;

internal static class WasmJsGlueGenerator
{
    /// <summary>
    ///     已内置实现的 WASM 导入字段名（如 console_log）。不生成桩函数，避免覆盖真实实现。
    /// </summary>
    private static readonly HashSet<string> _builtin_import_fields = new(StringComparer.Ordinal)
    {
        "console_log",
        "memory"
    };

    /// <summary>
    ///     生成 WASM JS 胶水代码。
    /// </summary>
    /// <param name="module">元编译模块，用于收集导入函数信息</param>
    /// <param name="outputName">输出文件名（不含扩展名），用于 WASM 模块 URL 引用</param>
    /// <returns>JS 胶水代码文本</returns>
    public static string generate(GenerateModule module, string outputName)
    {
        var moduleName = module.name;

        // 收集 [wasm] 外部导入函数，按模块分组
        var importModules =
            new Dictionary<string, List<(string Field, GenerateFunction Function)>>(StringComparer.Ordinal);
        foreach (var function in module.functions)
            if (function.try_get_external_import_link(CallingConvention.wasm, out var externalImportLink) &&
                externalImportLink is ExternalWasmFunctionImport wasmImportLink &&
                wasmImportLink.wasm_type is WasmFuncTypeRef funcTypeRef)
            {
                var importModule = funcTypeRef.module;
                var field = funcTypeRef.field;
                if (!importModules.TryGetValue(importModule, out var fields))
                {
                    fields = [];
                    importModules[importModule] = fields;
                }

                fields.Add((field, function));
            }

        // 生成 env 对象中的额外导入桩（跳过已内置实现的字段）
        var envStubsBuilder = new StringBuilder();
        foreach (var (modName, fields) in importModules)
            // env 模块的导入直接合入 env 对象
            if (string.Equals(modName, "env", StringComparison.OrdinalIgnoreCase))
                foreach (var (field, _) in fields)
                {
                    // 跳过已内置实现的导入（如 console_log、memory），避免覆盖真实实现
                    if (_builtin_import_fields.Contains(field)) continue;

                    envStubsBuilder.AppendLine(
                        $"        {field}(...args) {{");
                    envStubsBuilder.AppendLine(
                        $"            console.warn('未实现的 WASM 导入: env.{field}');");
                    envStubsBuilder.AppendLine(
                        "        },");
                }

        var envStubs = envStubsBuilder.ToString();

        // 生成非 env 模块的顶级导入
        var topLevelStubsBuilder = new StringBuilder();
        foreach (var (modName, fields) in importModules)
        {
            if (string.Equals(modName, "env", StringComparison.OrdinalIgnoreCase)) continue;

            topLevelStubsBuilder.AppendLine($"        {modName}: {{");
            foreach (var (field, _) in fields)
            {
                topLevelStubsBuilder.AppendLine($"            {field}(...args) {{");
                topLevelStubsBuilder.AppendLine(
                    $"                console.warn('未实现的 WASM 导入: {modName}.{field}');");
                topLevelStubsBuilder.AppendLine("            },");
            }

            topLevelStubsBuilder.AppendLine("        },");
        }

        var topLevelStubs = topLevelStubsBuilder.ToString();

        return
            $$"""
              const decoder = new TextDecoder("utf-8");

              function createImports(memory) {
                  const env = {
                      memory,
                      console_log(ptr) {
                          const memoryView = new Uint8Array(memory.buffer);
                          let end = ptr;
                          while (end < memoryView.length && memoryView[end] !== 0) {
                              end++;
                          }

                          const bytes = memoryView.subarray(ptr, end);
                          console.log(decoder.decode(bytes));
                      },
              {{envStubs}}    };

                  return { env{{topLevelStubs}} };
              }

              function resolveModuleUrl(moduleUrl) {
                  if (moduleUrl instanceof URL) {
                      return moduleUrl;
                  }

                  try {
                      return new URL(moduleUrl);
                  } catch {
                      return new URL(moduleUrl, import.meta.url);
                  }
              }

              async function loadModuleBytes(moduleUrl) {
                  const resolvedUrl = resolveModuleUrl(moduleUrl);

                  if (resolvedUrl.protocol === "file:") {
                      const { readFile } = await import("node:fs/promises");
                      return await readFile(resolvedUrl);
                  }

                  const response = await fetch(resolvedUrl);
                  if (!response.ok) {
                      throw new Error(`Failed to load WASM module: ${response.status} ${response.statusText}`);
                  }

                  return new Uint8Array(await response.arrayBuffer());
              }

              // VOA 运行时兼容接口：loadModule() 会将 glue.imports 合并到 WASM 的 importObject.env 中。
              // 注意：voa-runtime.js 已自行提供 memory 和 console_log，此处为空即可。
              const imports = {};

              // VOA 运行时兼容接口：WASM 模块加载完成后由 loadModule() 回调。
              function onReady(exports, memory) {
                  console.log('VOA WASM 模块 {{moduleName}} 加载完成');
              }

              export { imports, onReady };

              export async function instantiate(moduleUrl = new URL("./{{outputName}}.wasm", import.meta.url)) {
                  const memory = new WebAssembly.Memory({ initial: 1 });
                  const imports = createImports(memory);
                  const bytes = await loadModuleBytes(moduleUrl);
                  const { instance } = await WebAssembly.instantiate(bytes, imports);
                  return instance;
              }

              export async function run(moduleUrl = new URL("./{{outputName}}.wasm", import.meta.url)) {
                  const instance = await instantiate(moduleUrl);
                  const entry = instance.exports._start ?? instance.exports.main;
                  if (typeof entry === "function") {
                      entry();
                  }

                  return instance;
              }

              async function isDirectNodeExecution() {
                  if (typeof process === "undefined" || !Array.isArray(process.argv) || process.argv.length < 2) {
                      return false;
                  }

                  const { pathToFileURL } = await import("node:url");
                  return import.meta.url === pathToFileURL(process.argv[1]).href;
              }

              if (await isDirectNodeExecution()) {
                  await run();
              }

              export default instantiate;
              """;
    }
}
