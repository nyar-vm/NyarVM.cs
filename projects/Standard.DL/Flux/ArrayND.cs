using System.Runtime.CompilerServices;

namespace Std.DL.Flux;

/// <summary>
///     张量 —— Galatea 中唯一的计算载体
/// </summary>
public sealed class ArrayND : IDisposable
{
    private readonly float[] _data;
    private readonly int _offset;

    private ArrayND(float[] data, int[] shape, int offset, int length)
    {
        _data = data;
        Shape = shape;
        _offset = offset;
        Size = length;
    }

    /// <summary>张量形状</summary>
    public int[] Shape { get; }

    /// <summary>张量总元素数</summary>
    public int Size { get; }

    /// <summary>梯度张量（反向传播时累积）</summary>
    public ArrayND? Grad { get; private set; }

    /// <summary>
    ///     版本号：每次通过 AsWriteSpan 修改数据时递增
    ///     用于检测原地操作（in-place operation），在反向传播前验证数据一致性
    /// </summary>
    public int Version { get; private set; }

    /// <summary>获取平铺索引处的值</summary>
    public float this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data[_offset + index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _data[_offset + index] = value;
    }

    /// <summary>释放张量资源</summary>
    public void Dispose()
    {
        Grad?.Dispose();
    }

    /// <summary>创建指定形状的全零张量</summary>
    public static ArrayND Zeros(params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        return new ArrayND(new float[size], shape, 0, size);
    }

    /// <summary>创建指定形状的全一张量</summary>
    public static ArrayND Ones(params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = new float[size];
        Array.Fill(data, 1.0f);
        return new ArrayND(data, shape, 0, size);
    }

    /// <summary>创建指定形状的标准正态随机张量</summary>
    public static ArrayND RandomNormal(params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = new float[size];
        var rng = Random.Shared;
        for (var i = 0; i < size; i++) data[i] = BoxMuller(rng);
        return new ArrayND(data, shape, 0, size);
    }

    /// <summary>创建指定形状的随机张量（He 初始化）</summary>
    public static ArrayND HeNormal(int fanIn, params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = new float[size];
        var std = MathF.Sqrt(2.0f / fanIn);
        var rng = Random.Shared;
        for (var i = 0; i < size; i++) data[i] = BoxMuller(rng) * std;
        return new ArrayND(data, shape, 0, size);
    }

    /// <summary>从已有数组创建张量</summary>
    public static ArrayND FromArray(float[] data, params int[] shape)
    {
        return new ArrayND(data, shape, 0, data.Length);
    }

    /// <summary>
    ///     Xavier/Glorot 均匀初始化：U[-sqrt(6/(fanIn+fanOut)), sqrt(6/(fanIn+fanOut))]
    ///     适用于 sigmoid/tanh 激活函数
    /// </summary>
    /// <param name="fanIn">输入维度</param>
    /// <param name="fanOut">输出维度</param>
    /// <param name="shape">张量形状</param>
    /// <returns>初始化后的张量</returns>
    public static ArrayND XavierUniform(int fanIn, int fanOut, params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = new float[size];
        var limit = MathF.Sqrt(6.0f / (fanIn + fanOut));
        var rng = Random.Shared;
        for (var i = 0; i < size; i++) data[i] = rng.NextSingle() * 2.0f * limit - limit;
        return new ArrayND(data, shape, 0, size);
    }

    /// <summary>
    ///     Xavier/Glorot 正态初始化：N(0, sqrt(2/(fanIn+fanOut)))
    ///     适用于 sigmoid/tanh 激活函数
    /// </summary>
    /// <param name="fanIn">输入维度</param>
    /// <param name="fanOut">输出维度</param>
    /// <param name="shape">张量形状</param>
    /// <returns>初始化后的张量</returns>
    public static ArrayND XavierNormal(int fanIn, int fanOut, params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = new float[size];
        var std = MathF.Sqrt(2.0f / (fanIn + fanOut));
        var rng = Random.Shared;
        for (var i = 0; i < size; i++) data[i] = BoxMuller(rng) * std;
        return new ArrayND(data, shape, 0, size);
    }

