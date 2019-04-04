using Std.Data.Binary.Jvm.Data;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 常量池辅助方法集合。
///     提供常量池条目添加（带去重）和读取的工具方法，所有方法都返回 1 基索引（JVM 规范要求）。
/// </summary>
internal static class JvmConstantPoolHelper
{
    /// <summary>
    ///     向常量池追加 UTF-8 条目，若已存在相同值则返回原条目索引。
    /// </summary>
    public static ushort add_utf8(List<JvmConstant> pool, string value)
    {
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantUtf8 u && u.value == value)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantUtf8 { value = value });
        return (ushort)pool.Count;
    }

    /// <summary>
    ///     按 1 基索引读取常量池中的 UTF-8 字符串。
    /// </summary>
    public static string get_utf8(List<JvmConstant> pool, ushort index)
    {
        return ((JvmConstantUtf8)pool[index - 1]).value;
    }

    /// <summary>
    ///     向常量池追加 Class 引用条目（去重），同时确保对应的 UTF-8 名称条目存在。
    /// </summary>
    public static ushort add_class(List<JvmConstant> pool, string name)
    {
        var nameIdx = add_utf8(pool, name);
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantClass c && c.name_index == nameIdx)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantClass { name_index = nameIdx });
        return (ushort)pool.Count;
    }

    /// <summary>
    ///     向常量池追加 String 字面量条目（去重）。
    /// </summary>
    public static ushort add_string_const(List<JvmConstant> pool, string value)
    {
        var utfIdx = add_utf8(pool, value);
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantString s && s.string_index == utfIdx)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantString { string_index = utfIdx });
        return (ushort)pool.Count;
    }

    /// <summary>
    ///     兼容旧调用方的字符串常量追加入口。
    /// </summary>
    public static ushort add_string(List<JvmConstant> pool, string value)
    {
        return add_string_const(pool, value);
    }

    /// <summary>
    ///     向常量池追加 Integer 条目（去重）。
    /// </summary>
    public static ushort add_integer(List<JvmConstant> pool, int value)
    {
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantInteger it && it.value == value)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantInteger { value = value });
        return (ushort)pool.Count;
    }

    /// <summary>
    ///     兼容旧调用方的整数常量追加入口。
    /// </summary>
    public static ushort add_int(List<JvmConstant> pool, int value)
    {
        return add_integer(pool, value);
    }

    /// <summary>
    ///     向常量池追加 Long 条目（占用 2 个槽位，按 JVM 规范）。
    /// </summary>
    public static ushort add_long(List<JvmConstant> pool, long value)
    {
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantLong l && l.value == value)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantLong { value = value });
        var idx = (ushort)pool.Count;
        pool.Add(new JvmConstantPadding());
        return idx;
    }

    /// <summary>
    ///     向常量池追加 Float 条目（去重）。
    /// </summary>
    public static ushort add_float(List<JvmConstant> pool, float value)
    {
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantFloat f && f.value == value)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantFloat { value = value });
        return (ushort)pool.Count;
    }

    /// <summary>
    ///     向常量池追加 Double 条目（占用 2 个槽位）。
    /// </summary>
    public static ushort add_double(List<JvmConstant> pool, double value)
    {
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantDouble d && d.value == value)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantDouble { value = value });
        var idx = (ushort)pool.Count;
        pool.Add(new JvmConstantPadding());
        return idx;
    }

    /// <summary>
    ///     向常量池追加 NameAndType 条目（去重）。
    /// </summary>
    public static ushort add_name_and_type(List<JvmConstant> pool, string name, string desc)
    {
        var nameIdx = add_utf8(pool, name);
        var descIdx = add_utf8(pool, desc);
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantNameAndType nt && nt.name_index == nameIdx && nt.descriptor_index == descIdx)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantNameAndType { name_index = nameIdx, descriptor_index = descIdx });
        return (ushort)pool.Count;
    }

    /// <summary>
    ///     向常量池追加 Methodref 条目（去重），并自动创建 Class 与 NameAndType 子条目。
    /// </summary>
    public static ushort add_method_ref(List<JvmConstant> pool, string className, string methodName, string desc)
    {
        var classIdx = add_class(pool, className);
        var ntIdx = add_name_and_type(pool, methodName, desc);
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantMethodref m && m.class_index == classIdx && m.name_and_type_index == ntIdx)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantMethodref { class_index = classIdx, name_and_type_index = ntIdx });
        return (ushort)pool.Count;
    }

    /// <summary>
    ///     向常量池追加 Fieldref 条目（去重），并自动创建 Class 与 NameAndType 子条目。
    /// </summary>
    public static ushort add_field_ref(List<JvmConstant> pool, string className, string fieldName, string desc)
    {
        var classIdx = add_class(pool, className);
        var ntIdx = add_name_and_type(pool, fieldName, desc);
        for (var i = 0; i < pool.Count; i++)
            if (pool[i] is JvmConstantFieldref f && f.class_index == classIdx && f.name_and_type_index == ntIdx)
                return (ushort)(i + 1);

        pool.Add(new JvmConstantFieldref { class_index = classIdx, name_and_type_index = ntIdx });
        return (ushort)pool.Count;
    }
}