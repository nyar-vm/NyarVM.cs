using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     知识蒸馏框架 —— 用大模型（Teacher）的软标签指导小模型（Student）训练
///     核心思想：Student 同时学习硬标签（真实标签）和 Teacher 的软标签（logits 分布）
///     损失 = α × HardLoss(student_logits, labels) + (1-α) × SoftLoss(student_logits/T, teacher_logits/T)
///     其中 T 是温度参数，T 越大软标签越平滑，知识传递越丰富
/// </summary>
public static class KnowledgeDistillation
{
    /// <summary>
    ///     计算蒸馏损失（Softmax + KL 散度）
    ///     对 student 和 teacher 的 logits 施加温度缩放后计算 KL 散度
    /// </summary>
    /// <param name="studentLogits">学生模型 logits [batch, vocabSize]</param>
    /// <param name="teacherLogits">教师模型 logits [batch, vocabSize]</param>
    /// <param name="temperature">温度参数（>1 使分布更平滑）</param>
    /// <returns>蒸馏损失（标量）</returns>
    public static float DistillationLoss(ArrayND studentLogits, ArrayND teacherLogits, float temperature)
    {
        var batchSize = studentLogits.Shape[0];
        var vocabSize = studentLogits.Shape[1];

        var spanS = studentLogits.AsSpan();
        var spanT = teacherLogits.AsSpan();

        var totalLoss = 0.0f;

        for (var b = 0; b < batchSize; b++)
        {
            var maxS = float.NegativeInfinity;
            var maxT = float.NegativeInfinity;
            var off = b * vocabSize;

            for (var i = 0; i < vocabSize; i++)
            {
                if (spanS[off + i] > maxS) maxS = spanS[off + i];

                if (spanT[off + i] > maxT) maxT = spanT[off + i];
            }

            var sumExpS = 0.0f;
            var sumExpT = 0.0f;
            var logProbsS = new float[vocabSize];
            var probsT = new float[vocabSize];

            for (var i = 0; i < vocabSize; i++)
            {
                logProbsS[i] = (spanS[off + i] - maxS) / temperature;
                probsT[i] = MathF.Exp((spanT[off + i] - maxT) / temperature);
                sumExpS += MathF.Exp(logProbsS[i]);
                sumExpT += probsT[i];
            }

            var logSumExpS = MathF.Log(sumExpS);
            var klDiv = 0.0f;

            for (var i = 0; i < vocabSize; i++)
            {
                var logPS = logProbsS[i] - logSumExpS;
                var pT = probsT[i] / sumExpT;
                if (pT > 1e-10f) klDiv += pT * (MathF.Log(pT) - logPS);
            }

            totalLoss += klDiv * temperature * temperature;
        }

        return totalLoss / batchSize;
    }

    /// <summary>
    ///     计算蒸馏损失（带自动微分）
    /// </summary>
    /// <param name="studentLogits">学生模型 logits</param>
    /// <param name="teacherLogits">教师模型 logits</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>蒸馏损失</returns>
    public static ArrayND DistillationLossWithGrad(ArrayND studentLogits, ArrayND teacherLogits,
        float temperature, AutogradContext ctx)
    {
        var batchSize = studentLogits.Shape[0];
        var vocabSize = studentLogits.Shape[1];

        var loss = DistillationLoss(studentLogits, teacherLogits, temperature);
        var lossTensor = ArrayND.Zeros();
        lossTensor.AsWriteSpan()[0] = loss;

        ctx.Record(lossTensor, [studentLogits], grads =>
        {
            var dLoss = grads[0].AsSpan()[0];
            var dStudent = ArrayND.Zeros(batchSize, vocabSize);
            var spanS = studentLogits.AsSpan();
            var spanT = teacherLogits.AsSpan();
            var spanD = dStudent.AsWriteSpan();

            for (var b = 0; b < batchSize; b++)
            {
                var off = b * vocabSize;
                var maxS = float.NegativeInfinity;
                var maxT = float.NegativeInfinity;

                for (var i = 0; i < vocabSize; i++)
                {
                    if (spanS[off + i] > maxS) maxS = spanS[off + i];

                    if (spanT[off + i] > maxT) maxT = spanT[off + i];
                }

                var sumExpS = 0.0f;
                var sumExpT = 0.0f;
                var probsS = new float[vocabSize];
                var probsT = new float[vocabSize];

                for (var i = 0; i < vocabSize; i++)
                {
                    probsS[i] = MathF.Exp((spanS[off + i] - maxS) / temperature);
                    probsT[i] = MathF.Exp((spanT[off + i] - maxT) / temperature);
                    sumExpS += probsS[i];
                    sumExpT += probsT[i];
                }

                var scale = dLoss * temperature / batchSize;

                for (var i = 0; i < vocabSize; i++)
                {
                    var pS = probsS[i] / sumExpS;
                    var pT = probsT[i] / sumExpT;
                    spanD[off + i] = scale * (pS - pT);
                }
            }

            return [dStudent];
        });

        return lossTensor;
    }

