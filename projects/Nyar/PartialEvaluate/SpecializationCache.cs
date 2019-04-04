using Nyar.IR.Intent;

namespace Nyar.PartialEvaluate;

/// <summary>
///     特化缓存，存储已特化的函数版本
/// </summary>
public sealed class SpecializationCache
{
    private readonly Dictionary<SpecializationKey, AlgebraNode> _cache;

    /// <summary>
    ///     创建特化缓存
    /// </summary>
    public SpecializationCache()
    {
        _cache = new Dictionary<SpecializationKey, AlgebraNode>();
    }

    /// <summary>
    ///     获取缓存统计
    /// </summary>
    public int count => _cache.Count;

    /// <summary>
    ///     尝试获取已缓存的特化版本
    /// </summary>
    /// <param name="functionName">函数名。</param>
    /// <param name="knownArgs">已知参数。</param>
    /// <param name="specialized">特化结果。</param>
    /// <returns>是否找到缓存。</returns>
    public bool try_get(string functionName, IReadOnlyDictionary<string, ConstantValue> knownArgs,
        out AlgebraNode? specialized)
    {
        var key = new SpecializationKey(functionName, knownArgs);
        return _cache.TryGetValue(key, out specialized);
    }

    /// <summary>
    ///     缓存特化版本
    /// </summary>
    /// <param name="functionName">函数名。</param>
    /// <param name="knownArgs">已知参数。</param>
    /// <param name="specialized">特化结果。</param>
    public void store(string functionName, IReadOnlyDictionary<string, ConstantValue> knownArgs, AlgebraNode specialized)
    {
        var key = new SpecializationKey(functionName, knownArgs);
        _cache[key] = specialized;
    }

    /// <summary>
    ///     清空缓存
    /// </summary>
    public void clear()
    {
        _cache.Clear();
    }

    private sealed record SpecializationKey(string function_name, IReadOnlyDictionary<string, ConstantValue> args)
    {
        public bool Equals(SpecializationKey? other)
        {
            if (other is null) return false;

            if (function_name != other.function_name) return false;

            if (args.Count != other.args.Count) return false;

            foreach (var (key, value) in args)
            {
                if (!other.args.TryGetValue(key, out var otherValue)) return false;

                if (!value_equals(value, otherValue)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = function_name.GetHashCode();
            foreach (var (key, value) in args.OrderBy(x => x.Key))
                hash = HashCode.Combine(hash, key, value_hash_code(value));

            return hash;
        }

        private static bool value_equals(ConstantValue a, ConstantValue b)
        {
            return (a, b) switch
            {
                (ConstantValue.Int64 i1, ConstantValue.Int64 i2) => i1.value == i2.value,
                (ConstantValue.Float64 f1, ConstantValue.Float64 f2) =>
                    BitConverter.DoubleToUInt64Bits(f1.value) == BitConverter.DoubleToUInt64Bits(f2.value),
                (ConstantValue.Bool b1, ConstantValue.Bool b2) => b1.value == b2.value,
                (ConstantValue.String s1, ConstantValue.String s2) => s1.value == s2.value,
                _ => false
            };
        }

        private static int value_hash_code(ConstantValue value)
        {
            return value switch
            {
                ConstantValue.Int64 i => i.value.GetHashCode(),
                ConstantValue.Float64 f => BitConverter.DoubleToUInt64Bits(f.value).GetHashCode(),
                ConstantValue.Bool b => b.value.GetHashCode(),
                ConstantValue.String s => s.value.GetHashCode(),
                _ => 0
            };
        }
    }
}