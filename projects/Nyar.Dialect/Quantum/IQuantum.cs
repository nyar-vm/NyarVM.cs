using Nyar.Dialect.Quantum.Nodes;
using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Quantum;

/// <summary>
///     Quantum 方言的 OA 接口定义。
///     该接口声明量子比特、量子门、电路结构与量子算法相关操作。
/// </summary>
[Dialect("quantum")]
public interface IQuantum<T>
{
    /// <summary>
    ///     量子比特。
    /// </summary>
    [Operator("qubit")]
    Term<T> Qubit(string qubitId);

    /// <summary>
    ///     量子寄存器。
    /// </summary>
    [Operator("quantum_register")]
    Term<T> QuantumRegister(string registerId, int size);

    /// <summary>
    ///     经典寄存器。
    /// </summary>
    [Operator("classical_register")]
    Term<T> ClassicalRegister(string registerId, int size);

    /// <summary>
    ///     单量子比特门。
    /// </summary>
    [Operator("single_qubit_gate")]
    Term<T> SingleQubitGate(SingleGateType type, Term<T> target);

    /// <summary>
    ///     参数化单量子比特门。
    /// </summary>
    [Operator("parametric_single_gate")]
    Term<T> ParametricSingleGate(SingleGateType type, Term<T> target, Term<T> angle);

    /// <summary>
    ///     受控门。
    /// </summary>
    [Operator("controlled_gate")]
    Term<T> ControlledGate(ControlledGateType type, Term<T> control, Term<T> target);

    /// <summary>
    ///     多控制门。
    /// </summary>
    [Operator("multi_controlled_gate")]
    Term<T> MultiControlledGate(ControlledGateType type, IReadOnlyList<Term<T>> controls, Term<T> target);

    /// <summary>
    ///     交换门。
    /// </summary>
    [Operator("swap_gate")]
    Term<T> SwapGate(Term<T> qubitA, Term<T> qubitB);

    /// <summary>
    ///     Toffoli 门。
    /// </summary>
    [Operator("toffoli_gate")]
    Term<T> ToffoliGate(Term<T> controlA, Term<T> controlB, Term<T> target);

    /// <summary>
    ///     测量。
    /// </summary>
    [Operator("measure")]
    Term<T> Measure(Term<T> qubit, Term<T> classicalBit);

    /// <summary>
    ///     重置。
    /// </summary>
    [Operator("reset")]
    Term<T> Reset(Term<T> qubit);

    /// <summary>
    ///     条件量子操作。
    /// </summary>
    [Operator("conditional_operation")]
    Term<T> ConditionalOperation(Term<T> classicalCondition, Term<T> quantumOperation);

    /// <summary>
    ///     量子电路。
    /// </summary>
    [Operator("quantum_circuit")]
    Term<T> QuantumCircuit(IReadOnlyList<Term<T>> operations);

    /// <summary>
    ///     电路模块。
    /// </summary>
    [Operator("circuit_module")]
    Term<T> CircuitModule(string moduleName, IReadOnlyList<string> inputQubits, IReadOnlyList<Term<T>> body);

    /// <summary>
    ///     模块实例。
    /// </summary>
    [Operator("module_instance")]
    Term<T> ModuleInstance(string moduleName, IReadOnlyDictionary<string, Term<T>> qubitMapping);

    /// <summary>
    ///     电路深度。
    /// </summary>
    [Operator("circuit_depth")]
    Term<T> CircuitDepth(Term<T> circuit);

    /// <summary>
    ///     量子比特连通性。
    /// </summary>
    [Operator("qubit_connectivity")]
    Term<T> QubitConnectivity(IReadOnlyList<(string QubitA, string QubitB)> connections);

    /// <summary>
    ///     量子傅里叶变换。
    /// </summary>
    [Operator("quantum_fourier_transform")]
    Term<T> QuantumFourierTransform(IReadOnlyList<Term<T>> qubits);

    /// <summary>
    ///     Grover 扩散算子。
    /// </summary>
    [Operator("grover_diffusion")]
    Term<T> GroverDiffusion(IReadOnlyList<Term<T>> qubits);

    /// <summary>
    ///     相位估计。
    /// </summary>
    [Operator("phase_estimation")]
    Term<T> PhaseEstimation(Term<T> unitary, IReadOnlyList<Term<T>> eigenstateQubits,
        IReadOnlyList<Term<T>> phaseQubits);

    /// <summary>
    ///     变分量子本征求解。
    /// </summary>
    [Operator("variational_quantum_eigensolver")]
    Term<T> VariationalQuantumEigensolver(Term<T> ansatz, Term<T> hamiltonian, Term<T> optimizer);
}