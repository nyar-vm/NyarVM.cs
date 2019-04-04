namespace Std.DL.Flux;

/// <summary>
///     自动微分上下文 —— 记录前向计算图并执行反向传播
/// </summary>
public sealed class AutogradContext
{
    private readonly List<BackwardRecord> _tape = [];

    /// <summary>是否正在记录</summary>
    public bool IsRecording { get; private set; }

    /// <summary>
    ///     开始记录前向操作
    /// </summary>
    public void StartRecording()
    {
        IsRecording = true;
        _tape.Clear();
    }

    /// <summary>
    ///     停止记录
    /// </summary>
    public void StopRecording()
    {
        IsRecording = false;
    }

    /// <summary>
    ///     记录一个前向操作及其反向函数
    /// </summary>
    /// <param name="outputs">前向输出张量</param>
    /// <param name="inputs">前向输入张量</param>
    /// <param name="backwardFn">反向传播函数：接收输出梯度，返回输入梯度</param>
    public void Record(ArrayND[] outputs, ArrayND[] inputs, BackwardFn backwardFn)
    {
        if (!IsRecording) return;

        _tape.Add(new BackwardRecord(outputs, inputs, backwardFn));
    }

    /// <summary>
    ///     记录一个单输出前向操作
    /// </summary>
    public void Record(ArrayND output, ArrayND[] inputs, BackwardFn backwardFn)
    {
        Record([output], inputs, backwardFn);
    }

    /// <summary>
    ///     执行反向传播：从损失张量开始，逆序遍历计算图
    /// </summary>
    /// <param name="loss">损失张量</param>
    public void Backward(ArrayND loss)
    {
        loss.EnsureGrad();
        var lossGrad = loss.Grad!.AsWriteSpan();
        lossGrad.Clear();
        if (lossGrad.Length > 0) lossGrad[0] = 1.0f;

        for (var i = _tape.Count - 1; i >= 0; i--)
        {
            var record = _tape[i];
            var outputGrads = new ArrayND[record.Outputs.Length];
            for (var j = 0; j < record.Outputs.Length; j++)
            {
                var output = record.Outputs[j];
                output.EnsureGrad();
                outputGrads[j] = output.Grad!;
            }

            var inputGrads = record.BackwardFn(outputGrads);

            for (var j = 0; j < record.Inputs.Length && j < inputGrads.Length; j++)
            {
                if (inputGrads[j] is null) continue;

                record.Inputs[j].EnsureGrad();
                var grad = record.Inputs[j].Grad;
                if (grad is null) continue;

                var dest = grad.AsWriteSpan();
                var src = inputGrads[j]!.AsSpan();
                for (var k = 0; k < dest.Length; k++) dest[k] += src[k];
            }
        }
    }

    /// <summary>
    ///     从指定输出张量和其已知梯度出发，执行反向传播
    ///     用于激活检查点等需要从中间输出注入梯度的场景
    /// </summary>
    /// <param name="output">输出张量（必须在当前 tape 的某条记录的输出中）</param>
    /// <param name="dOutput">输出张量的已知梯度</param>
    public void BackwardFromGradient(ArrayND output, ArrayND dOutput)
    {
        output.EnsureGrad();
        var gradSpan = output.Grad!.AsWriteSpan();
        var dSpan = dOutput.AsSpan();
        for (var i = 0; i < gradSpan.Length; i++) gradSpan[i] = dSpan[i];

        for (var i = _tape.Count - 1; i >= 0; i--)
        {
            var record = _tape[i];
            var outputGrads = new ArrayND[record.Outputs.Length];
            for (var j = 0; j < record.Outputs.Length; j++)
            {
                var outTensor = record.Outputs[j];
                outTensor.EnsureGrad();
                outputGrads[j] = outTensor.Grad!;
            }

            var inputGrads = record.BackwardFn(outputGrads);

            for (var j = 0; j < record.Inputs.Length && j < inputGrads.Length; j++)
            {
                if (inputGrads[j] is null) continue;

                record.Inputs[j].EnsureGrad();
                var grad = record.Inputs[j].Grad;
                if (grad is null) continue;

                var dest = grad.AsWriteSpan();
                var src = inputGrads[j]!.AsSpan();
                for (var k = 0; k < dest.Length; k++) dest[k] += src[k];
            }
        }
    }

