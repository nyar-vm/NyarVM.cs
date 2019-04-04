namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 指令头码。
///     它只表示编码层中的头码字段，不等同于解码后的完整指令对象。
/// </summary>
public enum NyarHeadCode : byte
{
    #region 控制流

    /// <summary>
    ///     无操作
    /// </summary>
    nop = 0x00,

    /// <summary>
    ///     无条件跳转
    /// </summary>
    jump = 0x01,

    /// <summary>
    ///     条件跳转 (真)
    /// </summary>
    jump_if_true = 0x02,

    /// <summary>
    ///     条件跳转 (假)
    /// </summary>
    jump_if_false = 0x03,

    /// <summary>
    ///     函数调用
    /// </summary>
    call = 0x04,

    /// <summary>
    ///     返回
    /// </summary>
    @return = 0x05,

    /// <summary>
    ///     尾调用
    /// </summary>
    tail_call = 0x06,

    /// <summary>
    ///     异常抛出
    /// </summary>
    @throw = 0x07,

    /// <summary>
    ///     异常捕获
    /// </summary>
    @catch = 0x08,

    /// <summary>
    ///     异常保护块（对应 WASM try_table）
    /// </summary>
    try_block = 0x09,

    /// <summary>
    ///     异步让出
    /// </summary>
    yield = 0x0A,

    /// <summary>
    ///     异步恢复
    /// </summary>
    resume = 0x0B,

    /// <summary>
    ///     效应处理（已废弃，请使用 <see cref="perform_effect" />）
    /// </summary>
    effect_handle = 0x0C,

    /// <summary>
    ///     触发代数效应（raise），捕获续延并调用 handler
    /// </summary>
    perform_effect = 0x0D,

    /// <summary>
    ///     重新抛出异常（对应 WASM throw_ref）
    /// </summary>
    rethrow = 0x0E,

    /// <summary>
    ///     静态分派调用（编译期确定目标，无间接开销）
    /// </summary>
    call_static = 0x0F,

    #endregion

    #region 效应处理

    /// <summary>
    ///     进入效应处理器作用域，将效应处理器推入栈
    /// </summary>
    enter_effect_handler = 0x14,

    /// <summary>
    ///     退出效应处理器作用域，将效应处理器从栈弹出
    /// </summary>
    exit_effect_handler = 0x15,

    /// <summary>
    ///     进入 try 效应捕获块，将指定效应类型注册为捕获目标
    /// </summary>
    enter_try = 0x16,

    /// <summary>
    ///     退出 try 效应捕获块，卸载效应捕获
    /// </summary>
    exit_try = 0x17,

    #endregion

    #region 栈操作

    /// <summary>
    ///     压入常量
    /// </summary>
    @const = 0x10,

    /// <summary>
    ///     弹出栈顶
    /// </summary>
    pop = 0x11,

    /// <summary>
    ///     复制栈顶
    /// </summary>
    dup = 0x12,

    /// <summary>
    ///     交换栈顶两个元素
    /// </summary>
    swap = 0x13,

    /// <summary>
    ///     FFI 调用应通过 std 适配器声明（[vm]/[jvm]/[clr] 属性），禁止直接生成此操作码
    /// </summary>
    [Obsolete("FFI 调用应通过 std 适配器声明（[vm]/[jvm]/[clr] 属性），禁止直接生成此操作码")]
    builtin_call = 0x19,

    #endregion

    #region 局部变量

    /// <summary>
    ///     加载局部变量
    /// </summary>
    load_local = 0x20,

    /// <summary>
    ///     存储局部变量
    /// </summary>
    store_local = 0x21,

    /// <summary>
    ///     加载参数
    /// </summary>
    load_arg = 0x22,

    /// <summary>
    ///     加载全局变量
    /// </summary>
    load_global = 0x23,

    /// <summary>
    ///     存储全局变量
    /// </summary>
    store_global = 0x24,

