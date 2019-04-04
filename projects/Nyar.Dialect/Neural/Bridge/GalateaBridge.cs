using System.Collections.Immutable;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Neural.Nodes;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using PoolType = Nyar.Dialect.Neural.Nodes.PoolType;
using Reshape = Nyar.Dialect.Neural.Nodes.Reshape;

namespace Nyar.Dialect.Neural.Bridge;

/// <summary>
///     Galatea 计算图到 Nyar Oa EGraph 的桥接转换器。
///     将 Galatea 的 CompiledGraph 操作序列转换为 Nyar Tensor Oa 节点，
///     注入 EGraph 后可利用 Nyar 的饱和优化引擎进行算子融合和降级。
/// </summary>
public sealed class GalateaBridge
{
    /// <summary>
    ///     将 Galatea 风格的操作序列转换为 Nyar Oa EGraph。
    /// </summary>
    /// <param name="operations">操作列表，每个操作包含 OpType、Config、InputNames、Parameters。</param>
    /// <param name="outputNames">需要提取的输出名称列表。</param>
    /// <returns>桥接结果，包含 EGraph、输出 ID 映射和统计信息。</returns>
    public static BridgeResult ConvertToEGraph(
        IReadOnlyList<GalateaOperation> operations,
        IReadOnlyList<string> outputNames)
    {
        var egraph = new EGraph<Oa>();
        var nameToId = new Dictionary<string, Id>();
        var stats = new BridgeStats();

        foreach (var op in operations)
        {
            var inputIds = op.InputNames
                .Where(nameToId.ContainsKey)
                .Select(name => nameToId[name])
                .ToImmutableArray();

            var nodeId = ConvertOperation(egraph, op.OpType, op.Config, inputIds, stats);

            if (nodeId.HasValue)
            {
                nameToId[op.Name] = nodeId.Value;
            }
            else
            {
                stats.SkippedCount++;
                nameToId[op.Name] = inputIds.Length > 0 ? inputIds[0] : egraph.add(new Literal<object?>(null));
            }
        }

        var outputIds = outputNames
            .Where(nameToId.ContainsKey)
            .Select(name => nameToId[name])
            .ToImmutableArray();

        return new BridgeResult(egraph, nameToId, outputIds, stats);
    }

    /// <summary>
    ///     对已转换的 EGraph 执行 Tensor 方言优化并提取最优程序。
    /// </summary>
    /// <param name="egraph">已注入 Tensor Oa 节点的 EGraph。</param>
    /// <param name="rootId">提取根节点 ID。</param>
    /// <param name="iterations">饱和迭代次数，默认 5。</param>
    /// <returns>优化后的 Oa 节点。</returns>
    public static Oa OptimizeAndExtract(EGraph<Oa> egraph, Id rootId, int iterations = 5)
    {
        var dialect = new TensorDialect();
        var allRules = dialect.rules;
        var engine = new SaturationEngine<Oa>(allRules, iterations);
        engine.run(egraph);

        var extractor = new Extractor(egraph, new DefaultCostModel());
        return extractor.extract(rootId);
    }

