namespace Nyar.VM.TextVM;

/// <summary>
/// 双向字符串匹配算法（Two-Way）。基于 Crochemore 和 Perrin（1991）的两段式字符串搜索，
/// 在最坏情况下 O(n) 时间，在平均情况下具有亚线性跳跃行为。
/// 支持 <see cref="Byte"/> 和 <see cref="UInt16"/> 两种切片类型。
/// </summary>
public static class TwoWayMatcher
{
    /// <summary>
    /// 返回 <paramref name="pattern"/> 在 <paramref name="text"/> 中首次出现的起始索引。
    /// 若未找到则返回 -1。
    /// </summary>
    /// <param name="text">待搜索的文本切片。</param>
    /// <param name="pattern">要搜索的模式串。</param>
    /// <returns>首次匹配的起始索引，未找到时返回 -1。</returns>
    public static Int32 IndexOf(ReadOnlySpan<Byte> text, ReadOnlySpan<Byte> pattern)
    {
        if (pattern.Length == 0)
        {
            return 0;
        }

        if (pattern.Length > text.Length)
        {
            return -1;
        }

        (Int32 pos, Int32 per) = ComputeCriticalFactorization(pattern);

        Int32 n = text.Length;
        Int32 m = pattern.Length;
        Int32 memory = 0;
        Int32 i = 0;

        while (i <= n - m)
        {
            // 从右半部分的起始位置开始比较
            Int32 j = Math.Max(pos, memory);

            while (j < m && text[i + j] == pattern[j])
            {
                j++;
            }

            if (j < m)
            {
                // 右半部分不匹配，跳跃
                i += j - pos + 1;
                memory = 0;
            }
            else
            {
                // 右半部分完全匹配，从左到左比较左半部分
                Int32 k = pos - 1;

                while (k >= memory && text[i + k] == pattern[k])
                {
                    k--;
                }

                if (k < memory)
                {
                    return i;
                }

                i += per;
                memory = m - per;
            }
        }

        return -1;
    }

    /// <summary>
    /// 返回 <paramref name="pattern"/> 在 <paramref name="text"/> 中所有出现位置的起始索引。
    /// 结果按升序排列。
    /// </summary>
    /// <param name="text">待搜索的文本切片。</param>
    /// <param name="pattern">要搜索的模式串。</param>
    /// <returns>所有匹配起始索引的数组。</returns>
    public static Int32[] IndexOfAll(ReadOnlySpan<Byte> text, ReadOnlySpan<Byte> pattern)
    {
        if (pattern.Length == 0 || pattern.Length > text.Length)
        {
            return [];
        }

        (Int32 pos, Int32 per) = ComputeCriticalFactorization(pattern);

        Int32 n = text.Length;
        Int32 m = pattern.Length;
        List<Int32> result = [];
        Int32 memory = 0;
        Int32 i = 0;

        while (i <= n - m)
        {
            Int32 j = Math.Max(pos, memory);

            while (j < m && text[i + j] == pattern[j])
            {
                j++;
            }

            if (j < m)
            {
                i += j - pos + 1;
                memory = 0;
            }
            else
            {
                Int32 k = pos - 1;

                while (k >= memory && text[i + k] == pattern[k])
                {
                    k--;
                }

                if (k < memory)
                {
                    result.Add(i);
                }

                i += per;
                memory = m - per;
            }
        }

        return [.. result];
    }

    /// <summary>
    /// 返回 <paramref name="pattern"/>（<see cref="UInt16"/> 元素）在 <paramref name="text"/>（<see cref="UInt16"/> 元素）
    /// 中首次出现的起始索引。用于 UTF-16 字面量搜索。
    /// </summary>
    /// <param name="text">待搜索的文本切片（U16 元素）。</param>
    /// <param name="pattern">要搜索的模式串（U16 元素）。</param>
    /// <returns>首次匹配的起始索引，未找到时返回 -1。</returns>
    public static Int32 IndexOf(ReadOnlySpan<UInt16> text, ReadOnlySpan<UInt16> pattern)
    {
        if (pattern.Length == 0)
        {
            return 0;
        }

        if (pattern.Length > text.Length)
        {
            return -1;
        }

        (Int32 pos, Int32 per) = ComputeCriticalFactorization(pattern);

        Int32 n = text.Length;
        Int32 m = pattern.Length;
        Int32 memory = 0;
        Int32 i = 0;

        while (i <= n - m)
        {
            Int32 j = Math.Max(pos, memory);

            while (j < m && text[i + j] == pattern[j])
            {
                j++;
            }

            if (j < m)
            {
                i += j - pos + 1;
                memory = 0;
            }
            else
            {
                Int32 k = pos - 1;

                while (k >= memory && text[i + k] == pattern[k])
                {
                    k--;
                }

                if (k < memory)
                {
                    return i;
                }

                i += per;
                memory = m - per;
            }
        }

        return -1;
    }

