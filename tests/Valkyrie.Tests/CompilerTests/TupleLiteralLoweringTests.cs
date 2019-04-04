using Nyar.Assembler;
using Nyar.Assembler.Backends.Clr;
using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Binary.NyarIR.Data;

namespace Valkyrie.Tests.CompilerTests;

public sealed class TupleLiteralLoweringTests
{
    [Fact]
    public void BuildMir_TupleLiteral_ShouldLowerToAnonymousValueObject()
    {
        var mir = build_mir(
            """
            namespace app;

            micro main() -> i32 {
                let pair = (1 as i32, 2 as i32)
                return 0 as i32
            }
            """,
            "tuple_literal_lowering.v");

        var nodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(nodes, node => node is AlgebraNode.NewObject { type_name: "__tuple_arity2", field_count: 2 });
        Assert.Contains(nodes, node => node is AlgebraNode.SetField { field_name: "_0" });
        Assert.Contains(nodes, node => node is AlgebraNode.SetField { field_name: "_1" });
        Assert.DoesNotContain(nodes, node => node is AlgebraNode.ArrayLiteral);
    }

    [Fact]
    public void BuildClr_TupleLiteral_ShouldEmitAnonymousObjectConstruction()
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("tuple_literal", "clr-microsoft-unknown-managed", "tuple_literal.v");
        var targetProfile = new CanonicalTargetRegistry().resolve("clr-microsoft-unknown-managed");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            micro main() -> i32 {
                let pair = (1 as i32, 2 as i32)
                return 0 as i32
            }
            """,
            "tuple_literal.v");

        Assert.NotNull(parseResult.value);
        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);
        var mainFunction = Assert.Single(lir.module.functions);

        Assert.Contains(mainFunction.instructions,
            instruction => instruction.opcode == NyarHeadCode.new_object);
        Assert.Contains(mainFunction.instructions,
            instruction => instruction.opcode == NyarHeadCode.set_field);
        Assert.DoesNotContain(mainFunction.instructions,
            instruction => instruction.opcode == NyarHeadCode.set_offset_index);

        var backend = new ClrBackend();
        Assert.True(backend.validate(lir.module, out var diagnostics),
            string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.ToString())));

        var output = backend.compile(lir.module, new CompilationOptions());
        Assert.NotNull(output.data);
    }

    [Fact]
    public void BuildMir_TupleOrdinalAccess_ShouldLowerToOrdinalIndex()
    {
        var mir = build_mir(
            """
            namespace app;

            micro pick(pair: (ordinal: usize, value: i32)) -> i32 {
                return pair.2
            }
            """,
            "tuple_ordinal_access_lowering.v");

        var nodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(nodes, node => node is GetOrdinalIdx);
        Assert.DoesNotContain(nodes, node => node is AlgebraNode.GetField);
    }

    [Fact]
    public void BuildMir_TupleOrdinalAssignment_ShouldLowerToOrdinalStore()
    {
        var mir = build_mir(
            """
            namespace app;

