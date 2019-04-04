using Std.DL.Flux;

namespace Std.DL.Conflux;

/// <summary>多路 Flux 融合为总场</summary>
public interface IConfluxFusion
{
    /// <summary>使用默认策略融合</summary>
    ArrayND Fuse(params ArrayND[] inputs);

    /// <summary>使用指定策略融合</summary>
    ArrayND FuseWithStrategy(FusionStrategy strategy, params ArrayND[] inputs);
}