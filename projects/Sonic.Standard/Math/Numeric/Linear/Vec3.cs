using Core.Math;
using Core.Math.Linear;
using Std.Math.Foundation;

namespace Std.Math.Numeric.Linear;

/// <summary>
///     三维向量，表示具有 x、y 和 z 分量的三维空间向量
/// </summary>
/// <typeparam name="T">分量数值类型，必须为值类型且支持相等比较和类型转换</typeparam>
public readonly struct Vec3<T> :
    IAdditive<Vec3<T>>,
    INegation<Vec3<T>, Vec3<T>>,
    IApproximate<Vec3<T>>,
    IEquatable<Vec3<T>>,
    IClone<Vec3<T>>,
    IZero<Vec3<T>>,
    IOne<Vec3<T>>,
    IVector<T, int>
    where T : struct, IEquatable<T>, IConvertible
{
    /// <summary>x 鍒嗛�?/summary>
    public readonly T x;

    /// <summary>y 鍒嗛�?/summary>
    public readonly T y;

    /// <summary>z 鍒嗛�?/summary>
    public readonly T z;

    /// <summary>绾㈣壊閫氶亾鍒悕锛岀瓑浠蜂簬 x 鍒嗛�?/summary>
    public T r => x;

    /// <summary>缁胯壊閫氶亾鍒悕锛岀瓑浠蜂簬 y 鍒嗛�?/summary>
    public T g => y;

    /// <summary>钃濊壊閫氶亾鍒悕锛岀瓑浠蜂簬 z 鍒嗛�?/summary>
    public T b => z;

    #region IVector

    /// <summary>
    ///     获取或设置指定索引处的分量
    /// </summary>
    /// <param name="index">分量索引，0 为 x，1 为 y，2 为 z</param>
    /// <returns>指定索引处的分量值</returns>
    public T this[int index]
    {
        get => index switch
        {
            0 => x,
            1 => y,
            2 => z,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set => throw new NotSupportedException("Vec3 是不可变结构体，不支持索引器设置");
    }

    /// <summary>
    ///     获取向量的维度
    /// </summary>
    public int dimension => 3;

    #endregion

    /// <summary>
    ///     鏋勯€犱笁缁村悜�?    ///
    /// </summary>
    /// <param name="x">
    ///     x 鍒嗛�?/param>
    ///     <param name="y">
    ///         y 鍒嗛�?/param>
    ///         <param name="z">z 鍒嗛�?/param>
    public Vec3(T x, T y, T z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    #region 绫诲瀷杞崲杈呭�?

    /// <summary>
    ///     灏嗘硾鍨嬪€艰浆鎹负鍙岀簿搴︽诞鐐规�?    ///
    /// </summary>
    private static double to_d(T v)
    {
        return v.ToDouble(null);
    }

    /// <summary>
    ///     灏嗗弻绮惧害娴偣鏁拌浆鎹负娉涘瀷鍊?    ///
    /// </summary>
    private static T from_d(double v)
    {
        return (T)Convert.ChangeType(v, typeof(T));
    }

    #endregion

    #region 鍚戦噺杩愮畻

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓笁缁村悜閲忕殑鐐圭�?    ///
    /// </summary>
    /// <param name="other">
    ///     鍙︿竴涓笁缁村悜閲?/param>
    ///     <returns>鐐圭Н�?/returns>
    public double dot(Vec3<T> other)
    {
        return to_d(x) * to_d(other.x) + to_d(y) * to_d(other.y) + to_d(z) * to_d(other.z);
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓笁缁村悜閲忕殑鍙夌�?    ///
    /// </summary>
    /// <param name="other">
    ///     鍙︿竴涓笁缁村悜閲?/param>
    ///     <returns>鍙夌Н缁撴灉鍚戦噺</returns>
    public Vec3<T> cross(Vec3<T> other)
    {
        return new Vec3<T>(
            from_d(to_d(y) * to_d(other.z) - to_d(z) * to_d(other.y)),
            from_d(to_d(z) * to_d(other.x) - to_d(x) * to_d(other.z)),
            from_d(to_d(x) * to_d(other.y) - to_d(y) * to_d(other.x))
        );
    }

    /// <summary>
    ///     璁＄畻鍚戦噺鐨勯暱搴︼紙妯★級锛屽嵆鍒板師鐐圭殑璺濈�?    ///
    /// </summary>
    /// <returns>鍚戦噺闀垮害</returns>
    public double length()
    {
        return SonicMath.sqrt(length_squared());
    }

    /// <summary>
    ///     璁＄畻鍚戦噺闀垮害鐨勫钩鏂癸紝閬垮厤寮€鏂硅繍�?    ///
    /// </summary>
    /// <returns>闀垮害骞虫柟鍊?/returns>
    public double length_squared()
    {
        var dx = to_d(x);
        var dy = to_d(y);
        var dz = to_d(z);
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍚戦噺鐨勫崟浣嶅悜閲忥紙褰掍竴鍖栵級
    /// </summary>
    /// <returns>鍗曚綅鍚戦噺</returns>
    public Vec3<T> normalize()
    {
        var len = length();
        if (len == 0.0) return new Vec3<T>(from_d(0), from_d(0), from_d(0));

        var inv = 1.0 / len;
        return new Vec3<T>(
            from_d(to_d(x) * inv),
            from_d(to_d(y) * inv),
            from_d(to_d(z) * inv)
        );
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓笁缁村悜閲忕殑绾挎€ф彃鍊?    ///
    /// </summary>
    /// <param name="other">鐩爣鍚戦噺</param>
    /// <param name="t">鎻掑€煎洜瀛愶�? 杩斿洖鑷韩�? 杩斿洖鐩爣</param>
    /// <returns>鎻掑€肩粨鏋?/returns>
    public Vec3<T> lerp(Vec3<T> other, double t)
    {
        return new Vec3<T>(
            from_d(to_d(x) + (to_d(other.x) - to_d(x)) * t),
            from_d(to_d(y) + (to_d(other.y) - to_d(y)) * t),
            from_d(to_d(z) + (to_d(other.z) - to_d(z)) * t)
        );
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓笁缁村悜閲忎箣闂寸殑娆у嚑閲屽緱璺濈
    /// </summary>
    /// <param name="other">鐩爣鍚戦噺</param>
    /// <returns>涓ょ偣闂磋窛�?/returns>
    public double distance_to(Vec3<T> other)
    {
        var dx = to_d(x) - to_d(other.x);
        var dy = to_d(y) - to_d(other.y);
        var dz = to_d(z) - to_d(other.z);
        return SonicMath.sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓笁缁村悜閲忎箣闂寸殑澶硅锛堝姬搴︼�?    ///
    /// </summary>
    /// <param name="other">
    ///     鍙︿竴涓笁缁村悜閲?/param>
    ///     <returns>澶硅寮у害鍊?/returns>
    public double angle_between(Vec3<T> other)
    {
        var denom = length() * other.length();
        if (denom == 0.0) return 0.0;

        var cosVal = dot(other) / denom;
        return SonicMath.arc_cosine(SonicMath.clamp(cosVal, -1.0, 1.0));
    }

    /// <summary>
    ///     璁＄畻褰撳墠鍚戦噺鍏充簬鎸囧畾娉曠嚎鐨勫弽灏勫悜�?    ///
    /// </summary>
    /// <param name="normal">
    ///     鍙嶅皠闈㈡硶绾匡紝蹇呴』涓哄崟浣嶅悜閲?/param>
    ///     <returns>鍙嶅皠缁撴灉鍚戦�?/returns>
    public Vec3<T> reflect(Vec3<T> normal)
    {
        var d = 2.0 * dot(normal);
        return new Vec3<T>(
            from_d(to_d(x) - d * to_d(normal.x)),
            from_d(to_d(y) - d * to_d(normal.y)),
            from_d(to_d(z) - d * to_d(normal.z))
        );
    }

    /// <summary>
    ///     璁＄畻褰撳墠鍚戦噺鍏充簬鎸囧畾娉曠嚎鐨勬姌灏勫悜�?    ///
    /// </summary>
    /// <param name="normal">
    ///     鎶樺皠闈㈡硶绾匡紝蹇呴』涓哄崟浣嶅悜閲?/param>
    ///     <param name="eta">鎶樺皠鐜囨瘮锛堝叆灏勪粙璐ㄦ姌灏勭巼 / 鎶樺皠浠嬭川鎶樺皠鐜囷級</param>
    ///     <returns>鎶樺皠缁撴灉鍚戦噺锛涜嫢鍙戠敓鍏ㄥ弽灏勫垯杩斿洖闆跺悜閲?/returns>
    public Vec3<T> refract(Vec3<T> normal, double eta)
    {
        var cosI = -dot(normal);
        var sin2T = eta * eta * (1.0 - cosI * cosI);
        if (sin2T > 1.0) return new Vec3<T>(from_d(0), from_d(0), from_d(0));

        var cosT = SonicMath.sqrt(1.0 - sin2T);
        return new Vec3<T>(
            from_d(eta * to_d(x) + (eta * cosI - cosT) * to_d(normal.x)),
            from_d(eta * to_d(y) + (eta * cosI - cosT) * to_d(normal.y)),
            from_d(eta * to_d(z) + (eta * cosI - cosT) * to_d(normal.z))
        );
    }

    #endregion

    #region 鏍囬噺杩愮畻

    /// <summary>
    ///     灏嗗悜閲忕殑姣忎釜鍒嗛噺涔樹互鏍囬噺
    /// </summary>
    /// <param name="v">杈撳叆鍚戦噺</param>
    /// <param name="scalar">鏍囬噺涔樻暟</param>
    /// <returns>缂╂斁鍚庣殑鍚戦�?/returns>
    public static Vec3<T> scale(Vec3<T> v, T scalar)
    {
        var s = to_d(scalar);
        return new Vec3<T>(from_d(to_d(v.x) * s), from_d(to_d(v.y) * s), from_d(to_d(v.z) * s));
    }

    /// <summary>
    ///     灏嗗悜閲忕殑姣忎釜鍒嗛噺闄や互鏍囬噺
    /// </summary>
    /// <param name="v">杈撳叆鍚戦噺</param>
    /// <param name="scalar">鏍囬噺闄ゆ暟</param>
    /// <returns>缂╂斁鍚庣殑鍚戦�?/returns>
    public static Vec3<T> scale_inverse(Vec3<T> v, T scalar)
    {
        var s = to_d(scalar);
        return new Vec3<T>(from_d(to_d(v.x) / s), from_d(to_d(v.y) / s), from_d(to_d(v.z) / s));
    }

    #endregion

    #region IAdditive

    /// <summary>
    ///     向量加法，逐分量相加
    /// </summary>
    /// <param name="right">右侧操作数</param>
    /// <returns>相加结果</returns>
    public Vec3<T> add(Vec3<T> right)
    {
        return new Vec3<T>(
            from_d(to_d(x) + to_d(right.x)),
            from_d(to_d(y) + to_d(right.y)),
            from_d(to_d(z) + to_d(right.z))
        );
    }

    /// <summary>
    ///     向量减法，逐分量相减
    /// </summary>
    /// <param name="right">右侧操作数</param>
    /// <returns>相减结果</returns>
    public Vec3<T> sub(Vec3<T> right)
    {
        return new Vec3<T>(
            from_d(to_d(x) - to_d(right.x)),
            from_d(to_d(y) - to_d(right.y)),
            from_d(to_d(z) - to_d(right.z))
        );
    }

    /// <summary>
    ///     向量加法运算符，逐分量相加
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>相加结果</returns>
    public static Vec3<T> operator +(Vec3<T> left, Vec3<T> right)
    {
        return left.add(right);
    }

    #endregion

    #region INegation

    /// <summary>
    ///     鍚戦噺鍙栬礋锛屾瘡涓垎閲忓彇鍙?    ///
    /// </summary>
    /// <returns>鍙栬礋缁撴灉</returns>
    public Vec3<T> neg()
    {
        return new Vec3<T>(from_d(-to_d(x)), from_d(-to_d(y)), from_d(-to_d(z)));
    }

    #endregion

    #region IApproximate

    /// <summary>
    ///     鍒ゆ柇涓や釜涓夌淮鍚戦噺鏄惁鍦ㄧ粰瀹氳宸寖鍥村唴杩戜技鐩哥瓑锛岄€愬垎閲忔瘮杈?    ///
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <param name="absolute_tolerance">
    ///     缁濆璇樊瀹归�?/param>
    ///     <param name="relative_tolerance">
    ///         鐩稿璇樊瀹归�?/param>
    ///         <returns>鏄惁杩戜技鐩哥�?/returns>
    public bool is_close(Vec3<T> other, double absoluteTolerance, double relativeTolerance)
    {
        return is_component_close(to_d(x), to_d(other.x), absoluteTolerance, relativeTolerance)
               && is_component_close(to_d(y), to_d(other.y), absoluteTolerance, relativeTolerance)
               && is_component_close(to_d(z), to_d(other.z), absoluteTolerance, relativeTolerance);
    }

    /// <summary>
    ///     判断两个三维向量是否在给定逐分量容差范围内近似相等
    /// </summary>
    /// <param name="other">比较目标</param>
    /// <param name="epsilon">逐分量容差</param>
    /// <returns>是否近似相等</returns>
    public bool approx_equals(Vec3<T> other, Vec3<T> epsilon)
    {
        return SonicMath.abs(to_d(x) - to_d(other.x)) <= to_d(epsilon.x)
               && SonicMath.abs(to_d(y) - to_d(other.y)) <= to_d(epsilon.y)
               && SonicMath.abs(to_d(z) - to_d(other.z)) <= to_d(epsilon.z);
    }

    /// <summary>
    ///     鍒ゆ柇鍗曚釜鍒嗛噺鏄惁鍦ㄨ宸寖鍥村唴杩戜技鐩哥�?    ///
    /// </summary>
    private static bool is_component_close(double a, double b, double absoluteTolerance, double relativeTolerance)
    {
        var diff = SonicMath.abs(a - b);
        if (diff <= absoluteTolerance) return true;

        var maxAbs = SonicMath.max(SonicMath.abs(a), SonicMath.abs(b));
        return diff <= relativeTolerance * maxAbs;
    }

    #endregion

    #region IEquatable

    /// <summary>
    ///     鍒ゆ柇涓や釜涓夌淮鍚戦噺鏄惁鐩哥瓑锛屾瘡涓垎閲忓潎鐩哥瓑鏃惰繑鍥?true
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <returns>鏄惁鐩哥瓑</returns>
    public bool Equals(Vec3<T> other)
    {
        return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
    }

    #endregion

    #region IClone

    /// <summary>
    ///     杩斿洖褰撳墠涓夌淮鍚戦噺鐨勬繁鎷疯礉锛岀敱浜庢槸 readonly struct锛岀洿鎺ヨ繑鍥炶嚜韬嵆�?    ///
    /// </summary>
    /// <returns>鍏嬮殕缁撴灉</returns>
    public Vec3<T> clone()
    {
        return new Vec3<T>(x, y, z);
    }

    #endregion

    #region IZero

    /// <summary>
    ///     获取零向量 (0, 0, 0)
    /// </summary>
    public static Vec3<T> zero => new(from_d(0), from_d(0), from_d(0));

    #endregion

    #region IOne

    /// <summary>
    ///     获取单位向量 (1, 1, 1)
    /// </summary>
    public static Vec3<T> one => new(from_d(1), from_d(1), from_d(1));

    #endregion
}