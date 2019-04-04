namespace Nyar.Types;

/// <summary>
///     使用 NaN-Boxing 技术的值表示（仅限 64 位进程）
///     编码方案：
///     - Null: 全零（0x0000_0000_0000_0000），使 default(Value) 为 Null
///     - Double: 原始 IEEE 754 位模式（NaN 规范化为正 quiet NaN，+0.0 规范化为 -0.0）
///     - 其他类型: 使用负 quiet NaN 空间（sign=1, exponent=0x7FF, quiet=1）
///     类型标签存储在 bits 50-47（4 位 = 16 种类型），载荷存储在 bits 46-0（47 位）
/// </summary>
public readonly struct Value : IEquatable<Value>
{
    /// <summary>
    ///     原始位表示
    /// </summary>
    private readonly ulong _bits;

    /// <summary>
    ///     对象引用表（用于安全存储托管对象引用）
    /// </summary>
    private static readonly List<object?> _object_table = [];

    /// <summary>
    ///     对象引用表锁
    /// </summary>
    private static readonly object _table_lock = new();

    /// <summary>
    ///     GC 分配器引用，注册后 Value 工厂方法将委托给 GC 分配
    /// </summary>
    private static IGcAllocator? _gc_allocator;

    /// <summary>
    ///     获取共享对象表引用（仅供 GC 内部使用）
    /// </summary>
    public static List<object?> shared_object_table => _object_table;

    /// <summary>
    ///     获取共享对象表锁（仅供 GC 内部使用）
    /// </summary>
    public static object shared_table_lock => _table_lock;

    /// <summary>
    ///     注册 GC 分配器，使 Value 工厂方法通过 GC 分配对象
    /// </summary>
    /// <param name="allocator">GC 分配器实例。</param>
    public static void register_gc_allocator(IGcAllocator allocator)
    {
        _gc_allocator = allocator;
    }

    /// <summary>
    ///     注销 GC 分配器
    /// </summary>
    public static void unregister_gc_allocator()
    {
        _gc_allocator = null;
    }

    /// <summary>
    ///     值类型标签
    /// </summary>
    public ValueType type
    {
        get
        {
            if (_bits == 0) return ValueType.@null;

            if ((_bits & _nan_tag_mask) != _nan_tag_mask) return ValueType.f64;

            var typeTag = (int)((_bits >> _type_tag_shift) & _type_tag_mask);
            return typeTag switch
            {
                _type_tag_int => ValueType.i32,
                _type_tag_bool => ValueType.@bool,
                _type_tag_null => ValueType.@null,
                _type_tag_object => ValueType.@object,
                _type_tag_big_int => ValueType.big_int,
                _type_tag_string => ValueType.utf8,
                _type_tag_closure => ValueType.closure,
                _type_tag_continuation => ValueType.continuation,
                _type_tag_effect => ValueType.effect,
                _type_tag_long => ValueType.i64,
                _type_tag_witness_table => ValueType.witness_table,
                _ => ValueType.unknown
            };
        }
    }

    /// <summary>
    ///     判断值是否为对象类型
    /// </summary>
    public bool is_object => type == ValueType.@object;

    /// <summary>
    ///     获取双精度浮点值
    /// </summary>
    public double f64 => BitConverter.Int64BitsToDouble((long)_bits);

    /// <summary>
    ///     获取整数值
    /// </summary>
    public int i32 => (int)(_bits & _payload_mask);

    /// <summary>
    ///     获取 64 位整数值
    /// </summary>
    public long i64
    {
        get
        {
            var typeTag = (int)((_bits >> _type_tag_shift) & _type_tag_mask);
            if (typeTag == _type_tag_long) return get_object_from_table(_type_tag_long) as long? ?? 0L;

            if (typeTag == _type_tag_int) return i32;

            return 0L;
        }
    }

    /// <summary>
    ///     获取布尔值
    /// </summary>
    public bool @bool => _bits == _bool_true_tag;

    /// <summary>
    ///     获取对象引用
    /// </summary>
    public object? @object => get_object_from_table(_type_tag_object);

    /// <summary>
    ///     获取大整数引用
    /// </summary>
    public object? big_int => get_object_from_table(_type_tag_big_int);

    /// <summary>
    ///     获取字符串引用
    /// </summary>
    public object? utf8 => get_object_from_table(_type_tag_string);

    /// <summary>
    ///     获取闭包引用
    /// </summary>
    public object? closure => get_object_from_table(_type_tag_closure);

    /// <summary>
    ///     获取延续引用
    /// </summary>
    public object? continuation => get_object_from_table(_type_tag_continuation);

    /// <summary>
    ///     获取效果引用
    /// </summary>
    public object? effect => get_object_from_table(_type_tag_effect);

    /// <summary>
    ///     获取见证表引用
    /// </summary>
    public object? witness_table => get_object_from_table(_type_tag_witness_table);

    #region 标签常量

    /// <summary>
    ///     NaN 标签掩码：sign=1, exponent=0x7FF, quiet=1
    ///     所有非 Double 标签值的高 13 位必须匹配此模式（构成 IEEE 754 负 quiet NaN）
    /// </summary>
    private const ulong _nan_tag_mask = 0xFFF8_0000_0000_0000UL;

    /// <summary>
    ///     类型标签位移（bits 50-47，4 位 = 16 种类型）
    /// </summary>
    private const int _type_tag_shift = 47;

    /// <summary>
    ///     类型标签掩码（4 位）
    /// </summary>
    private const ulong _type_tag_mask = 0xFUL;

    /// <summary>
    ///     载荷掩码（bits 46-0，47 位）
    /// </summary>
    private const ulong _payload_mask = 0x0000_7FFF_FFFF_FFFFUL;

    /// <summary>
    ///     规范化正 quiet NaN 的位模式（用于 FromDouble 中 NaN 的规范化）
    /// </summary>
    private const ulong _canonical_nan_bits = 0x7FF8_0000_0000_0000UL;

    /// <summary>
    ///     负零的位模式（用于 FromDouble 中 +0.0 的规范化，保留全零给 Null）
    /// </summary>
    private const ulong _negative_zero_bits = 0x8000_0000_0000_0000UL;

    private const int _type_tag_int = 0;
    private const int _type_tag_bool = 1;
    private const int _type_tag_null = 2;
    private const int _type_tag_object = 3;
    private const int _type_tag_big_int = 4;
    private const int _type_tag_string = 5;
    private const int _type_tag_closure = 6;
    private const int _type_tag_continuation = 7;
    private const int _type_tag_effect = 8;
    private const int _type_tag_long = 9;
    private const int _type_tag_witness_table = 10;

    private const ulong _int_tag = _nan_tag_mask | ((ulong)_type_tag_int << _type_tag_shift);
    private const ulong _bool_true_tag = _nan_tag_mask | ((ulong)_type_tag_bool << _type_tag_shift) | 1;
    private const ulong _bool_false_tag = _nan_tag_mask | ((ulong)_type_tag_bool << _type_tag_shift);
    private const ulong _null_tag = _nan_tag_mask | ((ulong)_type_tag_null << _type_tag_shift);

    #endregion

    private Value(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    ///     构造完整标签值（NaN 前缀 + 类型标签）
    /// </summary>
    private static ulong make_tag(int typeTag)
    {
        return _nan_tag_mask | ((ulong)typeTag << _type_tag_shift);
    }

    /// <summary>
    ///     从对象表中获取对象引用
    /// </summary>
    private object? get_object_from_table(int expectedTypeTag)
    {
        var actualTypeTag = (int)((_bits >> _type_tag_shift) & _type_tag_mask);
        if (actualTypeTag != expectedTypeTag) return null;

        var index = (int)(_bits & _payload_mask);
        lock (_table_lock)
        {
            return index < _object_table.Count ? _object_table[index] : null;
        }
    }

    /// <summary>
    ///     将对象存入对象表并返回索引
    ///     优先委托给 GC 分配器（支持槽位复用和分代追踪），否则直接追加
    /// </summary>
    private static ulong store_object_in_table(object obj)
    {
        var allocator = _gc_allocator;
        if (allocator != null)
        {
            var index = allocator.allocate(obj);
            return (ulong)index;
        }

        lock (_table_lock)
        {
            var index = _object_table.Count;
            _object_table.Add(obj);
            return (ulong)index;
        }
    }

    #region 工厂方法

    /// <summary>
    ///     创建整数值
    /// </summary>
    /// <param name="value">整数值。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_int(int value)
    {
        var payload = (ulong)value & _payload_mask;
        return new Value(payload | _int_tag);
    }

    /// <summary>
    ///     创建 64 位整数值
    /// </summary>
    /// <param name="value">64 位整数值。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_long(long value)
    {
        if (value is >= int.MinValue and <= int.MaxValue) return from_int((int)value);

        var index = store_object_in_table(value);
        return new Value((index & _payload_mask) | make_tag(_type_tag_long));
    }

    /// <summary>
    ///     创建双精度浮点值
    ///     NaN 被规范化为正 quiet NaN，+0.0 被规范化为 -0.0
    /// </summary>
    /// <param name="value">浮点值。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_double(double value)
    {
        if (double.IsNaN(value)) return new Value(_canonical_nan_bits);

        var bits = (ulong)BitConverter.DoubleToInt64Bits(value);

        if (bits == 0) return new Value(_negative_zero_bits);

        return new Value(bits);
    }

    /// <summary>
    ///     创建布尔值
    /// </summary>
    /// <param name="value">布尔值。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_bool(bool value)
    {
        return new Value(value ? _bool_true_tag : _bool_false_tag);
    }

    /// <summary>
    ///     创建空值
    /// </summary>
    /// <returns>Value 实例。</returns>
    public static Value @null => new(0UL);

    /// <summary>
    ///     创建对象引用
    /// </summary>
    /// <param name="obj">对象。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_object(object obj)
    {
        var index = store_object_in_table(obj);
        return new Value((index & _payload_mask) | make_tag(_type_tag_object));
    }

    /// <summary>
    ///     创建大整数值
    /// </summary>
    /// <param name="obj">大整数对象。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_big_int(object obj)
    {
        var index = store_object_in_table(obj);
        return new Value((index & _payload_mask) | make_tag(_type_tag_big_int));
    }

    /// <summary>
    ///     创建字符串值
    /// </summary>
    /// <param name="obj">字符串对象。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_string(object obj)
    {
        var index = store_object_in_table(obj);
        return new Value((index & _payload_mask) | make_tag(_type_tag_string));
    }

    /// <summary>
    ///     创建闭包值
    /// </summary>
    /// <param name="obj">闭包对象。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_closure(object obj)
    {
        var index = store_object_in_table(obj);
        return new Value((index & _payload_mask) | make_tag(_type_tag_closure));
    }

    /// <summary>
    ///     创建延续值
    /// </summary>
    /// <param name="obj">延续对象。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_continuation(object obj)
    {
        var index = store_object_in_table(obj);
        return new Value((index & _payload_mask) | make_tag(_type_tag_continuation));
    }

    /// <summary>
    ///     创建效果值
    /// </summary>
    /// <param name="obj">效果对象。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_effect(object obj)
    {
        var index = store_object_in_table(obj);
        return new Value((index & _payload_mask) | make_tag(_type_tag_effect));
    }

    /// <summary>
    ///     创建见证表值
    /// </summary>
    /// <param name="obj">见证表对象。</param>
    /// <returns>Value 实例。</returns>
    public static Value from_witness_table(object obj)
    {
        var index = store_object_in_table(obj);
        return new Value((index & _payload_mask) | make_tag(_type_tag_witness_table));
    }

    #endregion

    #region IEquatable<Value>

    /// <inheritdoc />
    public bool Equals(Value other)
    {
        return _bits == other._bits;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Value other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _bits.GetHashCode();
    }

    #endregion

    #region 运算符

    /// <summary>
    ///     获取原始位表示（仅供 GC 内部使用）
    /// </summary>
    public ulong raw_bits => _bits;

    /// <summary>
    ///     判断两个值是否相等
    /// </summary>
    public static bool operator ==(Value left, Value right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     判断两个值是否不相等
    /// </summary>
    public static bool operator !=(Value left, Value right)
    {
        return !left.Equals(right);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString()
    {
        return type switch
        {
            ValueType.i32 => i32.ToString(),
            ValueType.i64 => i64.ToString(),
            ValueType.f64 => f64.ToString(),
            ValueType.@bool => @bool.ToString(),
            ValueType.@null => "null",
            ValueType.@object => @object?.ToString() ?? "null",
            ValueType.big_int => big_int?.ToString() ?? "null",
            ValueType.utf8 => utf8?.ToString() ?? "null",
            ValueType.closure => closure?.ToString() ?? "null",
            ValueType.continuation => continuation?.ToString() ?? "null",
            ValueType.effect => effect?.ToString() ?? "null",
            ValueType.witness_table => witness_table?.ToString() ?? "null",
            _ => "unknown"
        };
    }
}