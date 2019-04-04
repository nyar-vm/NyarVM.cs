namespace Std.Data.Binary.Jvm.Data;

/// <summary>
///     JVM ClassFile 数据
/// </summary>
public sealed class JvmClassFileData
{
    /// <summary>
    ///     魔数的xCAFEBABE的
    /// </summary>
    public uint magic { get; init; }

    /// <summary>
    ///     主次版本的
    /// </summary>
    public ushort minor_version { get; init; }

    public ushort major_version { get; init; }

    /// <summary>
    ///     常量的
    /// </summary>
    public IReadOnlyList<JvmConstant> constant_pool { get; init; }

    /// <summary>
    ///     访问标志
    /// </summary>
    public ushort access_flags { get; init; }

    /// <summary>
    ///     本类索引
    /// </summary>
    public ushort this_class { get; init; }

    /// <summary>
    ///     父类索引
    /// </summary>
    public ushort super_class { get; init; }

    /// <summary>
    ///     接口索引的
    /// </summary>
    public IReadOnlyList<ushort> interfaces { get; init; }

    /// <summary>
    ///     字段的
    /// </summary>
    public IReadOnlyList<JvmFieldInfo> fields { get; init; }

    /// <summary>
    ///     方法的
    /// </summary>
    public IReadOnlyList<JvmMethodInfo> methods { get; init; }

    /// <summary>
    ///     属性表
    /// </summary>
    public IReadOnlyList<JvmAttributeInfo> attributes { get; init; }
}

/// <summary>
///     常量池项基类
/// </summary>
public abstract class JvmConstant
{
    /// <summary>
    ///     常量类型
    /// </summary>
    public abstract JvmConstantKind kind { get; }
}

/// <summary>
///     常量类型
/// </summary>
public enum JvmConstantKind : byte
{
    padding = 0,
    utf8 = 1,
    integer = 3,
    @float = 4,
    @long = 5,
    @double = 6,
    @class = 7,
    @string = 8,
    fieldref = 9,
    methodref = 10,
    interface_methodref = 11,
    name_and_type = 12,
    method_handle = 15,
    method_type = 16,
    dynamic = 17,
    invoke_dynamic = 18,
    module = 19,
    package = 20
}

/// <summary>
///     UTF-8 常量
/// </summary>
public sealed class JvmConstantUtf8 : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.utf8;
    public string value { get; init; }
}

/// <summary>
///     整数常量
/// </summary>
public sealed class JvmConstantInteger : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.integer;
    public int value { get; init; }
}

/// <summary>
///     浮点数常的
/// </summary>
public sealed class JvmConstantFloat : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.@float;
    public float value { get; init; }
}

/// <summary>
///     长整数常的
/// </summary>
public sealed class JvmConstantLong : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.@long;
    public long value { get; init; }
}

/// <summary>
///     双精度浮点数常量
/// </summary>
public sealed class JvmConstantDouble : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.@double;
    public double value { get; init; }
}

/// <summary>
///     `Long` / `Double` 常量后的占位槽。
///     仅用于保持常量池索引与 JVM 双槽规则一致，不会被编码为真实常量项。
/// </summary>
public sealed class JvmConstantPadding : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.padding;
}

/// <summary>
///     类常的
/// </summary>
public sealed class JvmConstantClass : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.@class;
    public ushort name_index { get; init; }
}

/// <summary>
///     字符串常的
/// </summary>
public sealed class JvmConstantString : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.@string;
    public ushort string_index { get; init; }
}

/// <summary>
///     字段引用常量
/// </summary>
public sealed class JvmConstantFieldref : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.fieldref;
    public ushort class_index { get; init; }
    public ushort name_and_type_index { get; init; }
}

/// <summary>
///     方法引用常量
/// </summary>
public sealed class JvmConstantMethodref : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.methodref;
    public ushort class_index { get; init; }
    public ushort name_and_type_index { get; init; }
}

/// <summary>
///     接口方法引用常量
/// </summary>
public sealed class JvmConstantInterfaceMethodref : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.interface_methodref;
    public ushort class_index { get; init; }
    public ushort name_and_type_index { get; init; }
}

