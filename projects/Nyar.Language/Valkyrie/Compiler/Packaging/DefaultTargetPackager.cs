using System.IO.Compression;
using System.Text;
using Nyar.Assembler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Types.Targets;
using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.Clr.Encode;
using Std.Data.Binary.Jvm.Data;
using Std.Data.Binary.Jvm.Encode;
using Std.Data.Binary.Wasm.Data;
using Std.Data.Binary.Wasm.Encode;

namespace Nyar.Language.Valkyrie.Compiler.Packaging;

/// <summary>
///     默认目标打包器：将后端输出编码为主产物，并补齐启动脚本等 sidecar。
/// </summary>
public sealed class DefaultTargetPackager : ITargetPackager
{
    public ArtifactSet package(
        string moduleName,
        OutputSpec generated,
        TargetProfile targetProfile)
    {
        // 后端偏好的输出名优先（如 CLR 后端使用入口函数名），回退到管线模块名
        var effectiveName = string.IsNullOrWhiteSpace(generated.output_name) ? moduleName : generated.output_name;
        var primaryArtifact = build_primary_artifact(effectiveName, generated, targetProfile);
        var sidecarArtifacts = new List<CompilerArtifact>();

        foreach (var asset in generated.assets)
        {
            if (string.Equals(asset.name, primaryArtifact.name, StringComparison.Ordinal))
            {
                continue;
            }

            sidecarArtifacts.Add(new CompilerArtifact(asset.name, asset.content, asset.media_type));
        }

        add_launcher_artifacts(sidecarArtifacts, effectiveName, targetProfile, primaryArtifact);
        return new ArtifactSet(primaryArtifact, sidecarArtifacts);
    }

    private static CompilerArtifact build_primary_artifact(
        string moduleName,
        OutputSpec generated,
        TargetProfile targetProfile)
    {
        if (generated is OutputSpec<JvmClassFileData> jvmSpec)
        {
            var encoder = new JvmEncoder();
            var classBytes = encoder.encode(jvmSpec.data);
            return new CompilerArtifact($"{moduleName}.class", classBytes, "application/java-vm");
        }

        if (generated is OutputSpec<WasmModuleData> wasmSpec)
        {
            var bytes = WasmEncoder.encode_module(wasmSpec.data);
            return new CompilerArtifact($"{moduleName}.wasm", bytes, "application/wasm");
        }

        if (generated is OutputSpec<ClrModuleData> clrSpec)
        {
            var encoder = new ClrEncoder();
            var bytes = encoder.encode(clrSpec.data);
            return new CompilerArtifact($"{moduleName}{generated.file_extension}", bytes, "application/octet-stream");
        }

        if (generated is OutputSpec<byte[]> bytesSpec)
            return new CompilerArtifact(
                $"{moduleName}{generated.file_extension}",
                bytesSpec.data,
                string.IsNullOrWhiteSpace(generated.media_type) ? "application/octet-stream" : generated.media_type);

        throw new NotSupportedException($"暂不支持为 `{targetProfile.canonical_triple}` 组装主产物：{generated.GetType().Name}");
    }

