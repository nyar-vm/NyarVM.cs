using Nyar.Dialect.Quantum.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.Dialect.Quantum.Cost;

/// <summary>
///     Quantum 方言节点的成本估算钩子
/// </summary>
public sealed class QuantumCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Qubit or QuantumRegister or ClassicalRegister
            or SingleQubitGate or ParametricSingleGate
            or ControlledGate or MultiControlledGate or SwapGate or ToffoliGate
            or Measure or Reset or ConditionalOperation
            or QuantumCircuit or CircuitModule or ModuleInstance or CircuitDepth
            or QubitConnectivity
            or QuantumFourierTransform or GroverDiffusion or PhaseEstimation
            or VariationalQuantumEigensolver;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Qubit => CostVector.from_latency(1),
            QuantumRegister qr => new CostVector(qr.size * 2, 0, 0, 0),
            ClassicalRegister cr => new CostVector(cr.size, 0, 0, 0),
            SingleQubitGate => CostVector.from_latency(10),
            ParametricSingleGate => CostVector.from_latency(15),
            ControlledGate => CostVector.from_latency(30),
            MultiControlledGate mcg => new CostVector(mcg.controls.Count * 25, 0, 0, 0),
            SwapGate => CostVector.from_latency(20),
            ToffoliGate => CostVector.from_latency(50),
            Measure => CostVector.from_latency(100),
            Reset => CostVector.from_latency(80),
            ConditionalOperation => CostVector.from_latency(120),
            QuantumCircuit qc => new CostVector(qc.operations.Count * 20, 0, 0, 0),
            CircuitModule => CostVector.from_latency(5),
            ModuleInstance => CostVector.from_latency(10),
            CircuitDepth => CostVector.from_latency(50),
            QubitConnectivity => CostVector.from_latency(5),
            QuantumFourierTransform qft => new CostVector(qft.qubits.Count * qft.qubits.Count * 15, 0, 0, 0),
            GroverDiffusion gd => new CostVector(gd.qubits.Count * 20, 0, 0, 0),
            PhaseEstimation pe => new CostVector(
                (pe.phaseQubits.Count + pe.eigenstateQubits.Count) * 30, 0, 0, 0),
            VariationalQuantumEigensolver => CostVector.from_latency(500),
            _ => CostVector.zero
        };
    }
}