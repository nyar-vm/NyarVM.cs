using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     全连接层 —— Dense / Linear
/// </summary>
public sealed class Dense : ILayer, ITrainableModel
{
    private readonly int _fanIn;
    private readonly int _fanOut;

    /// <summary>
    ///     创建全连接层
    /// </summary>
    /// <param name="fanIn">输入维度</param>
    /// <param name="fanOut">输出维度</param>
    public Dense(int fanIn, int fanOut)
    {
        _fanIn = fanIn;
        _fanOut = fanOut;
        Weight = ArrayND.HeNormal(fanIn, fanIn, fanOut);
        Bias = ArrayND.Zeros(1, fanOut);
    }

    /// <summary>权重矩阵 [fanIn, fanOut]</summary>
    public ArrayND Weight { get; }

    /// <summary>偏置向量 [1, fanOut]</summary>
    public ArrayND Bias { get; }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        yield return new Parameter(Weight);
        yield return new Parameter(Bias);
    }

    /// <summary>
    ///     前向传播：output = input × Weight + Bias
    ///     支持多维输入，自动 flatten 除最后维以外的所有前导维度
    /// </summary>
    /// <param name="input">输入张量 [..., fanIn]</param>
    public ArrayND forward(ArrayND input)
    {
        if (input.Shape.Length == 2) return ArrayND.MatMul(input, Weight) + Bias;

        var ndim = input.Shape.Length;
        var batchSize = 1;
        for (var d = 0; d < ndim - 1; d++) batchSize *= input.Shape[d];
        var fanInDim = input.Shape[ndim - 1];
        var flatInput = input.Reshape(batchSize, fanInDim);
        var flatOutput = ArrayND.MatMul(flatInput, Weight) + Bias;

        var outShape = new int[ndim];
        Array.Copy(input.Shape, outShape, ndim);
        outShape[ndim - 1] = _fanOut;
        return flatOutput.Reshape(outShape);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入张量 [..., fanIn]</param>
    /// <param name="ctx">自动微分上下文</param>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var flatInput = input;
        var originalShape = input.Shape;
        var wasFlattened = false;

        if (input.Shape.Length > 2)
        {
            var ndim = input.Shape.Length;
            var batchSize = 1;
            for (var d = 0; d < ndim - 1; d++) batchSize *= input.Shape[d];
            flatInput = input.Reshape(batchSize, input.Shape[ndim - 1]);
            wasFlattened = true;
        }

        var inputMatmul = ArrayND.MatMul(flatInput, Weight);
        var output = inputMatmul + Bias;

        var savedFlatInput = flatInput;
        var savedWasFlattened = wasFlattened;
        var savedOriginalShape = originalShape;

        ctx.Record(output, [input, Weight, Bias], outputGrads =>
        {
            var dOutput = outputGrads[0];

            var dFlatInput = ArrayND.MatMul(dOutput, Weight.Transpose());
            var dWeight = ArrayND.MatMul(savedFlatInput.Transpose(), dOutput);
            var dBias = dOutput.SumAlongAxis0();

            var dInput = savedWasFlattened ? dFlatInput.Reshape(savedOriginalShape) : dFlatInput;

            return [dInput, dWeight, dBias];
        });

        if (wasFlattened)
        {
            var ndim = originalShape.Length;
            var outShape = new int[ndim];
            Array.Copy(originalShape, outShape, ndim);
            outShape[ndim - 1] = _fanOut;
            return output.Reshape(outShape);
        }

        return output;
    }
}