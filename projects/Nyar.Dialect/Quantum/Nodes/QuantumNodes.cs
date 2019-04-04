using Nyar.IR.Intent;

namespace Nyar.Dialect.Quantum.Nodes;

#region 量子比特与寄存器节点

[AlgebraNode]
public sealed partial record Qubit(string qubitId) : AlgebraNode;

[AlgebraNode]
public sealed partial record QuantumRegister(string registerId, int size) : AlgebraNode;

[AlgebraNode]
public sealed partial record ClassicalRegister(string registerId, int size) : AlgebraNode;

#endregion

#region 单量子比特门节点

[AlgebraNode]
public sealed partial record SingleQubitGate(SingleGateType type, Id target) : AlgebraNode;

public enum SingleGateType
{
    X,
    Y,
    Z,
    H,
    S,
    T,
    Rx,
    Ry,
    Rz
}

[AlgebraNode]
public sealed partial record ParametricSingleGate(SingleGateType type, Id target, Id angle) : AlgebraNode;

#endregion

#region 多量子比特门节点

[AlgebraNode]
public sealed partial record ControlledGate(ControlledGateType type, Id control, Id target) : AlgebraNode;

public enum ControlledGateType
{
    CX,
    CY,
    CZ,
    CH,
    CSWAP
}

[AlgebraNode]
public sealed partial record MultiControlledGate(ControlledGateType type, IReadOnlyList<Id> controls, Id target) : AlgebraNode;

[AlgebraNode]
public sealed partial record SwapGate(Id qubitA, Id qubitB) : AlgebraNode;

[AlgebraNode]
public sealed partial record ToffoliGate(Id controlA, Id controlB, Id target) : AlgebraNode;

#endregion

#region 测量与重置节点

[AlgebraNode]
public sealed partial record Measure(Id qubit, Id classicalBit) : AlgebraNode;

[AlgebraNode]
public sealed partial record Reset(Id qubit) : AlgebraNode;

[AlgebraNode]
public sealed partial record ConditionalOperation(Id classicalCondition, Id quantumOperation) : AlgebraNode;

#endregion

#region 电路结构节点

[AlgebraNode]
public sealed partial record QuantumCircuit(IReadOnlyList<Id> operations) : AlgebraNode;

[AlgebraNode]
public sealed partial record CircuitModule(string moduleName, IReadOnlyList<string> inputQubits, IReadOnlyList<Id> body)
    : AlgebraNode;

[AlgebraNode]
public sealed partial record ModuleInstance(string moduleName, IReadOnlyDictionary<string, Id> qubitMapping) : AlgebraNode;

[AlgebraNode]
public sealed partial record CircuitDepth(Id circuit) : AlgebraNode;

[AlgebraNode]
public sealed partial record QubitConnectivity(IReadOnlyList<(string QubitA, string QubitB)> connections) : AlgebraNode;

#endregion

#region 量子算法节点

[AlgebraNode]
public sealed partial record QuantumFourierTransform(IReadOnlyList<Id> qubits) : AlgebraNode;

[AlgebraNode]
public sealed partial record GroverDiffusion(IReadOnlyList<Id> qubits) : AlgebraNode;

[AlgebraNode]
public sealed partial record PhaseEstimation(
    Id unitary,
    IReadOnlyList<Id> eigenstateQubits,
    IReadOnlyList<Id> phaseQubits) : AlgebraNode;

[AlgebraNode]
public sealed partial record VariationalQuantumEigensolver(Id ansatz, Id hamiltonian, Id optimizer) : AlgebraNode;

#endregion