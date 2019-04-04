using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Std.Database.Core;

/// <summary>
///     使用 SIMD 指令加速字节序列比较操作，为键比较提供高性能实现
/// </summary>
internal static class SimdKeyComparison
{
    /// <summary>
    ///     使用 SIMD 加速的字节序列比较，等效于 <see cref="ReadOnlySpan{T}.SequenceCompareTo" />
    /// </summary>
    /// <param name="left">左侧字节序列</param>
    /// <param name="right">右侧字节序列</param>
    /// <returns>负值表示 left 小于 right，零表示相等，正值表示 left 大于 right</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int compare_to_simd(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var minLength = System.Math.Min(left.Length, right.Length);

        if (minLength == 0) return left.Length.CompareTo(right.Length);

        if (Vector256.IsHardwareAccelerated && minLength >= Vector256<byte>.Count)
            return compare_to_vector256(left, right, minLength);

        if (Vector128.IsHardwareAccelerated && minLength >= Vector128<byte>.Count)
            return compare_to_vector128(left, right, minLength);

        return left.SequenceCompareTo(right);
    }

    /// <summary>
    ///     使用 SIMD 加速的前缀匹配，等效于 <see cref="MemoryExtensions.StartsWith" />
    /// </summary>
    /// <param name="source">源字节序列</param>
    /// <param name="prefix">前缀字节序列</param>
    /// <returns>如果 source 以 prefix 开头则返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool starts_with_simd(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix)
    {
        if (prefix.Length > source.Length) return false;

        if (prefix.Length == 0) return true;

        if (Vector256.IsHardwareAccelerated && prefix.Length >= Vector256<byte>.Count)
            return starts_with_vector256(source, prefix);

        if (Vector128.IsHardwareAccelerated && prefix.Length >= Vector128<byte>.Count)
            return starts_with_vector128(source, prefix);

        return source.StartsWith(prefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int compare_to_vector256(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, int minLength)
    {
        var vectorSize = Vector256<byte>.Count;
        var vectorEnd = minLength - vectorSize;
        var i = 0;

        ref var leftRef = ref MemoryMarshal.GetReference(left);
        ref var rightRef = ref MemoryMarshal.GetReference(right);

        while (i <= vectorEnd)
        {
            var vLeft = Vector256.LoadUnsafe(ref leftRef, (nuint)i);
            var vRight = Vector256.LoadUnsafe(ref rightRef, (nuint)i);

            var eqMask = Vector256.Equals(vLeft, vRight);
            var allEq = eqMask == Vector256<byte>.AllBitsSet;

            if (!allEq)
            {
                var diffMask = ~eqMask;
                var diffBits = diffMask.ExtractMostSignificantBits();
                var firstDiffOffset = BitOperations.TrailingZeroCount(diffBits);
                var byteIndex = i + firstDiffOffset;
                return left[byteIndex] - right[byteIndex];
            }

            i += vectorSize;
        }

        if (i < minLength) return compare_remaining(left, right, i, minLength);

        return left.Length.CompareTo(right.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int compare_to_vector128(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, int minLength)
    {
        var vectorSize = Vector128<byte>.Count;
        var vectorEnd = minLength - vectorSize;
        var i = 0;

        ref var leftRef = ref MemoryMarshal.GetReference(left);
        ref var rightRef = ref MemoryMarshal.GetReference(right);

        while (i <= vectorEnd)
        {
            var vLeft = Vector128.LoadUnsafe(ref leftRef, (nuint)i);
            var vRight = Vector128.LoadUnsafe(ref rightRef, (nuint)i);

            var eqMask = Vector128.Equals(vLeft, vRight);
            var allEq = eqMask == Vector128<byte>.AllBitsSet;

            if (!allEq)
            {
                var diffMask = ~eqMask;
                var diffBits = diffMask.ExtractMostSignificantBits();
                var firstDiffOffset = BitOperations.TrailingZeroCount(diffBits);
                var byteIndex = i + firstDiffOffset;
                return left[byteIndex] - right[byteIndex];
            }

            i += vectorSize;
        }

        if (i < minLength) return compare_remaining(left, right, i, minLength);

        return left.Length.CompareTo(right.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool starts_with_vector256(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix)
    {
        var vectorSize = Vector256<byte>.Count;
        var remaining = prefix.Length;
        var i = 0;

        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var prefixRef = ref MemoryMarshal.GetReference(prefix);

        while (remaining >= vectorSize)
        {
            var vSource = Vector256.LoadUnsafe(ref sourceRef, (nuint)i);
            var vPrefix = Vector256.LoadUnsafe(ref prefixRef, (nuint)i);

            if (vSource != vPrefix) return false;

            i += vectorSize;
            remaining -= vectorSize;
        }

        if (remaining > 0) return source[i..prefix.Length].SequenceEqual(prefix[i..]);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool starts_with_vector128(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix)
    {
        var vectorSize = Vector128<byte>.Count;
        var remaining = prefix.Length;
        var i = 0;

        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var prefixRef = ref MemoryMarshal.GetReference(prefix);

        while (remaining >= vectorSize)
        {
            var vSource = Vector128.LoadUnsafe(ref sourceRef, (nuint)i);
            var vPrefix = Vector128.LoadUnsafe(ref prefixRef, (nuint)i);

            if (vSource != vPrefix) return false;

            i += vectorSize;
            remaining -= vectorSize;
        }

        if (remaining > 0) return source[i..prefix.Length].SequenceEqual(prefix[i..]);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int compare_remaining(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, int offset, int end)
    {
        for (var i = offset; i < end; i++)
        {
            var cmp = left[i].CompareTo(right[i]);
            if (cmp != 0) return cmp;
        }

        return left.Length.CompareTo(right.Length);
    }
}