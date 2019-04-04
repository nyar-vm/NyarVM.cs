using Nyar.Dialect.Neural.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.Dialect.Neural.Cost;

/// <summary>
///     Tensor 方言节点的成本估算钩子
/// </summary>
public sealed class TensorCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Conv2D or MatMul or Pool or BatchNorm or Relu or Softmax
            or Reshape or Transpose or Concat or Slice or FusedConvBnRelu or Grad
            or OptimizerStep or Loss
            or FusedAttention or RmsNorm or LayerNorm or Embedding
            or RotaryPositionEncoding or Silu or Gelu or LoRA or Checkpoint
            or KvCache or Cast or Sigmoid or Tanh or Dense or Dropout
            or Flatten or ElementWiseAdd or ElementWiseMul;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            #region 基础算子

            Conv2D => new CostVector(1000, 10, 1024 * 1024, 0),
            MatMul => new CostVector(500, 5, 512 * 1024, 0),
            Pool => new CostVector(100, 1, 128 * 1024, 0),
            BatchNorm => new CostVector(200, 2, 256 * 1024, 0),
            Relu => CostVector.from_latency(10),
            Softmax => new CostVector(100, 1, 64 * 1024, 0),
            Reshape => CostVector.from_latency(1),
            Transpose => new CostVector(50, 0, 128 * 1024, 0),
            Concat => new CostVector(50, 0, 128 * 1024, 0),
            Slice => CostVector.from_latency(10),
            FusedConvBnRelu => new CostVector(800, 8, 1024 * 1024, 0),
            Grad => new CostVector(2000, 20, 2048 * 1024, 0),
            OptimizerStep => new CostVector(100, 1, 256 * 1024, 0),
            Loss => new CostVector(50, 0, 64 * 1024, 0),

            #endregion

            #region Transformer/LLM 算子

            FusedAttention => new CostVector(2000, 15, 2048 * 1024, 0),
            RmsNorm => new CostVector(150, 1, 128 * 1024, 0),
            LayerNorm => new CostVector(200, 2, 256 * 1024, 0),
            Embedding => new CostVector(50, 0, 512 * 1024, 0),
            RotaryPositionEncoding => new CostVector(100, 1, 64 * 1024, 0),
            Silu => CostVector.from_latency(15),
            Gelu => new CostVector(20, 0, 0, 0),
            LoRA => new CostVector(600, 5, 512 * 1024, 0),
            Checkpoint => CostVector.from_latency(0),
            KvCache => new CostVector(10, 0, 1024 * 1024, 0),
            Cast => new CostVector(30, 0, 128 * 1024, 0),
            Sigmoid => CostVector.from_latency(15),
            Tanh => CostVector.from_latency(15),
            Dense => new CostVector(400, 4, 512 * 1024, 0),
            Dropout => CostVector.from_latency(5),
            Flatten => CostVector.from_latency(1),
            ElementWiseAdd => CostVector.from_latency(10),
            ElementWiseMul => CostVector.from_latency(10),

            #endregion

            _ => CostVector.zero
        };
    }
}