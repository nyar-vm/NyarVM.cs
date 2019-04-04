using Nyar.VM.NyarVM;
using Nyar.VM.NyarVM.Bytecode;

namespace Nyar.Tests.Command;

/// <summary>
///     test.simple_cli 端到端测试 — 验证 .v 源码通过 legion build 编译为 .nyar 后能被 NyarVM 正确加载和执行
/// </summary>
public sealed class SimpleCliEndToEndTests
{
    /// <summary>
    ///     获取编译产物 .nyar 文件的路径
    /// </summary>
    private static string GetNyarArtifactPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "valkyrie.v", "examples", "test.simple_cli",
            "dist", "nyar-unknown-windows", "test_simple_cli.nyar");
    }

    /// <summary>
    ///     验证 .nyar 文件存在且非空
    /// </summary>
    [Fact]
    public void NyarArtifact_Exists_And_NotEmpty()
    {
        var path = GetNyarArtifactPath();
        Assert.True(File.Exists(path), $"产物文件不存在: {path}");

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 0, "产物文件为空");
    }

    /// <summary>
    ///     验证 .nyar 文件能被 NyarVM 成功加载
    /// </summary>
    [Fact]
    public void Load_NyarArtifact_Into_NyarVM_Success()
    {
        var path = GetNyarArtifactPath();
        var bytecode = File.ReadAllBytes(path);

        var vm = new NyarVm();
        var exception = Record.Exception(() => vm.load(bytecode));

        Assert.Null(exception);
    }

    /// <summary>
    ///     验证解码后的模块中包含 main 函数
    /// </summary>
    [Fact]
    public void DecodedModule_Contains_MainFunction()
    {
        var path = GetNyarArtifactPath();
        var bytecode = File.ReadAllBytes(path);

        var module = NyarModuleConverter.decode(bytecode);

        var functionNames = module.functions.Select(f => f.name).ToList();
        var availableFunctions = string.Join(", ", functionNames);

        Assert.Contains(functionNames, name => name.Contains("main", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     验证解码后的模块可以被 NyarVM 加载并执行 main 函数
    /// </summary>
    [Fact]
    public void Run_MainFunction_In_NyarVM_Success()
    {
        var path = GetNyarArtifactPath();
        var bytecode = File.ReadAllBytes(path);

        var module = NyarModuleConverter.decode(bytecode);
        var mainFunc =
            module.functions.FirstOrDefault(f => f.name.Contains("main", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(mainFunc);

        var vm = new NyarVm();
        vm.load(module);

        var exception = Record.Exception(() =>
        {
            var result = vm.run(module.name, mainFunc.name);
        });

        Assert.Null(exception);
    }
}