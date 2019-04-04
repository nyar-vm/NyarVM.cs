using Nyar.Assembler;
using Nyar.Assembler.Backends.Clr;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Binary.Clr.Data;

namespace Valkyrie.Tests.CompilerTests;

public sealed class ClrArrayLiteralLoweringTests
{
    [Fact]
    public void BuildClr_ArrayLiteral_ShouldEmitNewarrAndTypedElementStores()
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("clr_array_literal", "clr-microsoft-unknown-managed", "clr_array_literal.v");
        var targetProfile = new CanonicalTargetRegistry().resolve("clr-microsoft-unknown-managed");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            [main]
            micro main() -> ExitCode {
                let values: [i32] = [1 as i32, 2 as i32, 3 as i32, 4 as i32]
                return ExitCode(0 as i32)
            }
            """,
            "clr_array_literal.v");

        Assert.NotNull(parseResult.value);
        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        var mainFunction = Assert.Single(lir.module.functions,
            function => function.name is "main" or "app.main");
        Assert.Contains(mainFunction.instructions,
            instruction => instruction.opcode == Std.Data.Binary.NyarIR.Data.NyarHeadCode.new_object
                           && instruction.operands.Count > 0
                           && instruction.operands[0] is GenerateOperand.Str { value: "[i32]" });
        Assert.Contains(mainFunction.instructions,
            instruction => instruction.opcode == Std.Data.Binary.NyarIR.Data.NyarHeadCode.set_offset_index
                           && instruction.operands.Count > 0
                           && instruction.operands[^1] is GenerateOperand.Str { value: "i32" });

        var backend = new ClrBackend();
        Assert.True(backend.validate(lir.module, out var diagnostics),
            string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.ToString())));

        var output = backend.compile(lir.module, new CompilationOptions());
        Assert.NotNull(output.data);

        var emittedMethods = output.data.types.SelectMany(type => type.methods).ToList();
        var emittedMain = Assert.Single(emittedMethods,
            method => method.name is "main" or "app.main");
        Assert.Contains(emittedMain.instructions, instruction => instruction.opcode == ClrOpcode.newarr);
        Assert.Contains(emittedMain.instructions, instruction => instruction.opcode == ClrOpcode.stelem_i4);
        Assert.DoesNotContain(emittedMain.instructions, instruction => instruction.opcode == ClrOpcode.stelem_ref);
    }
}
