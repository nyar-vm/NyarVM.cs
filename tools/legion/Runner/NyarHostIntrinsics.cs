using Legion.CLI.Commands;
using Nyar.Types;
using Nyar.VM.NyarVM;

namespace Legion.CLI.Runner;

/// <summary>
///     NyarVM 宿主 intrinsic 注册器。
///     为 nyar target 的字节码产物提供进程内宿主能力，包括标准 IO、文件系统和编译器委托。
/// </summary>
public static class NyarHostIntrinsics
{
    /// <summary>
    ///     向 NyarVm 注册全部宿主 intrinsic 模块
    /// </summary>
    /// <param name="vm">目标 NyarVm 实例。</param>
    /// <param name="workspaceRoot">工作区根目录，用于解析相对路径和编译委托。</param>
    public static void register(NyarVm vm, string workspaceRoot)
    {
        register_io(vm);
        register_fs(vm);
        register_host(vm, workspaceRoot);
    }

    /// <summary>
    ///     注册标准 IO 模块（io.println、io.print）
    /// </summary>
    private static void register_io(NyarVm vm)
    {
        var ioModule = new NyarModule("io");

        ioModule.native_functions.Add(new NyarNativeFunction("println", "io", args =>
        {
            var text = extract_string(args, 0);
            Console.WriteLine(text);
            return Value.@null;
        }));

        ioModule.native_functions.Add(new NyarNativeFunction("print", "io", args =>
        {
            var text = extract_string(args, 0);
            Console.Write(text);
            return Value.@null;
        }));

        ioModule.native_functions.Add(new NyarNativeFunction("eprintln", "io", args =>
        {
            var text = extract_string(args, 0);
            Console.Error.WriteLine(text);
            return Value.@null;
        }));

        vm.intrinsics.register_module(ioModule);
    }

    /// <summary>
    ///     注册文件系统模块（fs.file_exists、fs.read_file、fs.write_file）
    /// </summary>
    private static void register_fs(NyarVm vm)
    {
        var fsModule = new NyarModule("fs");

        fsModule.native_functions.Add(new NyarNativeFunction("file_exists", "fs", args =>
        {
            var path = extract_string(args, 0);
            return Value.from_bool(File.Exists(path));
        }));

        fsModule.native_functions.Add(new NyarNativeFunction("read_file", "fs", args =>
        {
            var path = extract_string(args, 0);
            try
            {
                var content = File.ReadAllText(path);
                return Value.from_string(content);
            }
            catch (IOException)
            {
                return Value.@null;
            }
        }));

        fsModule.native_functions.Add(new NyarNativeFunction("write_file", "fs", args =>
        {
            var path = extract_string(args, 0);
            var content = extract_string(args, 1);
            try
            {
                File.WriteAllText(path, content);
                return Value.from_bool(true);
            }
            catch (IOException)
            {
                return Value.from_bool(false);
            }
        }));

        vm.intrinsics.register_module(fsModule);
    }

    /// <summary>
    ///     注册宿主编译器委托模块（host.build_project）
    /// </summary>
    private static void register_host(NyarVm vm, string workspaceRoot)
    {
        var hostModule = new NyarModule("host");

        hostModule.native_functions.Add(new NyarNativeFunction("build_project", "host", args =>
        {
            // 参数：project_path, target, output_dir
            var projectPath = extract_string(args, 0);
            var target = extract_string(args, 1);
            var outputDir = extract_string(args, 2);

            // 解析为绝对路径
            var fullProjectPath = Path.IsPathRooted(projectPath)
                ? projectPath
                : Path.Combine(workspaceRoot, projectPath);
            var fullOutputDir = Path.IsPathRooted(outputDir)
                ? outputDir
                : Path.Combine(workspaceRoot, outputDir);

            // 委托给 LegionHelper 执行编译
            var contexts = LegionHelper.build_matrix.build_contexts(fullProjectPath, target, null, false);
            if (contexts.Count == 0)
            {
                Console.Error.WriteLine($"[host.build_project] 无法为目标 '{target}' 创建构建上下文");
                return Value.from_bool(false);
            }

            foreach (var ctx in contexts)
            {
                ctx.output_dir = fullOutputDir;
                var result = LegionHelper.legion_compiler.build(ctx);
                if (!result.success)
                {
                    Console.Error.WriteLine($"[host.build_project] 编译失败：{result.error}");
                    return Value.from_bool(false);
                }
            }

            return Value.from_bool(true);
        }));

        vm.intrinsics.register_module(hostModule);
    }

    /// <summary>
    ///     从参数数组中提取字符串
    /// </summary>
    private static string extract_string(Value[] args, int index)
    {
        if (index < 0 || index >= args.Length) return string.Empty;
        var value = args[index];
        return value.utf8 as string ?? value.@object as string ?? value.ToString() ?? string.Empty;
    }
}
