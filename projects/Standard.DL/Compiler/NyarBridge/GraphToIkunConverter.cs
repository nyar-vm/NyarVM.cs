using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Std.DL.Execution;

namespace Std.DL.Compiler.NyarBridge;

/// <summary>
///     ⛔ Galatea CompiledGraph → Nyar EGraph 遗留转换器。
///     将编译后的计算图映射为 Nyar IKun 节点并构建 EGraph&lt;IKun&gt;。
///     已冻结：请使用 OA algebra + Reifier → EGraph&lt;ENode&gt; 开放模型替代。
///     详见 .trae/specs/oa-full-pipeline/spec.md。
/// </summary>
[Obsolete("GraphToIkunConverter 依赖 EGraph<IKun> 封闭节点宇宙。请使用 OA algebra + Reifier → EGraph<ENode> 开放模型")]
public static class GraphToIkunConverter
{
    /// <summary>
    ///     将 CompiledGraph 转换为 Nyar EGraph
    /// </summary>
    /// <param name="graph">编译后的计算图</param>
    /// <returns>转换结果，包含 EGraph、名称→Id 映射和输出 Id 列表</returns>
    public static IkunConversionResult Convert(CompiledGraph graph)
    {
        var egraph = new EGraph<IKun>();
        var nameToId = new Dictionary<string, Id>();

        foreach (var inputName in graph.InputNames)
        {
            var inputId = egraph.Add(new Sym(inputName));
            nameToId[inputName] = inputId;
        }

        foreach (var op in graph.Operations)
        {
            var nodeId = ConvertOperation(op, egraph, nameToId);
            nameToId[op.Name] = nodeId;
        }

        var outputIds = new List<Id>();
        foreach (var outputName in graph.OutputNames)
            if (nameToId.TryGetValue(outputName, out var id))
                outputIds.Add(id);

        return new IkunConversionResult(egraph, nameToId, outputIds);
    }

    /// <summary>
    ///     将单个 GraphOperation 转换为 Nyar IKun 节点并添加到 EGraph
    /// </summary>
    private static Id ConvertOperation(GraphOperation op, EGraph<IKun> egraph, Dictionary<string, Id> nameToId)
    {
        var node = op.OpType switch
        {
            "Dense" => ConvertDense(op, egraph, nameToId),
            "Conv2D" => ConvertConv2D(op, egraph, nameToId),
            "ReLU" => new Relu(ResolveInputId(op, 0, nameToId)),
            "Softmax" => new Softmax(ResolveInputId(op, 0, nameToId), GetIntConfig(op, "axis", 1)),
            "GELU" => new Gelu(ResolveInputId(op, 0, nameToId)),
            "SiLU" => new Silu(ResolveInputId(op, 0, nameToId)),
            "MaxPool2D" => ConvertMaxPool(op, nameToId),
            "AvgPool2D" => ConvertAvgPool(op, nameToId),
            "Flatten" => new Flatten(ResolveInputId(op, 0, nameToId), GetIntConfig(op, "startDim", 1)),
            "Dropout" => new Dropout(ResolveInputId(op, 0, nameToId), GetFloatConfig(op, "rate", 0.5f)),
            "BatchNorm" => ConvertBatchNorm(op, egraph, nameToId),
            "LayerNorm" => ConvertLayerNorm(op, egraph, nameToId),
            "Embedding" => ConvertEmbedding(op, egraph, nameToId),
            "MatMul" => new MatMul(ResolveInputId(op, 0, nameToId), ResolveInputId(op, 1, nameToId)),
            "Reshape" => ConvertReshape(op, nameToId),
            "Concat" => ConvertConcat(op, nameToId),
            "Add" => new ElementWiseAdd(ResolveInputId(op, 0, nameToId), ResolveInputId(op, 1, nameToId)),
            "Mul" => new ElementWiseMul(ResolveInputId(op, 0, nameToId), ResolveInputId(op, 1, nameToId)),
            _ => throw new NotSupportedException($"不支持的操作类型：{op.OpType}")
        };

        return egraph.Add(node);
    }

    /// <summary>
    ///     解析指定索引的输入 Id
    /// </summary>
    private static Id ResolveInputId(GraphOperation op, int index, Dictionary<string, Id> nameToId)
    {
        if (index >= op.InputNames.Length) throw new InvalidOperationException($"操作 {op.Name} 缺少输入 #{index}");

        if (nameToId.TryGetValue(op.InputNames[index], out var id)) return id;

        throw new InvalidOperationException($"操作 {op.Name} 找不到输入：{op.InputNames[index]}");
    }

    /// <summary>
    ///     转换 Dense 操作为 MatMul + ElementWiseAdd
    /// </summary>
    private static IKun ConvertDense(GraphOperation op, EGraph<IKun> egraph, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var weightId = egraph.Add(new Sym($"{op.Name}_w"));
        nameToId[$"{op.Name}_w"] = weightId;
        var biasId = egraph.Add(new Sym($"{op.Name}_b"));
        nameToId[$"{op.Name}_b"] = biasId;

        var matmulId = egraph.Add(new MatMul(inputId, weightId));
        egraph.Add(new ElementWiseAdd(matmulId, biasId));

        return new MatMul(inputId, weightId);
    }