    /// <summary>重塑张量（共享底层数据）</summary>
    public ArrayND Reshape(params int[] newShape)
    {
        var newSize = newShape.Aggregate(1, (a, b) => a * b);
        if (newSize != Size) throw new ArgumentException($"无法将 {Size} 个元素重塑为 {newSize} 个元素");
        return new ArrayND(_data, newShape, _offset, Size);
    }

    /// <summary>
    ///     带自动微分的 Reshape：梯度直接 reshape 回原始形状
    /// </summary>
    /// <param name="newShape">目标形状</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>重塑后的张量</returns>
    public ArrayND ReshapeWithGrad(int[] newShape, AutogradContext ctx)
    {
        var result = Reshape(newShape);
        var originalShape = (int[])Shape.Clone();

        ctx.Record(result, [this], outputGrads =>
        {
            var dResult = outputGrads[0];
            return [dResult.Reshape(originalShape)];
        });

        return result;
    }

    /// <summary>
    ///     带自动微分的 Slice：梯度 scatter 回原始位置
    /// </summary>
    /// <param name="axis">切片轴</param>
    /// <param name="start">起始索引</param>
    /// <param name="count">切片数量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>切片后的张量</returns>
    public ArrayND SliceWithGrad(int axis, int start, int count, AutogradContext ctx)
    {
        var result = Slice(axis, start, count);
        var originalShape = (int[])Shape.Clone();

        ctx.Record(result, [this], outputGrads =>
        {
            var dSlice = outputGrads[0];
            var dFull = Zeros(originalShape);

            var ndim = originalShape.Length;
            Span<int> strides = stackalloc int[ndim];
            ComputeStrides(originalShape, strides);

            var spanDSlice = dSlice.AsSpan();
            var spanDFull = dFull.AsWriteSpan();
            var sliceSize = dSlice.Shape.Aggregate(1, (a, b) => a * b);

            for (var flat = 0; flat < sliceSize; flat++)
            {
                var remaining = flat;
                var srcFlat = 0;
                for (var d = ndim - 1; d >= 0; d--)
                {
                    var coord = remaining % dSlice.Shape[d];
                    remaining /= dSlice.Shape[d];
                    srcFlat += (d == axis ? coord + start : coord) * strides[d];
                }

                spanDFull[srcFlat] += spanDSlice[flat];
            }

            return [dFull];
        });

        return result;
    }

    /// <summary>获取底层数据的只读跨度</summary>
    public ReadOnlySpan<float> AsSpan()
    {
        return _data.AsSpan(_offset, Size);
    }

    /// <summary>获取底层数据的可写跨度</summary>
    public Span<float> AsWriteSpan()
    {
        Version++;
        return _data.AsSpan(_offset, Size);
    }