            micro set_second(pair: (ordinal: usize, value: i32)) -> Unit {
                pair.2 = 42
            }
            """,
            "tuple_ordinal_assign_lowering.v");

        var nodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(nodes, node => node is SetOrdinalIdx);
        Assert.DoesNotContain(nodes, node => node is AlgebraNode.SetField);
    }

    [Fact]
    public void BuildMir_MatchTuplePattern_ShouldLowerToOrdinalIndex()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(value: (i32, i32)) -> Unit {
                match value {
                    case (left, right):
                        return
                }
            }
            """,
            "match_tuple_lowering.v");

        var nodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(mirNodes, node => node is GetOrdinalIdx);
        Assert.DoesNotContain(mirNodes, node => node is AlgebraNode.GetField { field_name: "_0" or "_1" });
    }

    [Fact]
    public void BuildMir_LetTuplePattern_ShouldLowerToDestructuring()
    {
        var mir = build_mir(
            """
            namespace app;

            micro main() -> i32 {
                let (x, y) = (1 as i32, 2 as i32)
                return x + y
            }
            """,
            "let_tuple_pattern_lowering.v");

        var nodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        // 应包含临时变量声明、x 声明和 y 声明
        Assert.Contains(nodes, node => node is AlgebraNode.VarDecl { name: var name } && name.StartsWith("__destructure_tmp_"));
        Assert.Contains(nodes, node => node is AlgebraNode.VarDecl { name: "x" });
        Assert.Contains(nodes, node => node is AlgebraNode.VarDecl { name: "y" });
        
        // 应包含两次序号索引访问
        var ordinalAccesses = nodes.OfType<GetOrdinalIdx>().ToArray();
        Assert.Equal(2, ordinalAccesses.Length);
    }

    [Fact]
    public void BuildHirMirLir_InterpolationLikeString_ShouldFlowAsPlainUtf8Literal()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
            namespace string_pipeline;

            [main]
            micro main() -> Unit {
                let msg = "slot={slot + 1}";
            }
            """;
        var plan = new BuildPlan("string_pipeline", "jvm-openjdk-linux-managed", "string_pipeline.v");
        var parseResult = compiler.parse_source(source, "string_pipeline.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors,
            string.Join(Environment.NewLine, compiler.diagnostics.messages.Select(d => d.message)));

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(d => d.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var hirMain = get_hir_entry(hir);
        var hirLet = Assert.IsType<DeclareLet>(Assert.Single(hirMain.body!.statements));
        var hirLiteral = Assert.IsType<TermLiteralTextNode>(hirLet.initializer);
        Assert.Equal("slot={slot + 1}", hirLiteral.value);

        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var mirNodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();
        Assert.Contains(mirNodes, node => node is AlgebraNode.StringConstant { value: "slot={slot + 1}" });

        var lir = compiler.build_lir(mir, plan);
        var lirMain = get_lir_entry(lir.module);
        Assert.Contains(lirMain.instructions, instruction =>
            instruction.operands
                .OfType<GenerateOperand.Str>()
                .Any(operand => operand.value == "slot={slot + 1}"));
    }

    [Fact]
    public void BuildHirMirLir_RawInterpolatedString_ShouldLowerToToStringAndUtf8Concat()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
            namespace raw_string_pipeline;

            [main]
            micro main(slot: i32) -> Unit {
                let msg = r"{slot}";
            }
            """;
        var plan = new BuildPlan("raw_string_pipeline", "jvm-openjdk-linux-managed", "raw_string_pipeline.v");
        var parseResult = compiler.parse_source(source, "raw_string_pipeline.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors,
            string.Join(Environment.NewLine, compiler.diagnostics.messages.Select(d => d.message)));

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(d => d.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var hirMain = get_hir_entry(hir);
        var hirLet = Assert.IsType<DeclareLet>(Assert.Single(hirMain.body!.statements));
        var hirConcat = Assert.IsType<TermDotExpression>(hirLet.initializer);
        Assert.Equal("infix +", hirConcat.callee.name);
        Assert.NotNull(hirConcat.call_body);

        var hirLeft = Assert.IsType<TermLiteralTextNode>(hirConcat.caller);
        Assert.Equal(string.Empty, hirLeft.value);

        var hirRight = Assert.IsType<TermDotExpression>(Assert.Single(hirConcat.call_body!.term_arguments!.items).value);
        Assert.Equal("to_string", hirRight.callee.name);

        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);
        var lirMain = get_lir_entry(lir.module);

        Assert.Contains(lirMain.instructions, instruction => instruction.head_code == NyarHeadCode.utf8_concat);
    }

    [Fact]
    public void BuildHirMirLir_PrefixedStringLiteral_ShouldRemainValidWithoutSpecialSemantics()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
            namespace prefixed_string_pipeline;

            [main]
            micro main() -> Unit {
                let regex = re"(.)";
                let empty = sdsdf"";
            }
            """;
        var plan = new BuildPlan("prefixed_string_pipeline", "jvm-openjdk-linux-managed",
            "prefixed_string_pipeline.v");
        var parseResult = compiler.parse_source(source, "prefixed_string_pipeline.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors,
            string.Join(Environment.NewLine, compiler.diagnostics.messages.Select(d => d.message)));

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(d => d.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var hirMain = get_hir_entry(hir);
        Assert.Equal(2, hirMain.body!.statements.Count);

        var regexLet = Assert.IsType<DeclareLet>(hirMain.body.statements[0]);
        var regexLiteral = Assert.IsType<TermLiteralTextNode>(regexLet.initializer);
        Assert.Equal("re", regexLiteral.prefix);
        Assert.Equal("(.)", regexLiteral.value);

        var emptyLet = Assert.IsType<DeclareLet>(hirMain.body.statements[1]);
        var emptyLiteral = Assert.IsType<TermLiteralTextNode>(emptyLet.initializer);
        Assert.Equal("sdsdf", emptyLiteral.prefix);
        Assert.Equal(string.Empty, emptyLiteral.value);
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Mir.MirModule build_mir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("tuple_literal_lowering", "jvm-openjdk-linux-managed", fileName);
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        Assert.False(compiler.diagnostics.has_errors);

        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        return compiler.build_mir(hir, plan, targetProfile);
    }

    private static HirFunction get_hir_entry(HirModule module)
    {
        return module.functions.Single(function => function.is_logical_entry);
    }

    private static Nyar.Assembler.GenerateFunction get_lir_entry(Nyar.Assembler.GenerateModule module)
    {
        var exportedEntry = module.exports
            .FirstOrDefault(exportItem => exportItem.kind == Nyar.Assembler.GenerateExportKind.function);

        if (exportedEntry is not null &&
            exportedEntry.function_index >= 0 &&
            exportedEntry.function_index < module.functions.Count)
        {
            return module.functions[exportedEntry.function_index];
        }

        return module.functions.Single();
    }
}
