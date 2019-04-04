using System.Text;
using System.Text.RegularExpressions;
using Nyar.Assembler;
using Nyar.Assembler.Backends.Clr;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Text.Valkyrie.AST;

namespace Valkyrie.Tests.CompilerTests;

public sealed class ArrayLiteralClrLoweringTests
{
    [Fact]
    public void BuildClr_ArrayLiteralInferredAsArray_ShouldEmitTypedNewarr()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("array_literal_clr_lowering", "clr-microsoft-unknown-managed",
            "array_literal_clr_lowering.v");
        var targetProfile = new CanonicalTargetRegistry().resolve(plan.canonical_triple);
        var parseResult = compiler.parse_source(
            """
            namespace app;

            [main]
            micro main() -> ExitCode {
                let values: [i32] = [1 as i32, 2 as i32, 3 as i32, 4 as i32]
                if values.length() == 4 as usize {
                    return ExitCode(0 as i32)
                }

                return ExitCode(1 as i32)
            }
            """,
            "array_literal_clr_lowering.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        var lirMain = Assert.Single(lir.module.functions.Where(function =>
            string.Equals(function.name, "main", StringComparison.Ordinal) ||
            function.name.EndsWith(".main", StringComparison.Ordinal)));

        Assert.Contains(lirMain.instructions, instruction =>
            instruction.opcode == NyarHeadCode.new_object &&
            instruction.operands.OfType<GenerateOperand.Str>().Any(operand =>
                string.Equals(operand.value, "[i32]", StringComparison.Ordinal)));
        Assert.Equal(4, lirMain.instructions.Count(instruction =>
            instruction.opcode == NyarHeadCode.set_offset_index &&
            instruction.operands.OfType<GenerateOperand.Str>().Any(operand =>
                string.Equals(operand.value, "i32", StringComparison.Ordinal))));

        var backend = new ClrBackend();
        Assert.True(backend.validate(lir.module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.Message)));

        var clrOutput = backend.compile(lir.module, new CompilationOptions
        {
            generate_msil = true
        });

        Assert.NotNull(clrOutput.data);

        var instructions = collect_clr_instructions(clrOutput.data);
        Assert.Contains(instructions, instruction => instruction.opcode == ClrOpcode.newarr);
        Assert.Equal(4, instructions.Count(instruction => instruction.opcode == ClrOpcode.stelem_i4));

        var msilAsset = Assert.Single(clrOutput.assets.Where(asset =>
            asset.name.EndsWith(".msil", StringComparison.Ordinal)));
        var msil = Encoding.UTF8.GetString(msilAsset.content);
        var mainMethodBody = extract_method_body(msil, "main");

        Assert.Contains("newarr", mainMethodBody, StringComparison.Ordinal);
        Assert.Contains("stelem.i4", mainMethodBody, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Collections.ArrayList", mainMethodBody, StringComparison.Ordinal);
    }

    private static IReadOnlyList<ClrInstruction> collect_clr_instructions(ClrModuleData module)
    {
        return module.methods
            .Concat(module.types.SelectMany(type => type.methods))
            .SelectMany(method => method.instructions)
            .ToArray();
    }

    private static string extract_method_body(string msil, string methodName)
    {
        var pattern = $@"\.method .*?\b{Regex.Escape(methodName)}\([^)]*\)\s*\{{(?<body>[\s\S]*?)^\s*\}}";
        var match = Regex.Match(msil, pattern, RegexOptions.Multiline);
        Assert.True(match.Success, $"未找到方法 `{methodName}` 的 MSIL 文本。");
        return match.Groups["body"].Value;
    }
}
