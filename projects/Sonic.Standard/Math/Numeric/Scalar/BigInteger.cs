using Core.Math;
using Std.Math.Foundation;

namespace Std.Math.Numeric.Scalar;

using SystemMath = System.Math;

/// <summary>
///     浠绘剰绮惧害鏈夌鍙锋暣鏁帮紝浣跨敤灏忕搴?<c>ulong[]</c> 鑲綋瀛樺偍锛屼笉鍙彉銆?/// 鎵€鏈夎繍绠楄繑鍥炴柊瀹炰緥锛岀嚎绋嬪畨鍏ㄣ�?///
/// </summary>
public sealed class BigInteger : IAdditive<BigInteger>, IMultiplicative<BigInteger>, IApproximate<BigInteger>,
    IZero<BigInteger>, IOne<BigInteger>, IInteger<BigInteger>, IClone<BigInteger>, IComparable<BigInteger>,
    IEquatable<BigInteger>
{
    #region IApproximate

    /// <summary>
    ///     判断两个大整数是否在给定容差范围内近似相等
    /// </summary>
    /// <param name="other">比较目标</param>
    /// <param name="epsilon">容差</param>
    /// <returns>是否近似相等</returns>
    public bool approx_equals(BigInteger other, BigInteger epsilon)
    {
        var diff = sub(other).abs();
        return diff.CompareTo(epsilon) <= 0;
    }

    #endregion

    #region 甯搁�?

    /// <summary>
    ///     姣忎釜鑲綋鍖呭惈鐨勪綅鏁帮�?c>ulong</c> �?64 浣嶃�?    ///
    /// </summary>
    private const int _bits_per_limb = 64;

    /// <summary>
    ///     鍗佽繘鍒惰浆鎹㈠熀鏁帮紝姣忎釜鑲綋鏈€澶氬�?18 浣嶅崄杩涘埗鏁板瓧銆?    ///
    /// </summary>
    private const ulong _decimal_base = 1_000_000_000_000_000_000UL;

    /// <summary>
    ///     鍗佽繘鍒惰浆鎹㈠熀鏁扮殑浣嶆暟�?    ///
    /// </summary>
    private const int _decimal_digits_per_limb = 18;

    #endregion

    #region 瀹炰緥瀛楁�?

    /// <summary>
    ///     鏃犵鍙峰箙搴︼紝灏忕搴忚偄浣撴暟缁勶�?c>_limbs[0]</c> 涓烘渶浣庢湁鏁堜綅銆?    ///
    /// </summary>
    private readonly ulong[] _limbs;

    /// <summary>
    ///     绗﹀彿锛? 琛ㄧず闈炶礋�?1 琛ㄧず璐熸暟銆傞浂鐨勭鍙峰浐瀹氫�?1�?    ///
    /// </summary>
    private readonly int _sign;

    #endregion

    #region 闈欐€佺紦�?

    /// <summary>
    ///     缂撳瓨鐨勯浂鍊煎疄渚嬨€?    ///
    /// </summary>
    private static readonly BigInteger _zero = new();

    /// <summary>
    ///     缂撳瓨鐨勪竴鍊煎疄渚嬨€?    ///
    /// </summary>
    private static readonly BigInteger _one = new(1UL);

    #endregion

    #region 鏋勯€犲嚱鏁?

    /// <summary>
    ///     鏋勯€犲€间负闆剁殑 <see cref="BigInteger" />�?    ///
    /// </summary>
    public BigInteger()
    {
        _limbs = [0];
        _sign = 1;
    }

    /// <summary>
    ///     浠庢湁绗﹀�?64 浣嶆暣鏁版瀯閫?<see cref="BigInteger" />�?    ///
    /// </summary>
    /// <param name="value">鍒濆鍊笺€?/param>
    public BigInteger(long value)
    {
        if (value == 0)
        {
            _limbs = [0];
            _sign = 1;
            return;
        }

        _sign = value < 0 ? -1 : 1;
        _limbs = [(ulong)SystemMath.Abs(value)];
    }

    /// <summary>
    ///     浠庢棤绗﹀�?64 浣嶆暣鏁版瀯閫?<see cref="BigInteger" />�?    ///
    /// </summary>
    /// <param name="value">鍒濆鍊笺€?/param>
    public BigInteger(ulong value)
    {
        if (value == 0)
        {
            _limbs = [0];
            _sign = 1;
            return;
        }

        _limbs = [value];
        _sign = 1;
    }

    /// <summary>
    ///     鍐呴儴鏋勯€犲嚱鏁帮紝鐩存帴鎸囧畾绗﹀彿鍜岃偄浣撴暟缁勩€?    ///
    /// </summary>
    /// <param name="sign">
    ///     绗﹀彿锛? �?-1�?/param>
    ///     <param name="limbs">灏忕搴忚偄浣撴暟缁勩€?/param>
    private BigInteger(int sign, ulong[] limbs)
    {
        _sign = sign;
        _limbs = limbs;
    }

    #endregion

    #region 宸ュ巶鏂规硶

    /// <summary>
    ///     �?32 浣嶆暣鏁板垱�?<see cref="BigInteger" />�?    ///
    /// </summary>
    /// <param name="value">
    ///     鍒濆鍊笺€?/param>
    ///     <returns>瀵瑰簲鐨?<see cref="BigInteger" /> 瀹炰緥銆?/returns>
    public static BigInteger from_int(int value)
    {
        return new BigInteger(value);
    }

    /// <summary>
    ///     �?64 浣嶆暣鏁板垱�?<see cref="BigInteger" />�?    ///
    /// </summary>
    /// <param name="value">
    ///     鍒濆鍊笺€?/param>
    ///     <returns>瀵瑰簲鐨?<see cref="BigInteger" /> 瀹炰緥銆?/returns>
    public static BigInteger from_long(long value)
    {
        return new BigInteger(value);
    }

    /// <summary>
    ///     浠庡崄杩涘埗瀛楃涓茶В鏋愬垱�?<see cref="BigInteger" />�?    /// 鏀寔鍓嶅姝ｈ礋鍙凤紝蹇界暐鍓嶅闆躲€?    ///
    /// </summary>
    /// <param name="value">
    ///     鍗佽繘鍒跺瓧绗︿覆琛ㄧず�?/param>
    ///     <returns>
    ///         瀵瑰簲鐨?<see cref="BigInteger" /> 瀹炰緥銆?/returns>
    ///         <exception cref="FormatException">瀛楃涓叉牸寮忔棤鏁堛€?/exception>
    public static BigInteger from_string(string value)
    {
        if (string.IsNullOrEmpty(value)) throw new FormatException("字符串不能为空。");

        var span = value.AsSpan().Trim();
        if (span.IsEmpty) throw new FormatException("瀛楃涓蹭笉鑳戒负绌虹櫧");

        var sign = 1;
        if (span[0] == '-')
        {
            sign = -1;
            span = span[1..];
        }
        else if (span[0] == '+')
        {
            span = span[1..];
        }

        if (span.IsEmpty) throw new FormatException("符号后不能为空。");

        var start = 0;
        while (start < span.Length && span[start] == '0') start++;

        if (start == span.Length) return _zero;

        span = span[start..];

        foreach (var ch in span)
            if (ch is < '0' or > '9')
                throw new FormatException($"瀛楃涓插寘鍚棤鏁堝瓧绗︼�?{ch}'");

        var result = parse_positive_digits(span);
        if (sign == -1 && !result.is_zero) result = new BigInteger(-1, result._limbs);

        return result;
    }

    /// <summary>
    ///     获取缓存的零值实例
    /// </summary>
    public static BigInteger zero => zero;

    /// <summary>
    ///     获取缓存的一值实例
    /// </summary>
    public static BigInteger one => one;

    #endregion

    #region 灞炴�?

    /// <summary>
    ///     鍒ゆ柇褰撳墠鍊兼槸鍚︿负闆躲�?    ///
    /// </summary>
    public bool is_zero
    {
        get
        {
            foreach (var limb in _limbs)
                if (limb != 0)
                    return false;

            return true;
        }
    }

    /// <summary>
    ///     鍒ゆ柇褰撳墠鍊兼槸鍚︿负璐熸暟銆?    ///
    /// </summary>
    public bool is_negative => _sign == -1 && !is_zero;

    #endregion

    #region 绠楁湳杩愮畻

    /// <summary>
    ///     杩斿洖褰撳墠鍊间�?<paramref name="other" /> 鐨勫拰銆?    ///
    /// </summary>
    /// <param name="other">
    ///     鍔犳暟銆?/param>
    ///     <returns>鍔犳硶缁撴灉鐨勬柊瀹炰緥銆?/returns>
    public BigInteger add(BigInteger other)
    {
        if (is_zero) return other;

        if (other.is_zero) return this;

        if (_sign == other._sign)
        {
            var magnitude = add_magnitude(_limbs, other._limbs);
            return new BigInteger(_sign, magnitude);
        }

        var cmp = compare_magnitude(_limbs, other._limbs);
        if (cmp == 0) return _zero;

        if (cmp > 0)
        {
            var magnitude = sub_magnitude(_limbs, other._limbs);
            return new BigInteger(_sign, magnitude);
        }
        else
        {
            var magnitude = sub_magnitude(other._limbs, _limbs);
            return new BigInteger(other._sign, magnitude);
        }
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊煎噺鍘?<paramref name="other" /> 鐨勫樊銆?    ///
    /// </summary>
    /// <param name="other">
    ///     鍑忔暟銆?/param>
    ///     <returns>鍑忔硶缁撴灉鐨勬柊瀹炰緥銆?/returns>
    public BigInteger sub(BigInteger other)
    {
        return add(other.neg());
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊间�?<paramref name="other" /> 鐨勭Н�?    ///
    /// </summary>
    /// <param name="other">
    ///     涔樻暟銆?/param>
    ///     <returns>涔樻硶缁撴灉鐨勬柊瀹炰緥銆?/returns>
    public BigInteger mul(BigInteger other)
    {
        if (is_zero || other.is_zero) return _zero;

        var magnitude = multiply_magnitude(_limbs, other._limbs);
        var resultSign = _sign == other._sign ? 1 : -1;
        return new BigInteger(resultSign, magnitude);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊奸櫎浠?<paramref name="other" /> 鐨勫晢銆?    ///
    /// </summary>
    /// <param name="other">
    ///     闄ゆ暟銆?/param>
    ///     <returns>
    ///         鍟嗙殑鏂板疄渚嬨�?/returns>
    ///         <exception cref="DivideByZeroException">闄ゆ暟涓洪浂�?/exception>
    public BigInteger div(BigInteger other)
    {
        if (other.is_zero) throw new DivideByZeroException("除数不能为零。");

        if (is_zero) return _zero;

        var (quotient, _) = div_rem_magnitude(_limbs, other._limbs);
        var resultSign = _sign == other._sign ? 1 : -1;
        var result = new BigInteger(resultSign, quotient);
        return result.is_zero ? _zero : result;
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊奸櫎浠?<paramref name="other" /> 鐨勪綑鏁般€?    /// 浣欐暟鐨勭鍙蜂笌琚櫎鏁扮浉鍚屻€?    ///
    /// </summary>
    /// <param name="other">
    ///     闄ゆ暟銆?/param>
    ///     <returns>
    ///         浣欐暟鐨勬柊瀹炰緥銆?/returns>
    ///         <exception cref="DivideByZeroException">闄ゆ暟涓洪浂�?/exception>
    public BigInteger rem(BigInteger other)
    {
        if (other.is_zero) throw new DivideByZeroException("除数不能为零。");

        if (is_zero) return _zero;

        var (_, remainder) = div_rem_magnitude(_limbs, other._limbs);
        var result = new BigInteger(_sign, remainder);
        return result.is_zero ? _zero : result;
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊肩殑鐩稿弽鏁般�?    ///
    /// </summary>
    /// <returns>鐩稿弽鏁扮殑鏂板疄渚嬨€?/returns>
    public BigInteger neg()
    {
        if (is_zero) return _zero;

        return new BigInteger(-_sign, _limbs);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊肩殑缁濆鍊笺�?    ///
    /// </summary>
    /// <returns>缁濆鍊肩殑鏂板疄渚嬨€?/returns>
    public BigInteger abs()
    {
        if (is_zero || _sign == 1) return this;

        return new BigInteger(1, _limbs);
    }

    #endregion

    #region 姣旇�?

    /// <summary>
    ///     鍒ゆ柇褰撳墠鍊兼槸鍚︿笌 <paramref name="other" /> 鐩哥瓑銆?    ///
    /// </summary>
    /// <param name="other">
    ///     姣旇緝鐩爣�?/param>
    ///     <returns>鐩哥瓑杩斿洖 <c>true</c>锛屽惁鍒欒繑�?<c>false</c>�?/returns>
    public bool Equals(BigInteger other)
    {
        if (other is null) return false;

        return CompareTo(other) == 0;
    }

    /// <summary>
    ///     姣旇緝褰撳墠鍊间�?<paramref name="other" /> 鐨勫ぇ灏忋€?    ///
    /// </summary>
    /// <param name="other">
    ///     姣旇緝鐩爣�?/param>
    ///     <returns>灏忎�?0 琛ㄧず褰撳墠鍊艰緝灏忥紝0 琛ㄧず鐩哥瓑锛屽ぇ浜?0 琛ㄧず褰撳墠鍊艰緝澶с�?/returns>
    public int CompareTo(BigInteger other)
    {
        if (other is null) return 1;

        if (is_zero && other.is_zero) return 0;

        if (_sign != other._sign) return _sign > other._sign ? 1 : -1;

        var magnitudeCmp = compare_magnitude(_limbs, other._limbs);
        return _sign == 1 ? magnitudeCmp : -magnitudeCmp;
    }

    #endregion

    #region 浣嶈繍绠?

    /// <summary>
    ///     杩斿洖褰撳墠鍊煎乏绉?<paramref name="count" /> 浣嶇殑缁撴灉�?    ///
    /// </summary>
    /// <param name="count">
    ///     宸︾Щ浣嶆暟锛屽繀椤婚潪璐熴€?/param>
    ///     <returns>
    ///         宸︾Щ缁撴灉鐨勬柊瀹炰緥銆?/returns>
    ///         <exception cref="ArgumentOutOfRangeException">绉讳綅浣嶆暟涓鸿礋鏁般€?/exception>
    public BigInteger shift_left(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "移位位数不能为负数。");

        if (count == 0 || is_zero) return this;

        var limbShift = count / _bits_per_limb;
        var bitShift = count % _bits_per_limb;
        var resultLength = _limbs.Length + limbShift + 1;
        var result = new ulong[resultLength];

        if (bitShift == 0)
        {
            for (var i = 0; i < _limbs.Length; i++) result[i + limbShift] = _limbs[i];
        }
        else
        {
            var carry = 0UL;
            for (var i = 0; i < _limbs.Length; i++)
            {
                var shifted = (_limbs[i] << bitShift) | carry;
                carry = _limbs[i] >> (_bits_per_limb - bitShift);
                result[i + limbShift] = shifted;
            }

            result[_limbs.Length + limbShift] = carry;
        }

        normalize_array(result);
        return new BigInteger(_sign, result);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊煎彸绉?<paramref name="count" /> 浣嶇殑缁撴灉�?    /// 瀵硅礋鏁版墽琛岀畻鏈彸绉汇�?    ///
    /// </summary>
    /// <param name="count">
    ///     鍙崇Щ浣嶆暟锛屽繀椤婚潪璐熴€?/param>
    ///     <returns>
    ///         鍙崇Щ缁撴灉鐨勬柊瀹炰緥銆?/returns>
    ///         <exception cref="ArgumentOutOfRangeException">绉讳綅浣嶆暟涓鸿礋鏁般€?/exception>
    public BigInteger shift_right(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "移位位数不能为负数。");

        if (count == 0 || is_zero) return this;

        var limbShift = count / _bits_per_limb;
        var bitShift = count % _bits_per_limb;

        if (limbShift >= _limbs.Length) return _sign == -1 ? new BigInteger(-1, [1]) : _zero;

        var resultLength = _limbs.Length - limbShift;
        var result = new ulong[resultLength];

        if (bitShift == 0)
            for (var i = 0; i < resultLength; i++)
                result[i] = _limbs[i + limbShift];
        else
            for (var i = 0; i < resultLength; i++)
            {
                var shifted = _limbs[i + limbShift] >> bitShift;
                if (i + limbShift + 1 < _limbs.Length)
                    shifted |= _limbs[i + limbShift + 1] << (_bits_per_limb - bitShift);

                result[i] = shifted;
            }

        normalize_array(result);

        if (_sign == -1 && !is_all_zero(result))
        {
            var absResult = new BigInteger(1, result);
            absResult = absResult.add(_one);
            return absResult.neg();
        }

        if (is_all_zero(result)) return _sign == -1 ? new BigInteger(-1, [1]) : _zero;

        return new BigInteger(_sign, result);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊间�?<paramref name="other" /> 鎸変綅涓庣殑缁撴灉銆?    /// 璐熸暟鎸変簩杩涘埗琛ョ爜瑙ｉ噴銆?    ///
    /// </summary>
    /// <param name="other">
    ///     鎿嶄綔鏁般€?/param>
    ///     <returns>鎸変綅涓庣粨鏋滅殑鏂板疄渚嬨�?/returns>
    public BigInteger bit_and(BigInteger other)
    {
        if (is_zero || other.is_zero) return _zero;

        if (_sign == 1 && other._sign == 1)
        {
            var length = SystemMath.Min(_limbs.Length, other._limbs.Length);
            var result = new ulong[length];
            for (var i = 0; i < length; i++) result[i] = _limbs[i] & other._limbs[i];

            normalize_array(result);
            var r = new BigInteger(1, result);
            return r.is_zero ? _zero : r;
        }

        return bitwise_operation(other, (a, b) => a & b);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊间�?<paramref name="other" /> 鎸変綅鎴栫殑缁撴灉銆?    /// 璐熸暟鎸変簩杩涘埗琛ョ爜瑙ｉ噴銆?    ///
    /// </summary>
    /// <param name="other">
    ///     鎿嶄綔鏁般€?/param>
    ///     <returns>鎸変綅鎴栫粨鏋滅殑鏂板疄渚嬨�?/returns>
    public BigInteger bit_or(BigInteger other)
    {
        if (is_zero) return other;

        if (other.is_zero) return this;

        if (_sign == 1 && other._sign == 1)
        {
            var length = SystemMath.Max(_limbs.Length, other._limbs.Length);
            var result = new ulong[length];
            for (var i = 0; i < length; i++)
            {
                var a = i < _limbs.Length ? _limbs[i] : 0UL;
                var b = i < other._limbs.Length ? other._limbs[i] : 0UL;
                result[i] = a | b;
            }

            normalize_array(result);
            return new BigInteger(1, result);
        }

        return bitwise_operation(other, (a, b) => a | b);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊间�?<paramref name="other" /> 鎸変綅寮傛垨鐨勭粨鏋溿€?    /// 璐熸暟鎸変簩杩涘埗琛ョ爜瑙ｉ噴銆?    ///
    /// </summary>
    /// <param name="other">
    ///     鎿嶄綔鏁般€?/param>
    ///     <returns>鎸変綅寮傛垨缁撴灉鐨勬柊瀹炰緥銆?/returns>
    public BigInteger bit_xor(BigInteger other)
    {
        if (is_zero) return other;

        if (other.is_zero) return this;

        if (_sign == 1 && other._sign == 1)
        {
            var length = SystemMath.Max(_limbs.Length, other._limbs.Length);
            var result = new ulong[length];
            for (var i = 0; i < length; i++)
            {
                var a = i < _limbs.Length ? _limbs[i] : 0UL;
                var b = i < other._limbs.Length ? other._limbs[i] : 0UL;
                result[i] = a ^ b;
            }

            normalize_array(result);
            var r = new BigInteger(1, result);
            return r.is_zero ? _zero : r;
        }

        return bitwise_operation(other, (a, b) => a ^ b);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊兼寜浣嶅彇鍙嶇殑缁撴灉锛堜簩杩涘埗琛ョ爜锛夈€?    ///
    /// </summary>
    /// <returns>鎸変綅鍙栧弽缁撴灉鐨勬柊瀹炰緥銆?/returns>
    public BigInteger bit_not()
    {
        return neg().sub(_one);
    }

    #endregion

    #region 鏁拌�?

    /// <summary>
    ///     浣跨敤骞虫柟-涔樼畻娉曡�?<c>this^exponent mod modulus</c>�?    ///
    /// </summary>
    /// <param name="exponent">
    ///     鎸囨暟锛屽繀椤婚潪璐熴€?/param>
    ///     <param name="modulus">
    ///         妯℃暟锛屽繀椤讳负姝ｃ€?/param>
    ///         <returns>
    ///             妯″箓缁撴灉鐨勬柊瀹炰緥銆?/returns>
    ///             <exception cref="ArgumentOutOfRangeException">鎸囨暟涓鸿礋鎴栨ā鏁伴潪姝ｃ€?/exception>
    public BigInteger modular_power(BigInteger exponent, BigInteger modulus)
    {
        if (modulus.is_zero || modulus.is_negative) throw new ArgumentOutOfRangeException(nameof(modulus), "模数必须为正数。");

        if (exponent.is_negative) throw new ArgumentOutOfRangeException(nameof(exponent), "指数不能为负数。");

        if (modulus is { _sign: 1, _limbs: [1] }) return _zero;

        if (exponent.is_zero) return _one;

        var result = _one;
        var @base = rem(modulus);
        if (@base.is_negative) @base = @base.add(modulus);

        var exp = exponent.clone();
        while (!exp.is_zero)
        {
            if (!exp.is_even()) result = result.mul(@base).rem(modulus);

            exp = exp.shift_right(1);
            @base = @base.mul(@base).rem(modulus);
        }

        return result;
    }

    /// <summary>
    ///     浣跨敤鎵╁睍娆у嚑閲屽緱绠楁硶璁＄�?<c>this</c> �?<paramref name="modulus" /> 鐨勪箻娉曢€嗗厓�?    ///
    /// </summary>
    /// <param name="modulus">
    ///     妯℃暟锛屽繀椤讳负姝ｃ€?/param>
    ///     <returns>
    ///         涔樻硶閫嗗厓锛岃嫢涓嶅瓨鍦ㄥ垯杩斿洖 <c>null</c>�?/returns>
    ///         <exception cref="ArgumentOutOfRangeException">妯℃暟闈炴�?/exception>
    public BigInteger? modular_inverse(BigInteger modulus)
    {
        if (modulus.is_zero || modulus.is_negative) throw new ArgumentOutOfRangeException(nameof(modulus), "模数必须为正数。");

        var g = greatest_common_divisor(this, modulus);
        if (!g.Equals(_one)) return null;

        var (oldR, r) = (this, modulus);
        var (oldS, s) = (_one, _zero);

        while (!r.is_zero)
        {
            var quotient = oldR.div(r);
            (oldR, r) = (r, oldR.sub(quotient.mul(r)));
            (oldS, s) = (s, oldS.sub(quotient.mul(s)));
        }

        var result = oldS.rem(modulus);
        if (result.is_negative) result = result.add(modulus);

        return result;
    }

    /// <summary>
    ///     浣跨�?Miller-Rabin 绱犳€ф祴璇曞垽鏂綋鍓嶅€兼槸鍚﹀彲鑳戒负绱犳暟銆?    ///
    /// </summary>
    /// <param name="rounds">
    ///     娴嬭瘯杞暟锛岄粯璁?20銆傝疆鏁拌秺澶氾紝璇垽姒傜巼瓒婁綆�?/param>
    ///     <returns>
    ///         <c>true</c> 琛ㄧず鍙兘涓虹礌鏁帮紝<c>false</c> 琛ㄧず涓€瀹氫负鍚堟暟�?/returns>
    ///         <exception cref="ArgumentOutOfRangeException">褰撳墠鍊煎皬�?2�?/exception>
    public bool is_probably_prime(int rounds = 20)
    {
        if (CompareTo(_one) <= 0) return false;

        if (CompareTo(new BigInteger(3L)) <= 0) return true;

        if (is_even()) return false;

        var nMinusOne = sub(_one);
        var d = nMinusOne.clone();
        var r = 0;
        while (d.is_even())
        {
            d = d.shift_right(1);
            r++;
        }

        var random = new Random();
        for (var i = 0; i < rounds; i++)
        {
            var a = random_big_integer(new BigInteger(2L), nMinusOne, random);
            var x = a.modular_power(d, this);
            if (x.Equals(_one) || x.Equals(nMinusOne)) continue;

            var composite = true;
            for (var j = 0; j < r - 1; j++)
            {
                x = x.mul(x).rem(this);
                if (x.Equals(nMinusOne))
                {
                    composite = false;
                    break;
                }
            }

            if (composite) return false;
        }

        return true;
    }

    /// <summary>
    ///     杩斿洖澶т簬鎴栫瓑浜庡綋鍓嶅€肩殑鏈€灏忕礌鏁般€?    ///
    /// </summary>
    /// <returns>涓嬩竴涓礌鏁扮殑鏂板疄渚嬨�?/returns>
    public BigInteger next_prime()
    {
        var candidate = CompareTo(new BigInteger(2L)) < 0 ? new BigInteger(2L) : this;

        if (candidate.Equals(new BigInteger(2L))) return new BigInteger(2L);

        if (candidate.is_even()) candidate = candidate.add(_one);

        while (!candidate.is_probably_prime()) candidate = candidate.add(new BigInteger(2L));

        return candidate;
    }

    /// <summary>
    ///     浣跨敤娆у嚑閲屽緱绠楁硶璁＄畻 <paramref name="a" /> �?<paramref name="b" /> 鐨勬渶澶у叕绾︽暟�?    /// 缁撴灉濮嬬粓闈炶礋銆?    ///
    /// </summary>
    /// <param name="a">
    ///     绗竴涓搷浣滄暟銆?/param>
    ///     <param name="b">
    ///         绗簩涓搷浣滄暟銆?/param>
    ///         <returns>鏈€澶у叕绾︽暟鐨勬柊瀹炰緥銆?/returns>
    public static BigInteger greatest_common_divisor(BigInteger a, BigInteger b)
    {
        var x = a.abs();
        var y = b.abs();

        while (!y.is_zero)
        {
            var temp = y;
            y = x.rem(y);
            x = temp;
        }

        return x;
    }

    #endregion

    #region 绫诲瀷杞崲

    /// <summary>
    ///     灏嗗綋鍓嶅€艰浆鎹负鏈夌�?64 浣嶆暣鏁般€?    ///
    /// </summary>
    /// <returns>
    ///     64 浣嶆暣鏁板€笺€?/returns>
    ///     <exception cref="OverflowException">鍊艰秴鍑?<c>long</c> 鑼冨洿銆?/exception>
    public long to_int64()
    {
        if (is_zero) return 0;

        if (_limbs.Length > 1) throw new OverflowException("值超出 long 范围。");

        var magnitude = _limbs[0];
        if (_sign == 1)
        {
            if (magnitude > long.MaxValue) throw new OverflowException("值超出 long 范围。");

            return (long)magnitude;
        }

        if (magnitude > (ulong)long.MaxValue + 1) throw new OverflowException("值超出 long 范围。");

        return -(long)magnitude;
    }

    /// <summary>
    ///     灏嗗綋鍓嶅€艰浆鎹负鏃犵�?64 浣嶆暣鏁般€?    ///
    /// </summary>
    /// <returns>
    ///     鏃犵鍙?64 浣嶆暣鏁板€笺€?/returns>
    ///     <exception cref="OverflowException">鍊间负璐熸暟鎴栬秴鍑?<c>ulong</c> 鑼冨洿銆?/exception>
    public ulong to_uint64()
    {
        if (is_negative) throw new OverflowException("璐熸暟鏃犳硶杞崲涓?ulong");

        if (is_zero) return 0;

        if (_limbs.Length > 1) throw new OverflowException("值超出 ulong 范围。");

        return _limbs[0];
    }

    /// <summary>
    ///     灏嗗綋鍓嶅€艰浆鎹负鍙岀簿搴︽诞鐐规暟銆傚ぇ鏁板彲鑳芥崯澶辩簿搴︺€?    ///
    /// </summary>
    /// <returns>杩戜技鐨勫弻绮惧害娴偣鏁板€笺€?/returns>
    public double to_float64()
    {
        if (is_zero) return 0.0;

        var bits = bit_length();
        var exponent = bits - 1;
        var mantissa = 0.0;
        var bitIndex = exponent;

        for (var i = 0; i < 53 && bitIndex >= 0; i++, bitIndex--)
        {
            if (test_bit(bitIndex)) mantissa += 1.0;

            if (i < 52) mantissa *= 0.5;
        }

        var result = mantissa * SystemMath.Pow(2.0, exponent);
        return _sign == -1 ? -result : result;
    }

    #endregion

    #region 瀛楃涓蹭笌瀵硅�?

    /// <summary>
    ///     杩斿洖褰撳墠鍊肩殑鍗佽繘鍒跺瓧绗︿覆琛ㄧず銆?    ///
    /// </summary>
    /// <returns>鍗佽繘鍒跺瓧绗︿覆銆?/returns>
    public override string ToString()
    {
        if (is_zero) return "0";

        var sb = new StringBuilder();
        var value = abs();
        var ten = new BigInteger(10UL);

        while (!value.is_zero)
        {
            var remainder = value.rem(ten);
            sb.Insert(0, (char)('0' + remainder._limbs[0]));
            value = value.div(ten);
        }

        if (_sign == -1) sb.Insert(0, '-');

        return sb.ToString();
    }

    /// <summary>
    ///     杩斿洖褰撳墠瀵硅薄鐨勬繁鎷疯礉銆?    ///
    /// </summary>
    /// <returns>娣辨嫹璐濈殑鏂板疄渚嬨€?/returns>
    public BigInteger clone()
    {
        if (is_zero) return _zero;

        var newLimbs = new ulong[_limbs.Length];
        Array.Copy(_limbs, newLimbs, _limbs.Length);
        return new BigInteger(_sign, newLimbs);
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍊肩殑鍝堝笇鐮併�?    ///
    /// </summary>
    /// <returns>鍝堝笇鐮併€?/returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_sign);
        foreach (var limb in _limbs) hash.Add(limb);

        return hash.ToHashCode();
    }

    /// <summary>
    ///     鍒ゆ柇褰撳墠瀵硅薄鏄惁涓庢寚瀹氬璞＄浉绛夈�?    ///
    /// </summary>
    /// <param name="obj">
    ///     姣旇緝鐩爣�?/param>
    ///     <returns>鐩哥瓑杩斿洖 <c>true</c>锛屽惁鍒欒繑�?<c>false</c>�?/returns>
    public override bool Equals(object? obj)
    {
        return obj is BigInteger other && Equals(other);
    }

    #endregion

    #region 杩愮畻绗﹂噸�?

    /// <summary>
    ///     鍔犳硶杩愮畻绗︺�?    ///
    /// </summary>
    public static BigInteger operator +(BigInteger left, BigInteger right)
    {
        return left.add(right);
    }

    /// <summary>
    ///     鍑忔硶杩愮畻绗︺�?    ///
    /// </summary>
    public static BigInteger operator -(BigInteger left, BigInteger right)
    {
        return left.sub(right);
    }

    /// <summary>
    ///     涓€鍏冨彇璐熻繍绠楃銆?    ///
    /// </summary>
    public static BigInteger operator -(BigInteger value)
    {
        return value.neg();
    }

    /// <summary>
    ///     涓€鍏冨姞杩愮畻绗︺�?    ///
    /// </summary>
    public static BigInteger operator +(BigInteger value)
    {
        return value;
    }

    /// <summary>
    ///     涔樻硶杩愮畻绗︺�?    ///
    /// </summary>
    public static BigInteger operator *(BigInteger left, BigInteger right)
    {
        return left.mul(right);
    }

    /// <summary>
    ///     闄ゆ硶杩愮畻绗︺�?    ///
    /// </summary>
    public static BigInteger operator /(BigInteger left, BigInteger right)
    {
        return left.div(right);
    }

    /// <summary>
    ///     鍙栦綑杩愮畻绗︺�?    ///
    /// </summary>
    public static BigInteger operator %(BigInteger left, BigInteger right)
    {
        return left.rem(right);
    }

    /// <summary>
    ///     鐩哥瓑杩愮畻绗︺�?    ///
    /// </summary>
    public static bool operator ==(BigInteger? left, BigInteger? right)
    {
        if (left is null) return right is null;

        return right is not null && left.Equals(right);
    }

    /// <summary>
    ///     涓嶇瓑杩愮畻绗︺�?    ///
    /// </summary>
    public static bool operator !=(BigInteger? left, BigInteger? right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     灏忎簬杩愮畻绗︺�?    ///
    /// </summary>
    public static bool operator <(BigInteger? left, BigInteger? right)
    {
        if (left is null) return right is not null;

        if (right is null) return false;

        return left.CompareTo(right) < 0;
    }

    /// <summary>
    ///     澶т簬杩愮畻绗︺€?    ///
    /// </summary>
    public static bool operator >(BigInteger? left, BigInteger? right)
    {
        if (left is null) return false;

        if (right is null) return true;

        return left.CompareTo(right) > 0;
    }

    /// <summary>
    ///     灏忎簬绛変簬杩愮畻绗︺€?    ///
    /// </summary>
    public static bool operator <=(BigInteger? left, BigInteger? right)
    {
        if (left is null) return true;

        if (right is null) return false;

        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    ///     澶т簬绛変簬杩愮畻绗︺�?    ///
    /// </summary>
    public static bool operator >=(BigInteger? left, BigInteger? right)
    {
        if (left is null) return right is null;

        if (right is null) return true;

        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    ///     宸︾Щ杩愮畻绗︺€?    ///
    /// </summary>
    public static BigInteger operator <<(BigInteger value, int count)
    {
        return value.shift_left(count);
    }

    /// <summary>
    ///     鍙崇Щ杩愮畻绗︺€?    ///
    /// </summary>
    public static BigInteger operator >> (BigInteger value, int count)
    {
        return value.shift_right(count);
    }

    /// <summary>
    ///     鎸変綅涓庤繍绠楃銆?    ///
    /// </summary>
    public static BigInteger operator &(BigInteger left, BigInteger right)
    {
        return left.bit_and(right);
    }

    /// <summary>
    ///     鎸変綅鎴栬繍绠楃銆?    ///
    /// </summary>
    public static BigInteger operator |(BigInteger left, BigInteger right)
    {
        return left.bit_or(right);
    }

    /// <summary>
    ///     鎸変綅寮傛垨杩愮畻绗︺€?    ///
    /// </summary>
    public static BigInteger operator ^(BigInteger left, BigInteger right)
    {
        return left.bit_xor(right);
    }

    /// <summary>
    ///     鎸変綅鍙栧弽杩愮畻绗︺€?    ///
    /// </summary>
    public static BigInteger operator ~(BigInteger value)
    {
        return value.bit_not();
    }

    #endregion

    #region 鍐呴儴杈呭姪鏂规�?

    /// <summary>
    ///     姣旇緝涓や釜鏃犵鍙疯偄浣撴暟缁勭殑澶у皬�?    ///
    /// </summary>
    /// <param name="a">
    ///     绗竴涓偄浣撴暟缁勩€?/param>
    ///     <param name="b">
    ///         绗簩涓偄浣撴暟缁勩€?/param>
    ///         <returns>灏忎�?0 琛ㄧ�?a 杈冨皬锛? 琛ㄧず鐩哥瓑锛屽ぇ浜?0 琛ㄧ�?a 杈冨ぇ銆?/returns>
    private static int compare_magnitude(ulong[] a, ulong[] b)
    {
        var aLen = effective_length(a);
        var bLen = effective_length(b);

        if (aLen != bLen) return aLen > bLen ? 1 : -1;

        for (var i = aLen - 1; i >= 0; i--)
            if (a[i] != b[i])
                return a[i] > b[i] ? 1 : -1;

        return 0;
    }

    /// <summary>
    ///     鏃犵鍙疯偄浣撴暟缁勫姞娉曘�?    ///
    /// </summary>
    /// <param name="a">
    ///     鍔犳暟銆?/param>
    ///     <param name="b">
    ///         鍔犳暟銆?/param>
    ///         <returns>鍜岀殑鑲綋鏁扮粍銆?/returns>
    private static ulong[] add_magnitude(ulong[] a, ulong[] b)
    {
        var aLen = effective_length(a);
        var bLen = effective_length(b);
        var maxLen = SystemMath.Max(aLen, bLen);
        var result = new ulong[maxLen + 1];
        var carry = 0UL;

        for (var i = 0; i < maxLen; i++)
        {
            var aVal = i < aLen ? a[i] : 0UL;
            var bVal = i < bLen ? b[i] : 0UL;
            var sum = aVal + bVal + carry;
            result[i] = sum;
            carry = sum < aVal || (sum == aVal && bVal + carry > 0) ? 1UL : 0UL;
        }

        result[maxLen] = carry;
        normalize_array(result);
        return result;
    }

    /// <summary>
    ///     鏃犵鍙疯偄浣撴暟缁勫噺娉曪紝瑕佹眰 <paramref name="a" /> >= <paramref name="b" />�?    ///
    /// </summary>
    /// <param name="a">
    ///     琚噺鏁般€?/param>
    ///     <param name="b">
    ///         鍑忔暟銆?/param>
    ///         <returns>宸殑鑲綋鏁扮粍銆?/returns>
    private static ulong[] sub_magnitude(ulong[] a, ulong[] b)
    {
        var aLen = effective_length(a);
        var bLen = effective_length(b);
        var result = new ulong[aLen];
        var borrow = 0UL;

        for (var i = 0; i < aLen; i++)
        {
            var aVal = a[i];
            var bVal = i < bLen ? b[i] : 0UL;
            var diff = aVal - bVal - borrow;
            result[i] = diff;
            borrow = aVal < bVal + borrow ? 1UL : 0UL;
        }

        normalize_array(result);
        return result;
    }

    /// <summary>
    ///     鏃犵鍙疯偄浣撴暟缁勪箻娉曪紙鏁欑涔︾畻娉曪級�?    ///
    /// </summary>
    /// <param name="a">
    ///     涔樻暟銆?/param>
    ///     <param name="b">
    ///         涔樻暟銆?/param>
    ///         <returns>绉殑鑲綋鏁扮粍銆?/returns>
    private static ulong[] multiply_magnitude(ulong[] a, ulong[] b)
    {
        var aLen = effective_length(a);
        var bLen = effective_length(b);

        if (aLen == 0 || bLen == 0) return [0];

        var result = new ulong[aLen + bLen];

        for (var i = 0; i < aLen; i++)
        {
            var carry = 0UL;
            for (var j = 0; j < bLen; j++)
            {
                var product = a[i] * b[j];
                var lo = (uint)product;
                var hi = product >> 32;

                var midCarry = hi + ((lo & 0xFFFFFFFF00000000UL) >> 32);
                var newLo = (uint)(product & 0xFFFFFFFFUL);

                var sum = result[i + j] + newLo + carry;
                result[i + j] = sum;

                carry = midCarry;
                if (sum < result[i + j] - newLo - carry + carry) carry++;

                var highSum = result[i + j + 1] + (product >> 32) + carry;
                if (highSum < result[i + j + 1])
                    carry = 1;
                else
                    carry = 0;

                result[i + j + 1] = highSum;
            }

            if (carry > 0 && i + bLen < result.Length) result[i + bLen] += carry;
        }

        normalize_array(result);
        return result;
    }

    /// <summary>
    ///     鏃犵鍙疯偄浣撴暟缁勯暱闄ゆ硶锛岃繑鍥炲晢鍜屼綑鏁般�?    ///
    /// </summary>
    /// <param name="a">
    ///     琚櫎鏁般€?/param>
    ///     <param name="b">
    ///         闄ゆ暟銆?/param>
    ///         <returns>
    ///             鍟嗗拰浣欐暟鐨勮偄浣撴暟缁勫厓缁勩€?/returns>
    ///             <exception cref="DivideByZeroException">闄ゆ暟涓洪浂�?/exception>
    private static (ulong[] quotient, ulong[] remainder) div_rem_magnitude(ulong[] a, ulong[] b)
    {
        var aLen = effective_length(a);
        var bLen = effective_length(b);

        if (bLen == 0 || (bLen == 1 && b[0] == 0)) throw new DivideByZeroException("除数不能为零。");

        if (aLen == 0 || (aLen == 1 && a[0] == 0)) return ([0], [0]);

        var cmp = compare_magnitude(a, b);
        if (cmp < 0) return ([0], copy_limbs(a));

        if (cmp == 0) return ([1], [0]);

        if (bLen == 1) return div_rem_by_single(a, b[0]);

        return div_rem_multi_digit(a, b);
    }

    /// <summary>
    ///     绉婚櫎鑲綋鏁扮粍鏈熬鐨勫墠瀵奸浂鑲綋�?    ///
    /// </summary>
    private void normalize()
    {
        normalize_array(_limbs);
    }

    /// <summary>
    ///     璁＄畻褰撳墠鍊兼墍闇€鐨勪綅鏁般�?    ///
    /// </summary>
    /// <returns>浣嶆暟锛岄浂杩斿�?0�?/returns>
    public int bit_length()
    {
        if (is_zero) return 0;

        var len = effective_length(_limbs);
        var topLimb = _limbs[len - 1];
        var bits = (len - 1) * _bits_per_limb + bit_length_of_ulong(topLimb);
        return bits;
    }

    /// <summary>
    ///     鍒ゆ柇褰撳墠鍊兼槸鍚︿负鍋舵暟銆?    ///
    /// </summary>
    /// <returns>鍋舵暟杩斿洖 <c>true</c>锛屽鏁拌繑�?<c>false</c>�?/returns>
    private bool is_even()
    {
        return (_limbs[0] & 1) == 0;
    }

    /// <summary>
    ///     娴嬭瘯鎸囧畾浣嶆槸鍚︿负 1�?    ///
    /// </summary>
    /// <param name="index">
    ///     浣嶇储寮曪紝浠庢渶浣庝綅寮€濮嬨�?/param>
    ///     <returns>璇ヤ綅涓?1 杩斿�?<c>true</c>锛屽惁鍒欒繑�?<c>false</c>�?/returns>
    private bool test_bit(int index)
    {
        var limbIndex = index / _bits_per_limb;
        var bitIndex = index % _bits_per_limb;
        if (limbIndex >= _limbs.Length) return false;

        return (_limbs[limbIndex] & (1UL << bitIndex)) != 0;
    }

    /// <summary>
    ///     鑾峰彇鑲綋鏁扮粍涓湁鏁堬紙闈為浂锛夐儴鍒嗙殑闀垮害�?    ///
    /// </summary>
    /// <param name="limbs">
    ///     鑲綋鏁扮粍�?/param>
    ///     <returns>鏈夋晥闀垮害�?/returns>
    private static int effective_length(ulong[] limbs)
    {
        var len = limbs.Length;
        while (len > 0 && limbs[len - 1] == 0) len--;

        return len;
    }

    /// <summary>
    ///     绉婚櫎鏁扮粍鏈熬鐨勫墠瀵奸浂鑲綋锛岀‘淇濊嚦灏戜繚鐣欎竴涓厓绱犮€?    ///
    /// </summary>
    /// <param name="limbs">寰呰鑼冨寲鐨勮偄浣撴暟缁勩�?/param>
    private static void normalize_array(ulong[] limbs)
    {
        var len = limbs.Length;
        while (len > 1 && limbs[len - 1] == 0) len--;

        if (len < limbs.Length) Array.Resize(ref limbs, len);
    }

    /// <summary>
    ///     澶嶅埗鑲綋鏁扮粍銆?    ///
    /// </summary>
    /// <param name="limbs">
    ///     婧愯偄浣撴暟缁勩�?/param>
    ///     <returns>澶嶅埗鍚庣殑鏂版暟缁勩€?/returns>
    private static ulong[] copy_limbs(ulong[] limbs)
    {
        var len = effective_length(limbs);
        if (len == 0) return [0];

        var result = new ulong[len];
        Array.Copy(limbs, result, len);
        return result;
    }

    /// <summary>
    ///     璁＄�?<c>ulong</c> 鍊肩殑浣嶆暟锛堟渶楂樻湁鏁堜綅鐨勪綅�?+ 1锛夈�?    ///
    /// </summary>
    /// <param name="value">
    ///     寰呰绠楃殑鍊笺�?/param>
    ///     <returns>浣嶆暟銆?/returns>
    private static int bit_length_of_ulong(ulong value)
    {
        if (value == 0) return 0;

        var bits = 0;
        while (value != 0)
        {
            bits++;
            value >>= 1;
        }

        return bits;
    }

    /// <summary>
    ///     鍒ゆ柇鑲綋鏁扮粍鏄惁鍏ㄤ负闆躲€?    ///
    /// </summary>
    /// <param name="limbs">
    ///     鑲綋鏁扮粍�?/param>
    ///     <returns>鍏ㄩ浂杩斿洖 <c>true</c>锛屽惁鍒欒繑�?<c>false</c>�?/returns>
    private static bool is_all_zero(ulong[] limbs)
    {
        foreach (var limb in limbs)
            if (limb != 0)
                return false;

        return true;
    }

    /// <summary>
    ///     鍗曡偄浣撻櫎娉曪紝褰撻櫎鏁板彲鐢ㄥ崟�?<c>ulong</c> 琛ㄧず鏃朵娇鐢ㄣ�?    ///
    /// </summary>
    /// <param name="a">
    ///     琚櫎鏁般€?/param>
    ///     <param name="b">
    ///         闄ゆ暟銆?/param>
    ///         <returns>鍟嗗拰浣欐暟�?/returns>
    private static (ulong[] quotient, ulong[] remainder) div_rem_by_single(ulong[] a, ulong b)
    {
        var aLen = effective_length(a);
        var quotient = new ulong[aLen];
        var carry = 0UL;

        for (var i = aLen - 1; i >= 0; i--)
        {
            var dividend = (carry << 32) | (a[i] >> 32);
            var qHi = dividend / b;
            carry = dividend % b;

            var dividend2 = (carry << 32) | (a[i] & 0xFFFFFFFFUL);
            var qLo = dividend2 / b;
            carry = dividend2 % b;

            quotient[i] = (qHi << 32) | qLo;
        }

        normalize_array(quotient);
        return (quotient, [carry]);
    }

    /// <summary>
    ///     澶氳偄浣撻暱闄ゆ硶锛圞nuth 绠楁�?D锛夈�?    ///
    /// </summary>
    /// <param name="a">
    ///     琚櫎鏁般€?/param>
    ///     <param name="b">
    ///         闄ゆ暟銆?/param>
    ///         <returns>鍟嗗拰浣欐暟�?/returns>
    private static (ulong[] quotient, ulong[] remainder) div_rem_multi_digit(ulong[] a, ulong[] b)
    {
        var aLen = effective_length(a);
        var bLen = effective_length(b);

        var shift = leading_zero_count(b[bLen - 1]);
        var shiftedA = shift_left_limbs(a, shift);
        var shiftedB = shift_left_limbs(b, shift);

        var aLenS = effective_length(shiftedA);
        var bLenS = effective_length(shiftedB);

        var quotient = new ulong[aLenS - bLenS + 1];
        var divisorHigh = shiftedB[bLenS - 1];

        for (var i = aLenS - bLenS; i >= 0; i--)
        {
            var dividendHi = i + bLenS < aLenS ? shiftedA[i + bLenS] : 0UL;
            var dividendLo = shiftedA[i + bLenS - 1];

            var (qHat, rHat) = estimate_quotient(dividendHi, dividendLo, divisorHigh);

            while (true)
            {
                if (qHat >= _decimal_base || (bLenS >= 2 && i + bLenS - 2 >= 0 && i + bLenS - 2 < shiftedA.Length))
                {
                    var bSecond = bLenS >= 2 ? shiftedB[bLenS - 2] : 0UL;
                    var aSecond = i + bLenS - 2 >= 0 && i + bLenS - 2 < shiftedA.Length
                        ? shiftedA[i + bLenS - 2]
                        : 0UL;

                    if (qHat * bSecond > (rHat << 32) + aSecond)
                    {
                        qHat--;
                        rHat += divisorHigh;
                        if (rHat < _decimal_base) continue;
                    }
                }

                break;
            }

            var borrow = subtract_multiply(ref shiftedA, shiftedB, i, qHat, bLenS);

            if (borrow > 0)
            {
                qHat--;
                add_back(ref shiftedA, shiftedB, i, bLenS);
            }

            quotient[i] = qHat;
        }

        normalize_array(quotient);
        var remainderLimbs = shift_right_limbs(shiftedA, shift);
        normalize_array(remainderLimbs);
        return (quotient, remainderLimbs);
    }

    /// <summary>
    ///     浼扮畻鍟嗕綅�?    ///
    /// </summary>
    /// <param name="dividendHi">
    ///     琚櫎鏁伴珮浣嶃�?/param>
    ///     <param name="dividendLo">
    ///         琚櫎鏁颁綆浣嶃�?/param>
    ///         <param name="divisor">
    ///             闄ゆ暟鏈€楂樹綅�?/param>
    ///             <returns>浼扮畻鐨勫晢鍜屼綑鏁般€?/returns>
    private static (ulong qHat, ulong rHat) estimate_quotient(ulong dividendHi, ulong dividendLo, ulong divisor)
    {
        if (dividendHi == divisor) return (0xFFFFFFFFFFFFFFFFUL, dividendLo);

        var qHat = dividendHi == 0
            ? dividendLo / divisor
            : divide_two_ulong_by_one(dividendHi, dividendLo, divisor);
        var rHat = dividendHi == 0
            ? dividendLo - qHat * divisor
            : dividendHi * _decimal_base + dividendLo - qHat * divisor;
        return (qHat, rHat);
    }

    /// <summary>
    ///     鍙屽瓧闄や互鍗曞瓧銆?    ///
    /// </summary>
    /// <param name="hi">
    ///     �?64 浣嶃�?/param>
    ///     <param name="lo">
    ///         �?64 浣嶃�?/param>
    ///         <param name="divisor">
    ///             闄ゆ暟銆?/param>
    ///             <returns>鍟嗐�?/returns>
    private static ulong divide_two_ulong_by_one(ulong hi, ulong lo, ulong divisor)
    {
        if (hi == 0) return lo / divisor;

        if (hi >= divisor) return ulong.MaxValue;

        var shift = leading_zero_count(divisor);
        var dNorm = divisor << shift;
        var hiNorm = hi << shift;
        var loNorm = lo << shift;
        if (shift > 0) hiNorm |= lo >> (_bits_per_limb - shift);

        var qHat = hiNorm / (uint)(dNorm >> 32);
        var rHat = hiNorm % (uint)(dNorm >> 32);

        while (qHat > 0xFFFFFFFFUL || qHat * (uint)(dNorm & 0xFFFFFFFFUL) > ((rHat << 32) | (loNorm >> 32)))
        {
            qHat--;
            rHat += dNorm >> 32;
            if (rHat > 0xFFFFFFFFUL) break;
        }

        return qHat;
    }

    /// <summary>
    ///     璁＄�?<c>ulong</c> 鍊肩殑鍓嶅闆朵綅鏁般€?    ///
    /// </summary>
    /// <param name="value">
    ///     寰呰绠楃殑鍊笺�?/param>
    ///     <returns>鍓嶅闆朵綅鏁般�?/returns>
    private static int leading_zero_count(ulong value)
    {
        if (value == 0) return _bits_per_limb;

        var count = 0;
        while ((value & (1UL << 63)) == 0)
        {
            count++;
            value <<= 1;
        }

        return count;
    }

    /// <summary>
    ///     宸︾Щ鑲綋鏁扮粍鎸囧畾鐨勪綅鏁般�?    ///
    /// </summary>
    /// <param name="limbs">
    ///     婧愯偄浣撴暟缁勩�?/param>
    ///     <param name="shift">
    ///         宸︾Щ浣嶆暟銆?/param>
    ///         <returns>绉讳綅鍚庣殑鏂版暟缁勩€?/returns>
    private static ulong[] shift_left_limbs(ulong[] limbs, int shift)
    {
        if (shift == 0) return copy_limbs(limbs);

        var len = effective_length(limbs);
        var result = new ulong[len + 1];
        var carry = 0UL;

        for (var i = 0; i < len; i++)
        {
            result[i] = (limbs[i] << shift) | carry;
            carry = limbs[i] >> (_bits_per_limb - shift);
        }

        result[len] = carry;
        normalize_array(result);
        return result;
    }

    /// <summary>
    ///     鍙崇Щ鑲綋鏁扮粍鎸囧畾鐨勪綅鏁般�?    ///
    /// </summary>
    /// <param name="limbs">
    ///     婧愯偄浣撴暟缁勩�?/param>
    ///     <param name="shift">
    ///         鍙崇Щ浣嶆暟銆?/param>
    ///         <returns>绉讳綅鍚庣殑鏂版暟缁勩€?/returns>
    private static ulong[] shift_right_limbs(ulong[] limbs, int shift)
    {
        if (shift == 0) return copy_limbs(limbs);

        var len = effective_length(limbs);
        if (len <= shift / _bits_per_limb) return [0];

        var result = new ulong[len];
        var bitShift = shift % _bits_per_limb;
        var limbShift = shift / _bits_per_limb;

        if (bitShift == 0)
        {
            for (var i = 0; i < len - limbShift; i++) result[i] = limbs[i + limbShift];

            Array.Resize(ref result, len - limbShift);
        }
        else
        {
            for (var i = 0; i < len - limbShift; i++)
            {
                result[i] = limbs[i + limbShift] >> bitShift;
                if (i + limbShift + 1 < len) result[i] |= limbs[i + limbShift + 1] << (_bits_per_limb - bitShift);
            }

            Array.Resize(ref result, SystemMath.Max(1, len - limbShift));
        }

        normalize_array(result);
        return result;
    }

    /// <summary>
    ///     浠庤闄ゆ暟涓噺鍘婚櫎鏁颁笌鍟嗕綅鐨勪箻绉€?    ///
    /// </summary>
    /// <param name="a">
    ///     琚櫎鏁帮紙鍘熷湴淇敼锛夈�?/param>
    ///     <param name="b">
    ///         闄ゆ暟銆?/param>
    ///         <param name="offset">
    ///             鍋忕Щ浣嶇疆銆?/param>
    ///             <param name="q">
    ///                 鍟嗕綅銆?/param>
    ///                 <param name="bLen">
    ///                     闄ゆ暟鏈夋晥闀垮害�?/param>
    ///                     <returns>鍊熶綅銆?/returns>
    private static ulong subtract_multiply(ref ulong[] a, ulong[] b, int offset, ulong q, int bLen)
    {
        var carry = 0UL;
        var borrow = 0UL;

        for (var i = 0; i < bLen; i++)
        {
            var product = b[i] * q;
            var lo = product + carry;
            carry = product > lo ? 1UL : 0UL;
            carry += lo < product ? 1UL : 0UL;

            var aIdx = offset + i;
            if (aIdx < a.Length)
            {
                var diff = a[aIdx] - (lo & 0xFFFFFFFFFFFFFFFFUL) - borrow;
                a[aIdx] = diff;
                borrow = a[aIdx] > diff || (lo & 0xFFFFFFFFFFFFFFFFUL) + borrow > a[aIdx] + diff ? 1UL : 0UL;
            }
        }

        var highIdx = offset + bLen;
        if (highIdx < a.Length)
        {
            var diff = a[highIdx] - carry - borrow;
            a[highIdx] = diff;
            borrow = carry + borrow > a[highIdx] + diff ? 1UL : 0UL;
        }

        return borrow;
    }

    /// <summary>
    ///     鍥炲姞闄ゆ暟锛堝綋鍑忔硶鍊熶綅鏃朵慨姝ｏ級銆?    ///
    /// </summary>
    /// <param name="a">
    ///     琚櫎鏁帮紙鍘熷湴淇敼锛夈�?/param>
    ///     <param name="b">
    ///         闄ゆ暟銆?/param>
    ///         <param name="offset">
    ///             鍋忕Щ浣嶇疆銆?/param>
    ///             <param name="bLen">闄ゆ暟鏈夋晥闀垮害�?/param>
    private static void add_back(ref ulong[] a, ulong[] b, int offset, int bLen)
    {
        var carry = 0UL;

        for (var i = 0; i < bLen; i++)
        {
            var aIdx = offset + i;
            if (aIdx < a.Length)
            {
                var sum = a[aIdx] + b[i] + carry;
                carry = sum < a[aIdx] ? 1UL : 0UL;
                a[aIdx] = sum;
            }
        }

        var highIdx = offset + bLen;
        if (highIdx < a.Length) a[highIdx] += carry;
    }

    /// <summary>
    ///     瑙ｆ瀽姝ｆ暟鍗佽繘鍒舵暟瀛楀簭鍒椼€?    ///
    /// </summary>
    /// <param name="digits">
    ///     鍗佽繘鍒舵暟瀛楀瓧绗﹀簭鍒椼€?/param>
    ///     <returns>瑙ｆ瀽缁撴灉�?/returns>
    private static BigInteger parse_positive_digits(ReadOnlySpan<char> digits)
    {
        var result = _zero;
        var ten = new BigInteger(10UL);
        var superBase = new BigInteger(_decimal_base);

        var i = 0;
        while (i < digits.Length)
        {
            var chunkSize = SystemMath.Min(_decimal_digits_per_limb, digits.Length - i);
            var chunkValue = 0UL;

            for (var j = 0; j < chunkSize; j++) chunkValue = chunkValue * 10 + (ulong)(digits[i + j] - '0');

            if (chunkSize == _decimal_digits_per_limb)
            {
                result = result.mul(superBase).add(new BigInteger(chunkValue));
            }
            else
            {
                var multiplier = _one;
                for (var j = 0; j < chunkSize; j++) multiplier = multiplier.mul(ten);

                result = result.mul(multiplier).add(new BigInteger(chunkValue));
            }

            i += chunkSize;
        }

        return result;
    }

    /// <summary>
    ///     鍦ㄦ寚瀹氳寖鍥村唴鐢熸垚闅忔満澶ф暣鏁般�?    ///
    /// </summary>
    /// <param name="min">
    ///     鏈€灏忓€硷紙鍚級�?/param>
    ///     <param name="max">
    ///         鏈€澶у€硷紙涓嶅惈锛夈€?/param>
    ///         <param name="random">
    ///             闅忔満鏁扮敓鎴愬櫒�?/param>
    ///             <returns>闅忔満澶ф暣鏁般�?/returns>
    private static BigInteger random_big_integer(BigInteger min, BigInteger max, Random random)
    {
        var range = max.sub(min);
        if (range.is_zero) return min;

        var bytes = new byte[range.bit_length() / 8 + 1];
        BigInteger result;

        do
        {
            random.NextBytes(bytes);
            result = from_unsigned_bytes(bytes).rem(range);
        } while (result.Equals(range));

        return result.add(min);
    }

    /// <summary>
    ///     浠庢棤绗﹀彿瀛楄妭鏁扮粍鍒涘�?<see cref="BigInteger" />�?    ///
    /// </summary>
    /// <param name="bytes">
    ///     瀛楄妭鏁扮粍�?/param>
    ///     <returns>瀵瑰簲鐨?<see cref="BigInteger" />�?/returns>
    private static BigInteger from_unsigned_bytes(byte[] bytes)
    {
        var result = _zero;
        var shift = 0;

        for (var i = 0; i < bytes.Length; i += 8)
        {
            var value = 0UL;
            var bits = 0;

            for (var j = 0; j < 8 && i + j < bytes.Length; j++)
            {
                value |= (ulong)bytes[i + j] << (j * 8);
                bits += 8;
            }

            if (value != 0)
            {
                var limb = new BigInteger(value);
                result = result.add(limb.shift_left(shift));
            }

            shift += bits;
        }

        return result;
    }

    /// <summary>
    ///     閫氱敤浣嶈繍绠楋紝澶勭悊璐熸暟锛堜簩杩涘埗琛ョ爜锛夈�?    ///
    /// </summary>
    /// <param name="other">
    ///     鎿嶄綔鏁般€?/param>
    ///     <param name="op">
    ///         浣嶈繍绠楀鎵樸€?/param>
    ///         <returns>杩愮畻缁撴灉�?/returns>
    private BigInteger bitwise_operation(BigInteger other, Func<ulong, ulong, ulong> op)
    {
        var aIsNeg = _sign == -1;
        var bIsNeg = other._sign == -1;
        var a = aIsNeg ? twos_complement() : _limbs;
        var b = bIsNeg ? other.twos_complement() : other._limbs;

        var maxLen = SystemMath.Max(a.Length, b.Length) + 1;
        var aExt = extend_or_invert(a, aIsNeg, maxLen);
        var bExt = extend_or_invert(b, bIsNeg, maxLen);

        var result = new ulong[maxLen];
        for (var i = 0; i < maxLen; i++) result[i] = op(aExt[i], bExt[i]);

        var resultIsNeg = op(aIsNeg ? 1UL : 0UL, bIsNeg ? 1UL : 0UL) != 0;

        if (resultIsNeg)
        {
            result = twos_complement_of(result);
            normalize_array(result);
            return new BigInteger(-1, result);
        }

        normalize_array(result);
        var r = new BigInteger(1, result);
        return r.is_zero ? _zero : r;
    }

    /// <summary>
    ///     璁＄畻褰撳墠鍊肩粷瀵瑰€肩殑浜岃繘鍒惰ˉ鐮佽〃绀恒€?    ///
    /// </summary>
    /// <returns>浜岃繘鍒惰ˉ鐮佽偄浣撴暟缁勩€?/returns>
    private ulong[] twos_complement()
    {
        var result = new ulong[_limbs.Length + 1];
        var carry = 1UL;

        for (var i = 0; i < _limbs.Length; i++)
        {
            var inverted = ~_limbs[i];
            var sum = inverted + carry;
            result[i] = sum;
            carry = sum < inverted ? 1UL : 0UL;
        }

        result[_limbs.Length] = carry;
        return result;
    }

    /// <summary>
    ///     瀵圭粰瀹氳偄浣撴暟缁勮绠椾簩杩涘埗琛ョ爜�?    ///
    /// </summary>
    /// <param name="limbs">
    ///     婧愯偄浣撴暟缁勩�?/param>
    ///     <returns>浜岃繘鍒惰ˉ鐮佺粨鏋溿�?/returns>
    private static ulong[] twos_complement_of(ulong[] limbs)
    {
        var result = new ulong[limbs.Length];
        var carry = 1UL;

        for (var i = 0; i < limbs.Length; i++)
        {
            var inverted = ~limbs[i];
            var sum = inverted + carry;
            result[i] = sum;
            carry = sum < inverted ? 1UL : 0UL;
        }

        return result;
    }

    /// <summary>
    ///     鎵╁睍鑲綋鏁扮粍鍒版寚瀹氶暱搴︼紝璐熸暟鐢ㄧ鍙锋墿灞曪紙�?1 濉厖锛夈€?    ///
    /// </summary>
    /// <param name="limbs">
    ///     婧愯偄浣撴暟缁勩�?/param>
    ///     <param name="isNegative">
    ///         鏄惁涓鸿礋鏁般�?/param>
    ///         <param name="length">
    ///             鐩爣闀垮害�?/param>
    ///             <returns>鎵╁睍鍚庣殑鏁扮粍銆?/returns>
    private static ulong[] extend_or_invert(ulong[] limbs, bool isNegative, int length)
    {
        var result = new ulong[length];
        var fill = isNegative ? ulong.MaxValue : 0UL;
        for (var i = 0; i < length; i++) result[i] = i < limbs.Length ? limbs[i] : fill;

        return result;
    }

    #endregion
}