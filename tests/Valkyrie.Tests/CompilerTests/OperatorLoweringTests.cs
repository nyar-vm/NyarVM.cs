using Std.Data.Binary.NyarIR.Data;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Lir;
using Nyar.Language.Valkyrie.Compiler.Mir;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class OperatorLoweringTests
{
    [Fact]
    public void BuildMir_WithOverloadedPowerOperator_ShouldPreferMethodDispatch()
    {
        var hir = build_hir(
            """
            class PowBox {
                infix `^`(self, other: PowBox) -> PowBox {
                    self
                }
            }

            [main]
            micro main(a: PowBox, b: PowBox) -> PowBox {
                a ^ b
            }
            """);

        var mir = new MirBuilder().build(hir, "jvm");
        var nodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(nodes, node => node is PhysicalNode.Call);
    }

    [Fact]
    public void BuildHir_WithInfixOperatorMethod_ShouldPreserveOperatorMemberName()
    {
        var hir = build_hir(
            """
            class Vec {
                infix `+`(self, other: Vec) -> Vec {
                    self
                }
            }
            """);

        var type = Assert.Single(hir.types);
        var method = Assert.Single(type.methods);

        Assert.Equal("infix +", method.member_name);
        Assert.Contains("__operator_infix", method.name, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMir_WithOverloadedInfixOperator_ShouldPreferMethodDispatch()
    {
        var hir = build_hir(
            """
            class Vec {
                infix `+`(self, other: Vec) -> Vec {
                    self
                }
            }

            [main]
            micro main(a: Vec, b: Vec) -> Vec {
                a + b
            }
            """);

        var mir = new MirBuilder().build(hir, "jvm");

        Assert.Contains(
            mir.graph.classes.Values.SelectMany(@class => @class.nodes),
            node => node is PhysicalNode.Call);
    }

    [Fact]
    public void BuildMir_WithBitwiseAnd_ShouldRejectAndSuggestMethodDispatch()
    {
        var hir = build_hir(
            """
            [main]
            micro main() -> i32 {
                1 & 2
            }
            """);

        var ex = Assert.Throws<NotSupportedException>(() => new MirBuilder().build(hir, "jvm"));
        Assert.Contains(".bit_and(...)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildLir_WithLogicalNot_ShouldEmitZeroComparison()
    {
        var graph = new EGraph<IKun>();
        var operand = graph.add(new Sym("flag"));
        var not = graph.add(new Not(operand));
        var body = graph.add(new Ret(not));
        var lambda = graph.add(new Lambda(["flag"], body));
        var export = graph.add(new Export("main", lambda));
        var root = graph.add(new Mod("logical_not_lowering", [export]));
        graph.rebuild();

        var mir = new MirModule("logical_not_lowering", graph, root);
        mir.set_parameter_types("main", [HirTypeRef.@bool()]);
        mir.set_return_type("main", HirTypeRef.@bool());

        var lir = new LirBuilder().build(mir);
        var function = Assert.Single(lir.module.functions);

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.I32Eq);
        Assert.DoesNotContain(function.instructions, instruction => instruction.opcode == NyarHeadCode.Nop);
    }

    [Fact]
    public void BuildLir_WithIntrinsicI32AddCall_ShouldEmitI32Add()
    {
        var graph = new EGraph<IKun>();
        var left = graph.add(IKunBuilder.Constant(2));
        var right = graph.add(IKunBuilder.Constant(3));
        var target = graph.add(new Sym("__i32_add"));
        var call = graph.add(new PhysicalNode.Call(DispatchKind.@static, target, [left, right], null));
        var body = graph.add(new Ret(call));
        var lambda = graph.add(new Lambda([], body));
        var export = graph.add(new Export("main", lambda));
        var root = graph.add(new Mod("intrinsic_i32_add_lowering", [export]));
        graph.rebuild();

        var mir = new MirModule("intrinsic_i32_add_lowering", graph, root);
        mir.set_function_attributes("__i32_add", [new HirAttribute("intrinsic", ["i32.add"])]);
        mir.set_return_type("main", HirTypeRef.i32());

        var lir = new LirBuilder().build(mir);
        var function = Assert.Single(lir.module.functions);

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.I32Add);
        Assert.DoesNotContain(function.instructions, instruction => instruction.opcode == NyarHeadCode.CallStatic);
    }

    private static HirModule build_hir(string source)
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("operator_lowering", "jvm-openjdk-linux-managed", "operator_lowering.v");
        var ast = compiler.parse_source(source, "operator_lowering.v").Value!;
        var semantics = compiler.analyze(ast, plan);
        return compiler.build_hir(ast, semantics, plan);
    }
}