    private static void add_launcher_artifacts(
        ICollection<CompilerArtifact> sidecarArtifacts,
        string moduleName,
        TargetProfile targetProfile,
        CompilerArtifact primaryArtifact)
    {
        switch (targetProfile.backend_family)
        {
            case TargetBackendFamily.wasm:
            {
                if (targetProfile.host_kind is TargetHostKind.browser or TargetHostKind.java_script)
                {
                    var mjsName = $"{moduleName}.mjs";
                    add_if_missing(sidecarArtifacts,
                        new CompilerArtifact(mjsName, build_wasm_mjs_glue(moduleName, primaryArtifact.name),
                            "application/javascript"));
                }

                break;
            }
            case TargetBackendFamily.jvm:
            {
                var jarName = $"{moduleName}.jar";
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact(jarName, build_jvm_jar(moduleName, primaryArtifact.content),
                        "application/java-archive"));
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.run.ps1", build_jvm_ps1(jarName), "text/plain"));
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.run.sh", build_jvm_sh(jarName), "text/x-shellscript"));
                break;
            }
            case TargetBackendFamily.clr:
            {
                var exeName = $"{moduleName}.exe";
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.runtimeconfig.json", build_clr_runtime_config(),
                        "application/json"));
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.deps.json", build_clr_deps_json(moduleName),
                        "application/json"));
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.pdb", build_clr_portable_pdb_placeholder(),
                        "application/octet-stream"));
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.xml", build_clr_xml_doc(moduleName), "application/xml"));
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.run.ps1", build_clr_ps1(exeName), "text/plain"));
                add_if_missing(sidecarArtifacts,
                    new CompilerArtifact($"{moduleName}.run.sh", build_clr_sh(exeName), "text/x-shellscript"));
                break;
            }
        }
    }

    private static void add_if_missing(ICollection<CompilerArtifact> artifacts, CompilerArtifact artifact)
    {
        if (artifacts.Any(item => string.Equals(item.name, artifact.name, StringComparison.Ordinal))) return;

        artifacts.Add(artifact);
    }

    private static byte[] build_wasm_mjs_glue(string moduleName, string wasmFileName)
    {
        var script = $$"""
                       // VOA WASM 胶水代码 — 自动生成，请勿手动编辑。
                       // 由 loadModule() 动态 import，exports 被合并到 WASM 的 importObject.env。

                       /// <summary>
                       ///     WASM 导入对象，由后端将 [js_builtin] 桥接函数填入此处。
                       ///     键：导入模块名（如 "env"、"wasi_snapshot_preview1"），值：函数映射。
                       /// </summary>
                       const imports = {};

                       /// <summary>
                       ///     WASM 模块加载完成后的回调。
                       /// </summary>
                       /// <param name="exports">WASM 实例导出的函数表</param>
                       /// <param name="memory">WASM 线性内存</param>
                       function onReady(exports, memory) {
                           console.log('VOA WASM 模块 {{moduleName}} 加载完成');
                       }

                       export { imports, onReady };

                       """;
        return Encoding.UTF8.GetBytes(script + Environment.NewLine);
    }

    private static byte[] build_jvm_jar(string mainClassName, byte[] classBytes)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var manifestEntry = archive.CreateEntry("META-INF/MANIFEST.MF");
            using (var writer = new StreamWriter(manifestEntry.Open(), new UTF8Encoding(false)))
            {
                writer.Write("Manifest-Version: 1.0\r\n");
                writer.Write($"Main-Class: {mainClassName}\r\n");
                writer.Write("\r\n");
            }

            var classEntry = archive.CreateEntry($"{mainClassName}.class");
            using var classStream = classEntry.Open();
            classStream.Write(classBytes, 0, classBytes.Length);
        }

        return stream.ToArray();
    }

    private static byte[] build_jvm_ps1(string jarName)
    {
        var script = $"java -jar \"$PSScriptRoot/{jarName}\" @args{Environment.NewLine}";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] build_jvm_sh(string jarName)
    {
        var script = "#!/usr/bin/env sh\nDIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\njava -jar \"$DIR/" + jarName +
                     "\" \"$@\"\n";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] build_clr_ps1(string exeName)
    {
        var script = $"dotnet \"$PSScriptRoot/{exeName}\" @args{Environment.NewLine}";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] build_clr_sh(string exeName)
    {
        var script = "#!/usr/bin/env sh\nDIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\ndotnet \"$DIR/" + exeName +
                     "\" \"$@\"\n";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] build_clr_runtime_config()
    {
        const string json = """
                            {
                              "runtimeOptions": {
                                "tfm": "net10.0",
                                "framework": {
                                  "name": "Microsoft.NETCore.App",
                                  "version": "10.0.0"
                                },
                                "rollForward": "LatestMajor"
                              }
                            }
                            """;
        return Encoding.UTF8.GetBytes(json + Environment.NewLine);
    }

    /// <summary>
    ///     生成最小化 .deps.json 文件，声明对 Microsoft.NETCore.App 框架的依赖，
    ///     使运行时能正确解析 System.Runtime 等框架程序集的版本统一。
    /// </summary>
    private static byte[] build_clr_deps_json(string moduleName)
    {
        var json = $$"""
                     {
                       "runtimeTarget": {
                         "name": ".NETCoreApp,Version=v10.0",
                         "signature": ""
                       },
                       "compilationOptions": {},
                       "targets": {
                         ".NETCoreApp,Version=v10.0": {
                           "{{moduleName}}/1.0.0": {
                             "dependencies": {
                               "Microsoft.NETCore.App": "10.0.6"
                             },
                             "runtime": {
                               "{{moduleName}}.exe": {}
                             }
                           },
                           "Microsoft.NETCore.App/10.0.6": {}
                         }
                       },
                       "libraries": {
                         "Microsoft.NETCore.App/10.0.6": {
                           "type": "framework",
                           "serviceable": false,
                           "sha512": ""
                         }
                       }
                     }
                     """;
        return Encoding.UTF8.GetBytes(json + Environment.NewLine);
    }

    private static byte[] build_clr_portable_pdb_placeholder()
    {
        // 占位调试符号文件：用于补齐 sidecar 产物契约。
        // 后续由真正的 CLR 后端符号发射逻辑替换为可调试的 PDB 内容。
        return [];
    }

    private static byte[] build_clr_xml_doc(string moduleName)
    {
        var xml = $"""
                   <?xml version="1.0" encoding="utf-8"?>
                   <doc>
                     <assembly>
                       <name>{moduleName}</name>
                     </assembly>
                     <members>
                     </members>
                   </doc>
                   """;
        return Encoding.UTF8.GetBytes(xml + Environment.NewLine);
    }
}
