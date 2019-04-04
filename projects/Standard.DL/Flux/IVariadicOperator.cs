namespace Std.DL.Flux;

/// <summary>多元算子（Concat、Sum）</summary>
public interface IVariadicOperator<out TOut>
{
    /// <summary>应用变换</summary>
    TOut Apply(params ArrayND[] inputs);
}