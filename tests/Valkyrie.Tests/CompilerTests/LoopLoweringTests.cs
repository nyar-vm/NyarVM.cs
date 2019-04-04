using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Std.Data.Binary.NyarIR.Data;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class LoopLoweringTests
{
    [Fact]
    public void BuildMir_LoopIn_ShouldLowerIteratorProtocolAndItemVariable()
    {
        var mir = build_mir(
            """
            namespace app;

            trait Iterator {
                type Item;
                micro has_next(self) -> bool;
                micro next(mut self) -> i32;
            }

            trait IntoIterator {
                type Item;
                micro into_iterator(self) -> IntIterator;
            }

            structure Numbers {
            }

            structure IntIterator {
            }

            imply Numbers: IntoIterator {
                type Item = i32;

                micro into_iterator(self) -> IntIterator {
                    IntIterator {}
                }
            }

            imply IntIterator: Iterator {
                type Item = i32;

                micro has_next(self) -> bool {
                    false
                }

                micro next(mut self) -> i32 {
                    0
                }
            }

            [main]
            micro main(items: Numbers) -> Unit {
                loop item in items {
                    let matched = item;
                }
            }
            """,
            "loop_in_lowering.v");

        var repeatNode = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<AlgebraNode.Repeat>()
            .Single();

        var bodyNodes = mir.graph.classes[mir.graph.union_find.find(repeatNode.body).value].nodes;
        var allNodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(bodyNodes, node => node is AlgebraNode.Seq);
        Assert.Contains(allNodes, node => node is AlgebraNode.VarDecl { name: "matched" });
        Assert.Contains(allNodes, node => node is AlgebraNode.VarDecl { name: "item" });
    }

    [Fact]
    public void BuildMir_LoopIn_WithTuplePattern_ShouldLowerToOrdinalBindings()
    {
        var mir = build_mir(
            """
            namespace app;

            trait Iterator {
                type Item;
                micro has_next(self) -> bool;
                micro next(mut self) -> Option<(i32, i32)>;
            }

            trait IntoIterator {
                type Item;
                micro into_iterator(self) -> PairIterator;
            }

            structure Option<T> {
                value: T
            }

            imply Option<T> {
                micro unwrap(self) -> T {
                    self.value
                }
            }

            structure Pairs {
            }

            structure PairIterator {
            }

            imply Pairs: IntoIterator {
                type Item = (i32, i32);

                micro into_iterator(self) -> PairIterator {
                    PairIterator {}
                }
            }

            imply PairIterator: Iterator {
                type Item = (i32, i32);

                micro has_next(self) -> bool {
                    true
                }

                micro next(mut self) -> Option<(i32, i32)> {
                    Option<(i32, i32)> { value: (0 as i32, 1 as i32) }
                }
            }

            [main]
            micro main(items: Pairs) -> Unit {
                loop (left, right) in items {
                    let matched = left;
                    let other = right;
                    break;
                }
            }
            """,
            "loop_in_tuple_pattern_lowering.v");

        var allNodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(allNodes, node => node is AlgebraNode.VarDecl { name: "left" });
        Assert.Contains(allNodes, node => node is AlgebraNode.VarDecl { name: "right" });
        Assert.Contains(allNodes, node => node is GetOrdinalIdx);
        Assert.DoesNotContain(allNodes, node => node is AlgebraNode.GetField { field_name: "_0" or "_1" });
    }

    [Fact]
    public void BuildHir_LoopIn_ShouldKeepImplicitIteratorProtocolCallablesReachable()
    {
        var hir = build_hir(
            """
            namespace app;

            trait Iterator {
                type Item;
                micro has_next(self) -> bool;
                micro next(mut self) -> Option<i32>;
            }

            trait IntoIterator {
                type Item;
                micro into_iterator(self) -> IntIterator;
            }

            structure Option<T> {
                value: T
            }

            imply Option<T> {
                micro unwrap(self) -> T {
                    self.value
                }
            }

            structure Numbers {
            }

            structure IntIterator {
            }

            imply Numbers: IntoIterator {
                type Item = i32;

                micro into_iterator(self) -> IntIterator {
                    IntIterator {}
                }
            }

            imply IntIterator: Iterator {
                type Item = i32;

                micro has_next(self) -> bool {
                    false
                }

                micro next(mut self) -> Option<i32> {
                    Option<i32> { value: 0 }
                }
            }

            [main]
            micro main(items: Numbers) -> Unit {
                loop item in items {
                    let matched = item;
                }
            }
            """,
            "loop_in_reachable_hir.v");

        var reachable = hir.enumerate_reachable_aot_callables("main")
            .Select(callable => callable.name)
            .ToArray();

        Assert.Contains("app.Numbers.into_iterator", reachable);
        Assert.Contains("app.IntIterator.has_next", reachable);
        Assert.Contains("app.IntIterator.next", reachable);
        Assert.Contains("app.Option.unwrap", reachable);
    }

    [Fact]
    public void BuildMir_Loop_ShouldLowerInitializerConditionAndUpdate()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(limit: i32) -> Unit {
                loop let i = 0; i < limit; i += 1 {
                    let matched = i;
                }
            }
            """,
            "loop_counted_lowering.v");

        var repeatNode = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<AlgebraNode.Repeat>()
            .Single();

        var bodyNodes = mir.graph.classes[mir.graph.union_find.find(repeatNode.body).value].nodes;
        var allNodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(bodyNodes, node => node is AlgebraNode.Seq);
        Assert.Contains(allNodes, node => node is AlgebraNode.VarDecl { name: "i" });
        Assert.Contains(allNodes, node => node is AlgebraNode.VarDecl { name: "matched" });
    }

    [Fact]
    public void BuildLir_LoopIn_WithBreak_ShouldJumpToLoopEnd()
    {
        var function = build_lir_function(
            """
            namespace app;

            trait Iterator {
                type Item;
                micro has_next(self) -> bool;
                micro next(mut self) -> i32;
            }

            trait IntoIterator {
                type Item;
                micro into_iterator(self) -> IntIterator;
            }

            structure Numbers {
            }

            structure IntIterator {
            }

            imply Numbers: IntoIterator {
                type Item = i32;

                micro into_iterator(self) -> IntIterator {
                    IntIterator {}
                }
            }

            imply IntIterator: Iterator {
                type Item = i32;

                micro has_next(self) -> bool {
                    true
                }

                micro next(mut self) -> i32 {
                    0
                }
            }

            [main]
            micro main(items: Numbers) -> Unit {
                loop item in items {
                    break;
                }
            }
            """,
            "loop_in_break_lir.v");

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.jump_if_false);
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.jump);
        Assert.Contains(function.labels, label => label.name.Contains("repeat_end", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildLir_LoopIn_WithContinue_ShouldJumpToLoopStart()
    {
        var function = build_lir_function(
            """
            namespace app;

            trait Iterator {
                type Item;
                micro has_next(self) -> bool;
                micro next(mut self) -> i32;
            }

            trait IntoIterator {
                type Item;
                micro into_iterator(self) -> IntIterator;
            }

            structure Numbers {
            }

            structure IntIterator {
            }

            imply Numbers: IntoIterator {
                type Item = i32;

                micro into_iterator(self) -> IntIterator {
                    IntIterator {}
                }
            }

            imply IntIterator: Iterator {
                type Item = i32;

                micro has_next(self) -> bool {
                    true
                }

                micro next(mut self) -> i32 {
                    0
                }
            }

            [main]
            micro main(items: Numbers) -> Unit {
                loop item in items {
                    continue;
                }
            }
            """,
            "loop_in_continue_lir.v");

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.jump_if_false);
        Assert.True(function.instructions.Count(instruction => instruction.opcode == NyarHeadCode.jump) >= 1);
        Assert.Contains(function.labels, label => label.name.Contains("repeat_start", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildLir_Loop_WithBreak_ShouldUseRepeatEndAndSkipUnreachableStatements()
    {
        var function = build_lir_function(
            """
            namespace app;

            [main]
            micro main() -> Unit {
                loop {
                    break;
                    let after_break = 1;
                }
            }
            """,
            "loop_break_unreachable.v");

        Assert.Contains(get_jump_label_names(function), label => label.Contains("repeat_end", StringComparison.Ordinal));
        Assert.DoesNotContain(function.local_variables, local => local.name == "after_break");
    }

    [Fact]
    public void BuildLir_Loop_ShouldLowerCountedHeaderToLoopBranches()
    {
        var function = build_lir_function(
            """
            namespace app;

            [main]
            micro main(limit: i32) -> Unit {
                loop let i = 0; i < limit; i += 1 {
                    let matched = i;
                }
            }
            """,
            "loop_counted_lir.v");

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.jump_if_false);
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.jump);
        Assert.Contains(function.labels, label => label.name.Contains("repeat_start", StringComparison.Ordinal));
        Assert.Contains(function.labels, label => label.name.Contains("repeat_end", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildLir_While_WithContinue_ShouldUseRepeatStartAndSkipUnreachableStatements()
    {
        var function = build_lir_function(
            """
            namespace app;

            [main]
            micro main(flag: bool) -> Unit {
                while (flag) {
                    continue;
                    let after_continue = 1;
                }
            }
            """,
            "while_continue_unreachable.v");

        Assert.Contains(get_jump_label_names(function), label => label.Contains("repeat_start", StringComparison.Ordinal));
        Assert.DoesNotContain(function.local_variables, local => local.name == "after_continue");
    }

    [Fact]
    public void BuildLir_LoopIn_WithBreak_ShouldUseRepeatEndAndSkipUnreachableStatements()
    {
        var function = build_lir_function(
            """
            namespace app;

            trait Iterator {
                type Item;
                micro has_next(self) -> bool;
                micro next(mut self) -> i32;
            }

            trait IntoIterator {
                type Item;
                micro into_iterator(self) -> IntIterator;
            }

            structure Numbers {
            }

            structure IntIterator {
            }

            imply Numbers: IntoIterator {
                type Item = i32;

                micro into_iterator(self) -> IntIterator {
                    IntIterator {}
                }
            }

            imply IntIterator: Iterator {
                type Item = i32;

                micro has_next(self) -> bool {
                    true
                }

                micro next(mut self) -> i32 {
                    0
                }
            }

            [main]
            micro main(items: Numbers) -> Unit {
                loop item in items {
                    break;
                    let after_break = item;
                }
            }
            """,
            "loop_in_break_unreachable.v");

        Assert.Contains(get_jump_label_names(function), label => label.Contains("repeat_end", StringComparison.Ordinal));
        Assert.DoesNotContain(function.local_variables, local => local.name == "after_break");
    }

    [Fact]
    public void BuildLir_LoopIn_WithContinue_ShouldUseRepeatStartAndSkipUnreachableStatements()
    {
        var function = build_lir_function(
            """
            namespace app;

            trait Iterator {
                type Item;
                micro has_next(self) -> bool;
                micro next(mut self) -> i32;
            }

            trait IntoIterator {
                type Item;
                micro into_iterator(self) -> IntIterator;
            }

            structure Numbers {
            }

            structure IntIterator {
            }

            imply Numbers: IntoIterator {
                type Item = i32;

                micro into_iterator(self) -> IntIterator {
                    IntIterator {}
                }
            }

            imply IntIterator: Iterator {
                type Item = i32;

                micro has_next(self) -> bool {
                    true
                }

                micro next(mut self) -> i32 {
                    0
                }
            }

            [main]
            micro main(items: Numbers) -> Unit {
                loop item in items {
                    continue;
                    let after_continue = item;
                }
            }
            """,
            "loop_in_continue_unreachable.v");

        Assert.Contains(get_jump_label_names(function), label => label.Contains("repeat_start", StringComparison.Ordinal));
        Assert.DoesNotContain(function.local_variables, local => local.name == "after_continue");
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Mir.MirModule build_mir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("loop_lowering", "jvm-openjdk-linux-managed", fileName);
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

    private static HirModule build_hir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("loop_lowering", "jvm-openjdk-linux-managed", fileName);
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        return compiler.build_hir(stagedAst, semantics, plan);
    }

    private static Nyar.Assembler.GenerateFunction build_lir_function(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("loop_lowering", "jvm-openjdk-linux-managed", fileName);
        var mir = build_mir(source, fileName);
        var module = compiler.build_lir(mir, plan).module;
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

    private static IReadOnlyList<string> get_jump_label_names(Nyar.Assembler.GenerateFunction function)
    {
        return function.instructions
            .Where(instruction => instruction.opcode == NyarHeadCode.jump)
            .SelectMany(instruction => instruction.operands)
            .OfType<Nyar.Assembler.GenerateOperand.Label>()
            .Select(label => label.name)
            .ToArray();
    }
}