/// <summary>
///     名称和类型常的
/// </summary>
public sealed class JvmConstantNameAndType : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.name_and_type;
    public ushort name_index { get; init; }
    public ushort descriptor_index { get; init; }
}

/// <summary>
///     方法句柄常量
/// </summary>
public sealed class JvmConstantMethodHandle : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.method_handle;
    public byte reference_kind { get; init; }
    public ushort reference_index { get; init; }
}

/// <summary>
///     方法类型常量
/// </summary>
public sealed class JvmConstantMethodType : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.method_type;
    public ushort descriptor_index { get; init; }
}

/// <summary>
///     动态调用常的
/// </summary>
public sealed class JvmConstantInvokeDynamic : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.invoke_dynamic;
    public ushort bootstrap_method_attr_index { get; init; }
    public ushort name_and_type_index { get; init; }
}

/// <summary>
///     字段信息
/// </summary>
public sealed class JvmFieldInfo
{
    public ushort access_flags { get; init; }
    public ushort name_index { get; init; }
    public ushort descriptor_index { get; init; }
    public IReadOnlyList<JvmAttributeInfo> attributes { get; init; }
}

/// <summary>
///     方法信息
/// </summary>
public sealed class JvmMethodInfo
{
    public ushort access_flags { get; init; }
    public ushort name_index { get; init; }
    public ushort descriptor_index { get; init; }
    public IReadOnlyList<JvmAttributeInfo> attributes { get; init; }
}

/// <summary>
///     属性信息基的
/// </summary>
public abstract class JvmAttributeInfo
{
    public ushort attribute_name_index { get; init; }
    public uint attribute_length { get; init; }
}

/// <summary>
///     代码属的
/// </summary>
public sealed class JvmCodeAttribute : JvmAttributeInfo
{
    public ushort max_stack { get; init; }
    public ushort max_locals { get; init; }
    public uint code_length { get; init; }
    public byte[] code { get; init; }
    public ushort exception_table_length { get; init; }
    public IReadOnlyList<JvmExceptionTableEntry> exception_table { get; init; }
    public ushort attributes_count { get; init; }
    public IReadOnlyList<JvmAttributeInfo> attributes { get; init; }
}

/// <summary>
///     异常表项
/// </summary>
public sealed class JvmExceptionTableEntry
{
    public ushort start_pc { get; init; }
    public ushort end_pc { get; init; }
    public ushort handler_pc { get; init; }
    public ushort catch_type { get; init; }
}

/// <summary>
///     行号表属的
/// </summary>
public sealed class JvmLineNumberTableAttribute : JvmAttributeInfo
{
    public ushort line_number_table_length { get; init; }
    public IReadOnlyList<JvmLineNumberEntry> line_number_table { get; init; }
}

/// <summary>
///     行号表项
/// </summary>
public sealed class JvmLineNumberEntry
{
    public ushort start_pc { get; init; }
    public ushort line_number { get; init; }
}

/// <summary>
///     局部变量表属的
/// </summary>
public sealed class JvmLocalVariableTableAttribute : JvmAttributeInfo
{
    public ushort local_variable_table_length { get; init; }
    public IReadOnlyList<JvmLocalVariableEntry> local_variable_table { get; init; }
}

/// <summary>
///     局部变量表的
/// </summary>
public sealed class JvmLocalVariableEntry
{
    public ushort start_pc { get; init; }
    public ushort length { get; init; }
    public ushort name_index { get; init; }
    public ushort descriptor_index { get; init; }
    public ushort index { get; init; }
}

/// <summary>
///     常量值属的
/// </summary>
public sealed class JvmConstantValueAttribute : JvmAttributeInfo
{
    public ushort constant_value_index { get; init; }
}

/// <summary>
///     源文件属的
/// </summary>
public sealed class JvmSourceFileAttribute : JvmAttributeInfo
{
    public ushort source_file_index { get; init; }
}