    private static Id? ConvertOperation(
        EGraph<Oa> egraph,
        string opType,
        Dictionary<string, object> config,
        ImmutableArray<Id> inputIds,
        BridgeStats stats)
    {
        stats.TotalCount++;

        var ikun = opType.ToUpperInvariant() switch
        {
            "CONV2D" => ConvertConv2D(egraph, inputIds, config),
            "DENSE" or "LINEAR" => ConvertDense(egraph, inputIds, config),
            "MATMUL" => ConvertMatMul(egraph, inputIds),
            "RELU" => ConvertUnary<Relu>(egraph, inputIds),
            "SIGMOID" => ConvertUnary<Sigmoid>(egraph, inputIds),
            "TANH" => ConvertUnary<Tanh>(egraph, inputIds),
            "SILU" => ConvertUnary<Silu>(egraph, inputIds),
            "GELU" => ConvertUnary<Gelu>(egraph, inputIds),
            "SOFTMAX" => ConvertSoftmax(egraph, inputIds, config),
            "BATCHNORM" => ConvertBatchNorm(egraph, inputIds, config),
            "LAYERNORM" => ConvertLayerNorm(egraph, inputIds, config),
            "RMSNORM" => ConvertRmsNorm(egraph, inputIds, config),
            "MAXPOOL2D" => ConvertPool(egraph, inputIds, PoolType.Max, config),
            "AVGPOOL2D" => ConvertPool(egraph, inputIds, PoolType.Avg, config),
            "FLATTEN" => ConvertFlatten(egraph, inputIds, config),
            "DROPOUT" => ConvertDropout(egraph, inputIds, config),
            "EMBEDDING" => ConvertEmbedding(egraph, inputIds),
            "ELEMENTWISEADD" or "ADD" => ConvertBinary<ElementWiseAdd>(egraph, inputIds),
            "ELEMENTWISEMUL" or "MUL" => ConvertBinary<ElementWiseMul>(egraph, inputIds),
            "RESHAPE" => ConvertReshape(egraph, inputIds, config),
            "TRANSPOSE" => ConvertTranspose(egraph, inputIds, config),
            "CONCAT" => ConvertConcat(egraph, inputIds, config),
            "FUSEDCONVBNRELU" => ConvertFusedConvBnRelu(egraph, inputIds, config),
            "FUSEDATTENTION" => ConvertFusedAttention(egraph, inputIds, config),
            _ => null
        };

        if (ikun is not null)
        {
            stats.ConvertedCount++;
            return egraph.add(ikun);
        }

        return null;
    }

    private static Oa ConvertConv2D(EGraph<Oa> egraph, ImmutableArray<Id> inputs, Dictionary<string, object> config)
    {
        if (inputs.Length < 2) return new Literal<object?>(null);

        var weights = inputs.Length > 1 ? inputs[1] : inputs[0];
        var bias = inputs.Length > 2 ? inputs[2] : (Id?)null;
        var strideH = GetIntConfig(config, "stride_h", 1);
        var strideW = GetIntConfig(config, "stride_w", 1);
        var padH = GetIntConfig(config, "pad_h", 0);
        var padW = GetIntConfig(config, "pad_w", 0);

        return new Conv2D(inputs[0], weights, bias, (strideH, strideW), (padH, padW));
    }

    private static Oa ConvertDense(EGraph<Oa> egraph, ImmutableArray<Id> inputs, Dictionary<string, object> config)
    {
        if (inputs.Length < 2) return new Literal<object?>(null);

        var bias = inputs.Length > 2 ? inputs[2] : (Id?)null;
        return new Dense(inputs[0], inputs[1], bias);
    }

    private static Oa ConvertMatMul(EGraph<Oa> egraph, ImmutableArray<Id> inputs)
    {
        if (inputs.Length < 2) return new Literal<object?>(null);
        return new MatMul(inputs[0], inputs[1]);
    }

    private static Oa ConvertUnary<T>(EGraph<Oa> egraph, ImmutableArray<Id> inputs) where T : Oa
    {
        if (inputs.Length < 1) return new Literal<object?>(null);

        return typeof(T).Name switch
        {
            nameof(Relu) => new Relu(inputs[0]),
            nameof(Sigmoid) => new Sigmoid(inputs[0]),
            nameof(Tanh) => new Tanh(inputs[0]),
            nameof(Silu) => new Silu(inputs[0]),
            nameof(Gelu) => new Gelu(inputs[0]),
            _ => new Literal<object?>(null)
        };
    }

