using Nyar.Dialect.Hardware.Nodes;
using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Hardware;

/// <summary>
///     Hardware 方言的 OA 接口定义。
///     该接口声明硬件结构、微架构意图与门级操作。
/// </summary>
[Dialect("hardware")]
public interface IHardware<T>
{
    /// <summary>
    ///     模块定义。
    /// </summary>
    [Operator("module")]
    Term<T> Module(string name, IReadOnlyList<Port> ports, Term<T> body);

    /// <summary>
    ///     端口定义。
    /// </summary>
    [Operator("port")]
    Term<T> Port(PortDirection direction, string name, int bitWidth);

    /// <summary>
    ///     连线定义。
    /// </summary>
    [Operator("wire")]
    Term<T> Wire(string name, int bitWidth);

    /// <summary>
    ///     寄存器定义。
    /// </summary>
    [Operator("reg")]
    Term<T> Reg(string name, int bitWidth, Term<T>? initialValue);

    /// <summary>
    ///     时序块定义。
    /// </summary>
    [Operator("always")]
    Term<T> Always(AlwaysTrigger trigger, Term<T> body);

    /// <summary>
    ///     流水线结构。
    /// </summary>
    [Operator("pipeline")]
    Term<T> Pipeline(int stageCount, Term<T> body);

    /// <summary>
    ///     循环展开。
    /// </summary>
    [Operator("unroll")]
    Term<T> Unroll(int factor, Term<T> loop);

    /// <summary>
    ///     脉动阵列。
    /// </summary>
    [Operator("systolic_array")]
    Term<T> SystolicArray(int rows, int cols, Term<T> cellFunction);

    /// <summary>
    ///     门级逻辑。
    /// </summary>
    [Operator("gate")]
    Term<T> Gate(GateType type, IReadOnlyList<Term<T>> inputs);

    /// <summary>
    ///     触发器。
    /// </summary>
    [Operator("flip_flop")]
    Term<T> FlipFlop(Term<T> data, Term<T> clock, Term<T>? reset, Term<T>? resetValue);

    /// <summary>
    ///     位切片。
    /// </summary>
    [Operator("bit_slice")]
    Term<T> BitSlice(Term<T> value, int high, int low);

    /// <summary>
    ///     位拼接。
    /// </summary>
    [Operator("concat")]
    Term<T> Concat(IReadOnlyList<Term<T>> values);

    /// <summary>
    ///     多路复用。
    /// </summary>
    [Operator("mux")]
    Term<T> Mux(Term<T> select, IReadOnlyList<Term<T>> inputs);
}