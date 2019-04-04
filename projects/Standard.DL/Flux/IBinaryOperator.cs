namespace Std.DL.Flux;

/// <summary>二元算子（Add、MatMul）</summary>
public interface IBinaryOperator<in TInA, in TInB, out TOut>
{
    /// <summary>应用变换</summary>
    TOut Apply(TInA a, TInB b);
}