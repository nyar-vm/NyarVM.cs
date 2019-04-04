namespace Nyar.VM.NyarVM.Runtime;

/// <summary>
///     Witness Table 运行时，提供接口分派和类型信息查询
/// </summary>
public sealed class WitnessTable
{
    private readonly Dictionary<int, DispatchEntry> _dispatch_table;
    private readonly Dictionary<int, InterfaceTable> _interface_tables;
    private readonly Dictionary<int, TypeInfoRecord> _type_records;

    /// <summary>
    ///     创建 Witness Table 运行时
    /// </summary>
    public WitnessTable()
    {
        _dispatch_table = new Dictionary<int, DispatchEntry>();
        _interface_tables = new Dictionary<int, InterfaceTable>();
        _type_records = new Dictionary<int, TypeInfoRecord>();
    }

    /// <summary>
    ///     注册类型信息
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="typeName">类型名称。</param>
    /// <param name="size">类型大小（字节）。</param>
    /// <param name="fieldCount">字段数量。</param>
    public void register_type(int typeId, string typeName, int size, int fieldCount)
    {
        _type_records[typeId] = new TypeInfoRecord(typeId, typeName, size, fieldCount);
    }

    /// <summary>
    ///     注册接口实现
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="interfaceId">接口 ID。</param>
    /// <param name="methodTable">方法分派表（接口方法索引 → 具体方法索引）。</param>
    public void register_interface(int typeId, int interfaceId, Dictionary<int, int> methodTable)
    {
        var key = combine_key(typeId, interfaceId);
        _interface_tables[key] = new InterfaceTable(typeId, interfaceId, methodTable);
    }

    /// <summary>
    ///     注册方法分派条目
    /// </summary>
    /// <param name="methodId">方法 ID。</param>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="methodName">方法名称。</param>
    /// <param name="functionIndex">VM 函数索引。</param>
    public void register_method(int methodId, int typeId, string methodName, int functionIndex)
    {
        _dispatch_table[methodId] = new DispatchEntry(methodId, typeId, methodName, functionIndex);
    }

    /// <summary>
    ///     接口分派：查找接口方法对应的具体函数索引
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="interfaceId">接口 ID。</param>
    /// <param name="methodIndex">接口方法索引。</param>
    /// <returns>具体函数索引，未找到返回 -1。</returns>
    public int dispatch(int typeId, int interfaceId, int methodIndex)
    {
        var key = combine_key(typeId, interfaceId);
        if (!_interface_tables.TryGetValue(key, out var iTable)) return -1;

        if (!iTable.method_table.TryGetValue(methodIndex, out var concreteMethodId)) return -1;

        if (!_dispatch_table.TryGetValue(concreteMethodId, out var entry)) return -1;

        return entry.function_index;
    }

    /// <summary>
    ///     查找类型的方法分派条目
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="methodName">方法名称。</param>
    /// <returns>分派条目，未找到返回 null。</returns>
    public DispatchEntry? find_method(int typeId, string methodName)
    {
        foreach (var entry in _dispatch_table.Values)
            if (entry.type_id == typeId && entry.method_name == methodName)
                return entry;

        return null;
    }

    /// <summary>
    ///     查询类型信息
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <returns>类型信息记录，未找到返回 null。</returns>
    public TypeInfoRecord? get_type_info(int typeId)
    {
        return _type_records.GetValueOrDefault(typeId);
    }

    /// <summary>
    ///     检查类型是否实现指定接口
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="interfaceId">接口 ID。</param>
    /// <returns>是否实现。</returns>
    public bool implements_interface(int typeId, int interfaceId)
    {
        var key = combine_key(typeId, interfaceId);
        return _interface_tables.ContainsKey(key);
    }

    /// <summary>
    ///     热更新：替换类型的 Witness Table（替换接口分派表）
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="interfaceId">接口 ID。</param>
    /// <param name="newMethodTable">新的方法分派表。</param>
    public void hot_swap(int typeId, int interfaceId, Dictionary<int, int> newMethodTable)
    {
        var key = combine_key(typeId, interfaceId);
        _interface_tables[key] = new InterfaceTable(typeId, interfaceId, newMethodTable);
    }

    private static int combine_key(int typeId, int interfaceId)
    {
        return (typeId << 16) | (interfaceId & 0xFFFF);
    }
}

/// <summary>
///     方法分派条目
/// </summary>
public sealed class DispatchEntry
{
    /// <summary>
    ///     创建方法分派条目
    /// </summary>
    public DispatchEntry(int methodId, int typeId, string methodName, int functionIndex)
    {
        method_id = methodId;
        type_id = typeId;
        method_name = methodName;
        function_index = functionIndex;
    }

    /// <summary>
    ///     方法 ID
    /// </summary>
    public int method_id { get; }

    /// <summary>
    ///     类型 ID
    /// </summary>
    public int type_id { get; }

    /// <summary>
    ///     方法名称
    /// </summary>
    public string method_name { get; }

    /// <summary>
    ///     VM 函数索引
    /// </summary>
    public int function_index { get; set; }
}

/// <summary>
///     接口分派表
/// </summary>
public sealed class InterfaceTable
{
    /// <summary>
    ///     创建接口分派表
    /// </summary>
    public InterfaceTable(int typeId, int interfaceId, Dictionary<int, int> methodTable)
    {
        type_id = typeId;
        interface_id = interfaceId;
        method_table = methodTable;
    }

    /// <summary>
    ///     类型 ID
    /// </summary>
    public int type_id { get; }

    /// <summary>
    ///     接口 ID
    /// </summary>
    public int interface_id { get; }

    /// <summary>
    ///     方法分派表（接口方法索引 → 具体方法 ID）
    /// </summary>
    public Dictionary<int, int> method_table { get; }
}

/// <summary>
///     类型信息记录
/// </summary>
public sealed class TypeInfoRecord
{
    /// <summary>
    ///     创建类型信息记录
    /// </summary>
    public TypeInfoRecord(int typeId, string typeName, int size, int fieldCount)
    {
        type_id = typeId;
        type_name = typeName;
        this.size = size;
        field_count = fieldCount;
    }

    /// <summary>
    ///     类型 ID
    /// </summary>
    public int type_id { get; }

    /// <summary>
    ///     类型名称
    /// </summary>
    public string type_name { get; }

    /// <summary>
    ///     类型大小（字节）
    /// </summary>
    public int size { get; }

    /// <summary>
    ///     字段数量
    /// </summary>
    public int field_count { get; }
}