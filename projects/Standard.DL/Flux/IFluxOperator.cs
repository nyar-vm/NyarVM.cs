namespace Std.DL.Flux;

/// <summary>流变换算子契约</summary>
public interface IFluxOperator<in TIn, out TOut>
{
    /// <summary>应用变换</summary>
    TOut Apply(TIn input);
}