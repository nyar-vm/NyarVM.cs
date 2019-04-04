namespace Std.DL.Flux;

/// <summary>一元算子（ReLU、SiLU、Conv2D）</summary>
public interface IUnaryOperator<in TIn, out TOut> : IFluxOperator<TIn, TOut>
{
}