    /// <summary>
    ///     存储参数
    /// </summary>
    store_arg = 0x25,

    #endregion

    #region i32 操作

    /// <summary>
    ///     i32 加法
    /// </summary>
    i32_add = 0x30,

    /// <summary>
    ///     i32 减法
    /// </summary>
    i32_sub = 0x31,

    /// <summary>
    ///     i32 乘法
    /// </summary>
    i32_mul = 0x32,

    /// <summary>
    ///     i32 有符号除法
    /// </summary>
    i32_div_s = 0x33,

    /// <summary>
    ///     i32 无符号除法
    /// </summary>
    i32_div_u = 0x34,

    /// <summary>
    ///     i32 有符号取余
    /// </summary>
    i32_rem_s = 0x35,

    /// <summary>
    ///     i32 无符号取余
    /// </summary>
    i32_rem_u = 0x36,

    /// <summary>
    ///     i32 取反
    /// </summary>
    i32_neg = 0x37,

    /// <summary>
    ///     i32 按位与
    /// </summary>
    i32_and = 0x38,

    /// <summary>
    ///     i32 按位或
    /// </summary>
    i32_or = 0x39,

    /// <summary>
    ///     i32 按位异或
    /// </summary>
    i32_xor = 0x3A,

    /// <summary>
    ///     i32 左移
    /// </summary>
    i32_shl = 0x3B,

    /// <summary>
    ///     i32 有符号右移
    /// </summary>
    i32_shr_s = 0x3C,

    /// <summary>
    ///     i32 无符号右移
    /// </summary>
    i32_shr_u = 0x3D,

    /// <summary>
    ///     i32 按位取反
    /// </summary>
    i32_not = 0x3E,

    #endregion

    #region i32 比较

    /// <summary>
    ///     i32 相等
    /// </summary>
    i32_eq = 0x40,

    /// <summary>
    ///     i32 不等
    /// </summary>
    i32_ne = 0x41,

    /// <summary>
    ///     i32 有符号小于
    /// </summary>
    i32_lt_s = 0x42,

    /// <summary>
    ///     i32 无符号小于
    /// </summary>
    i32_lt_u = 0x43,

    /// <summary>
    ///     i32 有符号小于等于
    /// </summary>
    i32_le_s = 0x44,

    /// <summary>
    ///     i32 无符号小于等于
    /// </summary>
    i32_le_u = 0x45,

    /// <summary>
    ///     i32 有符号大于
    /// </summary>
    i32_gt_s = 0x46,

    /// <summary>
    ///     i32 无符号大于
    /// </summary>
    i32_gt_u = 0x47,

    /// <summary>
    ///     i32 有符号大于等于
    /// </summary>
    i32_ge_s = 0x48,

    /// <summary>
    ///     i32 无符号大于等于
    /// </summary>
    i32_ge_u = 0x49,

    /// <summary>
    ///     将栈上的 any（Object）拆箱为 i32（int）。
    ///     用于 LIR 比较码选择为 i32 但实际值为 Object 的场景。
    ///     JVM 后端发射：checkcast Integer; invokevirtual Integer.intValue()
    /// </summary>
    any_to_i32 = 0x4A,

    /// <summary>
    ///     将栈上的 any（Object）转换为 utf8（String）。
    ///     用于 LIR 中 any 类型值赋值给 utf8 类型变量的场景。
    ///     JVM 后端发射：checkcast java/lang/String
    /// </summary>
    any_to_utf8 = 0x4B,

    #endregion

    #region i64 操作

    /// <summary>
    ///     i64 加法
    /// </summary>
    i64_add = 0x50,

    /// <summary>
    ///     i64 减法
    /// </summary>
    i64_sub = 0x51,

    /// <summary>
    ///     i64 乘法
    /// </summary>
    i64_mul = 0x52,

    /// <summary>
    ///     i64 有符号除法
    /// </summary>
    i64_div_s = 0x53,

