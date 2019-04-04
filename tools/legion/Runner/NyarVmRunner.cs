using Nyar.Types;
using Nyar.VM.NyarVM;

namespace Legion.CLI.Runner;

/// <summary>
///     NyarVM 进程内 Runner，直接在进程中加载 .nyar 字节码并执行
/// </summary>
public sealed class NyarVmRunner : IRunner
{
    #region IRunner 实现

    /// <summary>
    ///     检查 Runner 是否可用（进程内执行，无外部依赖，始终可用）
    /// </summary>
    /// <returns>始终返回 true</returns>
    public bool is_available()
    {
        return true;
    }

    /// <summary>
    ///     加载 .nyar 字节码产物并在进程中执行
    /// </summary>
    /// <param name="artifactPath">产物文件路径（.nyar 字节码文件）</param>
    /// <param name="entryPoint">入口函数名，为 null 时默认使用 "main"</param>
    /// <returns>执行结果，包含退出码、标准输出和错误输出</returns>
    public ExternalRunResult run(string artifactPath, string? entryPoint)
    {
        var resolvedEntryPoint = entryPoint ?? "main";
        var moduleName = Path.GetFileNameWithoutExtension(artifactPath);

        byte[] bytecode;
        try
        {
            bytecode = File.ReadAllBytes(artifactPath);
        }
        catch (IOException ex)
        {
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = $"读取产物文件失败：{ex.Message}"
            };
        }

        var vm = new NyarVm();

        try
        {
            vm.load(bytecode);
        }
        catch (NyarRuntimeException ex)
        {
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = $"加载字节码失败：{ex.Message}"
            };
        }

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var resultValue = vm.run(moduleName, resolvedEntryPoint);
            Console.SetOut(originalOut);

            return new ExternalRunResult
            {
                exit_code = 0,
                stdout = stringWriter.ToString()
            };
        }
        catch (NyarRuntimeException ex)
        {
            Console.SetOut(originalOut);
            var error = $"执行错误：{ex.Message}";
            if (ex.Message.Contains("函数未找的", StringComparison.Ordinal))
            {
                error = $"{error}{Environment.NewLine}{describe_module_functions(vm, moduleName)}";
            }

            return new ExternalRunResult
            {
                exit_code = 1,
                stdout = stringWriter.ToString(),
                stderr = error
            };
        }
        catch (Exception ex)
        {
            Console.SetOut(originalOut);
            var error = $"未预期的错误：{ex.Message}";
            var stackSnapshot = describe_frame_snapshots(vm);
            if (!string.IsNullOrWhiteSpace(stackSnapshot))
            {
                error = $"{error}{Environment.NewLine}{stackSnapshot}";
            }

            return new ExternalRunResult
            {
                exit_code = 1,
                stdout = stringWriter.ToString(),
                stderr = error
            };
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     输出模块函数与导出列表，帮助定位测试入口名不匹配问题。
    /// </summary>
    /// <param name="vm">运行时 VM</param>
    /// <param name="moduleName">模块名称</param>
    /// <returns>模块摘要</returns>
    private static string describe_module_functions(NyarVm vm, string moduleName)
    {
        if (vm.get_module(moduleName) is not NyarModule module)
        {
            return $"模块 `{moduleName}` 未加载为 `NyarModule`，无法枚举函数。";
        }

        var functionNames = module.functions
            .Select(function => function.name)
            .OrderBy(name => name, StringComparer.Ordinal);
        var exportNames = module.exports
            .Select(exportItem => exportItem.name)
            .OrderBy(name => name, StringComparer.Ordinal);

        return
            $"模块函数：{string.Join(", ", functionNames)}{Environment.NewLine}模块导出：{string.Join(", ", exportNames)}";
    }

    /// <summary>
    ///     输出当前 VM 的帧栈快照，帮助定位自递归与错误分派。
    /// </summary>
    /// <param name="vm">运行时 VM</param>
    /// <returns>帧栈摘要</returns>
    private static string describe_frame_snapshots(NyarVm vm)
    {
        var frames = vm.get_frame_snapshots();
        if (frames.Count == 0)
        {
            return string.Empty;
        }

        var frameDescriptions = frames
            .Take(12)
            .Select(frame => $"{frame.function_name}@pc={frame.pc}")
            .ToArray();

        return $"帧栈快照：{string.Join(" -> ", frameDescriptions)}";
    }

    #endregion
}
