using System.Reflection;
using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Std.Data.Binary.NyarIR.Data;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Text.Valkyrie.AST.Pattern;

namespace Valkyrie.Tests.CompilerTests;

public sealed class MatchLoweringTests
{
    [Fact]
    public void BuildMir_MatchNumberPattern_ShouldPreserveLiteralValue()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main() -> Unit {
                match value {
                    case 42:
                        return
                }
            }
            """,
            "match_lowering.v");
        var literalPattern = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.LiteralPattern>()
            .Single();
        var literalValueClassId = mir.graph.union_find.find(literalPattern.value);
        var literalValueClass = mir.graph.classes[literalValueClassId.value];

        Assert.Contains(literalValueClass.nodes.OfType<IKun.Constant>(), constant => constant.value == 42);
    }

    [Fact]
    public void BuildMir_MatchObjectPattern_ShouldPreserveFieldNames()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main() -> Unit {
                match value {
                    case Some { value: inner, flag }:
                        return
                }
            }
            """,
            "match_object_lowering.v");
        var objectPattern = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.ObjectPattern>()
            .Single();
        var fieldNodes = objectPattern.fields
            .Select(fieldId =>
            {
                var fieldClassId = mir.graph.union_find.find(fieldId);
                return mir.graph.classes[fieldClassId.value].nodes.OfType<IKun.ObjectPatternField>().Single();
            })
            .ToArray();

        Assert.Equal("Some", objectPattern.type_name);
        Assert.Collection(fieldNodes,
            field =>
            {
                Assert.Equal("value", field.name);
                var patternClassId = mir.graph.union_find.find(field.pattern);
                var variablePattern = mir.graph.classes[patternClassId.value].nodes
                    .OfType<IKun.VariablePattern>()
                    .Single();
                Assert.Equal("inner", variablePattern.name);
            },
            field =>
            {
                Assert.Equal("flag", field.name);
                var patternClassId = mir.graph.union_find.find(field.pattern);
                var variablePattern = mir.graph.classes[patternClassId.value].nodes
                    .OfType<IKun.VariablePattern>()
                    .Single();
                Assert.Equal("flag", variablePattern.name);
            });
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

        Assert.Contains(nodes, node => node is GetOrdinalIdx);
        Assert.DoesNotContain(nodes, node => node is AlgebraNode.GetField { field_name: "_0" or "_1" });
    }

    [Fact]
    public void BuildMir_MatchBooleanPattern_ShouldPreserveLiteralValue()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main() -> Unit {
                match value {
                    case true:
                        return
                }
            }
            """,
            "match_boolean_lowering.v");
        var literalPattern = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.LiteralPattern>()
            .Single();
        var literalValueClassId = mir.graph.union_find.find(literalPattern.value);
        var literalValueClass = mir.graph.classes[literalValueClassId.value];

        Assert.Contains(literalValueClass.nodes.OfType<IKun.BooleanConstant>(), constant => constant.value);
    }

    [Fact]
    public void ParseAndStage_MatchNullPattern_ShouldPreservePatternNode()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_null_ast_boundary.v");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            [main]
            micro main(value: Box) -> Unit {
                match value {
                    case null:
                        return
                }
            }
            """,
            "match_null_ast_boundary.v");

        Assert.NotNull(parseResult.value);
        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var function = Assert.IsType<DeclareMicro>(stagedAst.Declarations.Last());
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));

        _ = Assert.IsType<PatternLiteralNullNode>(caseArm.Pattern);
    }

    [Fact]
    public void BuildHir_MatchNullPattern_ShouldPreservePatternNode()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_null_hir_boundary.v");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            [main]
            micro main(value: Box) -> Unit {
                match value {
                    case null:
                        return
                }
            }
            """,
            "match_null_hir_boundary.v");

        Assert.NotNull(parseResult.value);
        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var function = Assert.Single(hir.functions);
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.syntax.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));

        _ = Assert.IsType<PatternLiteralNullNode>(caseArm.Pattern);
    }

    [Fact]
    public void IkunBuilder_LiteralPattern_WithNone_ShouldRemainLiteralPattern()
    {
        var graph = new EGraph<IKun>();
        var noneId = graph.add(IKunBuilder.None());
        _ = graph.add(IKunBuilder.LiteralPattern(noneId));

        Assert.Contains(
            graph.classes.Values.SelectMany(@class => @class.nodes).OfType<IKun.LiteralPattern>(),
            pattern => pattern.value == noneId);
    }

    [Fact]
    public void HirToMirLowerer_BuildMatchPattern_WithNullPattern_ShouldProduceLiteralPattern()
    {
        var lowerer = new global::Nyar.Language.Valkyrie.Compiler.Mir.HirToMirLowerer();
        var graph = new EGraph<IKun>();
        var method = typeof(global::Nyar.Language.Valkyrie.Compiler.Mir.HirToMirLowerer).GetMethod(
            "build_match_pattern",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var result = method!.Invoke(lowerer, [new PatternLiteralNullNode(), graph]);
        var patternId = Assert.IsType<Id>(result);
        var patternClass = graph.classes[graph.union_find.find(patternId).value];

        Assert.Contains(patternClass.nodes, node => node is IKun.LiteralPattern);
    }

    [Fact]
    public void BuildMir_MatchNullPattern_ShouldPreserveNullLiteral()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(value: Box) -> Unit {
                match value {
                    case null:
                        return
                }
            }
            """,
            "match_null_lowering.v");
        var allNodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();
        var literalPatterns = allNodes
            .OfType<IKun.LiteralPattern>()
            .ToArray();
        var matchArmPatternTypes = allNodes
            .OfType<IKun.MatchArm>()
            .Select(arm =>
            {
                var patternClassId = mir.graph.union_find.find(arm.pattern);
                return string.Join("|", mir.graph.classes[patternClassId.value].nodes.Select(node => node.GetType().Name));
            })
            .ToArray();

        Assert.True(literalPatterns.Length == 1,
            $"literal patterns: {literalPatterns.Length}; arms: {string.Join(", ", matchArmPatternTypes)}; nodes: {string.Join(", ", allNodes.Select(node => node.GetType().Name).Distinct())}");

        var literalPattern = literalPatterns.Single();
        var literalValueClassId = mir.graph.union_find.find(literalPattern.value);
        var literalValueClass = mir.graph.classes[literalValueClassId.value];

        Assert.Contains(literalValueClass.nodes, node => node is IKun.None);
    }

    [Fact]
    public void BuildMir_MatchGuard_ShouldPreserveGuardExpression()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main() -> Unit {
                match value {
                    case captured if captured:
                        return
                }
            }
            """,
            "match_guard_lowering.v");
        var matchArm = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.MatchArm>()
            .Single();

        Assert.NotNull(matchArm.guard);
        var guardClassId = mir.graph.union_find.find(matchArm.guard!.value);
        var guardClass = mir.graph.classes[guardClassId.value];
        Assert.Contains(guardClass.nodes.OfType<IKun.Symbol>(), symbol => symbol.name == "captured");
    }

    [Fact]
    public void BuildLir_MatchNumberPattern_ShouldLowerToControlFlow()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_lir_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main() -> Unit {
                match value {
                    case 42:
                        let matched = 1;
                    else:
                        let fallback = 2;
                }
            }
            """,
            "match_lir_boundary.v");

        var lir = compiler.build_lir(mir, plan);
        var function = Assert.Single(lir.module.functions);

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.JumpIfFalse);
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.Jump);
        Assert.NotEmpty(function.labels);
    }

    [Fact]
    public void BuildLir_MatchVariablePattern_ShouldBindScopedLocal()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_variable_binding.v");
        var mir = build_mir(
            """
            namespace app;

            micro main() -> Unit {
                match value {
                    case captured:
                        let copy = captured;
                }
            }
            """,
            "match_variable_binding.v");

        var lir = compiler.build_lir(mir, plan);
        var function = Assert.Single(lir.module.functions);

        Assert.Contains(function.local_variables, local => local.name == "captured");
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.StoreLocal);
    }

    [Fact]
    public void BuildLir_MatchGuard_ShouldEvaluateGuardBeforeBody()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_guard_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(flag: bool) -> Unit {
                match flag {
                    case captured if captured:
                        let matched = 1;
                    else:
                        let fallback = 0;
                }
            }
            """,
            "match_guard_boundary.v");

        var lir = compiler.build_lir(mir, plan);
        var function = Assert.Single(lir.module.functions);

        Assert.Contains(function.local_variables, local => local.name == "captured");
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.JumpIfFalse);
    }

    [Fact]
    public void BuildLir_MatchGuardWithComparison_ShouldLowerComparisonBeforeBranch()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_guard_compare_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(value: i32) -> Unit {
                match value {
                    case captured if captured > 0:
                        let matched = 1;
                    else:
                        let fallback = 0;
                }
            }
            """,
            "match_guard_compare_boundary.v");

        var lir = compiler.build_lir(mir, plan);
        var function = Assert.Single(lir.module.functions);

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.I32GtS);
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.JumpIfFalse);
    }

    [Fact]
    public void BuildLir_MatchNullPattern_ShouldLowerToReferenceComparison()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_null_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(value: Box) -> Unit {
                match value {
                    case null:
                        let matched = 1;
                    else:
                        let fallback = 0;
                }
            }
            """,
            "match_null_boundary.v");

        var lir = compiler.build_lir(mir, plan);
        var function = Assert.Single(lir.module.functions);

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.RefEq);
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.JumpIfFalse);
    }

    [Fact]
    public void BuildLir_MatchNumberPattern_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main() -> Unit {
                match value {
                    case 42:
                        let matched = 1;
                    else:
                        let fallback = 2;
                }
            }
            """,
            "match_backend_boundary.v");

        var module = compiler.build_lir(mir, plan).module;

        Assert.True(new JvmBackend().validate(module, out var jvmDiagnostics),
            string.Join(Environment.NewLine, jvmDiagnostics.Select(diagnostic => diagnostic.message)));
        Assert.True(new ClrBackend().validate(module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.message)));
        var jvmOutput = new JvmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        var clrOutput = new ClrBackend().compile(module, new Nyar.Assembler.CompilationOptions());

        Assert.Equal(".class", jvmOutput.file_extension);
        Assert.NotEmpty(jvmOutput.media_type);
        Assert.NotNull(jvmOutput.data);
        Assert.NotNull(clrOutput.data);
        Assert.NotEmpty(clrOutput.file_extension);
        Assert.Contains(clrOutput.assets, asset => asset.name.EndsWith(".msil", StringComparison.Ordinal));
        Assert.True(new WasmBackend().validate(module, out var wasmDiagnostics),
            string.Join(Environment.NewLine, wasmDiagnostics.Select(diagnostic => diagnostic.message)));
        var wasmOutput = new WasmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        Assert.Equal(".wasm", wasmOutput.file_extension);
        Assert.NotNull(wasmOutput.data);
    }

    [Fact]
    public void BuildLir_MatchBooleanPattern_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_boolean_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main() -> Unit {
                match value {
                    case true:
                        let matched = 1;
                    else:
                        let fallback = 0;
                }
            }
            """,
            "match_boolean_backend_boundary.v");

        var module = compiler.build_lir(mir, plan).module;

        Assert.True(new JvmBackend().validate(module, out var jvmDiagnostics),
            string.Join(Environment.NewLine, jvmDiagnostics.Select(diagnostic => diagnostic.message)));
        Assert.True(new ClrBackend().validate(module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.message)));

        var jvmOutput = new JvmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        var clrOutput = new ClrBackend().compile(module, new Nyar.Assembler.CompilationOptions());

        Assert.Equal(".class", jvmOutput.file_extension);
        Assert.NotNull(jvmOutput.data);
        Assert.NotNull(clrOutput.data);
        Assert.True(new WasmBackend().validate(module, out var wasmDiagnostics),
            string.Join(Environment.NewLine, wasmDiagnostics.Select(diagnostic => diagnostic.message)));
        var wasmOutput = new WasmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        Assert.Equal(".wasm", wasmOutput.file_extension);
        Assert.NotNull(wasmOutput.data);
    }

    [Fact]
    public void BuildLir_MatchNullPattern_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_null_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(value: Box) -> Unit {
                match value {
                    case null:
                        let matched = 1;
                    else:
                        let fallback = 0;
                }
            }
            """,
            "match_null_backend_boundary.v");

        var module = compiler.build_lir(mir, plan).module;

        Assert.True(new JvmBackend().validate(module, out var jvmDiagnostics),
            string.Join(Environment.NewLine, jvmDiagnostics.Select(diagnostic => diagnostic.message)));
        Assert.True(new ClrBackend().validate(module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.message)));

        var jvmOutput = new JvmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        var clrOutput = new ClrBackend().compile(module, new Nyar.Assembler.CompilationOptions());

        Assert.Equal(".class", jvmOutput.file_extension);
        Assert.NotNull(jvmOutput.data);
        Assert.NotNull(clrOutput.data);
        Assert.True(new WasmBackend().validate(module, out var wasmDiagnostics),
            string.Join(Environment.NewLine, wasmDiagnostics.Select(diagnostic => diagnostic.message)));
        var wasmOutput = new WasmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        Assert.Equal(".wasm", wasmOutput.file_extension);
        Assert.NotNull(wasmOutput.data);
    }

    [Fact]
    public void BuildLir_MatchGuard_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_guard_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(flag: bool) -> Unit {
                match flag {
                    case captured if captured:
                        let matched = 1;
                    else:
                        let fallback = 0;
                }
            }
            """,
            "match_guard_backend_boundary.v");

        var module = compiler.build_lir(mir, plan).module;

        Assert.True(new JvmBackend().validate(module, out var jvmDiagnostics),
            string.Join(Environment.NewLine, jvmDiagnostics.Select(diagnostic => diagnostic.message)));
        Assert.True(new ClrBackend().validate(module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.message)));

        var jvmOutput = new JvmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        var clrOutput = new ClrBackend().compile(module, new Nyar.Assembler.CompilationOptions());

        Assert.Equal(".class", jvmOutput.file_extension);
        Assert.NotNull(jvmOutput.data);
        Assert.NotNull(clrOutput.data);
        Assert.True(new WasmBackend().validate(module, out var wasmDiagnostics),
            string.Join(Environment.NewLine, wasmDiagnostics.Select(diagnostic => diagnostic.message)));
        var wasmOutput = new WasmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        Assert.Equal(".wasm", wasmOutput.file_extension);
        Assert.NotNull(wasmOutput.data);
    }

    [Fact]
    public void BuildLir_MatchGuardWithComparison_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_guard_compare_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(value: i32) -> Unit {
                match value {
                    case captured if captured > 0:
                        let matched = 1;
                    else:
                        let fallback = 0;
                }
            }
            """,
            "match_guard_compare_backend_boundary.v");

        var module = compiler.build_lir(mir, plan).module;

        Assert.True(new JvmBackend().validate(module, out var jvmDiagnostics),
            string.Join(Environment.NewLine, jvmDiagnostics.Select(diagnostic => diagnostic.message)));
        Assert.True(new ClrBackend().validate(module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.message)));

        var jvmOutput = new JvmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        var clrOutput = new ClrBackend().compile(module, new Nyar.Assembler.CompilationOptions());

        Assert.Equal(".class", jvmOutput.file_extension);
        Assert.NotNull(jvmOutput.data);
        Assert.NotNull(clrOutput.data);
        Assert.True(new WasmBackend().validate(module, out var wasmDiagnostics),
            string.Join(Environment.NewLine, wasmDiagnostics.Select(diagnostic => diagnostic.message)));
        var wasmOutput = new WasmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        Assert.Equal(".wasm", wasmOutput.file_extension);
        Assert.NotNull(wasmOutput.data);
    }

    [Fact]
    public void BuildLir_MatchObjectPattern_ShouldFailExplicitly_UntilObjectLoweringIsImplemented()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", "match_object_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main() -> Unit {
                match value {
                    case Some { value: inner, flag }:
                        return
                }
            }
            """,
            "match_object_boundary.v");

        var exception = Assert.Throws<NotSupportedException>(() => compiler.build_lir(mir, plan));

        Assert.Contains("对象/构造器模式", exception.Message, StringComparison.Ordinal);
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Mir.MirModule build_mir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("match_lowering", "jvm-openjdk-linux-managed", fileName);
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        return compiler.build_mir(hir, plan, targetProfile);
    }
}

