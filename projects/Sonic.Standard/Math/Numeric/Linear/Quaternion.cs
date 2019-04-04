using Core.Math;
using Std.Math.Foundation;

namespace Std.Math.Numeric.Linear;

/// <summary>
///     四元数，表示三维空间中的旋转，由标量部 w 和向量部 (x, y, z) 组成
/// </summary>
/// <typeparam name="T">分量数值类型，必须为值类型且支持相等比较和类型转换</typeparam>
public readonly struct Quaternion<T> :
    IMultiplicative<Quaternion<T>>,
    INegation<Quaternion<T>, Quaternion<T>>,
    IApproximate<Quaternion<T>>,
    IEquatable<Quaternion<T>>,
    IClone<Quaternion<T>>,
    IOne<Quaternion<T>>,
    IQuaternion<T>
    where T : struct, IEquatable<T>, IConvertible
{
    /// <summary>鏍囬噺閮ㄥ垎锛堝疄閮級</summary>
    public readonly T w;

    /// <summary>i 鍒嗛噺锛堣櫄閮�?/summary>
    public readonly T x;

    /// <summary>j 鍒嗛噺锛堣櫄閮�?/summary>
    public readonly T y;

    /// <summary>k 鍒嗛噺锛堣櫄閮�?/summary>
    public readonly T z;

    #region IQuaternion

    /// <summary>
    ///     获取四元数的 W 分量（实部）
    /// </summary>
    T IQuaternion<T>.w => w;

    /// <summary>
    ///     获取四元数的 X 分量
    /// </summary>
    T IQuaternion<T>.x => x;

    /// <summary>
    ///     获取四元数的 Y 分量
    /// </summary>
    T IQuaternion<T>.y => y;

    /// <summary>
    ///     获取四元数的 Z 分量
    /// </summary>
    T IQuaternion<T>.z => z;

    #endregion

    /// <summary>
    ///     鏋勯€犲洓鍏冩暟
    /// </summary>
    /// <param name="w">鏍囬噺閮ㄥ垎</param>
    /// <param name="x">
    ///     i 鍒嗛�?/param>
    ///     <param name="y">
    ///         j 鍒嗛�?/param>
    ///         <param name="z">k 鍒嗛�?/param>
    public Quaternion(T w, T x, T y, T z)
    {
        this.w = w;
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

    #region 宸ュ巶鏂规硶

    /// <summary>
    ///     鑾峰彇鍗曚綅鍥涘厓鏁?(1, 0, 0, 0)锛岃〃绀烘棤鏃嬭�?    ///
    /// </summary>
    /// <returns>鍗曚綅鍥涘厓�?/returns>
    public static Quaternion<T> identity()
    {
        return new Quaternion<T>(from_d(1), from_d(0), from_d(0), from_d(0));
    }

    /// <summary>
    ///     浠庢棆杞酱鍜屾棆杞搴︽瀯閫犲洓鍏冩暟锛岃酱蹇呴』涓哄崟浣嶅悜閲?    ///
    /// </summary>
    /// <param name="axis">
    ///     鏃嬭浆杞达紝蹇呴』涓哄崟浣嶅悜閲?/param>
    ///     <param name="angle">鏃嬭浆瑙掑害锛堝姬搴︼級</param>
    ///     <returns>琛ㄧず璇ユ棆杞殑鍥涘厓�?/returns>
    public static Quaternion<T> from_axis_angle(Vec3<T> axis, double angle)
    {
        var half = angle * 0.5;
        var s = SonicMath.sin(half);
        var c = SonicMath.cos(half);
        return new Quaternion<T>(
            from_d(c),
            from_d(to_d(axis.x) * s),
            from_d(to_d(axis.y) * s),
            from_d(to_d(axis.z) * s)
        );
    }

    /// <summary>
    ///     浠庢鎷夎鏋勯€犲洓鍏冩暟锛岄噰鐢?ZYX 鏃嬭浆椤哄簭锛堝亸鑸?淇�?缈绘粴锛?    ///
    /// </summary>
    /// <param name="pitch">
    ///     淇话瑙掞紙寮у害锛夛紝缁?X 杞存棆杞?/param>
    ///     <param name="yaw">
    ///         鍋忚埅瑙掞紙寮у害锛夛紝缁?Y 杞存棆杞?/param>
    ///         <param name="roll">
    ///             缈绘粴瑙掞紙寮у害锛夛紝缁?Z 杞存棆杞?/param>
    ///             <returns>琛ㄧず璇ユ棆杞殑鍥涘厓�?/returns>
    public static Quaternion<T> from_euler_angles(double pitch, double yaw, double roll)
    {
        var hp = pitch * 0.5;
        var hy = yaw * 0.5;
        var hr = roll * 0.5;

        var sp = SonicMath.sin(hp);
        var cp = SonicMath.cos(hp);
        var sy = SonicMath.sin(hy);
        var cy = SonicMath.cos(hy);
        var sr = SonicMath.sin(hr);
        var cr = SonicMath.cos(hr);

        return new Quaternion<T>(
            from_d(cr * cp * cy + sr * sp * sy),
            from_d(cr * sp * cy + sr * cp * sy),
            from_d(cr * cp * sy - sr * sp * cy),
            from_d(sr * cp * cy - cr * sp * sy)
        );
    }

    #endregion

    #region 灞炴€т笌鍩烘湰杩愮畻

    /// <summary>
    ///     璁＄畻鍥涘厓鏁扮殑闀垮害锛堟ā�?    ///
    /// </summary>
    /// <returns>鍥涘厓鏁伴暱�?/returns>
    public double length()
    {
        return SonicMath.sqrt(length_squared());
    }

    /// <summary>
    ///     璁＄畻鍥涘厓鏁伴暱搴︾殑骞虫柟锛岄伩鍏嶅紑鏂硅繍�?    ///
    /// </summary>
    /// <returns>闀垮害骞虫柟鍊?/returns>
    public double length_squared()
    {
        var dw = to_d(w);
        var dx = to_d(x);
        var dy = to_d(y);
        var dz = to_d(z);
        return dw * dw + dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍥涘厓鏁扮殑鍗曚綅鍥涘厓鏁帮紙褰掍竴鍖栵�?    ///
    /// </summary>
    /// <returns>鍗曚綅鍥涘厓�?/returns>
    public Quaternion<T> normalize()
    {
        var len = length();
        if (len == 0.0) return identity();

        var inv = 1.0 / len;
        return new Quaternion<T>(
            from_d(to_d(w) * inv),
            from_d(to_d(x) * inv),
            from_d(to_d(y) * inv),
            from_d(to_d(z) * inv)
        );
    }

    /// <summary>
    ///     璁＄畻鍥涘厓鏁扮殑鍏辫江锛屽�?(w, -x, -y, -z)
    /// </summary>
    /// <returns>鍏辫江鍥涘厓�?/returns>
    public Quaternion<T> conjugate()
    {
        return new Quaternion<T>(
            w,
            from_d(-to_d(x)),
            from_d(-to_d(y)),
            from_d(-to_d(z))
        );
    }

    /// <summary>
    ///     璁＄畻鍥涘厓鏁扮殑閫嗭紝鍗冲叡杞櫎浠ラ暱搴﹀钩鏂?    ///
    /// </summary>
    /// <returns>閫嗗洓鍏冩暟</returns>
    public Quaternion<T> inverse()
    {
        var len2 = length_squared();
        if (len2 == 0.0) return identity();

        var inv = 1.0 / len2;
        return new Quaternion<T>(
            from_d(to_d(w) * inv),
            from_d(-to_d(x) * inv),
            from_d(-to_d(y) * inv),
            from_d(-to_d(z) * inv)
        );
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓洓鍏冩暟鐨勭偣绉?    ///
    /// </summary>
    /// <param name="other">
    ///     鍙︿竴涓洓鍏冩�?/param>
    ///     <returns>鐐圭Н�?/returns>
    public double dot(Quaternion<T> other)
    {
        return to_d(w) * to_d(other.w) + to_d(x) * to_d(other.x)
                                       + to_d(y) * to_d(other.y) + to_d(z) * to_d(other.z);
    }

    #endregion

    #region IMultiplicative

    /// <summary>
    ///     鍥涘厓鏁颁箻娉曪紙Hamilton 涔樼Н锛夛紝缁勫悎涓や釜鏃嬭浆
    /// </summary>
    /// <param name="right">
    ///     鍙充晶鎿嶄綔�?/param>
    ///     <returns>涔樼Н缁撴灉鍥涘厓�?/returns>
    public Quaternion<T> mul(Quaternion<T> right)
    {
        var aw = to_d(w);
        var ax = to_d(x);
        var ay = to_d(y);
        var az = to_d(z);
        var bw = to_d(right.w);
        var bx = to_d(right.x);
        var by = to_d(right.y);
        var bz = to_d(right.z);

        return new Quaternion<T>(
            from_d(aw * bw - ax * bx - ay * by - az * bz),
            from_d(aw * bx + ax * bw + ay * bz - az * by),
            from_d(aw * by - ax * bz + ay * bw + az * bx),
            from_d(aw * bz + ax * by - ay * bx + az * bw)
        );
    }

    /// <summary>
    ///     鍥涘厓鏁伴櫎娉曪紝绛変环浜庝箻浠ュ彸渚у洓鍏冩暟鐨勯€?    ///
    /// </summary>
    /// <param name="right">
    ///     鍙充晶鎿嶄綔�?/param>
    ///     <returns>闄ゆ硶缁撴灉鍥涘厓鏁?/returns>
    public Quaternion<T> div(Quaternion<T> right)
    {
        return mul(right.inverse());
    }

    /// <summary>
    ///     四元数乘法运算符（Hamilton 乘积），组合两个旋转
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>乘积结果四元数</returns>
    public static Quaternion<T> operator *(Quaternion<T> left, Quaternion<T> right)
    {
        return left.mul(right);
    }

    #endregion

    #region INegation

    /// <summary>
    ///     鍥涘厓鏁板彇璐燂紝姣忎釜鍒嗛噺鍙栧弽
    /// </summary>
    /// <returns>鍙栬礋缁撴灉</returns>
    public Quaternion<T> neg()
    {
        return new Quaternion<T>(
            from_d(-to_d(w)),
            from_d(-to_d(x)),
            from_d(-to_d(y)),
            from_d(-to_d(z))
        );
    }

    #endregion

    #region 鎻掑�?

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓洓鍏冩暟鐨勭嚎鎬ф彃鍊?    ///
    /// </summary>
    /// <param name="other">
    ///     鐩爣鍥涘厓�?/param>
    ///     <param name="t">鎻掑€煎洜瀛愶�? 杩斿洖鑷韩�? 杩斿洖鐩爣</param>
    ///     <returns>鎻掑€肩粨鏋?/returns>
    public Quaternion<T> lerp(Quaternion<T> other, double t)
    {
        return new Quaternion<T>(
            from_d(to_d(w) + (to_d(other.w) - to_d(w)) * t),
            from_d(to_d(x) + (to_d(other.x) - to_d(x)) * t),
            from_d(to_d(y) + (to_d(other.y) - to_d(y)) * t),
            from_d(to_d(z) + (to_d(other.z) - to_d(z)) * t)
        );
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓洓鍏冩暟鐨勭悆闈㈢嚎鎬ф彃鍊硷紝娌挎渶鐭姬璺緞鏃嬭�?    ///
    /// </summary>
    /// <param name="other">
    ///     鐩爣鍥涘厓�?/param>
    ///     <param name="t">鎻掑€煎洜瀛愶�? 杩斿洖鑷韩�? 杩斿洖鐩爣</param>
    ///     <returns>鎻掑€肩粨鏋?/returns>
    public Quaternion<T> slerp(Quaternion<T> other, double t)
    {
        var dotVal = dot(other);

        var ow = to_d(other.w);
        var ox = to_d(other.x);
        var oy = to_d(other.y);
        var oz = to_d(other.z);

        if (dotVal < 0.0)
        {
            dotVal = -dotVal;
            ow = -ow;
            ox = -ox;
            oy = -oy;
            oz = -oz;
        }

        if (dotVal > 0.9995)
            return new Quaternion<T>(
                from_d(to_d(w) + (ow - to_d(w)) * t),
                from_d(to_d(x) + (ox - to_d(x)) * t),
                from_d(to_d(y) + (oy - to_d(y)) * t),
                from_d(to_d(z) + (oz - to_d(z)) * t)
            ).normalize();

        var theta = SonicMath.arc_cosine(dotVal);
        var sinTheta = SonicMath.sin(theta);
        var invSin = 1.0 / sinTheta;
        var coeff0 = SonicMath.sin((1.0 - t) * theta) * invSin;
        var coeff1 = SonicMath.sin(t * theta) * invSin;

        return new Quaternion<T>(
            from_d(coeff0 * to_d(w) + coeff1 * ow),
            from_d(coeff0 * to_d(x) + coeff1 * ox),
            from_d(coeff0 * to_d(y) + coeff1 * oy),
            from_d(coeff0 * to_d(z) + coeff1 * oz)
        );
    }

    #endregion

    #region 鏃嬭浆鎿嶄綔

    /// <summary>
    ///     浣跨敤褰撳墠鍥涘厓鏁版棆杞悜閲忥紝璁＄�?q * v * q^(-1)
    /// </summary>
    /// <param name="v">
    ///     寰呮棆杞悜閲?/param>
    ///     <returns>鏃嬭浆鍚庣殑鍚戦�?/returns>
    public Vec3<T> rotate_vector(Vec3<T> v)
    {
        var qv = new Quaternion<T>(from_d(0), v.x, v.y, v.z);
        var result = mul(qv).mul(inverse());
        return new Vec3<T>(result.x, result.y, result.z);
    }

    #endregion

    #region IApproximate

    /// <summary>
    ///     鍒ゆ柇涓や釜鍥涘厓鏁版槸鍚﹀湪缁欏畾璇樊鑼冨洿鍐呰繎浼肩浉绛夛紝閫愬垎閲忔瘮杈?    ///
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <param name="absoluteTolerance">
    ///     缁濆璇樊瀹归�?/param>
    ///     <param name="relativeTolerance">
    ///         鐩稿璇樊瀹归�?/param>
    ///         <returns>鏄惁杩戜技鐩哥�?/returns>
    public bool is_close(Quaternion<T> other, double absoluteTolerance, double relativeTolerance)
    {
        return is_component_close(to_d(w), to_d(other.w), absoluteTolerance, relativeTolerance)
               && is_component_close(to_d(x), to_d(other.x), absoluteTolerance, relativeTolerance)
               && is_component_close(to_d(y), to_d(other.y), absoluteTolerance, relativeTolerance)
               && is_component_close(to_d(z), to_d(other.z), absoluteTolerance, relativeTolerance);
    }

    /// <summary>
    ///     判断两个四元数是否在给定逐分量容差范围内近似相等
    /// </summary>
    /// <param name="other">比较目标</param>
    /// <param name="epsilon">逐分量容差</param>
    /// <returns>是否近似相等</returns>
    public bool approx_equals(Quaternion<T> other, Quaternion<T> epsilon)
    {
        return SonicMath.abs(to_d(w) - to_d(other.w)) <= to_d(epsilon.w)
               && SonicMath.abs(to_d(x) - to_d(other.x)) <= to_d(epsilon.x)
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
    ///     鍒ゆ柇涓や釜鍥涘厓鏁版槸鍚︾浉绛夛紝姣忎釜鍒嗛噺鍧囩浉绛夋椂杩斿�?true
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <returns>鏄惁鐩哥瓑</returns>
    public bool Equals(Quaternion<T> other)
    {
        return w.Equals(other.w) && x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
    }

    #endregion

    #region IClone

    /// <summary>
    ///     杩斿洖褰撳墠鍥涘厓鏁扮殑娣辨嫹璐濓紝鐢变簬鏄?readonly struct锛岀洿鎺ヨ繑鍥炶嚜韬嵆�?    ///
    /// </summary>
    /// <returns>鍏嬮殕缁撴灉</returns>
    public Quaternion<T> clone()
    {
        return new Quaternion<T>(w, x, y, z);
    }

    #endregion

    #region IOne

    /// <summary>
    ///     获取单位四元数 (1, 0, 0, 0)，表示无旋转
    /// </summary>
    public static Quaternion<T> one => identity();

    #endregion
}