    /// <summary>
    /// 计算模式串的关键分解位置（critical factorization position）和周期。
    /// </summary>
    /// <param name="pattern">模式串。</param>
    /// <returns>包含关键分解位置和周期的元组。</returns>
    private static (Int32 pos, Int32 per) ComputeCriticalFactorization(ReadOnlySpan<Byte> pattern)
    {
        (Int32 i, Int32 p) = MaximalSuffix(pattern, reversed: false);
        (Int32 j, Int32 q) = MaximalSuffix(pattern, reversed: true);

        if (i >= j)
        {
            return (i, p);
        }

        return (j, q);
    }

    /// <summary>
    /// 计算模式串的关键分解位置和周期（针对 <see cref="UInt16"/> 切片的重载）。
    /// </summary>
    /// <param name="pattern">模式串（U16 元素）。</param>
    /// <returns>包含关键分解位置和周期的元组。</returns>
    private static (Int32 pos, Int32 per) ComputeCriticalFactorization(ReadOnlySpan<UInt16> pattern)
    {
        (Int32 i, Int32 p) = MaximalSuffix(pattern, reversed: false);
        (Int32 j, Int32 q) = MaximalSuffix(pattern, reversed: true);

        if (i >= j)
        {
            return (i, p);
        }

        return (j, q);
    }

    /// <summary>
    /// 计算模式串的最大后缀（maximal suffix）及其关联周期。
    /// </summary>
    /// <param name="pattern">模式串。</param>
    /// <param name="reversed">若为 true 则使用逆序比较（用于计算反向最大后缀）。</param>
    /// <returns>包含最大后缀起始位置和周期的元组。</returns>
    private static (Int32 position, Int32 period) MaximalSuffix(ReadOnlySpan<Byte> pattern, Boolean reversed)
    {
        Int32 n = pattern.Length;
        Int32 i = 0;
        Int32 j = 1;
        Int32 k = 0;
        Int32 p = 1;

        while (j + k < n)
        {
            Byte a = pattern[i + k];
            Byte b = pattern[j + k];

            if (a == b)
            {
                k++;
                continue;
            }

            if (reversed ? a > b : a < b)
            {
                i = j;
                j = i + 1;
                k = 0;
                p = j - i;
            }
            else
            {
                j += k + 1;
                k = 0;
                p = j - i;
            }
        }

        return (i, p);
    }

    /// <summary>
    /// 计算模式串的最大后缀及其关联周期（针对 <see cref="UInt16"/> 切片的重载）。
    /// </summary>
    /// <param name="pattern">模式串（U16 元素）。</param>
    /// <param name="reversed">若为 true 则使用逆序比较。</param>
    /// <returns>包含最大后缀起始位置和周期的元组。</returns>
    private static (Int32 position, Int32 period) MaximalSuffix(ReadOnlySpan<UInt16> pattern, Boolean reversed)
    {
        Int32 n = pattern.Length;
        Int32 i = 0;
        Int32 j = 1;
        Int32 k = 0;
        Int32 p = 1;

        while (j + k < n)
        {
            UInt16 a = pattern[i + k];
            UInt16 b = pattern[j + k];

            if (a == b)
            {
                k++;
                continue;
            }

            if (reversed ? a > b : a < b)
            {
                i = j;
                j = i + 1;
                k = 0;
                p = j - i;
            }
            else
            {
                j += k + 1;
                k = 0;
                p = j - i;
            }
        }

        return (i, p);
    }
}
