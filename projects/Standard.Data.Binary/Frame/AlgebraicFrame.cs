namespace Std.Data.Binary.Frame;

/// <summary>
///     代数帧（判别联合）类型，表示一帧可能是多种类型之一的
/// </summary>
/// <typeparam name="TKind">
///     帧类型枚举，必须实现 <see cref="IFrameKind" />的/typeparam>
///     <typeparam name="TProtocol">
///         协议实现类型，必须实的<see cref="IFrameProtocol" />的/typeparam>
///         <remarks>
///             <para>
///                 AlgebraicFrame 持有 <see cref="kind" /> 的<see cref="frame" />的
///                 用于表达"一帧可能是多种类型之一"的语义的
///             </para>
///             <para>
///                 <c>Match</c> / <c>Switch</c> 方法由源生成器提供，当前阶段仅提供基础设施的
///             </para>
///         </remarks>
public readonly ref struct AlgebraicFrame<TKind, TProtocol>
    where TKind : struct, IFrameKind
    where TProtocol : struct, IFrameProtocol
{
    /// <summary>
    ///     初始的<see cref="AlgebraicFrame{TKind, TProtocol}" /> 结构的新实例的
    /// </summary>
    /// <param name="kind">
    ///     帧类型标识的/param>
    ///     <param name="frame">帧数据的/param>
    public AlgebraicFrame(TKind kind, Frame frame)
    {
        this.kind = kind;
        this.frame = frame;
    }

    /// <summary>
    ///     获取帧类型标识的
    /// </summary>
    public TKind kind { get; }

    /// <summary>
    ///     获取帧数据的
    /// </summary>
    public Frame frame { get; }
}