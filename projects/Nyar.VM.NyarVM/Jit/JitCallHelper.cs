using System.Collections.Generic;
using Nyar.Types;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     JIT 调用辅助类，�?JIT 编译后的函数提供跨函数调用能�?///     使用 ThreadStatic 保证线程安全
/// </summary>
internal static class JitCallHelper
{
    private static int resolve_offset_index(int rawIndex, int count)
    {
        return rawIndex < 0 ? count + rawIndex : rawIndex;
    }

    private static int? resolve_ordinal_index(int rawOrdinal, int count)
    {
        if (rawOrdinal == 0) return null;

        return rawOrdinal > 0 ? rawOrdinal - 1 : count + rawOrdinal;
    }

    #region 通用调用

    /// <summary>
    ///     �?JIT 编译代码中调用指定函数（Value 接口�?    ///     优先使用 JIT 编译版本，否则回退到解释执�?    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    /// <param name="args">函数参数�?/param>
    /// <returns>函数返回值�?/returns>
    public static Value call(int funcIndex, Value[] args)
    {
        var compiled = _compiler?.get_compiled_function(funcIndex);
        if (compiled != null) return compiled.execute(args);

        return _interpreter?.Invoke(funcIndex, args) ?? Value.@null;
    }

    #endregion

    #region i32 专用快速路�?
    /// <summary>
    ///     �?JIT 编译代码中调�?arity=1 �?i32 函数（消�?NaN-Boxing 开销�?    ///     优化路径：数组缓�?�?字典查找 �?解释执行
    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    /// <param name="arg0">int 参数�?/param>
    /// <returns>int 返回值�?/returns>
    public static int call_int1(int funcIndex, int arg0)
    {
        var funcs = _int_int_funcs;
        if (funcs != null && funcIndex < funcs.Length)
        {
            var del = funcs[funcIndex];
            if (del != null)
            {
                var saved = _self_recursive_target;
                _self_recursive_target = del;
                var result = del(arg0);
                _self_recursive_target = saved;
                return result;
            }
        }

        var compiled = _compiler?.get_compiled_function(funcIndex);
        if (compiled != null)
        {
            if (compiled.int_int_delegate != null)
            {
                register_int_int(funcIndex, compiled.int_int_delegate);
                var saved = _self_recursive_target;
                _self_recursive_target = compiled.int_int_delegate;
                var result = compiled.int_int_delegate(arg0);
                _self_recursive_target = saved;
                return result;
            }

            return compiled.execute([Value.from_int(arg0)]).i32;
        }

        return (_interpreter?.Invoke(funcIndex, [Value.from_int(arg0)]) ?? Value.@null).i32;
    }

    #endregion

    #region f64 专用快速路�?
    /// <summary>
    ///     �?JIT 编译代码中调�?arity=1 �?f64 函数（消�?NaN-Boxing 开销�?    ///     优化路径：数组缓�?�?字典查找 �?解释执行
    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    /// <param name="arg0">double 参数�?/param>
    /// <returns>double 返回值�?/returns>
    public static Value call_double1(int funcIndex, double arg0)
    {
        var funcs = _double_double_funcs;
        if (funcs != null && funcIndex < funcs.Length)
        {
            var del = funcs[funcIndex];
            if (del != null)
            {
                var saved = _self_recursive_double_target;
                _self_recursive_double_target = del;
                var result = del(arg0);
                _self_recursive_double_target = saved;
                return Value.from_double(result);
            }
        }

        var compiled = _compiler?.get_compiled_function(funcIndex);
        if (compiled != null)
        {
            if (compiled.double_double_delegate != null)
            {
                register_double_double(funcIndex, compiled.double_double_delegate);
                var saved = _self_recursive_double_target;
                _self_recursive_double_target = compiled.double_double_delegate;
                var result = compiled.double_double_delegate(arg0);
                _self_recursive_double_target = saved;
                return Value.from_double(result);
            }

            return compiled.execute([Value.from_double(arg0)]);
        }

        return _interpreter?.Invoke(funcIndex, [Value.from_double(arg0)]) ?? Value.@null;
    }

    #endregion