/// <summary>
///     内部类属的
/// </summary>
public sealed class JvmInnerClassesAttribute : JvmAttributeInfo
{
    public ushort number_of_classes { get; init; }
    public IReadOnlyList<JvmInnerClassInfo> classes { get; init; }
}

/// <summary>
///     内部类信的
/// </summary>
public sealed class JvmInnerClassInfo
{
    public ushort inner_class_info_index { get; init; }
    public ushort outer_class_info_index { get; init; }
    public ushort inner_name_index { get; init; }
    public ushort inner_class_access_flags { get; init; }
}

/// <summary>
///     引导方法表属的
/// </summary>
public sealed class JvmBootstrapMethodsAttribute : JvmAttributeInfo
{
    public ushort num_bootstrap_methods { get; init; }
    public IReadOnlyList<JvmBootstrapMethod> bootstrap_methods { get; init; }
}

/// <summary>
///     引导方法
/// </summary>
public sealed class JvmBootstrapMethod
{
    public ushort bootstrap_method_ref { get; init; }
    public ushort num_bootstrap_arguments { get; init; }
    public IReadOnlyList<ushort> bootstrap_arguments { get; init; }
}

/// <summary>
///     JVM 操作的
/// </summary>
public enum JvmOpcode : byte
{
    nop = 0,
    aconst_null = 1,
    iconst_m1 = 2,
    iconst0 = 3,
    iconst1 = 4,
    iconst2 = 5,
    iconst3 = 6,
    iconst4 = 7,
    iconst5 = 8,
    lconst0 = 9,
    lconst1 = 10,
    fconst0 = 11,
    fconst1 = 12,
    fconst2 = 13,
    dconst0 = 14,
    dconst1 = 15,
    bipush = 16,
    sipush = 17,
    ldc = 18,
    ldc_w = 19,
    ldc2_w = 20,
    iload = 21,
    lload = 22,
    fload = 23,
    dload = 24,
    aload = 25,
    iload0 = 26,
    iload1 = 27,
    iload2 = 28,
    iload3 = 29,
    lload0 = 30,
    lload1 = 31,
    lload2 = 32,
    lload3 = 33,
    fload0 = 34,
    fload1 = 35,
    fload2 = 36,
    fload3 = 37,
    dload0 = 38,
    dload1 = 39,
    dload2 = 40,
    dload3 = 41,
    aload0 = 42,
    aload1 = 43,
    aload2 = 44,
    aload3 = 45,
    iaload = 46,
    laload = 47,
    faload = 48,
    daload = 49,
    aaload = 50,
    baload = 51,
    caload = 52,
    saload = 53,
    istore = 54,
    lstore = 55,
    fstore = 56,
    dstore = 57,
    astore = 58,
    istore0 = 59,
    istore1 = 60,
    istore2 = 61,
    istore3 = 62,
    lstore0 = 63,
    lstore1 = 64,
    lstore2 = 65,
    lstore3 = 66,
    fstore0 = 67,
    fstore1 = 68,
    fstore2 = 69,
    fstore3 = 70,
    dstore0 = 71,
    dstore1 = 72,
    dstore2 = 73,
    dstore3 = 74,
    astore0 = 75,
    astore1 = 76,
    astore2 = 77,
    astore3 = 78,
    iastore = 79,
    lastore = 80,
    fastore = 81,
    dastore = 82,
    aastore = 83,
    bastore = 84,
    castore = 85,
    sastore = 86,
    pop = 87,
    pop2 = 88,
    dup = 89,
    dup_x1 = 90,
    dup_x2 = 91,
    dup2 = 92,
    dup2_x1 = 93,
    dup2_x2 = 94,
    swap = 95,
    iadd = 96,
    ladd = 97,
    fadd = 98,
    dadd = 99,
    isub = 100,
    lsub = 101,
    fsub = 102,
    dsub = 103,
    imul = 104,
    lmul = 105,
    fmul = 106,
    dmul = 107,
    idiv = 108,
    ldiv = 109,
    fdiv = 110,
    ddiv = 111,
    irem = 112,
    lrem = 113,
    frem = 114,
    drem = 115,
    ineg = 116,
    lneg = 117,
    fneg = 118,
    dneg = 119,
    ishl = 120,
    lshl = 121,
    ishr = 122,
    lshr = 123,
    iushr = 124,
    lushr = 125,
    iand = 126,
    land = 127,
    ior = 128,
    lor = 129,
    ixor = 130,
    lxor = 131,
    iinc = 132,
    i2_l = 133,
    i2_f = 134,
    i2_d = 135,
    l2_i = 136,
    l2_f = 137,
    l2_d = 138,
    f2_i = 139,
    f2_l = 140,
    f2_d = 141,
    d2_i = 142,
    d2_l = 143,
    d2_f = 144,
    i2_b = 145,
    i2_c = 146,
    i2_s = 147,
    i2l = i2_l,
    i2d = i2_d,
    l2i = l2_i,
    l2d = l2_d,
    d2i = d2_i,
    d2l = d2_l,
    lcmp = 148,
    fcmpl = 149,
    fcmpg = 150,
    dcmpl = 151,
    dcmpg = 152,
    ifeq = 153,
    ifne = 154,
    iflt = 155,
    ifge = 156,
    ifgt = 157,
    ifle = 158,
    ificmpeq = 159,
    ificmpne = 160,
    ificmplt = 161,
    ificmpge = 162,
    ificmpgt = 163,
    ificmple = 164,
    ifacmpeq = 165,
    ifacmpne = 166,
    if_icmpeq = ificmpeq,
    if_icmpne = ificmpne,
    if_icmplt = ificmplt,
    if_icmpge = ificmpge,
    if_icmpgt = ificmpgt,
    if_icmple = ificmple,
    if_acmpeq = ifacmpeq,
    if_acmpne = ifacmpne,
    @goto = 167,
    jsr = 168,
    ret = 169,
    tableswitch = 170,
    lookupswitch = 171,
    ireturn = 172,
    lreturn = 173,
    freturn = 174,
    dreturn = 175,
    areturn = 176,
    @return = 177,
    getstatic = 178,
    putstatic = 179,
    getfield = 180,
    putfield = 181,
    invokevirtual = 182,
    invokespecial = 183,
    invokestatic = 184,
    invokeinterface = 185,
    invokedynamic = 186,
    @new = 187,
    newarray = 188,
    anewarray = 189,
    arraylength = 190,
    athrow = 191,
    checkcast = 192,
    instanceof = 193,
    monitorenter = 194,
    monitorexit = 195,
    wide = 196,
    multianewarray = 197,
    iconst_0 = iconst0,
    iconst_1 = iconst1,
    iconst_2 = iconst2,
    iconst_3 = iconst3,
    iconst_4 = iconst4,
    iconst_5 = iconst5,
    lconst_0 = lconst0,
    lconst_1 = lconst1,
    fconst_0 = fconst0,
    dconst_0 = dconst0,
    iload_0 = iload0,
    iload_1 = iload1,
    iload_2 = iload2,
    iload_3 = iload3,
    aload_0 = aload0,
    aload_1 = aload1,
    aload_2 = aload2,
    aload_3 = aload3,
    istore_0 = istore0,
    istore_1 = istore1,
    istore_2 = istore2,
    istore_3 = istore3,
    ifnull = 198,
    ifnonnull = 199,
    goto_w = 200,
    jsr_w = 201,