    /// <summary>
    ///     i64 无符号除法
    /// </summary>
    i64_div_u = 0x54,

    /// <summary>
    ///     i64 取反
    /// </summary>
    i64_neg = 0x55,

    /// <summary>
    ///     i64 有符号取余
    /// </summary>
    i64_rem_s = 0x56,

    /// <summary>
    ///     i64 无符号取余
    /// </summary>
    i64_rem_u = 0x57,

    /// <summary>
    ///     i64 按位与
    /// </summary>
    i64_and = 0x58,

    /// <summary>
    ///     i64 按位或
    /// </summary>
    i64_or = 0x59,

    /// <summary>
    ///     i64 按位异或
    /// </summary>
    i64_xor = 0x5A,

    /// <summary>
    ///     i64 左移
    /// </summary>
    i64_shl = 0x5B,

    /// <summary>
    ///     i64 有符号右移
    /// </summary>
    i64_shr_s = 0x5C,

    /// <summary>
    ///     i64 无符号右移
    /// </summary>
    i64_shr_u = 0x5D,

    /// <summary>
    ///     i64 按位取反
    /// </summary>
    i64_not = 0x5E,

    #endregion

    #region i64 比较

    /// <summary>
    ///     i64 相等
    /// </summary>
    i64_eq = 0x5F,

    /// <summary>
    ///     i64 不等
    /// </summary>
    i64_ne = 0x65,

    /// <summary>
    ///     i64 有符号小于
    /// </summary>
    i64_lt_s = 0x66,

    /// <summary>
    ///     i64 有符号小于等于
    /// </summary>
    i64_le_s = 0x67,

    /// <summary>
    ///     i64 有符号大于
    /// </summary>
    i64_gt_s = 0x68,

    /// <summary>
    ///     i64 有符号大于等于
    /// </summary>
    i64_ge_s = 0x69,

    /// <summary>
    ///     引用相等（用于 `null` / 对象引用语义）
    /// </summary>
    ref_eq = 0x6A,

    /// <summary>
    ///     引用不等（用于 `null` / 对象引用语义）
    /// </summary>
    ref_ne = 0x6B,

    /// <summary>
    ///     i64 无符号小于
    /// </summary>
    i64_lt_u = 0x6C,

    /// <summary>
    ///     i64 无符号小于等于
    /// </summary>
    i64_le_u = 0x6D,

    /// <summary>
    ///     i64 无符号大于
    /// </summary>
    i64_gt_u = 0x6E,

    /// <summary>
    ///     i64 无符号大于等于
    /// </summary>
    i64_ge_u = 0x6F,

    #endregion

    #region f32 操作

    /// <summary>
    ///     f32 加法
    /// </summary>
    f32_add = 0x60,

    /// <summary>
    ///     f32 减法
    /// </summary>
    f32_sub = 0x61,

    /// <summary>
    ///     f32 乘法
    /// </summary>
    f32_mul = 0x62,

    /// <summary>
    ///     f32 除法
    /// </summary>
    f32_div = 0x63,

    /// <summary>
    ///     f32 取反
    /// </summary>
    f32_neg = 0x64,

    #endregion

    #region f64 操作

    /// <summary>
    ///     f64 加法
    /// </summary>
    f64_add = 0x70,

    /// <summary>
    ///     f64 减法
    /// </summary>
    f64_sub = 0x71,

    /// <summary>
    ///     f64 乘法
    /// </summary>
    f64_mul = 0x72,

    /// <summary>
    ///     f64 除法
    /// </summary>
    f64_div = 0x73,

    /// <summary>
    ///     f64 取反
    /// </summary>
    f64_neg = 0x74,

    /// <summary>
    ///     f64 平方根
    /// </summary>
    f64_sqrt = 0x75,

    /// <summary>
    ///     f64 相等比较
    /// </summary>
    f64_eq = 0x76,

    /// <summary>
    ///     f64 不等比较
    /// </summary>
    f64_ne = 0x77,