    #region 值辅�?
    /// <summary>
    ///     执行引用相等比较，仅�?`null` 与引用类值返回真�?    /// </summary>
    /// <param name="left">左值�?/param>
    /// <param name="right">右值�?/param>
    /// <returns>布尔结果�?/returns>
    public static Value ref_eq(Value left, Value right)
    {
        return Value.from_bool(are_reference_operands_equal(left, right));
    }

    /// <summary>
    ///     执行引用不等比较，仅�?`null` 与引用类值返回真�?    /// </summary>
    /// <param name="left">左值�?/param>
    /// <param name="right">右值�?/param>
    /// <returns>布尔结果�?/returns>
    public static Value ref_ne(Value left, Value right)
    {
        return Value.from_bool(!are_reference_operands_equal(left, right));
    }

    private static bool are_reference_operands_equal(Value left, Value right)
    {
        if (left.type == ValueType.@null || right.type == ValueType.@null)
            return left.type == ValueType.@null && right.type == ValueType.@null;

        if (!is_reference_like(left) || !is_reference_like(right)) return false;

        return left == right;
    }

    private static bool is_reference_like(Value value)
    {
        return value.type is ValueType.@object or ValueType.utf8 or ValueType.closure or ValueType.continuation
            or ValueType.effect or ValueType.witness_table;
    }

    #endregion

    #region ThreadStatic 上下�?
    /// <summary>
    ///     当前线程�?JIT 编译器实�?    /// </summary>
    [ThreadStatic] private static JitCompiler? _compiler;

    /// <summary>
    ///     当前线程的解释执行委�?    /// </summary>
    [ThreadStatic] private static Func<int, Value[], Value>? _interpreter;

    /// <summary>
    ///     当前线程的全局变量加载委托
    /// </summary>
    [ThreadStatic] private static Func<int, Value>? _load_global;

    /// <summary>
    ///     当前线程的全局变量存储委托
    /// </summary>
    [ThreadStatic] private static Action<int, Value>? _store_global;

    /// <summary>
    ///     当前线程�?IntInt 委托缓存数组（按函数索引直接访问，替代字典查找）
    /// </summary>
    [ThreadStatic] private static Func<int, int>?[]? _int_int_funcs;

    /// <summary>
    ///     当前线程�?DoubleDouble 委托缓存数组（按函数索引直接访问，替代字典查找）
    /// </summary>
    [ThreadStatic] private static Func<double, double>?[]? _double_double_funcs;

    /// <summary>
    ///     当前线程的自递归目标委托（当前正在执行的 arity=1 i32 函数的委托）
    ///     自递归调用直接读取此字段，避免数组查找和方法调用开销
    /// </summary>
    [ThreadStatic] internal static Func<int, int>? _self_recursive_target;

    /// <summary>
    ///     当前线程的自递归目标委托（当前正在执行的 arity=1 f64 函数的委托）
    /// </summary>
    [ThreadStatic] internal static Func<double, double>? _self_recursive_double_target;

    #endregion

    #region 上下文管�?
    /// <summary>
    ///     设置当前线程�?JIT 上下�?    /// </summary>
    /// <param name="compiler">JIT 编译器实例�?/param>
    /// <param name="interpreter">解释执行委托�?/param>
    public static void set_context(JitCompiler compiler, Func<int, Value[], Value> interpreter)
    {
        _compiler = compiler;
        _interpreter = interpreter;
    }

    /// <summary>
    ///     设置当前线程的全局变量访问委托
    /// </summary>
    /// <param name="loadGlobal">全局变量加载委托�?/param>
    /// <param name="storeGlobal">全局变量存储委托�?/param>
    public static void set_global_access(Func<int, Value> loadGlobal, Action<int, Value> storeGlobal)
    {
        _load_global = loadGlobal;
        _store_global = storeGlobal;
    }

    #region 全局变量访问

    /// <summary>
    ///     �?JIT 编译代码中加载全局变量
    /// </summary>
    /// <param name="globalIndex">全局变量索引�?/param>
    /// <returns>全局变量的值�?/returns>
    public static Value load_global(int globalIndex)
    {
        return _load_global?.Invoke(globalIndex) ?? Value.@null;
    }