    /// <summary>
    ///     转换 Conv2D 操作
    /// </summary>
    private static IKun ConvertConv2D(GraphOperation op, EGraph<IKun> egraph, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var strideH = GetIntConfig(op, "strideH", 1);
        var strideW = GetIntConfig(op, "strideW", 1);
        var padH = GetIntConfig(op, "padH", 0);
        var padW = GetIntConfig(op, "padW", 0);

        var weightId = egraph.Add(new Sym($"{op.Name}_w"));
        nameToId[$"{op.Name}_w"] = weightId;
        var biasId = (Id?)egraph.Add(new Sym($"{op.Name}_b"));
        nameToId[$"{op.Name}_b"] = biasId.Value;

        return new Conv2D(inputId, weightId, biasId, (strideH, strideW), (padH, padW));
    }

    /// <summary>
    ///     转换 MaxPool 操作
    /// </summary>
    private static IKun ConvertMaxPool(GraphOperation op, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var kH = GetIntConfig(op, "kernelH", 2);
        var kW = GetIntConfig(op, "kernelW", 2);
        var sH = GetIntConfig(op, "strideH", 2);
        var sW = GetIntConfig(op, "strideW", 2);

        return new Pool(PoolType.Max, inputId, (kH, kW), (sH, sW));
    }

    /// <summary>
    ///     转换 AvgPool 操作
    /// </summary>
    private static IKun ConvertAvgPool(GraphOperation op, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var kH = GetIntConfig(op, "kernelH", 2);
        var kW = GetIntConfig(op, "kernelW", 2);
        var sH = GetIntConfig(op, "strideH", 2);
        var sW = GetIntConfig(op, "strideW", 2);

        return new Pool(PoolType.Avg, inputId, (kH, kW), (sH, sW));
    }

    /// <summary>
    ///     转换 BatchNorm 操作
    /// </summary>
    private static IKun ConvertBatchNorm(GraphOperation op, EGraph<IKun> egraph, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var epsilon = GetDoubleConfig(op, "epsilon", 1e-5);

        var scaleId = egraph.Add(new Sym($"{op.Name}_scale"));
        nameToId[$"{op.Name}_scale"] = scaleId;
        var biasId = egraph.Add(new Sym($"{op.Name}_bias"));
        nameToId[$"{op.Name}_bias"] = biasId;
        var meanId = egraph.Add(new Sym($"{op.Name}_running_mean"));
        nameToId[$"{op.Name}_running_mean"] = meanId;
        var varId = egraph.Add(new Sym($"{op.Name}_running_var"));
        nameToId[$"{op.Name}_running_var"] = varId;

        return new BatchNorm(inputId, scaleId, biasId, meanId, varId, epsilon);
    }

    /// <summary>
    ///     转换 LayerNorm 操作
    /// </summary>
    private static IKun ConvertLayerNorm(GraphOperation op, EGraph<IKun> egraph, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var epsilon = GetDoubleConfig(op, "epsilon", 1e-5);

        var weightId = egraph.Add(new Sym($"{op.Name}_weight"));
        nameToId[$"{op.Name}_weight"] = weightId;
        var biasId = egraph.Add(new Sym($"{op.Name}_bias"));
        nameToId[$"{op.Name}_bias"] = biasId;

        return new LayerNorm(inputId, weightId, biasId, epsilon);
    }

    /// <summary>
    ///     转换 Embedding 操作
    /// </summary>
    private static IKun ConvertEmbedding(GraphOperation op, EGraph<IKun> egraph, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var weightId = egraph.Add(new Sym($"{op.Name}_weight"));
        nameToId[$"{op.Name}_weight"] = weightId;

        return new Embedding(inputId, weightId);
    }

    /// <summary>
    ///     转换 Reshape 操作
    /// </summary>
    private static IKun ConvertReshape(GraphOperation op, Dictionary<string, Id> nameToId)
    {
        var inputId = ResolveInputId(op, 0, nameToId);
        var shapeStr = op.Config.TryGetValue("shape", out var s) ? s.ToString()! : "";
        var shape = shapeStr.Split(',').Select(int.Parse).ToList();

        return new Reshape(inputId, shape);
    }

    /// <summary>
    ///     转换 Concat 操作
    /// </summary>
    private static IKun ConvertConcat(GraphOperation op, Dictionary<string, Id> nameToId)
    {
        var inputs = op.InputNames.Select(name => nameToId[name]).ToList();
        var axis = GetIntConfig(op, "axis", 0);

        return new Concat(inputs, axis);
    }

    /// <summary>
    ///     从配置中获取整数值
    /// </summary>
    private static int GetIntConfig(GraphOperation op, string key, int defaultValue)
    {
        return op.Config.TryGetValue(key, out var val) ? int.Parse(val.ToString()!) : defaultValue;
    }

    /// <summary>
    ///     从配置中获取浮点值
    /// </summary>
    private static float GetFloatConfig(GraphOperation op, string key, float defaultValue)
    {
        return op.Config.TryGetValue(key, out var val) ? float.Parse(val.ToString()!) : defaultValue;
    }

    /// <summary>
    ///     从配置中获取双精度浮点值
    /// </summary>
    private static double GetDoubleConfig(GraphOperation op, string key, double defaultValue)
    {
        return op.Config.TryGetValue(key, out var val) ? double.Parse(val.ToString()!) : defaultValue;
    }
}

/// <summary>
///     ⛔ GraphToIkun 转换结果（遗留）。IKun 节点宇宙已冻结，
///     请使用 EGraph&lt;ENode&gt; 开放模型替代。
/// </summary>
[Obsolete("IKun 节点宇宙已冻结。请使用 EGraph<ENode> 开放模型")]
public sealed record IkunConversionResult(
    EGraph<IKun> Egraph,
    IReadOnlyDictionary<string, Id> NameToId,
    IReadOnlyList<Id> OutputIds);