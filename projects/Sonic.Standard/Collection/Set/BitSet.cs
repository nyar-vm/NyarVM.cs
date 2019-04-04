using Std.Math;

namespace Std.Collection.Set;

/// <summary>
///     基于 <see cref="UInt64" /> 位数组的位集合，支持高效的集合操作�?///
/// </summary>
public class BitSet
{
    private ulong[] _words;

    /// <summary>
    ///     初始化一个空�?<see cref="BitSet" /> 实例�?    ///
    /// </summary>
    public BitSet()
    {
        _words = [];
    }

    /// <summary>
    ///     初始化一个指定容量的 <see cref="BitSet" /> 实例，预分配足够的存储空间�?    ///
    /// </summary>
    /// <param name="bitCapacity">位容量上限�?/param>
    public BitSet(int bitCapacity)
    {
        var wordCount = (bitCapacity + 63) / 64;
        _words = new ulong[wordCount];
    }

    /// <summary>
    ///     获取位集合是否为空，即没有任何位被置�?<c>true</c>�?    ///
    /// </summary>
    public bool is_empty
    {
        get
        {
            foreach (var word in _words)
                if (word != 0)
                    return false;

            return true;
        }
    }

    /// <summary>
    ///     将指定位置的位设置为 <c>true</c>�?    ///
    /// </summary>
    /// <param name="index">位索引�?/param>
    public void set(int index)
    {
        var wordIndex = index / 64;
        ensure_capacity(wordIndex + 1);
        _words[wordIndex] |= 1UL << (index % 64);
    }

    /// <summary>
    ///     将指定位置的位设置为 <c>false</c>�?    ///
    /// </summary>
    /// <param name="index">位索引�?/param>
    public void clear(int index)
    {
        var wordIndex = index / 64;
        if (wordIndex < _words.Length) _words[wordIndex] &= ~(1UL << (index % 64));
    }

    /// <summary>
    ///     获取指定位置位的值�?    ///
    /// </summary>
    /// <param name="index">
    ///     位索引�?/param>
    ///     <returns><c>true</c> 表示该位已设置，<c>false</c> 表示未设置�?/returns>
    public bool get(int index)
    {
        var wordIndex = index / 64;
        if (wordIndex >= _words.Length) return false;

        return (_words[wordIndex] & (1UL << (index % 64))) != 0;
    }

    /// <summary>
    ///     切换指定位置的位值�?    ///
    /// </summary>
    /// <param name="index">位索引�?/param>
    public void toggle(int index)
    {
        var wordIndex = index / 64;
        ensure_capacity(wordIndex + 1);
        _words[wordIndex] ^= 1UL << (index % 64);
    }

    /// <summary>
    ///     与另一�?<see cref="BitSet" /> 执行交集操作，结果保存在当前实例中�?    ///
    /// </summary>
    /// <param name="other">另一个位集合�?/param>
    public void intersect_with(BitSet other)
    {
        var minLength = SonicMath.min(_words.Length, other._words.Length);
        for (var i = 0; i < minLength; i++) _words[i] &= other._words[i];

        if (_words.Length > minLength) Array.Clear(_words, minLength, _words.Length - minLength);
    }

    /// <summary>
    ///     与另一�?<see cref="BitSet" /> 执行并集操作，结果保存在当前实例中�?    ///
    /// </summary>
    /// <param name="other">另一个位集合�?/param>
    public void union_with(BitSet other)
    {
        if (other._words.Length > _words.Length) Array.Resize(ref _words, other._words.Length);

        for (var i = 0; i < other._words.Length; i++) _words[i] |= other._words[i];
    }

    /// <summary>
    ///     与另一�?<see cref="BitSet" /> 执行差集操作，结果保存在当前实例中�?    ///
    /// </summary>
    /// <param name="other">另一个位集合�?/param>
    public void except_with(BitSet other)
    {
        var minLength = SonicMath.min(_words.Length, other._words.Length);
        for (var i = 0; i < minLength; i++) _words[i] &= ~other._words[i];
    }

    /// <summary>
    ///     与另一�?<see cref="BitSet" /> 执行对称差操作，结果保存在当前实例中�?    ///
    /// </summary>
    /// <param name="other">另一个位集合�?/param>
    public void symmetric_except_with(BitSet other)
    {
        if (other._words.Length > _words.Length) Array.Resize(ref _words, other._words.Length);

        for (var i = 0; i < other._words.Length; i++) _words[i] ^= other._words[i];
    }

    /// <summary>
    ///     清空所有位，全部置�?<c>false</c>�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void clear_all()
    {
        Array.Clear(_words, 0, _words.Length);
    }

    private void ensure_capacity(int wordCount)
    {
        if (wordCount > _words.Length)
        {
            var newWords = new ulong[wordCount];
            Array.Copy(_words, newWords, _words.Length);
            _words = newWords;
        }
    }
}