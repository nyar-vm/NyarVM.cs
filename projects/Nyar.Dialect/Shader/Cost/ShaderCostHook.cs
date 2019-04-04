using Nyar.Dialect.Shader.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;
using ShaderBarrier = Nyar.Dialect.Shader.Nodes.Barrier;

namespace Nyar.Dialect.Shader.Cost;

/// <summary>
///     Shader 方言节点的成本估算钩子
/// </summary>
public sealed class ShaderCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is ShaderDecl or ShaderStageDecl or ComputeKernel or ShaderBarrier
            or GlobalInvocationId or LocalInvocationId or WorkGroupId or NumWorkGroups
            or UniformLoad or StorageLoad or StorageStore or SharedLoad or SharedStore
            or Vec or VecExtract or VecLoad or ArrayLoad or Mat or MatMul
            or Sample or SampleLod or SampleGrad or SampleDref
            or TextureLoad or TextureStore
            or TextureSize or TextureQueryLod or TextureQueryLevels or TextureQuerySamples
            or ImageGather or ImageDrefGather
            or AtomicAdd or AtomicExchange or AtomicCompareExchange
            or Dot or Cross or Length or Normalize or Reflect or Refract
            or UniformBufferDecl or UniformBufferLoad
            or PushConstantDecl or PushConstantLoad
            or CombinedImageSampler or SamplerDecl or SampledImageDecl or StorageImageDecl
            or StorageBufferDecl or InputAttachmentDecl
            or VertexBinding or BuiltinVariable or GeometryInput or GeometryOutput;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            ShaderDecl => CostVector.from_latency(1),
            ShaderStageDecl => CostVector.from_latency(1),
            ComputeKernel => CostVector.from_latency(100),
            ShaderBarrier => new CostVector(50, 0, 0, 0),
            GlobalInvocationId => CostVector.from_latency(0.1),
            LocalInvocationId => CostVector.from_latency(0.1),
            WorkGroupId => CostVector.from_latency(0.1),
            NumWorkGroups => CostVector.from_latency(0.1),
            UniformLoad => CostVector.from_latency(5),
            StorageLoad => CostVector.from_latency(10),
            StorageStore => CostVector.from_latency(10),
            SharedLoad => CostVector.from_latency(2),
            SharedStore => CostVector.from_latency(2),
            Vec => CostVector.from_latency(1),
            VecExtract => CostVector.from_latency(0.5),
            VecLoad => CostVector.from_latency(2),
            ArrayLoad => CostVector.from_latency(3),
            Mat => CostVector.from_latency(5),
            MatMul => new CostVector(50, 0, 128, 0),
            Sample => new CostVector(20, 0, 64, 0),
            SampleLod => new CostVector(20, 0, 64, 0),
            SampleGrad => new CostVector(25, 0, 64, 0),
            SampleDref => new CostVector(22, 0, 64, 0),
            TextureLoad => new CostVector(15, 0, 32, 0),
            TextureStore => new CostVector(15, 0, 32, 0),
            TextureSize => CostVector.from_latency(5),
            TextureQueryLod => CostVector.from_latency(5),
            TextureQueryLevels => CostVector.from_latency(3),
            TextureQuerySamples => CostVector.from_latency(3),
            ImageGather => new CostVector(25, 0, 64, 0),
            ImageDrefGather => new CostVector(28, 0, 64, 0),
            AtomicAdd => new CostVector(30, 0, 0, 0),
            AtomicExchange => new CostVector(30, 0, 0, 0),
            AtomicCompareExchange => new CostVector(40, 0, 0, 0),
            Dot => CostVector.from_latency(5),
            Cross => CostVector.from_latency(5),
            Length => CostVector.from_latency(5),
            Normalize => CostVector.from_latency(10),
            Reflect => CostVector.from_latency(5),
            Refract => CostVector.from_latency(10),
            UniformBufferDecl => CostVector.from_latency(0),
            UniformBufferLoad => CostVector.from_latency(5),
            PushConstantDecl => CostVector.from_latency(0),
            PushConstantLoad => CostVector.from_latency(3),
            CombinedImageSampler => CostVector.from_latency(0),
            SamplerDecl => CostVector.from_latency(0),
            SampledImageDecl => CostVector.from_latency(0),
            StorageImageDecl => CostVector.from_latency(0),
            StorageBufferDecl => CostVector.from_latency(0),
            InputAttachmentDecl => CostVector.from_latency(0),
            VertexBinding => CostVector.from_latency(0),
            BuiltinVariable => CostVector.from_latency(0.1),
            GeometryInput => CostVector.from_latency(0),
            GeometryOutput => CostVector.from_latency(0),
            _ => CostVector.zero
        };
    }
}