    /// <summary>
    ///     �?JIT 编译代码中存储全局变量
    /// </summary>
    /// <param name="globalIndex">全局变量索引�?/param>
    /// <param name="value">要存储的值�?/param>
    public static void store_global(int globalIndex, Value value)
    {
        _store_global?.Invoke(globalIndex, value);
    }

    #endregion

    /// <summary>
    ///     注册 IntInt 委托到缓存数�?    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    /// <param name="del">IntInt 委托�?/param>
    internal static void register_int_int(int funcIndex, Func<int, int> del)
    {
        _int_int_funcs ??= new Func<int, int>?[64];
        if (funcIndex >= _int_int_funcs.Length) Array.Resize(ref _int_int_funcs, funcIndex + 1);

        _int_int_funcs[funcIndex] = del;
    }

    /// <summary>
    ///     从缓存数组移�?IntInt 委托
    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    internal static void unregister_int_int(int funcIndex)
    {
        if (_int_int_funcs != null && funcIndex < _int_int_funcs.Length) _int_int_funcs[funcIndex] = null;
    }

    /// <summary>
    ///     注册 DoubleDouble 委托到缓存数�?    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    /// <param name="del">DoubleDouble 委托�?/param>
    internal static void register_double_double(int funcIndex, Func<double, double> del)
    {
        _double_double_funcs ??= new Func<double, double>?[64];
        if (funcIndex >= _double_double_funcs.Length) Array.Resize(ref _double_double_funcs, funcIndex + 1);

        _double_double_funcs[funcIndex] = del;
    }

    /// <summary>
    ///     从缓存数组移�?DoubleDouble 委托
    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    internal static void unregister_double_double(int funcIndex)
    {
        if (_double_double_funcs != null && funcIndex < _double_double_funcs.Length)
            _double_double_funcs[funcIndex] = null;
    }

    #endregion

    #region 对象操作

    /// <summary>
    ///     �?JIT 编译代码中创建新对象
    /// </summary>
    /// <param name="initialCapacity">初始容量（预留，当前未使用）�?/param>
    /// <returns>包含空字典的新对象值�?/returns>
    public static Value new_object(int initialCapacity)
    {
        return Value.from_object(new Dictionary<string, Value>(initialCapacity));
    }

    /// <summary>
    ///     �?JIT 编译代码中获取对象字�?    /// </summary>
    /// <param name="obj">对象值�?/param>
    /// <param name="fieldName">字段名值�?/param>
    /// <returns>字段值，不存在则返回 Null�?/returns>
    public static Value get_field(Value obj, Value fieldName)
    {
        if (obj.@object is Dictionary<string, Value> dict && fieldName.utf8 is string name)
            return dict.GetValueOrDefault(name, Value.@null);

        return Value.@null;
    }

    /// <summary>
    ///     �?JIT 编译代码中设置对象字�?    /// </summary>
    /// <param name="obj">对象值�?/param>
    /// <param name="fieldName">字段名值�?/param>
    /// <param name="value">要设置的值�?/param>
    /// <returns>设置的值�?/returns>
    public static Value set_field(Value obj, Value fieldName, Value value)
    {
        if (obj.@object is Dictionary<string, Value> dict && fieldName.utf8 is string name) dict[name] = value;

        return value;
    }

    /// <summary>
    ///     在 JIT 编译代码中获取数组元素。
    /// </summary>
    public static Value array_get(Value obj, Value index)
    {
        if (obj.@object is List<Value> list && index.type == ValueType.i32)
        {
            var resolvedIndex = resolve_offset_index(index.i32, list.Count);
            return resolvedIndex >= 0 && resolvedIndex < list.Count ? list[resolvedIndex] : Value.@null;
        }

        return Value.@null;
    }

    /// <summary>
    ///     在 JIT 编译代码中设置数组元素。
    /// </summary>
    public static Value array_set(Value obj, Value index, Value value)
    {
        if (obj.@object is List<Value> list && index.type == ValueType.i32)
        {
            var resolvedIndex = resolve_offset_index(index.i32, list.Count);
            if (resolvedIndex >= 0 && resolvedIndex < list.Count)
            {
                list[resolvedIndex] = value;
            }
        }

        return Value.@null;
    }

