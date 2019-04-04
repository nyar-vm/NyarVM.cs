#if false // 跳过：使用 Iris.* 命名空间（不存在于 Sonic.Standard），且 CommandSourceGenerator 未适配
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Commander.Testing;

/// <summary>
/// Iris.SourceGen 编译期命令解析代码生成器测试
/// </summary>
public sealed class SourceGenTests
{
    private const string CommonUsings = @"
using System.Threading;
using System.Threading.Tasks;
using Iris.Attributes;
using Iris.Commanding;
using Iris.Sonic.Core;
using Iris.CLI.Attributes;
using Iris.Localization;
using Iris.Ports;
";

    #region __GetModel 生成测试

    [Fact]
    public async Task ClassWithCommandAttribute_ShouldGenerateGetModel()
    {
        var code = CommonUsings + @"

[Command(""build"", ""构建项目"")]
public partial class BuildCommand : ICommand
{
    public string Name => ""build"";
    public string? Description => ""构建项目"";

    [Option(""--output"", ""输出目录"")]
    public string Output { get; set; } = string.Empty;

    public Task<ExitCode> ExecuteAsync(ICommandContext ctx, CancellationToken cancellation = default)
        => Task.FromResult(ExitCode.success);
}
";

        await VerifyGeneratedSource(code, result =>
        {
            Assert.Contains("__GetModel()", result);
            Assert.Contains("""build""", result);
        });
    }

    #endregion

    #region __Parse 生成测试

    [Fact]
    public async Task ClassWithCommandAttribute_ShouldGenerateParseMethod()
    {
        var code = CommonUsings + @"

[Command(""build"", ""构建项目"")]
public partial class BuildCommand : ICommand
{
    public string Name => ""build"";
    public string? Description => ""构建项目"";

    [Option(""--output"", ""输出目录"")]
    public string Output { get; set; } = string.Empty;

    public Task<ExitCode> ExecuteAsync(ICommandContext ctx, CancellationToken cancellation = default)
        => Task.FromResult(ExitCode.success);
}
";

        await VerifyGeneratedSource(code, result =>
        {
            Assert.Contains("__Parse(", result);
            Assert.Contains("switch (optName)", result);
            Assert.Contains("case \"output\"", result);
        });
    }

    [Fact]
    public async Task ParseMethod_ShouldContainUnknownOptionCheck()
    {
        var code = CommonUsings + @"

[Command(""run"", ""运行"")]
public partial class RunCommand : ICommand
{
    public string Name => ""run"";
    public string? Description => ""运行"";

    [Option(""--mode"", ""模式"")]
    public string Mode { get; set; } = string.Empty;

    public Task<ExitCode> ExecuteAsync(ICommandContext ctx, CancellationToken cancellation = default)
        => Task.FromResult(ExitCode.success);
}
";

        await VerifyGeneratedSource(code, result =>
        {
            Assert.Contains("未知选项", result);
        });
    }

    #endregion

    #region __Completions 生成测试

    [Fact]
    public async Task ClassWithCommandAttribute_ShouldGenerateCompletions()
    {
        var code = CommonUsings + @"

[Command(""list"", ""列出"")]
public partial class ListCommand : ICommand
{
    public string Name => ""list"";
    public string? Description => ""列出"";

    [Option(""--verbose"", ""详细输出"")]
    public bool Verbose { get; set; }

    [Option(""--filter"", ""过滤器"")]
    public string Filter { get; set; } = string.Empty;

    public Task<ExitCode> ExecuteAsync(ICommandContext ctx, CancellationToken cancellation = default)
        => Task.FromResult(ExitCode.success);
}
";

        await VerifyGeneratedSource(code, result =>
        {
            Assert.Contains("__Completions(", result);
            Assert.Contains("""--verbose""", result);
            Assert.Contains("""--filter""", result);
        });
    }

    #endregion

    #region 约束测试

