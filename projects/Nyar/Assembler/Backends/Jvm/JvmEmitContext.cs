using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm.Data;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 字节码发射上下文。
///     聚合了 <see cref="ByteBufferWriter" />、常量池、模块字符串池及 BootstrapMethods，
///     并代理常用的字节写入和常量池条目添加方法，避免在调用点散落十余个透传参数。
/// </summary>
public ref struct JvmEmitContext
{
    /// <summary>
    ///     底层字节缓冲区写入器。
    /// </summary>
    public ByteBufferWriter writer;

    /// <summary>
    ///     JVM 常量池条目列表。
    /// </summary>
    public List<JvmConstant> constant_pool;

    /// <summary>
    ///     模块级字符串常量池。
    /// </summary>
    public IReadOnlyList<string> module_strings;

    /// <summary>
    ///     BootstrapMethods 属性条目列表。
    /// </summary>
    public List<JvmBootstrapMethod> bootstrap_methods;

    /// <summary>
    ///     当前正在编译的类的内部名（以 '/' 分隔）。
    /// </summary>
    public string class_name;

    /// <summary>
    ///     当前已写入的字节数。
    /// </summary>
    public int position => writer.position;

    /// <summary>
    ///     获取已写入字节的数组。
    /// </summary>
    public byte[] to_array()
    {
        return writer.to_array();
    }

    #region 字节写入代理

    /// <summary>
    ///     写入单个无符号 8 位整数。
    /// </summary>
    public void write_u1(byte value)
    {
        writer.write_u8(value);
    }

    /// <summary>
    ///     以大端序写入无符号 16 位整数。
    /// </summary>
    public void write_u2_be(ushort value)
    {
        writer.write_u16_be(value);
    }

    /// <summary>
    ///     以大端序写入有符号 16 位整数。
    /// </summary>
    public void write_i2_be(short value)
    {
        writer.write_i16_be(value);
    }

    /// <summary>
    ///     以大端序写入无符号 32 位整数。
    /// </summary>
    public void write_u4_be(uint value)
    {
        writer.write_u32_be(value);
    }

    /// <summary>
    ///     写入 JVM 操作码。
    /// </summary>
    public void emit_op(JvmOpcode opcode)
    {
        writer.write_u8((byte)opcode);
    }

    #endregion

    #region 常量池条目代理

    /// <summary>
    ///     向常量池添加 UTF-8 条目（去重）并返回 1 基索引。
    /// </summary>
    public ushort add_utf8(string value)
    {
        return JvmConstantPoolHelper.add_utf8(constant_pool, value);
    }

    /// <summary>
    ///     根据 1 基索引读取常量池中的 UTF-8 字符串。
    /// </summary>
    public string get_utf8(ushort index)
    {
        return JvmConstantPoolHelper.get_utf8(constant_pool, index);
    }

    /// <summary>
    ///     向常量池添加 Class 引用条目（去重）并返回 1 基索引。
    /// </summary>
    public ushort add_class(string name)
    {
        return JvmConstantPoolHelper.add_class(constant_pool, name);
    }

    /// <summary>
    ///     向常量池添加 String 字面量条目（去重）并返回 1 基索引。
    /// </summary>
    public ushort add_string_const(string value)
    {
        return JvmConstantPoolHelper.add_string_const(constant_pool, value);
    }

    /// <summary>
    ///     向常量池添加 Integer 条目（去重）并返回 1 基索引。
    /// </summary>
    public ushort add_integer(int value)
    {
        return JvmConstantPoolHelper.add_integer(constant_pool, value);
    }

    /// <summary>
    ///     向常量池添加 Long 条目（占用 2 个槽位）。
    /// </summary>
    public ushort add_long(long value)
    {
        return JvmConstantPoolHelper.add_long(constant_pool, value);
    }

    /// <summary>
    ///     向常量池添加 Float 条目（去重）。
    /// </summary>
    public ushort add_float(float value)
    {
        return JvmConstantPoolHelper.add_float(constant_pool, value);
    }

    /// <summary>
    ///     向常量池添加 Double 条目（占用 2 个槽位）。
    /// </summary>
    public ushort add_double(double value)
    {
        return JvmConstantPoolHelper.add_double(constant_pool, value);
    }

    /// <summary>
    ///     向常量池添加 Methodref 条目（去重）。
    /// </summary>
    public ushort add_method_ref(string className, string methodName, string descriptor)
    {
        return JvmConstantPoolHelper.add_method_ref(constant_pool, className, methodName, descriptor);
    }

    /// <summary>
    ///     向常量池添加 Fieldref 条目（去重）。
    /// </summary>
    public ushort add_field_ref(string className, string fieldName, string descriptor)
    {
        return JvmConstantPoolHelper.add_field_ref(constant_pool, className, fieldName, descriptor);
    }

    #endregion
}