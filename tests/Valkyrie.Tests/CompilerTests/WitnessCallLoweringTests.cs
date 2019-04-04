using Nyar.Assembler;
using Std.Data.Binary.NyarIR.Data;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Lir;
using Nyar.Language.Valkyrie.Compiler.Mir;

namespace Valkyrie.Tests.CompilerTests;

public sealed class WitnessCallLoweringTests
{
    [Fact]
    public void BuildLir_WithWitnessCall_ShouldEmitInterfaceIdAndSlotOperands()
    {
        var graph = new EGraph<IKun>();
        var receiver = graph.add(new Sym("self"));
        var argument = graph.add(new Sym("value"));
        var witness = graph.add(new Sym("app.Display"));
        var call = graph.add(new PhysicalNode.Call(
            DispatchKind.Witness,
            receiver,
            [argument],
            witness)
        {
            MethodIndex = 2,
            MethodName = "show"
        });
        var body = graph.add(new Ret(call));
        var lambda = graph.add(new Lambda(["self", "value"], body));
        var export = graph.add(new Export("main", lambda));
        var root = graph.add(new Mod("witness_lowering", [export]));
        graph.rebuild();

        var mir = new MirModule("witness_lowering", graph, root);
        mir.set_parameter_types("main", [HirTypeRef.named("ExternRef"), HirTypeRef.i32()]);
        mir.set_return_type("main", HirTypeRef.i32());

        var lir = new LirBuilder().build(mir);
        var function = Assert.Single(lir.module.functions);
        var callInstruction = Assert.Single(function.instructions, instruction => instruction.opcode == NyarHeadCode.CallWitness);

        Assert.Equal(2, callInstruction.operands.Count);
        Assert.Equal(
            GenerateWitnessIdentity.compute_interface_id("app.Display"),
            Assert.IsType<GenerateOperand.I32>(callInstruction.operands[0]).value);
        Assert.Equal(2, Assert.IsType<GenerateOperand.I32>(callInstruction.operands[1]).value);
    }
}