    /// <summary>
    ///     清空计算图
    /// </summary>
    public void Clear()
    {
        _tape.Clear();
    }

    /// <summary>
    ///     执行反向传播并保留梯度计算图（用于高阶梯度）
    ///     与 Backward 不同，此方法在反向传播过程中同时记录梯度操作，
    ///     使得梯度本身可以被再次微分
    /// </summary>
    /// <param name="loss">损失张量</param>
    /// <param name="gradContext">用于记录梯度操作的新 AutogradContext</param>
    public void BackwardWithGradGraph(ArrayND loss, AutogradContext gradContext)
    {
        ArgumentNullException.ThrowIfNull(gradContext);

        loss.EnsureGrad();
        var lossGrad = loss.Grad!.AsWriteSpan();
        lossGrad.Clear();
        if (lossGrad.Length > 0) lossGrad[0] = 1.0f;

        gradContext.StartRecording();

        for (var i = _tape.Count - 1; i >= 0; i--)
        {
            var record = _tape[i];
            var outputGrads = new ArrayND[record.Outputs.Length];
            for (var j = 0; j < record.Outputs.Length; j++)
            {
                var output = record.Outputs[j];
                output.EnsureGrad();
                outputGrads[j] = output.Grad!;
            }

            var inputGrads = record.BackwardFn(outputGrads);

            for (var j = 0; j < record.Inputs.Length && j < inputGrads.Length; j++)
            {
                if (inputGrads[j] is null) continue;

                record.Inputs[j].EnsureGrad();
                var grad = record.Inputs[j].Grad;
                if (grad is null) continue;

                gradContext.Record(inputGrads[j]!, [record.Inputs[j]], dg =>
                {
                    var result = new ArrayND?[1];
                    result[0] = dg[0];
                    return result;
                });

                var dest = grad.AsWriteSpan();
                var src = inputGrads[j]!.AsSpan();
                for (var k = 0; k < dest.Length; k++) dest[k] += src[k];
            }
        }

        gradContext.StopRecording();
    }

    /// <summary>
    ///     计算高阶梯度：对指定张量的梯度再次求梯度
    ///     典型用途：计算 Hessian 矩阵对角线、梯度惩罚等
    /// </summary>
    /// <param name="output">输出张量</param>
    /// <param name="input">输入张量</param>
    /// <returns>二阶梯度张量（∂²output/∂input²）</returns>
    public ArrayND? HigherOrderGrad(ArrayND output, ArrayND input)
    {
        var gradCtx = new AutogradContext();
        BackwardWithGradGraph(output, gradCtx);

        if (input.Grad is null) return null;

        input.Grad.EnsureGrad();
        var gradOfGradSpan = input.Grad.Grad!.AsWriteSpan();
        gradOfGradSpan.Clear();
        if (gradOfGradSpan.Length > 0) gradOfGradSpan[0] = 1.0f;

        gradCtx.Backward(input.Grad);
        return input.Grad.Grad;
    }
}

/// <summary>
///     反向传播函数委托：接收输出梯度数组，返回输入梯度数组
/// </summary>
public delegate ArrayND?[] BackwardFn(ArrayND[] outputGrads);

/// <summary>
///     反向传播记录
/// </summary>
internal sealed class BackwardRecord
{
    public BackwardRecord(ArrayND[] outputs, ArrayND[] inputs, BackwardFn backwardFn)
    {
        Outputs = outputs;
        Inputs = inputs;
        BackwardFn = backwardFn;
    }

    public ArrayND[] Outputs { get; }
    public ArrayND[] Inputs { get; }
    public BackwardFn BackwardFn { get; }
}

/// <summary>
///     可训练参数接口
/// </summary>
public interface IParameter
{
    /// <summary>参数张量</summary>
    ArrayND Value { get; }

    /// <summary>参数梯度</summary>
    ArrayND? Grad => Value.Grad;
}

/// <summary>
///     可训练参数包装
/// </summary>
public sealed class Parameter : IParameter
{
    /// <summary>
    ///     创建可训练参数
    /// </summary>
    /// <param name="value">参数张量</param>
    public Parameter(ArrayND value)
    {
        Value = value;
    }

    /// <summary>参数张量</summary>
    public ArrayND Value { get; }
}

/// <summary>
///     可训练层接口
/// </summary>
public interface ILayer
{
    /// <summary>获取所有可训练参数</summary>
    IEnumerable<IParameter> Parameters();
}