    private static Oa ConvertSoftmax(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 1) return new Literal<object?>(null);
        var axis = GetIntConfig(config, "axis", -1);
        return new Softmax(inputs[0], axis);
    }

    private static Oa ConvertBatchNorm(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 5) return new Literal<object?>(null);
        var epsilon = GetDoubleConfig(config, "epsilon", 1e-5);
        return new BatchNorm(inputs[0], inputs[1], inputs[2], inputs[3], inputs[4], epsilon);
    }

    private static Oa ConvertLayerNorm(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 3) return new Literal<object?>(null);
        var epsilon = GetDoubleConfig(config, "epsilon", 1e-5);
        return new LayerNorm(inputs[0], inputs[1], inputs[2], epsilon);
    }

    private static Oa ConvertRmsNorm(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 2) return new Literal<object?>(null);
        var epsilon = GetDoubleConfig(config, "epsilon", 1e-6);
        return new RmsNorm(inputs[0], inputs[1], epsilon);
    }

    private static Oa ConvertPool(EGraph<Oa> egraph, ImmutableArray<Id> inputs, PoolType poolType,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 1) return new Literal<object?>(null);
        var kh = GetIntConfig(config, "kernel_h", 2);
        var kw = GetIntConfig(config, "kernel_w", 2);
        var sh = GetIntConfig(config, "stride_h", 2);
        var sw = GetIntConfig(config, "stride_w", 2);
        return new Pool(poolType, inputs[0], (kh, kw), (sh, sw));
    }

    private static Oa ConvertFlatten(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 1) return new Literal<object?>(null);
        var startDim = GetIntConfig(config, "start_dim", 1);
        return new Flatten(inputs[0], startDim);
    }

    private static Oa ConvertDropout(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 1) return new Literal<object?>(null);
        var rate = GetDoubleConfig(config, "rate", 0.5);
        return new Dropout(inputs[0], rate);
    }

    private static Oa ConvertEmbedding(EGraph<Oa> egraph, ImmutableArray<Id> inputs)
    {
        if (inputs.Length < 2) return new Literal<object?>(null);
        return new Embedding(inputs[0], inputs[1]);
    }

    private static Oa ConvertBinary<T>(EGraph<Oa> egraph, ImmutableArray<Id> inputs) where T : Oa
    {
        if (inputs.Length < 2) return new Literal<object?>(null);

        return typeof(T).Name switch
        {
            nameof(ElementWiseAdd) => new ElementWiseAdd(inputs[0], inputs[1]),
            nameof(ElementWiseMul) => new ElementWiseMul(inputs[0], inputs[1]),
            _ => new Literal<object?>(null)
        };
    }

    private static Oa ConvertReshape(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 1) return new Literal<object?>(null);
        var shape = GetIntListConfig(config, "shape");
        return new Reshape(inputs[0], shape);
    }

    private static Oa ConvertTranspose(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 1) return new Literal<object?>(null);
        var perm = GetIntListConfig(config, "perm");
        return new Transpose(inputs[0], perm);
    }

    private static Oa ConvertConcat(EGraph<Oa> egraph, ImmutableArray<Id> inputs, Dictionary<string, object> config)
    {
        if (inputs.Length < 1) return new Literal<object?>(null);
        var axis = GetIntConfig(config, "axis", 0);
        return new Concat(inputs.ToArray(), axis);
    }

    private static Oa ConvertFusedConvBnRelu(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 7) return new Literal<object?>(null);
        var epsilon = GetDoubleConfig(config, "epsilon", 1e-5);
        return new FusedConvBnRelu(inputs[0], inputs[1], null, inputs[2], inputs[3], inputs[4], inputs[5], epsilon);
    }

    private static Oa ConvertFusedAttention(EGraph<Oa> egraph, ImmutableArray<Id> inputs,
        Dictionary<string, object> config)
    {
        if (inputs.Length < 3) return new Literal<object?>(null);
        var heads = GetIntConfig(config, "heads", 8);
        var headDim = GetIntConfig(config, "head_dim", 64);
        var dropoutRate = GetOptionalDoubleConfig(config, "dropout_rate");
        var mask = inputs.Length > 3 ? inputs[3] : (Id?)null;
        return new FusedAttention(inputs[0], inputs[1], inputs[2], mask, heads, headDim, dropoutRate);
    }

    #region 配置辅助

    private static int GetIntConfig(Dictionary<string, object> config, string key, int defaultValue)
    {
        if (config.TryGetValue(key, out var value))
            return value switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)d,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => defaultValue
            };

        return defaultValue;
    }

    private static double GetDoubleConfig(Dictionary<string, object> config, string key, double defaultValue)
    {
        if (config.TryGetValue(key, out var value))
            return value switch
            {
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                string s when double.TryParse(s, out var parsed) => parsed,
                _ => defaultValue
            };

        return defaultValue;
    }

    private static double? GetOptionalDoubleConfig(Dictionary<string, object> config, string key)
    {
        if (config.TryGetValue(key, out var value))
            return value switch
            {
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                _ => null
            };

        return null;
    }

    private static IReadOnlyList<int> GetIntListConfig(Dictionary<string, object> config, string key)
    {
        if (config.TryGetValue(key, out var value) && value is IReadOnlyList<int> list) return list;

        return [0, -1];
    }

    #endregion
}