    /// <summary>
    ///     调试器断点保留指的
    /// </summary>
    breakpoint = 202,

    /// <summary>
    ///     实现相关指令 1
    /// </summary>
    impdep1 = 254,

    /// <summary>
    ///     实现相关指令 2
    /// </summary>
    impdep2 = 255
}

/// <summary>
///     JVM 的字段/方法访问标志
/// </summary>
[Flags]
public enum JvmAccessFlags : ushort
{
    @public = 0x0001,
    @private = 0x0002,
    @protected = 0x0004,
    @static = 0x0008,
    final = 0x0010,
    super = 0x0020,
    synchronized = 0x0020,
    @volatile = 0x0040,
    bridge = 0x0040,
    transient = 0x0080,
    varargs = 0x0080,
    native = 0x0100,
    @interface = 0x0200,
    @abstract = 0x0400,
    strict = 0x0800,
    synthetic = 0x1000,
    annotation = 0x2000,
    @enum = 0x4000,
    module = 0x8000
}

/// <summary>
///     CONSTANT_Dynamic 常量（JVM 11+的
/// </summary>
public sealed class JvmConstantDynamic : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.dynamic;
    public ushort bootstrap_method_attr_index { get; init; }
    public ushort name_and_type_index { get; init; }
}

/// <summary>
///     CONSTANT_Module 常量（JVM 9+的
/// </summary>
public sealed class JvmConstantModule : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.module;
    public ushort name_index { get; init; }
}