    /// <summary>
    ///     f64 小于比较
    /// </summary>
    f64_lt = 0x78,

    /// <summary>
    ///     f64 小于等于比较
    /// </summary>
    f64_le = 0x79,

    /// <summary>
    ///     f64 大于比较
    /// </summary>
    f64_gt = 0x7A,

    /// <summary>
    ///     f64 大于等于比较
    /// </summary>
    f64_ge = 0x7B,

    #endregion

    #region 类型转换

    /// <summary>
    ///     i32 扩展到 i64 (有符号)
    /// </summary>
    i32_extend_i64_s = 0x80,

    /// <summary>
    ///     i32 扩展到 i64 (无符号)
    /// </summary>
    i32_extend_i64_u = 0x81,

    /// <summary>
    ///     i64 截断到 i32 (有符号)
    /// </summary>
    i64_trunc_i32_s = 0x82,

    /// <summary>
    ///     i64 截断到 i32 (无符号)
    /// </summary>
    i64_trunc_i32_u = 0x83,

    /// <summary>
    ///     i32 转换到 f32 (有符号)
    /// </summary>
    i32_to_f32_s = 0x84,

    /// <summary>
    ///     i32 转换到 f64 (有符号)
    /// </summary>
    i32_to_f64_s = 0x85,

    /// <summary>
    ///     i64 转换到 f64 (有符号)
    /// </summary>
    i64_to_f64 = 0x86,

    /// <summary>
    ///     f64 转换到 i32 (有符号)
    /// </summary>
    f64_to_i32 = 0x87,

    /// <summary>
    ///     f64 转换到 i64 (有符号)
    /// </summary>
    f64_to_i64 = 0x88,

    #endregion

    #region 内存操作

    /// <summary>
    ///     分配内存
    /// </summary>
    alloc = 0x90,

    /// <summary>
    ///     释放内存
    /// </summary>
    free = 0x91,

    /// <summary>
    ///     加载 i32
    /// </summary>
    i32_load = 0x92,

    /// <summary>
    ///     存储 i32
    /// </summary>
    i32_store = 0x93,

    /// <summary>
    ///     加载 i64
    /// </summary>
    i64_load = 0x94,

    /// <summary>
    ///     存储 i64
    /// </summary>
    i64_store = 0x95,

    #endregion

    #region 对象操作

    /// <summary>
    ///     创建对象
    /// </summary>
    new_object = 0xA0,

    /// <summary>
    ///     获取属性
    /// </summary>
    get_field = 0xA1,

    /// <summary>
    ///     设置属性
    /// </summary>
    set_field = 0xA2,

    /// <summary>
    ///     数组读取
    /// </summary>
    array_get = 0xA3,

    /// <summary>
    ///     数组写入
    /// </summary>
    array_set = 0xA4,

    /// <summary>
    ///     获取序数索引
    /// </summary>
    get_ordinal_index = 0xB8,

    /// <summary>
    ///     设置序数索引
    /// </summary>
    set_ordinal_index = 0xB9,

    /// <summary>
    ///     获取偏移索引
    /// </summary>
    get_offset_index = 0xBA,

    /// <summary>
    ///     设置偏移索引
    /// </summary>
    set_offset_index = 0xBB,

    /// <summary>
    ///     获取长度
    /// </summary>
    length = 0xA5,

    /// <summary>
    ///     创建闭包
    /// </summary>
    new_closure = 0xA6,

    /// <summary>
    ///     获取上值
    /// </summary>
    get_upvalue = 0xA7,

    /// <summary>
    ///     设置上值
    /// </summary>
    set_upvalue = 0xA8,

    /// <summary>
    ///     见证表分派调用（通过 Witness Table 间接调用，支持热更新）
    /// </summary>
    call_witness = 0xA9,

    /// <summary>
    ///     动态分派调用（运行时方法查找 + 内联缓存）
    /// </summary>
    call_dynamic = 0xAA,