    /// <summary>
    ///     �?JIT 编译代码中获取序数索�?    /// </summary>
    public static Value get_ordinal_index(Value obj, Value ordinal)
    {
        if (obj.@object is List<Value> list && ordinal.type == ValueType.i32)
        {
            var resolvedIndex = resolve_ordinal_index(ordinal.i32, list.Count);
            return resolvedIndex >= 0 && resolvedIndex < list.Count ? list[resolvedIndex.Value] : Value.@null;
        }

        return Value.@null;
    }

    /// <summary>
    ///     �?JIT 编译代码中获取偏移索�?    /// </summary>
    public static Value get_offset_index(Value obj, Value index)
    {
        if (obj.@object is List<Value> list && index.type == ValueType.i32)
        {
            var resolvedIndex = resolve_offset_index(index.i32, list.Count);
            return resolvedIndex >= 0 && resolvedIndex < list.Count ? list[resolvedIndex] : Value.@null;
        }

        if (obj.@object is Dictionary<string, Value> dict && index.utf8 is string key)
            return dict.GetValueOrDefault(key, Value.@null);

        return Value.@null;
    }

    /// <summary>
    ///     �?JIT 编译代码中设置序数索�?    /// </summary>
    public static Value set_ordinal_index(Value obj, Value ordinal, Value value)
    {
        if (obj.@object is List<Value> list && ordinal.type == ValueType.i32)
        {
            var resolvedIndex = resolve_ordinal_index(ordinal.i32, list.Count);
            if (resolvedIndex >= 0 && resolvedIndex < list.Count) list[resolvedIndex.Value] = value;
        }

        return value;
    }

    /// <summary>
    ///     �?JIT 编译代码中设置偏移索�?    /// </summary>
    public static Value set_offset_index(Value obj, Value index, Value value)
    {
        if (obj.@object is List<Value> list && index.type == ValueType.i32)
        {
            var resolvedIndex = resolve_offset_index(index.i32, list.Count);
            if (resolvedIndex >= 0 && resolvedIndex < list.Count) list[resolvedIndex] = value;
        }
        else if (obj.@object is Dictionary<string, Value> dict && index.utf8 is string key)
        {
            dict[key] = value;
        }

        return value;
    }

    /// <summary>
    ///     �?JIT 编译代码中获取长�?    /// </summary>
    /// <param name="obj">对象值�?/param>
    /// <returns>长度值�?/returns>
    public static Value length(Value obj)
    {
        if (obj.@object is List<Value> list) return Value.from_int(list.Count);

        if (obj.@object is Dictionary<string, Value> dict) return Value.from_int(dict.Count);

        if (obj.utf8 is string str) return Value.from_int(str.Length);

        return Value.from_int(0);
    }

    /// <summary>
    ///     �?JIT 编译代码中按偏移量静态访问字�?    /// </summary>
    /// <param name="obj">对象值�?/param>
    /// <param name="fieldOffset">字段偏移量�?/param>
    /// <returns>字段值�?/returns>
    public static Value access_static(Value obj, int fieldOffset)
    {
        if (obj.@object is List<Value> list && fieldOffset >= 0 && fieldOffset < list.Count) return list[fieldOffset];

        if (obj.@object is Dictionary<string, Value> dict)
        {
            var keys = dict.Keys.ToList();
            if (fieldOffset >= 0 && fieldOffset < keys.Count) return dict[keys[fieldOffset]];
        }

        return Value.@null;
    }

    /// <summary>
    ///     �?JIT 编译代码中按见证偏移量访问字�?    /// </summary>
    /// <param name="obj">对象值�?/param>
    /// <param name="witnessOffset">见证偏移量�?/param>
    /// <returns>字段值�?/returns>
    public static Value access_witness(Value obj, int witnessOffset)
    {
        if (obj.@object is Dictionary<string, Value> dict)
        {
            var keys = dict.Keys.ToList();
            if (witnessOffset >= 0 && witnessOffset < keys.Count) return dict[keys[witnessOffset]];
        }

        if (obj.@object is List<Value> list && witnessOffset >= 0 && witnessOffset < list.Count)
            return list[witnessOffset];

        return Value.@null;
    }

    #endregion

    #region 内存操作

