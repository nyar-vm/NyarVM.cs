namespace Std.DL.Flux;

/// <summary>
///     激活函数
/// </summary>
public static class Activations
{
    /// <summary>
    ///     ReLU 前向：output = max(0, input)
    /// </summary>
    public static ArrayND ReLUForward(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanOut[i] = MathF.Max(0.0f, spanIn[i]);
        return result;
    }

    /// <summary>
    ///     ReLU 前向（带自动微分记录）
    /// </summary>
    public static ArrayND ReLU(ArrayND input, AutogradContext ctx)
    {
        var output = ReLUForward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = ReLUBackward(output, dOutput);
            return [dInput];
        });

        return output;
    }

    /// <summary>
    ///     ReLU 反向梯度：grad[i] = output[i] > 0 ? upstreamGrad[i] : 0
    /// </summary>
    public static ArrayND ReLUBackward(ArrayND output, ArrayND upstreamGrad)
    {
        var result = ArrayND.Zeros(output.Shape);
        var spanOut = output.AsSpan();
        var spanUp = upstreamGrad.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanOut.Length; i++) spanR[i] = spanOut[i] > 0.0f ? spanUp[i] : 0.0f;
        return result;
    }

    /// <summary>
    ///     Sigmoid 前向：output = 1 / (1 + exp(-input))
    /// </summary>
    public static ArrayND SigmoidForward(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanOut[i] = 1.0f / (1.0f + MathF.Exp(-spanIn[i]));
        return result;
    }

    /// <summary>
    ///     Sigmoid 前向（带自动微分记录）
    /// </summary>
    public static ArrayND Sigmoid(ArrayND input, AutogradContext ctx)
    {
        var output = SigmoidForward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = SigmoidBackward(output, dOutput);
            return [dInput];
        });

        return output;
    }

    /// <summary>
    ///     Sigmoid 反向梯度：grad[i] = output[i] * (1 - output[i]) * upstreamGrad[i]
    /// </summary>
    public static ArrayND SigmoidBackward(ArrayND output, ArrayND upstreamGrad)
    {
        var result = ArrayND.Zeros(output.Shape);
        var spanOut = output.AsSpan();
        var spanUp = upstreamGrad.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanOut.Length; i++) spanR[i] = spanOut[i] * (1.0f - spanOut[i]) * spanUp[i];
        return result;
    }

    /// <summary>
    ///     Tanh 前向：output = tanh(input)
    /// </summary>
    public static ArrayND TanhForward(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanOut[i] = MathF.Tanh(spanIn[i]);
        return result;
    }

    /// <summary>
    ///     Tanh 前向（带自动微分记录）
    /// </summary>
    public static ArrayND Tanh(ArrayND input, AutogradContext ctx)
    {
        var output = TanhForward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = TanhBackward(output, dOutput);
            return [dInput];
        });

        return output;
    }

    /// <summary>
    ///     Tanh 反向梯度：grad[i] = (1 - output[i]^2) * upstreamGrad[i]
    /// </summary>
    public static ArrayND TanhBackward(ArrayND output, ArrayND upstreamGrad)
    {
        var result = ArrayND.Zeros(output.Shape);
        var spanOut = output.AsSpan();
        var spanUp = upstreamGrad.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanOut.Length; i++) spanR[i] = (1.0f - spanOut[i] * spanOut[i]) * spanUp[i];
        return result;
    }

    /// <summary>
    ///     Softmax 前向：output[i] = exp(input[i] - max) / sum(exp(input[j] - max))
    /// </summary>
    public static ArrayND SoftmaxForward(ArrayND input)
    {
        var batch = input.Shape[0];
        var classes = input.Shape[1];
        var result = ArrayND.Zeros(batch, classes);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var maxVal = float.MinValue;
            for (var c = 0; c < classes; c++) maxVal = MathF.Max(maxVal, spanIn[n * classes + c]);

            var sumExp = 0.0f;
            for (var c = 0; c < classes; c++)
            {
                spanOut[n * classes + c] = MathF.Exp(spanIn[n * classes + c] - maxVal);
                sumExp += spanOut[n * classes + c];
            }

            for (var c = 0; c < classes; c++) spanOut[n * classes + c] /= sumExp;
        }

        return result;
    }

    /// <summary>
    ///     Softmax 前向（带自动微分记录）
    /// </summary>
    public static ArrayND Softmax(ArrayND input, AutogradContext ctx)
    {
        var output = SoftmaxForward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = SoftmaxBackward(output, dOutput);
            return [dInput];
        });

        return output;
    }

    /// <summary>
    ///     Softmax 反向梯度：利用雅可比矩阵的对称性
    ///     dInput[n,c] = output[n,c] * (upstream[n,c] - sum_k(upstream[n,k] * output[n,k]))
    /// </summary>
    public static ArrayND SoftmaxBackward(ArrayND output, ArrayND upstreamGrad)
    {
        var batch = output.Shape[0];
        var classes = output.Shape[1];
        var result = ArrayND.Zeros(batch, classes);
        var spanOut = output.AsSpan();
        var spanUp = upstreamGrad.AsSpan();
        var spanR = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var weightedSum = 0.0f;
            for (var c = 0; c < classes; c++) weightedSum += spanUp[n * classes + c] * spanOut[n * classes + c];

            for (var c = 0; c < classes; c++)
                spanR[n * classes + c] = spanOut[n * classes + c] * (spanUp[n * classes + c] - weightedSum);
        }

        return result;
    }

    /// <summary>
    ///     GELU 前向（tanh 近似）：output = 0.5 * x * (1 + tanh(sqrt(2/pi) * (x + 0.044715 * x^3)))
    /// </summary>
    public static ArrayND GELUForward(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();
        var coeff = MathF.Sqrt(2.0f / MathF.PI);

        for (var i = 0; i < spanIn.Length; i++)
        {
            var x = spanIn[i];
            var x3 = x * x * x;
            var inner = coeff * (x + 0.044715f * x3);
            spanOut[i] = 0.5f * x * (1.0f + MathF.Tanh(inner));
        }

        return result;
    }

    /// <summary>
    ///     GELU 前向（带自动微分记录）
    /// </summary>
    public static ArrayND GELU(ArrayND input, AutogradContext ctx)
    {
        var output = GELUForward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = GELUBackward(input, dOutput);
            return [dInput];
        });

        return output;
    }

    /// <summary>
    ///     GELU 反向梯度（解析梯度）
    /// </summary>
    public static ArrayND GELUBackward(ArrayND input, ArrayND upstreamGrad)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanUp = upstreamGrad.AsSpan();
        var spanR = result.AsWriteSpan();
        var coeff = MathF.Sqrt(2.0f / MathF.PI);

        for (var i = 0; i < spanIn.Length; i++)
        {
            var x = spanIn[i];
            var x3 = x * x * x;
            var inner = coeff * (x + 0.044715f * x3);
            var tanhInner = MathF.Tanh(inner);
            var sech2 = 1.0f - tanhInner * tanhInner;

            var dGelu = 0.5f * (1.0f + tanhInner)
                        + 0.5f * x * sech2 * coeff * (1.0f + 3.0f * 0.044715f * x * x);

            spanR[i] = dGelu * spanUp[i];
        }

        return result;
    }

    /// <summary>
    ///     SiLU 前向：output = x * sigmoid(x)
    /// </summary>
    public static ArrayND SiLUForward(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();

        for (var i = 0; i < spanIn.Length; i++)
        {
            var x = spanIn[i];
            var sigmoidX = 1.0f / (1.0f + MathF.Exp(-x));
            spanOut[i] = x * sigmoidX;
        }

        return result;
    }

    /// <summary>
    ///     SiLU 前向（带自动微分记录）
    /// </summary>
    public static ArrayND SiLU(ArrayND input, AutogradContext ctx)
    {
        var output = SiLUForward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = SiLUBackward(input, dOutput);
            return [dInput];
        });

        return output;
    }

    /// <summary>
    ///     SiLU 反向梯度：d/dx(x*sigmoid(x)) = sigmoid(x) + x * sigmoid(x) * (1-sigmoid(x))
    /// </summary>
    public static ArrayND SiLUBackward(ArrayND input, ArrayND upstreamGrad)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanUp = upstreamGrad.AsSpan();
        var spanR = result.AsWriteSpan();

        for (var i = 0; i < spanIn.Length; i++)
        {
            var x = spanIn[i];
            var sigX = 1.0f / (1.0f + MathF.Exp(-x));
            var dSiLU = sigX + x * sigX * (1.0f - sigX);
            spanR[i] = dSiLU * spanUp[i];
        }

        return result;
    }

    /// <summary>
    ///     Softmax 沿指定轴：output[..., i, ...] = exp(input[...] - max) / sum
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="axis">要归一化的轴</param>
    /// <returns>输出张量（同形状）</returns>
    public static ArrayND SoftmaxAxis(ArrayND input, int axis)
    {
        var ndim = input.Shape.Length;
        var result = ArrayND.Zeros(input.Shape);

        Span<int> strides = stackalloc int[ndim];
        ArrayND.ComputeStrides(input.Shape, strides);
        var axisSize = input.Shape[axis];
        var axisStride = strides[axis];

        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();

        var groupsOuter = input.Size / (axisSize * axisStride);
        var groupsInner = 1;
        for (var d = axis + 1; d < ndim; d++) groupsInner *= input.Shape[d];

        for (var outer = 0; outer < groupsOuter; outer++)
        {
            var baseOff = outer * axisSize * axisStride;

            for (var inner = 0; inner < groupsInner; inner++)
            {
                var maxVal = float.MinValue;
                for (var i = 0; i < axisSize; i++)
                {
                    var flat = baseOff + i * groupsInner + inner;
                    maxVal = MathF.Max(maxVal, spanIn[flat]);
                }

                var sumExp = 0.0f;
                for (var i = 0; i < axisSize; i++)
                {
                    var flat = baseOff + i * groupsInner + inner;
                    spanOut[flat] = MathF.Exp(spanIn[flat] - maxVal);
                    sumExp += spanOut[flat];
                }

                var invSum = 1.0f / sumExp;
                for (var i = 0; i < axisSize; i++)
                {
                    var flat = baseOff + i * groupsInner + inner;
                    spanOut[flat] *= invSum;
                }
            }
        }

        return result;
    }
}