/// <summary>
///     CONSTANT_Package 常量（JVM 9+的
/// </summary>
public sealed class JvmConstantPackage : JvmConstant
{
    public override JvmConstantKind kind => JvmConstantKind.package;
    public ushort name_index { get; init; }
}

/// <summary>
///     方法句柄引用类型
/// </summary>
public enum JvmMethodHandleKind : byte
{
    get_field = 1,
    get_static = 2,
    put_field = 3,
    put_static = 4,
    invoke_virtual = 5,
    invoke_static = 6,
    invoke_special = 7,
    new_invoke_special = 8,
    invoke_interface = 9
}

/// <summary>
///     StackMapTable 属性（字节码验证必需的
/// </summary>
public sealed class JvmStackMapTableAttribute : JvmAttributeInfo
{
    public ushort number_of_entries { get; init; }
    public IReadOnlyList<JvmStackMapFrame> entries { get; init; }
}

/// <summary>
///     StackMapFrame 基类
/// </summary>
public abstract class JvmStackMapFrame
{
    /// <summary>
    ///     帧类型（0-255的
    /// </summary>
    public byte frame_type { get; init; }
}

/// <summary>
///     same_frame（frame_type 0-63的
/// </summary>
public sealed class JvmSameFrame : JvmStackMapFrame
{
}

/// <summary>
///     same_locals_1_stack_item_frame（frame_type 64-127的
/// </summary>
public sealed class JvmSameLocals1StackItemFrame : JvmStackMapFrame
{
    public IReadOnlyList<JvmVerificationTypeInfo> stack { get; init; }
}

/// <summary>
///     chop_frame（frame_type 248-250的
/// </summary>
public sealed class JvmChopFrame : JvmStackMapFrame
{
    public ushort offset_delta { get; init; }
}

/// <summary>
///     same_frame_extended（frame_type 251的
/// </summary>
public sealed class JvmSameFrameExtended : JvmStackMapFrame
{
    public ushort offset_delta { get; init; }
}

/// <summary>
///     append_frame（frame_type 252-254的
/// </summary>
public sealed class JvmAppendFrame : JvmStackMapFrame
{
    public ushort offset_delta { get; init; }
    public IReadOnlyList<JvmVerificationTypeInfo> locals { get; init; }
}

/// <summary>
///     full_frame（frame_type 255的
/// </summary>
public sealed class JvmFullFrame : JvmStackMapFrame
{
    public ushort offset_delta { get; init; }
    public ushort number_of_locals { get; init; }
    public IReadOnlyList<JvmVerificationTypeInfo> locals { get; init; }
    public ushort number_of_stack_items { get; init; }
    public IReadOnlyList<JvmVerificationTypeInfo> stack { get; init; }
}

/// <summary>
///     校验类型信息
/// </summary>
public sealed class JvmVerificationTypeInfo
{
    public byte tag { get; init; }
    public ushort? cpool_index { get; init; }
    public ushort? offset { get; init; }
}

/// <summary>
///     异常属的
/// </summary>
public sealed class JvmExceptionsAttribute : JvmAttributeInfo
{
    public ushort number_of_exceptions { get; init; }
    public IReadOnlyList<ushort> exception_index_table { get; init; }
}