    /// <summary>元素级加法（支持广播）</summary>
    public static ArrayND operator +(ArrayND a, ArrayND b)
    {
        if (a.Shape.Length != b.Shape.Length)
            throw new ArgumentException($"张量维度不匹配：({string.Join(",", a.Shape)}) + ({string.Join(",", b.Shape)})");

        var shape = new int[a.Shape.Length];
        for (var d = 0; d < shape.Length; d++) shape[d] = System.Math.Max(a.Shape[d], b.Shape[d]);

        var result = Zeros(shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        var total = shape.Aggregate(1, (x, y) => x * y);
        Span<int> stridesA = stackalloc int[shape.Length];
        Span<int> stridesB = stackalloc int[shape.Length];
        ComputeStrides(a.Shape, stridesA);
        ComputeStrides(b.Shape, stridesB);

        for (var flat = 0; flat < total; flat++)
        {
            var idxA = 0;
            var idxB = 0;
            var remaining = flat;
            for (var d = shape.Length - 1; d >= 0; d--)
            {
                var coord = remaining % shape[d];
                remaining /= shape[d];
                if (a.Shape[d] != 1) idxA += coord * stridesA[d];

                if (b.Shape[d] != 1) idxB += coord * stridesB[d];
            }

            spanR[flat] = spanA[idxA] + spanB[idxB];
        }

        return result;
    }

    /// <summary>元素级减法（支持广播）</summary>
    public static ArrayND operator -(ArrayND a, ArrayND b)
    {
        if (a.Shape.Length != b.Shape.Length)
            throw new ArgumentException($"张量维度不匹配：({string.Join(",", a.Shape)}) - ({string.Join(",", b.Shape)})");

        var shape = new int[a.Shape.Length];
        for (var d = 0; d < shape.Length; d++) shape[d] = System.Math.Max(a.Shape[d], b.Shape[d]);

        var result = Zeros(shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        var total = shape.Aggregate(1, (x, y) => x * y);
        Span<int> stridesA = stackalloc int[shape.Length];
        Span<int> stridesB = stackalloc int[shape.Length];
        ComputeStrides(a.Shape, stridesA);
        ComputeStrides(b.Shape, stridesB);

        for (var flat = 0; flat < total; flat++)
        {
            var idxA = 0;
            var idxB = 0;
            var remaining = flat;
            for (var d = shape.Length - 1; d >= 0; d--)
            {
                var coord = remaining % shape[d];
                remaining /= shape[d];
                if (a.Shape[d] != 1) idxA += coord * stridesA[d];

                if (b.Shape[d] != 1) idxB += coord * stridesB[d];
            }

            spanR[flat] = spanA[idxA] - spanB[idxB];
        }

        return result;
    }

    /// <summary>元素级乘法（支持广播）</summary>
    public static ArrayND operator *(ArrayND a, ArrayND b)
    {
        if (a.Shape.Length != b.Shape.Length)
            throw new ArgumentException($"张量维度不匹配：({string.Join(",", a.Shape)}) * ({string.Join(",", b.Shape)})");

        var shape = new int[a.Shape.Length];
        for (var d = 0; d < shape.Length; d++) shape[d] = System.Math.Max(a.Shape[d], b.Shape[d]);

        var result = Zeros(shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        var total = shape.Aggregate(1, (x, y) => x * y);
        Span<int> stridesA = stackalloc int[shape.Length];
        Span<int> stridesB = stackalloc int[shape.Length];
        ComputeStrides(a.Shape, stridesA);
        ComputeStrides(b.Shape, stridesB);

        for (var flat = 0; flat < total; flat++)
        {
            var idxA = 0;
            var idxB = 0;
            var remaining = flat;
            for (var d = shape.Length - 1; d >= 0; d--)
            {
                var coord = remaining % shape[d];
                remaining /= shape[d];
                if (a.Shape[d] != 1) idxA += coord * stridesA[d];

                if (b.Shape[d] != 1) idxB += coord * stridesB[d];
            }

            spanR[flat] = spanA[idxA] * spanB[idxB];
        }

        return result;
    }

    /// <summary>元素级除法（支持广播）</summary>
    public static ArrayND operator /(ArrayND a, ArrayND b)
    {
        if (a.Shape.Length != b.Shape.Length)
            throw new ArgumentException($"张量维度不匹配：({string.Join(",", a.Shape)}) / ({string.Join(",", b.Shape)})");

        var shape = new int[a.Shape.Length];
        for (var d = 0; d < shape.Length; d++) shape[d] = System.Math.Max(a.Shape[d], b.Shape[d]);

        var result = Zeros(shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        var total = shape.Aggregate(1, (x, y) => x * y);
        Span<int> stridesA = stackalloc int[shape.Length];
        Span<int> stridesB = stackalloc int[shape.Length];
        ComputeStrides(a.Shape, stridesA);
        ComputeStrides(b.Shape, stridesB);

        for (var flat = 0; flat < total; flat++)
        {
            var idxA = 0;
            var idxB = 0;
            var remaining = flat;
            for (var d = shape.Length - 1; d >= 0; d--)
            {
                var coord = remaining % shape[d];
                remaining /= shape[d];
                if (a.Shape[d] != 1) idxA += coord * stridesA[d];

                if (b.Shape[d] != 1) idxB += coord * stridesB[d];
            }

            spanR[flat] = spanA[idxA] / spanB[idxB];
        }

        return result;
    }

    /// <summary>标量除法</summary>
    public static ArrayND operator /(ArrayND a, float scalar)
    {
        var result = Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanA.Length; i++) spanR[i] = spanA[i] / scalar;
        return result;
    }

    /// <summary>逐元素平方根</summary>
    public static ArrayND Sqrt(ArrayND a)
    {
        var result = Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanA.Length; i++) spanR[i] = MathF.Sqrt(spanA[i]);
        return result;
    }

    /// <summary>逐元素指数</summary>
    public static ArrayND Exp(ArrayND a)
    {
        var result = Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanA.Length; i++) spanR[i] = MathF.Exp(spanA[i]);
        return result;
    }

    /// <summary>逐元素自然对数</summary>
    public static ArrayND Log(ArrayND a)
    {
        var result = Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanA.Length; i++) spanR[i] = MathF.Log(spanA[i]);
        return result;
    }