    /// <summary>
    ///     计算综合蒸馏训练损失
    ///     loss = alpha * hardLoss + (1 - alpha) * distillationLoss
    /// </summary>
    /// <param name="studentLogits">学生模型 logits [batch, vocabSize]</param>
    /// <param name="teacherLogits">教师模型 logits [batch, vocabSize]</param>
    /// <param name="targets">真实标签 [batch, 1]</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="alpha">硬标签损失权重（0~1）</param>
    /// <returns>综合损失</returns>
    public static float CombinedLoss(ArrayND studentLogits, ArrayND teacherLogits,
        ArrayND targets, float temperature, float alpha)
    {
        var hardLoss = HardLabelLoss(studentLogits, targets);
        var softLoss = DistillationLoss(studentLogits, teacherLogits, temperature);
        return alpha * hardLoss + (1.0f - alpha) * softLoss;
    }

    /// <summary>
    ///     计算硬标签交叉熵损失
    /// </summary>
    /// <param name="logits">模型输出 [batch, numClasses]</param>
    /// <param name="targets">目标类别 [batch, 1]</param>
    /// <returns>平均交叉熵损失</returns>
    public static float HardLabelLoss(ArrayND logits, ArrayND targets)
    {
        var batchSize = logits.Shape[0];
        var numClasses = logits.Shape[1];
        var spanL = logits.AsSpan();
        var spanT = targets.AsSpan();

        var totalLoss = 0.0f;

        for (var b = 0; b < batchSize; b++)
        {
            var targetClass = (int)spanT[b];
            if (targetClass < 0 || targetClass >= numClasses) continue;

            var off = b * numClasses;
            var maxLogit = float.NegativeInfinity;
            for (var i = 0; i < numClasses; i++)
                if (spanL[off + i] > maxLogit)
                    maxLogit = spanL[off + i];

            var sumExp = 0.0f;
            for (var i = 0; i < numClasses; i++)
                sumExp += MathF.Exp(spanL[off + i] - maxLogit);

            var logSumExp = maxLogit + MathF.Log(sumExp);
            totalLoss += logSumExp - spanL[off + targetClass];
        }

        return totalLoss / batchSize;
    }
}

/// <summary>
///     蒸馏训练器 —— 编排 Teacher-Student 蒸馏训练循环
/// </summary>
public sealed class DistillationTrainer
{
    private readonly IOptimizer _optimizer;
    private readonly ILRScheduler? _scheduler;

    /// <summary>
    ///     创建蒸馏训练器
    /// </summary>
    /// <param name="teacher">教师模型（冻结参数）</param>
    /// <param name="student">学生模型（训练参数）</param>
    /// <param name="optimizer">优化器（仅用于学生模型）</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="alpha">硬标签损失权重</param>
    /// <param name="scheduler">学习率调度器（可选）</param>
    public DistillationTrainer(ITrainableModel teacher, ITrainableModel student,
        IOptimizer optimizer, float temperature = 4.0f, float alpha = 0.3f,
        ILRScheduler? scheduler = null)
    {
        Teacher = teacher;
        Student = student;
        _optimizer = optimizer;
        Temperature = temperature;
        Alpha = alpha;
        _scheduler = scheduler;
    }