/// <summary>
///     签名属性（泛型签名的
/// </summary>
public sealed class JvmSignatureAttribute : JvmAttributeInfo
{
    public ushort signature_index { get; init; }
}

/// <summary>
///     合成属性（编译器生成标记）
/// </summary>
public sealed class JvmSyntheticAttribute : JvmAttributeInfo
{
}

/// <summary>
///     废弃属的
/// </summary>
public sealed class JvmDeprecatedAttribute : JvmAttributeInfo
{
}

/// <summary>
///     外围方法属的
/// </summary>
public sealed class JvmEnclosingMethodAttribute : JvmAttributeInfo
{
    public ushort class_index { get; init; }
    public ushort method_index { get; init; }
}

/// <summary>
///     源文件调试扩展属的
/// </summary>
public sealed class JvmSourceDebugExtensionAttribute : JvmAttributeInfo
{
    public byte[] debug_extension { get; init; }
}

/// <summary>
///     方法参数属的
/// </summary>
public sealed class JvmMethodParametersAttribute : JvmAttributeInfo
{
    public byte parameter_count { get; init; }
    public IReadOnlyList<JvmMethodParameterInfo> parameters { get; init; }
}

/// <summary>
///     方法参数信息
/// </summary>
public sealed class JvmMethodParameterInfo
{
    public ushort name_index { get; init; }
    public ushort access_flags { get; init; }
}

/// <summary>
///     模块属性（JVM 9+的
/// </summary>
public sealed class JvmModuleAttribute : JvmAttributeInfo
{
    public ushort module_name_index { get; init; }
    public ushort module_flags { get; init; }
    public ushort module_version_index { get; init; }
    public IReadOnlyList<JvmModuleRequire> requires { get; init; }
    public IReadOnlyList<JvmModuleExport> exports { get; init; }
    public IReadOnlyList<JvmModuleOpen> opens { get; init; }
    public IReadOnlyList<ushort> uses_index { get; init; }
    public IReadOnlyList<JvmModuleProvide> provides { get; init; }
}

/// <summary>
///     模块 requires 的
/// </summary>
public sealed class JvmModuleRequire
{
    public ushort requires_index { get; init; }
    public ushort requires_flags { get; init; }
    public ushort? requires_version_index { get; init; }
}

/// <summary>
///     模块 exports 的
/// </summary>
public sealed class JvmModuleExport
{
    public ushort exports_index { get; init; }
    public ushort exports_flags { get; init; }
    public IReadOnlyList<ushort> exports_to_index { get; init; }
}

/// <summary>
///     模块 opens 的
/// </summary>
public sealed class JvmModuleOpen
{
    public ushort opens_index { get; init; }
    public ushort opens_flags { get; init; }
    public IReadOnlyList<ushort> opens_to_index { get; init; }
}

/// <summary>
///     模块 provides 的
/// </summary>
public sealed class JvmModuleProvide
{
    public ushort provides_index { get; init; }
    public IReadOnlyList<ushort> provides_with_index { get; init; }
}

/// <summary>
///     巢主属性（JVM 11+的
/// </summary>
public sealed class JvmNestHostAttribute : JvmAttributeInfo
{
    public ushort host_class_index { get; init; }
}

/// <summary>
///     巢成员属性（JVM 11+的
/// </summary>
public sealed class JvmNestMembersAttribute : JvmAttributeInfo
{
    public ushort number_of_classes { get; init; }
    public IReadOnlyList<ushort> class_indexes { get; init; }
}

/// <summary>
///     记录属性（JVM 16+的
/// </summary>
public sealed class JvmRecordAttribute : JvmAttributeInfo
{
    public ushort components_count { get; init; }
    public IReadOnlyList<JvmRecordComponentInfo> components { get; init; }
}

/// <summary>
///     记录组件信息
/// </summary>
public sealed class JvmRecordComponentInfo
{
    public ushort name_index { get; init; }
    public ushort descriptor_index { get; init; }
    public IReadOnlyList<JvmAttributeInfo> attributes { get; init; }
}

