using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Diagnostics;

namespace Nyar.Tests.Utf8Eq;

public sealed class Utf8EqClrTests
{
    [Fact]
    public void BuildLir_Utf8Eq_ShouldCompileForClr()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
            namespace utf8_eq_test;
            
            using std.io;
            
            [main]
            micro main() -> unit {
                let a: utf8 = "hello";
                let b: utf8 = "hello";
            
                if a == b {
                    std.io.print_line("PASS: utf8 texts are equal");
                }
                else {
                    std.io.print_line("FAIL: utf8 texts should be equal");
                }
            }
            """;
        var plan = new BuildPlan("utf8_eq_test", "clr-microsoft-unknown-managed", "test_utf8_eq.v");
        
        var parseResult = compiler.parse_source(source, "test_utf8_eq.v");
        Assert.NotNull(parseResult.value);
        
        var diag = compiler.diagnostics;
        if (diag.has_errors)
        {
            var errors = string.Join("\n", diag.messages
                .Where(d => d.severity == DiagnosticSeverity.error)
                .Select(d => d.message));
            Assert.Fail($"解析错误:\n{errors}");
        }

        var stagedAst = new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        if (semantics.has_errors)
        {
            Assert.Fail($"语义错误:\n{string.Join("\n", semantics.diagnostics.Select(d => d.message))}");
        }

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var targetProfile = new CanonicalTargetRegistry().resolve(plan.canonical_triple);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lirResult = compiler.build_lir(mir, plan);
        
        Assert.NotEmpty(lirResult.module.functions);
        var mainFunc = lirResult.module.functions
            .FirstOrDefault(f => f.name == "utf8_eq_test.main");
        Assert.NotNull(mainFunc);
        
        // 验证 CLR 后端可以编译
        var clrBackend = new ClrBackend();
        Assert.True(clrBackend.validate(lirResult.module, out var clrDiagnostics),
            string.Join("\n", clrDiagnostics.Select(d => d.ToString())));
    }
}