    /// <summary>
    ///     教师模型
    /// </summary>
    public ITrainableModel Teacher { get; }

    /// <summary>
    ///     学生模型
    /// </summary>
    public ITrainableModel Student { get; }

    /// <summary>
    ///     温度参数
    /// </summary>
    public float Temperature { get; }

    /// <summary>
    ///     硬标签权重
    /// </summary>
    public float Alpha { get; }

    /// <summary>
    ///     执行一步蒸馏训练
    /// </summary>
    /// <param name="input">输入</param>
    /// <param name="targets">目标标签</param>
    /// <returns>(综合损失, 硬标签损失, 软标签损失)</returns>
    public (float totalLoss, float hardLoss, float softLoss) TrainStep(ArrayND input, ArrayND targets)
    {
        var teacherLogits = Teacher.forward(input);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var studentLogits = Student.forward(input, ctx);

        var hardLoss = KnowledgeDistillation.HardLabelLoss(studentLogits, targets);
        var softLoss = KnowledgeDistillation.DistillationLoss(studentLogits, teacherLogits, Temperature);
        var totalLoss = Alpha * hardLoss + (1.0f - Alpha) * softLoss;

        var lossTensor = KnowledgeDistillation.DistillationLossWithGrad(
            studentLogits, teacherLogits, Temperature, ctx);

        ctx.BackwardFromGradient(lossTensor, ArrayND.Ones());

        _optimizer.Step(Student.Parameters());
        _optimizer.ZeroGrad(Student.Parameters());

        _scheduler?.Step();

        return (totalLoss, hardLoss, softLoss);
    }

    /// <summary>
    ///     执行一个 epoch 的蒸馏训练
    /// </summary>
    /// <param name="inputs">训练输入</param>
    /// <param name="targets">训练标签</param>
    /// <param name="batchSize">批大小</param>
    /// <returns>epoch 平均损失</returns>
    public DistillationEpochResult TrainEpoch(ArrayND inputs, ArrayND targets, int batchSize = 32)
    {
        var numSamples = inputs.Shape[0];
        var numBatches = (numSamples + batchSize - 1) / batchSize;

        var totalLoss = 0.0f;
        var totalHard = 0.0f;
        var totalSoft = 0.0f;

        for (var b = 0; b < numBatches; b++)
        {
            var start = b * batchSize;
            var end = System.Math.Min(start + batchSize, numSamples);
            var size = end - start;

            var batchInput = SliceBatch(inputs, start, size);
            var batchTarget = SliceBatch(targets, start, size);

            var (loss, hard, soft) = TrainStep(batchInput, batchTarget);
            totalLoss += loss * size;
            totalHard += hard * size;
            totalSoft += soft * size;
        }

        return new DistillationEpochResult
        {
            TotalLoss = totalLoss / numSamples,
            HardLoss = totalHard / numSamples,
            SoftLoss = totalSoft / numSamples,
            NumBatches = numBatches
        };
    }

    private static ArrayND SliceBatch(ArrayND source, int start, int size)
    {
        var shape = source.Shape;
        var featureSize = 1;
        for (var d = 1; d < shape.Length; d++) featureSize *= shape[d];

        var result = ArrayND.Zeros([size, .. shape[1..]]);
        var spanSrc = source.AsSpan();
        var spanDst = result.AsWriteSpan();

        var srcOff = start * featureSize;
        for (var i = 0; i < size * featureSize; i++)
            spanDst[i] = spanSrc[srcOff + i];

        return result;
    }
}

/// <summary>
///     蒸馏训练 epoch 结果
/// </summary>
public struct DistillationEpochResult
{
    /// <summary>
    ///     综合损失
    /// </summary>
    public float TotalLoss;

    /// <summary>
    ///     硬标签损失
    /// </summary>
    public float HardLoss;

    /// <summary>
    ///     软标签损失
    /// </summary>
    public float SoftLoss;

    /// <summary>
    ///     批次数
    /// </summary>
    public int NumBatches;
}