    /// <summary>
    ///     �?JIT 编译代码中分配内存块
    /// </summary>
    /// <param name="size">请求的字节数�?/param>
    /// <returns>分配的起始地址（i32）�?/returns>
    public static Value mem_alloc(int size)
    {
        var heap = _compiler?._heap;
        if (heap == null) return Value.from_int(0);

        var address = heap.alloc(size);
        return Value.from_int(address);
    }

    /// <summary>
    ///     �?JIT 编译代码中释放内存块
    /// </summary>
    /// <param name="address">要释放的起始地址�?/param>
    public static void mem_free(Value address)
    {
        var heap = _compiler?._heap;

        heap?.free(address.i32);
    }

    /// <summary>
    ///     �?JIT 编译代码中加�?i32 �?    /// </summary>
    /// <param name="address">基地址值�?/param>
    /// <param name="offset">偏移量�?/param>
    /// <returns>加载�?i32 值�?/returns>
    public static Value mem_i32_load(Value address, int offset)
    {
        var heap = _compiler?._heap;
        if (heap == null) return Value.from_int(0);

        var value = heap.load_i32(address.i32 + offset);
        return Value.from_int(value);
    }

    /// <summary>
    ///     �?JIT 编译代码中存�?i32 �?    /// </summary>
    /// <param name="address">基地址值�?/param>
    /// <param name="offset">偏移量�?/param>
    /// <param name="value">要存储的值�?/param>
    public static void mem_i32_store(Value address, int offset, Value value)
    {
        var heap = _compiler?._heap;

        heap?.store_i32(address.i32 + offset, value.i32);
    }

    /// <summary>
    ///     �?JIT 编译代码中加�?i64 �?    /// </summary>
    /// <param name="address">基地址值�?/param>
    /// <param name="offset">偏移量�?/param>
    /// <returns>加载�?i64 值�?/returns>
    public static Value mem_i64_load(Value address, int offset)
    {
        var heap = _compiler?._heap;
        if (heap == null) return Value.from_long(0);

        var value = heap.load_i64(address.i32 + offset);
        return Value.from_long(value);
    }

    /// <summary>
    ///     �?JIT 编译代码中存�?i64 �?    /// </summary>
    /// <param name="address">基地址值�?/param>
    /// <param name="offset">偏移量�?/param>
    /// <param name="value">要存储的值�?/param>
    public static void mem_i64_store(Value address, int offset, Value value)
    {
        var heap = _compiler?._heap;

        heap?.store_i64(address.i32 + offset, value.i64);
    }

    #endregion

    #region 闭包操作

    /// <summary>
    ///     �?JIT 编译代码中创建闭�?    /// </summary>
    /// <param name="funcIndex">函数索引�?/param>
    /// <returns>包含闭包的值�?/returns>
    public static Value new_closure(int funcIndex)
    {
        var module = _compiler?._module;
        if (module == null || funcIndex < 0 || funcIndex >= module.functions.Count) return Value.@null;

        var func = module.functions[funcIndex];
        var upvalues = new List<Value>();
        var closure = new NyarClosure(func, upvalues);
        return Value.from_closure(closure);
    }

    /// <summary>
    ///     �?JIT 编译代码中获取上�?    /// </summary>
    /// <param name="closureValue">闭包值�?/param>
    /// <param name="upvalueIndex">上值索引�?/param>
    /// <returns>上值�?/returns>
    public static Value get_upvalue(Value closureValue, int upvalueIndex)
    {
        if (closureValue.closure is NyarClosure nc && upvalueIndex >= 0 && upvalueIndex < nc.upvalues.Count)
            return nc.upvalues[upvalueIndex];

        return Value.@null;
    }

    /// <summary>
    ///     �?JIT 编译代码中设置上�?    /// </summary>
    /// <param name="closureValue">闭包值�?/param>
    /// <param name="upvalueIndex">上值索引�?/param>
    /// <param name="value">要设置的值�?/param>
    /// <returns>设置的值�?/returns>
    public static Value set_upvalue(Value closureValue, int upvalueIndex, Value value)
    {
        if (closureValue.closure is NyarClosure nc)
        {
            while (nc.upvalues.Count <= upvalueIndex) nc.upvalues.Add(Value.@null);

            nc.upvalues[upvalueIndex] = value;
        }

        return value;
    }

    #endregion
}