/// <summary>
///     允许的子类属性（JVM 17+的
/// </summary>
public sealed class JvmPermittedSubclassesAttribute : JvmAttributeInfo
{
    public ushort number_of_classes { get; init; }
    public IReadOnlyList<ushort> class_indexes { get; init; }
}

/// <summary>
///     运行时可见注解属的
/// </summary>
public sealed class JvmRuntimeVisibleAnnotationsAttribute : JvmAttributeInfo
{
    public ushort num_annotations { get; init; }
    public IReadOnlyList<JvmAnnotation> annotations { get; init; }
}

/// <summary>
///     运行时不可见注解属的
/// </summary>
public sealed class JvmRuntimeInvisibleAnnotationsAttribute : JvmAttributeInfo
{
    public ushort num_annotations { get; init; }
    public IReadOnlyList<JvmAnnotation> annotations { get; init; }
}

/// <summary>
///     运行时可见参数注解属的
/// </summary>
public sealed class JvmRuntimeVisibleParameterAnnotationsAttribute : JvmAttributeInfo
{
    public byte num_parameters { get; init; }
    public IReadOnlyList<JvmParameterAnnotations> parameter_annotations { get; init; }
}

/// <summary>
///     运行时不可见参数注解属的
/// </summary>
public sealed class JvmRuntimeInvisibleParameterAnnotationsAttribute : JvmAttributeInfo
{
    public byte num_parameters { get; init; }
    public IReadOnlyList<JvmParameterAnnotations> parameter_annotations { get; init; }
}

/// <summary>
///     参数注解列表
/// </summary>
public sealed class JvmParameterAnnotations
{
    public ushort num_annotations { get; init; }
    public IReadOnlyList<JvmAnnotation> annotations { get; init; }
}

/// <summary>
///     Java 注解
/// </summary>
public sealed class JvmAnnotation
{
    public ushort type_index { get; init; }
    public ushort num_element_value_pairs { get; init; }
    public IReadOnlyList<JvmElementValuePair> element_value_pairs { get; init; }
}

/// <summary>
///     注解元素-值对
/// </summary>
public sealed class JvmElementValuePair
{
    public ushort element_name_index { get; init; }
    public JvmElementValue value { get; init; }
}

/// <summary>
///     注解元素的
/// </summary>
public sealed class JvmElementValue
{
    public byte tag { get; init; }
    public ushort? const_value_index { get; init; }
    public ushort? type_name_index { get; init; }
    public ushort? class_info_index { get; init; }
    public JvmAnnotation? annotation_value { get; init; }
    public ushort? array_num_values { get; init; }
    public IReadOnlyList<JvmElementValue>? array_values { get; init; }
    public ushort? enum_const_name_index { get; init; }
}

/// <summary>
///     注解默认值属的
/// </summary>
public sealed class JvmAnnotationDefaultAttribute : JvmAttributeInfo
{
    public JvmElementValue default_value { get; init; }
}

/// <summary>
///     局部变量类型表属性（泛型方法调试信息的
/// </summary>
public sealed class JvmLocalVariableTypeTableAttribute : JvmAttributeInfo
{
    public ushort local_variable_type_table_length { get; init; }
    public IReadOnlyList<JvmLocalVariableTypeEntry> local_variable_type_table { get; init; }
}

/// <summary>
///     局部变量类型表的
/// </summary>
public sealed class JvmLocalVariableTypeEntry
{
    public ushort start_pc { get; init; }
    public ushort length { get; init; }
    public ushort name_index { get; init; }
    public ushort signature_index { get; init; }
    public ushort index { get; init; }
}

/// <summary>
///     原始二进制属性（用于保留未知属性格式的原始数据的
/// </summary>
public sealed class JvmRawAttribute : JvmAttributeInfo
{
    /// <summary>
    ///     属性名称（从常量池解析的
    /// </summary>
    public string name { get; init; }

    /// <summary>
    ///     属性的原始字节数据
    /// </summary>
    public byte[] raw_data { get; init; }
}
