using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm.Data;

namespace Std.Data.Binary.Jvm.Scanner;

/// <summary>
///     JVM ClassFile 二进制格式扫描器，基的<see cref="SpanScanner" /> 提供的.class 文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     JVM ClassFile 使用大端字节序（Big-Endian），与多数其他二进制格式不同的
///     扫描器通过逐段跳过的方式快速提取结构统计信息，不完整解析常量池的
/// </remarks>
public ref struct JvmScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="JvmScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 JVM ClassFile 字节数据的/param>
    public JvmScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 JVM ClassFile 头部，提取版本与类元信息的
    /// </summary>
    public JvmScanHeader scan_header()
    {
        if (_scanner.length < 10) throw new InvalidDataException("JVM ClassFile 数据过短，无法读取文件头");

        if (!_scanner.match_magic(JvmConstants.magic_big_endian))
            throw new InvalidDataException("JVM ClassFile 魔数不匹配，期望 0xCAFEBABE");

        _scanner.consume_magic(JvmConstants.magic_big_endian);

        var minorVersion = _scanner.buffer.read_u16_be();
        var majorVersion = _scanner.buffer.read_u16_be();

        var header = new JvmScanHeader
        {
            minor_version = minorVersion,
            major_version = majorVersion
        };

        var constantPoolCount = _scanner.buffer.read_u16_be();
        header.constant_pool_count = constantPoolCount;

        var className = string.Empty;

        for (var i = 1; i < constantPoolCount; i++)
        {
            if (_scanner.buffer.remaining < 1) break;

            var tag = _scanner.buffer.read_u8();

            switch ((JvmConstantKind)tag)
            {
                case JvmConstantKind.utf8:
                    if (_scanner.buffer.remaining < 2) break;

                    var length = _scanner.buffer.read_u16_be();

                    if (_scanner.buffer.remaining >= length) _scanner.buffer.position += length;

                    break;

                case JvmConstantKind.integer:
                case JvmConstantKind.@float:
                case JvmConstantKind.fieldref:
                case JvmConstantKind.methodref:
                case JvmConstantKind.interface_methodref:
                case JvmConstantKind.name_and_type:
                case JvmConstantKind.invoke_dynamic:
                case JvmConstantKind.dynamic:
                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.@long:
                case JvmConstantKind.@double:
                    if (_scanner.buffer.remaining >= 8) _scanner.buffer.position += 8;

                    i++;
                    break;

                case JvmConstantKind.@class:
                case JvmConstantKind.@string:
                case JvmConstantKind.method_type:
                case JvmConstantKind.module:
                case JvmConstantKind.package:
                    if (_scanner.buffer.remaining >= 2) _scanner.buffer.position += 2;

                    break;

                case JvmConstantKind.method_handle:
                    if (_scanner.buffer.remaining >= 3) _scanner.buffer.position += 3;

                    break;
            }
        }

        if (_scanner.buffer.remaining < 6) return header;

        header.access_flags = _scanner.buffer.read_u16_be();
        header.this_class_index = _scanner.buffer.read_u16_be();
        header.super_class_index = _scanner.buffer.read_u16_be();

        return header;
    }

    /// <summary>
    ///     扫描 JVM ClassFile，提取统计信息的
    /// </summary>
    public JvmStatistics scan_statistics()
    {
        var stats = new JvmStatistics();

        if (_scanner.length < 10) throw new InvalidDataException("JVM ClassFile 数据过短，无法读取文件头");

        if (!_scanner.match_magic(JvmConstants.magic_big_endian))
            throw new InvalidDataException("JVM ClassFile 魔数不匹配，期望 0xCAFEBABE");

        _scanner.consume_magic(JvmConstants.magic_big_endian);

        stats.minor_version = _scanner.buffer.read_u16_be();
        stats.major_version = _scanner.buffer.read_u16_be();

        var constantPoolCount = _scanner.buffer.read_u16_be();
        stats.constant_pool_count = constantPoolCount;

        for (var i = 1; i < constantPoolCount; i++)
        {
            if (_scanner.buffer.remaining < 1) break;

            var tag = _scanner.buffer.read_u8();

            switch ((JvmConstantKind)tag)
            {
                case JvmConstantKind.utf8:
                    if (_scanner.buffer.remaining < 2) break;

                    var utf8Length = _scanner.buffer.read_u16_be();
                    stats.utf8_constant_count++;

                    if (_scanner.buffer.remaining >= utf8Length) _scanner.buffer.position += utf8Length;

                    break;

                case JvmConstantKind.integer:
                    stats.integer_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.@float:
                    stats.float_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.@long:
                    stats.long_constant_count++;

                    if (_scanner.buffer.remaining >= 8) _scanner.buffer.position += 8;

                    i++;
                    break;

                case JvmConstantKind.@double:
                    stats.double_constant_count++;

                    if (_scanner.buffer.remaining >= 8) _scanner.buffer.position += 8;

                    i++;
                    break;

                case JvmConstantKind.@class:
                    stats.class_constant_count++;

                    if (_scanner.buffer.remaining >= 2) _scanner.buffer.position += 2;

                    break;

                case JvmConstantKind.@string:
                    stats.string_constant_count++;

                    if (_scanner.buffer.remaining >= 2) _scanner.buffer.position += 2;

                    break;

                case JvmConstantKind.fieldref:
                    stats.fieldref_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.methodref:
                    stats.methodref_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.interface_methodref:
                    stats.interface_methodref_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.name_and_type:
                    stats.name_and_type_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.method_handle:
                    stats.method_handle_constant_count++;

                    if (_scanner.buffer.remaining >= 3) _scanner.buffer.position += 3;

                    break;

                case JvmConstantKind.method_type:
                    stats.method_type_constant_count++;

                    if (_scanner.buffer.remaining >= 2) _scanner.buffer.position += 2;

                    break;

                case JvmConstantKind.invoke_dynamic:
                    stats.invoke_dynamic_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.dynamic:
                    stats.dynamic_constant_count++;

                    if (_scanner.buffer.remaining >= 4) _scanner.buffer.position += 4;

                    break;

                case JvmConstantKind.module:
                    stats.module_constant_count++;

                    if (_scanner.buffer.remaining >= 2) _scanner.buffer.position += 2;

                    break;

                case JvmConstantKind.package:
                    stats.package_constant_count++;

                    if (_scanner.buffer.remaining >= 2) _scanner.buffer.position += 2;

                    break;
            }
        }

        if (_scanner.buffer.remaining < 8) return stats;

        stats.access_flags = _scanner.buffer.read_u16_be();
        _scanner.buffer.position += 4;

        var interfacesCount = _scanner.buffer.read_u16_be();
        stats.interface_count = interfacesCount;

        if (_scanner.buffer.remaining >= interfacesCount * 2) _scanner.buffer.position += interfacesCount * 2;

        if (_scanner.buffer.remaining < 2) return stats;

        var fieldsCount = _scanner.buffer.read_u16_be();
        stats.field_count = fieldsCount;

        for (var i = 0; i < fieldsCount; i++)
        {
            if (_scanner.buffer.remaining < 8) break;

            _scanner.buffer.position += 6;

            if (_scanner.buffer.remaining < 2) break;

            var fieldAttrCount = _scanner.buffer.read_u16_be();

            for (var j = 0; j < fieldAttrCount; j++)
            {
                if (_scanner.buffer.remaining < 6) break;

                _scanner.buffer.position += 2;
                var attrLength = _scanner.buffer.read_u32_be();

                if (_scanner.buffer.remaining >= (int)attrLength) _scanner.buffer.position += (int)attrLength;
            }
        }

        if (_scanner.buffer.remaining < 2) return stats;

        var methodsCount = _scanner.buffer.read_u16_be();
        stats.method_count = methodsCount;

        for (var i = 0; i < methodsCount; i++)
        {
            if (_scanner.buffer.remaining < 8) break;

            _scanner.buffer.position += 6;

            if (_scanner.buffer.remaining < 2) break;

            var methodAttrCount = _scanner.buffer.read_u16_be();

            for (var j = 0; j < methodAttrCount; j++)
            {
                if (_scanner.buffer.remaining < 6) break;

                _scanner.buffer.position += 2;
                var attrLength = _scanner.buffer.read_u32_be();

                stats.total_method_attribute_bytes += attrLength;

                if (_scanner.buffer.remaining >= (int)attrLength) _scanner.buffer.position += (int)attrLength;
            }
        }

        if (_scanner.buffer.remaining < 2) return stats;

        var classAttrCount = _scanner.buffer.read_u16_be();
        stats.class_attribute_count = classAttrCount;

        for (var i = 0; i < classAttrCount; i++)
        {
            if (_scanner.buffer.remaining < 6) break;

            _scanner.buffer.position += 2;
            var attrLength = _scanner.buffer.read_u32_be();

            if (_scanner.buffer.remaining >= (int)attrLength) _scanner.buffer.position += (int)attrLength;
        }

        return stats;
    }

    /// <summary>
    ///     快速判断数据是否为有效的JVM ClassFile的
    /// </summary>
    public bool is_jvm_class()
    {
        if (_scanner.length < 4) return false;

        if (!_scanner.match_magic(JvmConstants.magic_big_endian)) return false;

        try
        {
            _scanner.consume_magic(JvmConstants.magic_big_endian);

            if (_scanner.buffer.remaining < 4) return false;

            _scanner.buffer.read_u16_be();
            _scanner.buffer.read_u16_be();

            if (_scanner.buffer.remaining < 2) return false;

            var constantPoolCount = _scanner.buffer.read_u16_be();

            return constantPoolCount > 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
///     JVM ClassFile 扫描头部信息的
/// </summary>
public sealed class JvmScanHeader
{
    /// <summary>
    ///     次版本号的
    /// </summary>
    public ushort minor_version { get; init; }

    /// <summary>
    ///     主版本号的
    /// </summary>
    public ushort major_version { get; init; }

    /// <summary>
    ///     常量池条目数的
    /// </summary>
    public ushort constant_pool_count { get; set; }

    /// <summary>
    ///     类访问标志的
    /// </summary>
    public ushort access_flags { get; set; }

    /// <summary>
    ///     本类常量池索引的
    /// </summary>
    public ushort this_class_index { get; set; }

    /// <summary>
    ///     父类常量池索引的
    /// </summary>
    public ushort super_class_index { get; set; }

    /// <summary>
    ///     Java 版本友好名称的
    /// </summary>
    public string java_version => major_version switch
    {
        45 => "Java 1.1",
        46 => "Java 1.2",
        47 => "Java 1.3",
        48 => "Java 1.4",
        49 => "Java 5",
        50 => "Java 6",
        51 => "Java 7",
        52 => "Java 8",
        53 => "Java 9",
        54 => "Java 10",
        55 => "Java 11",
        56 => "Java 12",
        57 => "Java 13",
        58 => "Java 14",
        59 => "Java 15",
        60 => "Java 16",
        61 => "Java 17",
        62 => "Java 18",
        63 => "Java 19",
        64 => "Java 20",
        65 => "Java 21",
        66 => "Java 22",
        67 => "Java 23",
        68 => "Java 24",
        _ => major_version >= 45 ? $"Java {major_version - 44}" : $"未知 ({major_version})"
    };

    /// <summary>
    ///     类是否为接口的
    /// </summary>
    public bool is_interface => (access_flags & 0x0200) != 0;

    /// <summary>
    ///     类是否为抽象类的
    /// </summary>
    public bool is_abstract => (access_flags & 0x0400) != 0;

    /// <summary>
    ///     类是否为注解类型的
    /// </summary>
    public bool is_annotation => (access_flags & 0x2000) != 0;

    /// <summary>
    ///     类是否为枚举的
    /// </summary>
    public bool is_enum => (access_flags & 0x4000) != 0;

    /// <summary>
    ///     类是否为模块信息的
    /// </summary>
    public bool is_module => (access_flags & 0x8000) != 0;

    /// <summary>
    ///     类类型描述的
    /// </summary>
    public string class_kind
    {
        get
        {
            if (is_annotation) return "@interface";

            if (is_interface) return "interface";

            if (is_enum) return "enum";

            if (is_module) return "module-info";

            return "class";
        }
    }

    /// <summary>
    ///     是否为抽象类型（含接口）的
    /// </summary>
    public bool is_abstract_type => is_abstract || is_interface;
}

/// <summary>
///     JVM ClassFile 统计信息的
/// </summary>
public sealed class JvmStatistics
{
    /// <summary>
    ///     次版本号的
    /// </summary>
    public ushort minor_version { get; set; }

    /// <summary>
    ///     主版本号的
    /// </summary>
    public ushort major_version { get; set; }

    /// <summary>
    ///     常量池条目总数的
    /// </summary>
    public ushort constant_pool_count { get; set; }

    /// <summary>
    ///     UTF-8 常量数量的
    /// </summary>
    public int utf8_constant_count { get; set; }

    /// <summary>
    ///     整数常量数量的
    /// </summary>
    public int integer_constant_count { get; set; }

    /// <summary>
    ///     浮点数常量数量的
    /// </summary>
    public int float_constant_count { get; set; }

    /// <summary>
    ///     长整数常量数量的
    /// </summary>
    public int long_constant_count { get; set; }

    /// <summary>
    ///     双精度浮点数常量数量的
    /// </summary>
    public int double_constant_count { get; set; }

    /// <summary>
    ///     类引用常量数量的
    /// </summary>
    public int class_constant_count { get; set; }

    /// <summary>
    ///     字符串常量数量的
    /// </summary>
    public int string_constant_count { get; set; }

    /// <summary>
    ///     字段引用常量数量的
    /// </summary>
    public int fieldref_constant_count { get; set; }

    /// <summary>
    ///     方法引用常量数量的
    /// </summary>
    public int methodref_constant_count { get; set; }

    /// <summary>
    ///     接口方法引用常量数量的
    /// </summary>
    public int interface_methodref_constant_count { get; set; }

    /// <summary>
    ///     名称和类型常量数量的
    /// </summary>
    public int name_and_type_constant_count { get; set; }

    /// <summary>
    ///     方法句柄常量数量的
    /// </summary>
    public int method_handle_constant_count { get; set; }

    /// <summary>
    ///     方法类型常量数量的
    /// </summary>
    public int method_type_constant_count { get; set; }

    /// <summary>
    ///     动态调用常量数量的
    /// </summary>
    public int invoke_dynamic_constant_count { get; set; }

    /// <summary>
    ///     动态常量数量的
    /// </summary>
    public int dynamic_constant_count { get; set; }

    /// <summary>
    ///     模块常量数量的
    /// </summary>
    public int module_constant_count { get; set; }

    /// <summary>
    ///     包常量数量的
    /// </summary>
    public int package_constant_count { get; set; }

    /// <summary>
    ///     类访问标志的
    /// </summary>
    public ushort access_flags { get; set; }

    /// <summary>
    ///     接口数量的
    /// </summary>
    public ushort interface_count { get; set; }

    /// <summary>
    ///     字段数量的
    /// </summary>
    public ushort field_count { get; set; }

    /// <summary>
    ///     方法数量的
    /// </summary>
    public ushort method_count { get; set; }

    /// <summary>
    ///     类级属性数量的
    /// </summary>
    public ushort class_attribute_count { get; set; }

    /// <summary>
    ///     方法属性总字节数（含 Code 属性等）的
    /// </summary>
    public uint total_method_attribute_bytes { get; set; }

    /// <summary>
    ///     Java 版本友好名称的
    /// </summary>
    public string java_version => major_version switch
    {
        45 => "Java 1.1",
        46 => "Java 1.2",
        47 => "Java 1.3",
        48 => "Java 1.4",
        49 => "Java 5",
        50 => "Java 6",
        51 => "Java 7",
        52 => "Java 8",
        53 => "Java 9",
        54 => "Java 10",
        55 => "Java 11",
        56 => "Java 12",
        57 => "Java 13",
        58 => "Java 14",
        59 => "Java 15",
        60 => "Java 16",
        61 => "Java 17",
        62 => "Java 18",
        63 => "Java 19",
        64 => "Java 20",
        65 => "Java 21",
        66 => "Java 22",
        67 => "Java 23",
        68 => "Java 24",
        _ => major_version >= 45 ? $"Java {major_version - 44}" : $"未知 ({major_version})"
    };
}