    /// <summary>
    ///     静态字段访问（编译期确定偏移量）
    /// </summary>
    access_static = 0xAB,

    /// <summary>
    ///     见证表字段访问（通过 Witness Table 间接访问）
    /// </summary>
    access_witness = 0xAC,

    /// <summary>
    ///     动态字段访问（运行时名称查找 + 内联缓存）
    /// </summary>
    access_dynamic = 0xAD,

    /// <summary>
    ///     内联缓存更新（JIT 去虚拟化时使用）
    /// </summary>
    inline_cache_update = 0xAE,

    /// <summary>
    ///     字段写入（字段名由常量池索引指定）
    /// </summary>
    field_store = 0xAF,

    #endregion

    #region UTF-8 文本操作

    /// <summary>
    ///     UTF-8 文本拼接
    /// </summary>
    utf8_concat = 0xB0,

    /// <summary>
    ///     UTF-8 文本长度（字节）
    /// </summary>
    utf8_len_bytes = 0xB1,

    /// <summary>
    ///     UTF-8 文本长度（字符）
    /// </summary>
    utf8_len_chars = 0xB2,

    /// <summary>
    ///     UTF-8 文本子串
    /// </summary>
    utf8_substr = 0xB3,

    /// <summary>
    ///     UTF-8 文本相等比较
    /// </summary>
    utf8_eq = 0xB4,

    /// <summary>
    ///     UTF-8 文本不等比较
    /// </summary>
    utf8_ne = 0xB5,

    /// <summary>
    ///     字符串拼接（utf8_concat 的语义别名）
    /// </summary>
    string_concat = utf8_concat,

    /// <summary>
    ///     字符串长度（字节）（utf8_len_bytes 的语义别名）
    /// </summary>
    string_len_bytes = utf8_len_bytes,

    /// <summary>
    ///     字符串长度（字符）（utf8_len_chars 的语义别名）
    /// </summary>
    string_len_chars = utf8_len_chars,

    /// <summary>
    ///     字符串子串（utf8_substr 的语义别名）
    /// </summary>
    string_substr = utf8_substr,

    /// <summary>
    ///     字符串相等比较（utf8_eq 的语义别名）
    /// </summary>
    string_eq = utf8_eq,

    /// <summary>
    ///     字符串不等比较（utf8_ne 的语义别名）
    /// </summary>
    string_ne = utf8_ne,

    /// <summary>
    ///     索引写入（对象、索引、值从栈弹出）
    /// </summary>
    index_store = 0xB6,

    /// <summary>
    ///     数组追加元素（将值追加到列表末尾）
    /// </summary>
    array_push = 0xB7,

    #endregion

    #region BigInt 操作

    /// <summary>
    ///     BigInt 加法
    /// </summary>
    big_int_add = 0xC0,

    /// <summary>
    ///     BigInt 减法
    /// </summary>
    big_int_sub = 0xC1,

    /// <summary>
    ///     BigInt 乘法
    /// </summary>
    big_int_mul = 0xC2,

    #endregion

    #region FFI 操作

    /// <summary>
    ///     调用内置函数（通过 Intrinsic ID 索引）
    /// </summary>
    call_intrinsic = 0xD0,

    /// <summary>
    ///     调用原生函数（通过 FFI 函数名索引）
    /// </summary>
    call_native = 0xD1,

    /// <summary>
    ///     加载原生库（将库路径压入 FFI 管理器）
    /// </summary>
    load_native_lib = 0xD2,

    /// <summary>
    ///     获取原生函数指针（从已加载库中查找导出函数）
    /// </summary>
    get_native_func = 0xD3,

    #endregion

    #region SIMD 操作

    /// <summary>
    ///     SIMD 指令前缀的编码字段。
    ///     该一级 opcode 只表示“这是一条 SIMD 指令”，完整指令语义仍需结合第一个操作数中的二级指令编码解码。
    /// </summary>
    simd = 0xE0

    #endregion
}