    [Fact]
    public async Task ClassWithRequiredAttribute_ShouldGenerateConstraintCheck()
    {
        var code = CommonUsings + @"

[Command(""copy"", ""复制"")]
public partial class CopyCommand : ICommand
{
    public string Name => ""copy"";
    public string? Description => ""复制"";

    [Option(""--source"", ""源路径"")]
    [Requires(""target"")]
    public string Source { get; set; } = string.Empty;

    [Option(""--target"", ""目标路径"")]
    public string Target { get; set; } = string.Empty;

    public Task<ExitCode> ExecuteAsync(ICommandContext ctx, CancellationToken cancellation = default)
        => Task.FromResult(ExitCode.success);
}
";

        await VerifyGeneratedSource(code, result =>
        {
            Assert.Contains("需要同时提供", result);
        });
    }

    [Fact]
    public async Task ClassWithConflictsWithAttribute_ShouldGenerateConflictCheck()
    {
        var code = CommonUsings + @"

[Command(""mode"", ""模式"")]
public partial class ModeCommand : ICommand
{
    public string Name => ""mode"";
    public string? Description => ""模式"";

    [Option(""--verbose"", ""详细输出"")]
    [ConflictsWith(""quiet"")]
    public bool Verbose { get; set; }

    [Option(""--quiet"", ""安静模式"")]
    public bool Quiet { get; set; }

    public Task<ExitCode> ExecuteAsync(ICommandContext ctx, CancellationToken cancellation = default)
        => Task.FromResult(ExitCode.success);
}
";

        await VerifyGeneratedSource(code, result =>
        {
            Assert.Contains("不能同时使用", result);
        });
    }

    #endregion

    #region 边界测试

    [Fact]
    public async Task ClassWithoutCommandAttribute_ShouldNotGenerateCode()
    {
        var code = CommonUsings + @"

public partial class NoCommandClass : ICommand
{
    public string Name => ""no"";
    public string? Description => null;

    public Task<ExitCode> ExecuteAsync(ICommandContext ctx, CancellationToken cancellation = default)
        => Task.FromResult(ExitCode.success);
}
";

        await VerifyGeneratedSource(code, result =>
        {
            Assert.DoesNotContain("__Parse(", result);
            Assert.DoesNotContain("__GetModel()", result);
        });
    }

    #endregion

    #region 测试基础设施

    private static Task VerifyGeneratedSource(string source, Action<string> assert)
    {
        var compilation = CreateCompilation(source);

        var compileDiagnostics = compilation.GetDiagnostics();
        var errors = compileDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            var errorSummary = string.Join("\n", errors.Select(e => $"{e.Location}: {e.GetMessage()} [{e.Id}]"));
            Assert.Fail($"Compilation errors:\n{errorSummary}");
        }

        var generator = new CommandSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var originalTreeCount = compilation.SyntaxTrees.Count();
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
        var generatedTrees = outputCompilation.SyntaxTrees.Skip(originalTreeCount).ToList();

        if (generatedTrees.Count == 0)
        {
            assert(string.Empty);
        }
        else
        {
            var allGenerated = string.Join("\n\n", generatedTrees.Select(t => t.ToString()));
            assert(allGenerated);
        }

        return Task.CompletedTask;
    }

    private static Compilation CreateCompilation(string source)
    {
        var references = new List<MetadataReference>();

        foreach (var asm in AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!.ToString()!.Split(Path.PathSeparator))
        {
            if (!asm.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = Path.GetFileName(asm);
            if (fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("netstandard.dll", StringComparison.OrdinalIgnoreCase))
            {
                references.Add(MetadataReference.CreateFromFile(asm));
            }
        }

        var irisCore = typeof(Iris.Commanding.ICommand).Assembly;
        references.Add(MetadataReference.CreateFromFile(irisCore.Location));

        var irisCli = typeof(Iris.CLI.CommandParser).Assembly;
        references.Add(MetadataReference.CreateFromFile(irisCli.Location));

        foreach (var refAsm in irisCore.GetReferencedAssemblies())
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(Assembly.Load(refAsm).Location));
            }
            catch
            {
            }
        }

        foreach (var refAsm in irisCli.GetReferencedAssemblies())
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(Assembly.Load(refAsm).Location));
            }
            catch
            {
            }
        }

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable);

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            options);
    }

    #endregion
}

#endif