    /// <summary>标量乘法</summary>
    public static ArrayND operator *(ArrayND a, float scalar)
    {
        var result = Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanA.Length; i++) spanR[i] = spanA[i] * scalar;
        return result;
    }

    /// <summary>标量乘法</summary>
    public static ArrayND operator *(float scalar, ArrayND a)
    {
        return a * scalar;
    }

    /// <summary>矩阵乘法：C[m,k] = A[m,n] × B[n,k]</summary>
    public static ArrayND MatMul(ArrayND a, ArrayND b)
    {
        var m = a.Shape[0];
        var n = a.Shape[1];
        var k = b.Shape[1];

        if (a.Shape.Length != 2 || b.Shape.Length != 2 || a.Shape[1] != b.Shape[0])
            throw new ArgumentException($"矩阵乘法形状不匹配：({string.Join(",", a.Shape)}) × ({string.Join(",", b.Shape)})");

        var result = Zeros(m, k);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        for (var i = 0; i < m; i++)
        for (var j = 0; j < k; j++)
        {
            var sum = 0.0f;
            for (var t = 0; t < n; t++) sum += spanA[i * n + t] * spanB[t * k + j];
            spanR[i * k + j] = sum;
        }

        return result;
    }

    /// <summary>转置（默认交换最后两个轴）</summary>
    public ArrayND Transpose()
    {
        return Transpose(Shape.Length - 2, Shape.Length - 1);
    }

    /// <summary>转置：交换两个轴</summary>
    public ArrayND Transpose(int dim0, int dim1)
    {
        var ndim = Shape.Length;
        if (dim0 < 0 || dim0 >= ndim || dim1 < 0 || dim1 >= ndim)
            throw new ArgumentOutOfRangeException($"轴索引无效：dim0={dim0}, dim1={dim1}, ndim={ndim}");

        if (dim0 == dim1) return Clone();

        var newShape = (int[])Shape.Clone();
        newShape[dim0] = Shape[dim1];
        newShape[dim1] = Shape[dim0];

        var result = Zeros(newShape);
        var spanSrc = AsSpan();
        var spanDst = result.AsWriteSpan();

        Span<int> srcStrides = stackalloc int[ndim];
        ComputeStrides(Shape, srcStrides);
        Span<int> dstStrides = stackalloc int[ndim];
        ComputeStrides(newShape, dstStrides);

        var total = result.Size;
        for (var flat = 0; flat < total; flat++)
        {
            var remaining = flat;
            var srcFlat = 0;
            for (var d = ndim - 1; d >= 0; d--)
            {
                var coord = remaining % newShape[d];
                remaining /= newShape[d];
                var srcDim = d == dim0 ? dim1 : d == dim1 ? dim0 : d;
                srcFlat += coord * srcStrides[srcDim];
            }

            spanDst[flat] = spanSrc[srcFlat];
        }

        return result;
    }

    /// <summary>沿指定轴求和</summary>
    public ArrayND Sum(int axis, bool keepDims = false)
    {
        var ndim = Shape.Length;
        var newShape = keepDims ? (int[])Shape.Clone() : [.. Shape.Where((_, i) => i != axis)];
        if (keepDims) newShape[axis] = 1;

        var result = Zeros(newShape);
        var spanSrc = AsSpan();
        var spanDst = result.AsWriteSpan();

        Span<int> srcStrides = stackalloc int[ndim];
        ComputeStrides(Shape, srcStrides);

        var axisSize = Shape[axis];
        var axisStride = srcStrides[axis];
        var groupSize = axisSize * axisStride;
        var groupsOuter = Size / groupSize;

        for (var g = 0; g < groupsOuter; g++)
        {
            var baseOff = g * groupSize;
            for (var i = 0; i < axisStride; i++)
            {
                var sum = 0.0f;
                for (var k = 0; k < axisSize; k++) sum += spanSrc[baseOff + k * axisStride + i];
                var dstFlat = keepDims ? baseOff + i : g * axisStride + i;
                spanDst[dstFlat] = sum;
            }
        }

        return result;
    }

    /// <summary>沿 batch 维度求和（兼容旧 API）</summary>
    public ArrayND SumAlongAxis0()
    {
        return Sum(0);
    }

    /// <summary>为反向传播分配梯度张量</summary>
    public void EnsureGrad()
    {
        if (Grad is null) Grad = Zeros(Shape);
    }

    /// <summary>将梯度归零</summary>
    public void ZeroGrad()
    {
        if (Grad is not null) Grad.AsWriteSpan().Clear();
    }

    /// <summary>复制张量数据</summary>
    public ArrayND Clone()
    {
        var copy = new float[Size];
        AsSpan().CopyTo(copy);
        return new ArrayND(copy, (int[])Shape.Clone(), 0, Size);
    }

    /// <summary>沿指定轴切片</summary>
    public ArrayND Slice(int axis, int start, int count)
    {
        var ndim = Shape.Length;
        if (start < 0 || count <= 0 || start + count > Shape[axis])
            throw new ArgumentOutOfRangeException(
                $"Slice 越界：axis={axis}, start={start}, count={count}, size={Shape[axis]}");

        var newShape = (int[])Shape.Clone();
        newShape[axis] = count;

        Span<int> strides = stackalloc int[ndim];
        ComputeStrides(Shape, strides);
        var axisStride = strides[axis];
        var axisSize = Shape[axis];

        var result = Zeros(newShape);
        var spanSrc = AsSpan();
        var spanDst = result.AsWriteSpan();
        var sliceSize = newShape.Aggregate(1, (a, b) => a * b);

        for (var flat = 0; flat < sliceSize; flat++)
        {
            var remaining = flat;
            var srcFlat = 0;
            for (var d = ndim - 1; d >= 0; d--)
            {
                var coord = remaining % newShape[d];
                remaining /= newShape[d];
                srcFlat += (d == axis ? coord + start : coord) * strides[d];
            }

            spanDst[flat] = spanSrc[srcFlat];
        }

        return result;
    }

    /// <summary>沿指定轴拼接多个张量</summary>
    public static ArrayND Concat(int axis, params ArrayND[] arrays)
    {
        if (arrays.Length == 0) throw new ArgumentException("至少需要一个张量");

        if (arrays.Length == 1) return arrays[0].Clone();

        var ndim = arrays[0].Shape.Length;
        var newShape = (int[])arrays[0].Shape.Clone();
        newShape[axis] = arrays.Sum(a => a.Shape[axis]);

        var result = Zeros(newShape);
        Span<int> dstStrides = stackalloc int[ndim];
        ComputeStrides(newShape, dstStrides);

        var offset = 0;
        foreach (var arr in arrays)
        {
            var copySize = arr.Size;
            var arrFlat = arr.AsSpan();
            var spanDst = result.AsWriteSpan();
            for (var i = 0; i < copySize; i++) spanDst[offset + i] = arrFlat[i];
            offset += copySize;
        }

        return result;
    }

    /// <summary>
    ///     带自动微分的张量拼接
    /// </summary>
    /// <param name="axis">拼接轴</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <param name="arrays">要拼接的张量数组</param>
    /// <returns>拼接后的张量</returns>
    public static ArrayND ConcatWithGrad(int axis, AutogradContext ctx, params ArrayND[] arrays)
    {
        var result = Concat(axis, arrays);

        var axisSizes = arrays.Select(a => a.Shape[axis]).ToArray();
        var totalAxisSize = axisSizes.Sum();

        ctx.Record(result, arrays, outputGrads =>
        {
            var dResult = outputGrads[0];
            var splitGrads = new ArrayND?[arrays.Length];

            var ndim = result.Shape.Length;
            Span<int> strides = stackalloc int[ndim];
            ComputeStrides(result.Shape, strides);
            var axisStride = strides[axis];

            var offset = 0;
            for (var k = 0; k < arrays.Length; k++)
            {
                var dArr = Zeros(arrays[k].Shape);
                var spanDResult = dResult.AsSpan();
                var spanDArr = dArr.AsWriteSpan();

                var arrSize = arrays[k].Size;
                for (var i = 0; i < arrSize; i++) spanDArr[i] = spanDResult[offset + i];
                offset += arrSize;

                splitGrads[k] = dArr;
            }

            return splitGrads;
        });

        return result;
    }

    /// <summary>批量矩阵乘法：a[..., M, K] × b[..., K, N] → [..., M, N]</summary>
    public static ArrayND BatchMatMul(ArrayND a, ArrayND b)
    {
        var aNd = a.Shape.Length;
        var bNd = b.Shape.Length;

        if (aNd < 2 || bNd < 2) throw new ArgumentException("批量矩阵乘法至少需要二维输入");

        if (a.Shape[aNd - 1] != b.Shape[bNd - 2])
            throw new ArgumentException($"批量矩阵乘法内维度不匹配：{a.Shape[aNd - 1]} != {b.Shape[bNd - 2]}");

        var M = a.Shape[aNd - 2];
        var K = a.Shape[aNd - 1];
        var N = b.Shape[bNd - 1];

        var aBatchDims = aNd - 2;
        var bBatchDims = bNd - 2;
        var batchDims = System.Math.Max(aBatchDims, bBatchDims);
        var batchShape = new int[batchDims];
        for (var d = 0; d < batchDims; d++)
        {
            var aIdx = d - (batchDims - aBatchDims);
            var bIdx = d - (batchDims - bBatchDims);
            var aDim = aIdx >= 0 ? a.Shape[aIdx] : 1;
            var bDim = bIdx >= 0 ? b.Shape[bIdx] : 1;
            batchShape[d] = System.Math.Max(aDim, bDim);
        }

        var batchTotal = batchShape.Length > 0 ? batchShape.Aggregate(1, (x, y) => x * y) : 1;

        var resultShape = new int[batchDims + 2];
        Array.Copy(batchShape, resultShape, batchDims);
        resultShape[batchDims] = M;
        resultShape[batchDims + 1] = N;

        var result = Zeros(resultShape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        var aFlatM = aBatchDims > 0 ? a.Size / (M * K) : 1;
        var bFlatB = bBatchDims > 0 ? b.Size / (K * N) : 1;

        for (var batch = 0; batch < batchTotal; batch++)
        {
            var aBatch = batch % aFlatM;
            var bBatch = batch % bFlatB;
            var aOff = aBatch * M * K;
            var bOff = bBatch * K * N;
            var rOff = batch * M * N;

            for (var i = 0; i < M; i++)
            for (var j = 0; j < N; j++)
            {
                var sum = 0.0f;
                for (var t = 0; t < K; t++) sum += spanA[aOff + i * K + t] * spanB[bOff + t * N + j];
                spanR[rOff + i * N + j] = sum;
            }
        }

        return result;
    }

    /// <summary>从第 0 维切片（兼容旧 API）</summary>
    public ArrayND SliceRows(int start, int count)
    {
        return Slice(0, start, count);
    }

    /// <summary>
    ///     尝试获取张量独占的底层数组引用。
    ///     仅当张量从偏移 0 开始且长度等于数组长度时返回数组（即张量独占该数组），
    ///     否则返回 null（张量是视图，不能安全回收底层数组）。
    /// </summary>
    /// <returns>独占的 float[] 或 null</returns>
    internal float[]? TryGetOwnedData()
    {
        if (_offset == 0 && Size == _data.Length) return _data;

        return null;
    }

    private static float BoxMuller(Random rng)
    {
        var u1 = 1.0f - rng.NextSingle();
        var u2 = 1.0f - rng.NextSingle();
        return MathF.Sqrt(-2.0f * MathF.Log(System.Math.Max(u1, 1e-8f))) * MathF.Sin(2.0f * MathF.PI * u2);
    }

    internal static void ComputeStrides(int[] shape, Span<int> strides)
    {
        var stride = 1;
        for (var d = shape.Length - 1; d >= 0; d--)
        {
            strides[d] = stride;
            stride *= shape[d];
        }
    }
}