using Nyar.IR.Intent;

namespace Nyar.Dialect.Hardware.Nodes;

#region HIR 层（硬件结构）

[AlgebraNode]
public sealed partial record Module(string name, IReadOnlyList<Port> ports, Id body) : AlgebraNode;

[AlgebraNode]
public sealed partial record Port(PortDirection direction, string name, int bitWidth) : AlgebraNode;

public enum PortDirection
{
    Input,
    Output,
    Inout
}

[AlgebraNode]
public sealed partial record Wire(string name, int bitWidth) : AlgebraNode;

[AlgebraNode]
public sealed partial record Reg(string name, int bitWidth, Id? initialValue) : AlgebraNode;

[AlgebraNode]
public sealed partial record Always(AlwaysTrigger trigger, Id body) : AlgebraNode;

public enum AlwaysTrigger
{
    Combinational,
    PositiveEdge,
    NegativeEdge
}

#endregion

#region MIR 层（微架构意图）

[AlgebraNode]
public sealed partial record Pipeline(int stageCount, Id body) : AlgebraNode;

[AlgebraNode]
public sealed partial record Unroll(int factor, Id loop) : AlgebraNode;

[AlgebraNode]
public sealed partial record SystolicArray(int rows, int cols, Id cellFunction) : AlgebraNode;

#endregion

#region LIR 层（门级）

[AlgebraNode]
public sealed partial record Gate(GateType type, IReadOnlyList<Id> inputs) : AlgebraNode;

public enum GateType
{
    And,
    Or,
    Not,
    Nand,
    Nor,
    Xor,
    Xnor
}

[AlgebraNode]
public sealed partial record FlipFlop(Id data, Id clock, Id? reset, Id? resetValue) : AlgebraNode;

[AlgebraNode]
public sealed partial record BitSlice(Id value, int high, int low) : AlgebraNode;

[AlgebraNode]
public sealed partial record Concat(IReadOnlyList<Id> values) : AlgebraNode;

[AlgebraNode]
public sealed partial record Mux(Id select, IReadOnlyList<Id> inputs) : AlgebraNode;

#endregion