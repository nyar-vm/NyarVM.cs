using System.Text;
using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     模型摘要工具 —— 参数统计、层结构展示
/// </summary>
public static class ModelSummary
{
    /// <summary>
    ///     统计模型的总参数数量
    /// </summary>
    /// <param name="parameters">模型参数集合</param>
    /// <returns>可训练参数总数</returns>
    public static int CountParameters(IEnumerable<IParameter> parameters)
    {
        return parameters.Sum(p => p.Value.Size);
    }

    /// <summary>
    ///     统计 Sequential 模型的参数并生成摘要
    /// </summary>
    /// <param name="model">Sequential 模型</param>
    /// <param name="inputShape">输入形状（例：[1, 784]）</param>
    /// <returns>格式化的模型摘要字符串</returns>
    public static string Summarize(Sequential model, int[] inputShape)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine("  模型摘要");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine($"{"层名",-25} {"输出形状",-20} {"参数量",-10}");
        sb.AppendLine("───────────────────────────────────────");

        var totalParams = 0;
        var currentShape = inputShape;

        for (var i = 0; i < model.Count; i++)
        {
            var layer = model[i];
            var name = $"{i}: {layer.GetType().Name}";
            var layerParams = CountParameters(layer.Parameters());
            totalParams += layerParams;

            currentShape = InferOutputShape(layer, currentShape);

            var shapeStr = $"[{string.Join(", ", currentShape)}]";
            sb.AppendLine($"{name,-25} {shapeStr,-20} {layerParams,-10}");
        }

        sb.AppendLine("───────────────────────────────────────");
        sb.AppendLine($"  总参数量: {totalParams:N0}");
        sb.AppendLine("═══════════════════════════════════════");

        return sb.ToString();
    }

    private static int[] InferOutputShape(ITrainableModel layer, int[] inputShape)
    {
        return layer switch
        {
            Dense dense => [inputShape[0], dense.Weight.Shape[1]],
            LayerNorm => inputShape,
            RMSNorm => inputShape,
            BatchNorm => inputShape,
            Dropout => inputShape,
            Sequential seq => InferSequentialShape(seq, inputShape),
            _ => inputShape
        };
    }

    private static int[] InferSequentialShape(Sequential seq, int[] inputShape)
    {
        var shape = inputShape;
        for (var i = 0; i < seq.Count; i++) shape = InferOutputShape(seq[i], shape);
        return shape;
    }

    private readonly record struct LayerInfo(string Name, int[] OutputShape, int Params);
}