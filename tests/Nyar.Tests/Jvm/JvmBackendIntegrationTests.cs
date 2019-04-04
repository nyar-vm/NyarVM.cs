using Acorn.Jvm.Data;
using Nyar.Assembler;
using Nyar.Binary.Nyar.Data;
using Nyar.Types;

namespace Nyar.Tests.Jvm;

/// <summary>
/// JVM 后端集成测试。
/// 只覆盖当前后端已承诺支持的 `GenerateModule -> JvmClassFileData` 路径，
/// 避免继续依赖已经废弃的旧测试接口。
/// </summary>
public class JvmBackendIntegrationTests
{
    [Fact]
    public void Compile_EmptyModule_Should_Produce_ValidClassFile()
    {
        var backend = new JvmBackend();
        var module = create_minimal_module("TestModule");

        var result = backend.Compile(module, new CompilationOptions());

        Assert.Equal(".class", result.FileExtension);
        Assert.Equal("classfile", result.MediaType);
        Assert.Equal(0xCAFEBABE, result.Data.Magic);
        Assert.Equal(65, result.Data.MajorVersion);
        Assert.NotEmpty(result.Data.ConstantPool);
    }

    [Fact]
    public void Compile_WithExportedEntry_Should_Generate_JavaMainWrapper()
    {
        var backend = new JvmBackend();
        var module = create_minimal_module("EntryModule");
        module.Exports.Add(new GenerateModuleExport("main", GenerateExportKind.Function, 0));

        var result = backend.Compile(module, new CompilationOptions());
        var classFile = result.Data;

        var hasJavaMain = classFile.Methods.Any(method =>
            get_utf8(classFile, method.NameIndex) == "main" &&
            get_utf8(classFile, method.DescriptorIndex) == "([Ljava/lang/String;)V");

        Assert.True(hasJavaMain);
    }

    [Fact]
    public void Compile_WithStringConstant_Should_AddStringToConstantPool()
    {
        var backend = new JvmBackend();
        var module = create_minimal_module("StringModule");
        var stringIndex = module.Constants.AddString("hello world");
        var func = module.Functions[0];
        func.Instructions.Clear();
        func.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Const(stringIndex, GenerateValueType.String)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Pop));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));

        var result = backend.Compile(module, new CompilationOptions());

        Assert.Contains(result.Data.ConstantPool.OfType<JvmConstantUtf8>(), constant => constant.Value == "hello world");
    }

    [Fact]
    public void Compile_WithEntryFunctionName_Should_UseDistinctOutputName()
    {
        var backend = new JvmBackend();
        var module = new GenerateModule("multi.entry.module");
        var alpha = new GenerateFunction("tests.alpha", "void");
        alpha.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(alpha);
        module.AddExport(new GenerateModuleExport("alpha", GenerateExportKind.Function, 0));

        var beta = new GenerateFunction("tests.beta-case", "void");
        beta.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(beta);
        module.AddExport(new GenerateModuleExport("beta", GenerateExportKind.Function, 1));

        var alphaResult = backend.Compile(module, new CompilationOptions
        {
            EntryFunctionName = "tests.alpha"
        });
        var betaResult = backend.Compile(module, new CompilationOptions
        {
            EntryFunctionName = "tests.beta-case"
        });

        Assert.Equal("alpha", alphaResult.OutputName);
        Assert.Equal("beta_case", betaResult.OutputName);
        Assert.NotEqual(alphaResult.OutputName, betaResult.OutputName);
    }

    [Fact]
    public void Compile_PrintCall_Should_MapTo_PrintStreamMethod()
    {
        var backend = new JvmBackend();
        var module = create_minimal_module("PrintModule");
        var func = module.Functions[0];
        func.Instructions.Clear();
        func.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("hello")));
        func.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef("print", create_function_type([GenerateValueType.String], []))));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));

        var result = backend.Compile(module, new CompilationOptions());
        var methodRefs = result.Data.ConstantPool.OfType<JvmConstantMethodref>().ToList();

        Assert.NotEmpty(methodRefs);
        Assert.Contains(result.Data.ConstantPool.OfType<JvmConstantUtf8>(), constant => constant.Value == "java/io/PrintStream");
        Assert.Contains(result.Data.ConstantPool.OfType<JvmConstantUtf8>(), constant => constant.Value == "print");
    }

    [Fact]
    public void Validate_ModuleWithWitnessEntriesOnly_Should_Succeed()
    {
        var backend = new JvmBackend();
        var module = create_minimal_module("WitnessModule");
        module.AddWitnessEntry(new GenerateWitnessDispatchEntry(
            "Display",
            0,
            "show",
            "User",
            "Display_User_show"));

        var success = backend.Validate(module, out var diagnostics);

        Assert.True(module.HasWitnessEntries);
        Assert.False(module.HasWitnessDispatch);
        Assert.True(success);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_ModuleWithWitnessDispatch_Should_Reject()
    {
        var backend = new JvmBackend();
        var module = create_minimal_module("WitnessDispatchModule");
        module.AddWitnessEntry(new GenerateWitnessDispatchEntry(
            "Display",
            0,
            "show",
            "User",
            "Display_User_show"));

        var func = module.Functions[0];
        func.Instructions.Clear();
        func.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallWitness,
            new GenerateOperand.I32(1),
            new GenerateOperand.I32(0)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));

        var success = backend.Validate(module, out var diagnostics);

        Assert.True(module.HasWitnessDispatch);
        Assert.False(success);
        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Level == DiagnosticLevel.Error &&
            diagnostic.Message.Contains("witness", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_PlainModule_Should_Succeed()
    {
        var backend = new JvmBackend();
        var module = create_minimal_module("ValidModule");

        var success = backend.Validate(module, out var diagnostics);

        Assert.True(success);
        Assert.Empty(diagnostics);
    }

    private static GenerateModule create_minimal_module(string name)
    {
        var module = new GenerateModule(name);
        var func = new GenerateFunction("main", "void");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(func);
        return module;
    }

    private static GenerateFunctionType create_function_type(
        IReadOnlyList<GenerateValueType> parameters,
        IReadOnlyList<GenerateValueType> results)
    {
        return new GenerateFunctionType
        {
            Parameters = parameters,
            Results = results
        };
    }

    private static string get_utf8(JvmClassFileData classFile, ushort index)
    {
        return ((JvmConstantUtf8)classFile.constant_pool[index - 1]).value;
    }
}
