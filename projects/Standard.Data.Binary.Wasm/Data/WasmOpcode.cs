namespace Std.Data.Binary.Wasm.Data;

/// <summary>
///     WebAssembly 操作码枚举的
///     覆盖 WASM Sonic.Core MVP、GC 提案前缀指令的SIMD 前缀指令的
///     多字节前缀指令的xFB/0xFC/0xFD/0xFE）用 ushort 高字节表达前缀的
/// </summary>
public enum WasmOpcode : ushort
{
    #region 控制流指的

    /// <summary>不可达指令，触发陷阱的/summary>
    unreachable = 0x00,

    /// <summary>空操作的/summary>
    nop = 0x01,

    /// <summary>块结构开始，可选块类型和返回值的/summary>
    block = 0x02,

    /// <summary>循环结构开始，可选块类型和返回值的/summary>
    loop = 0x03,

    /// <summary>条件分支开始，可选块类型和返回值的/summary>
    @if = 0x04,

    /// <summary>条件分支 else 子句的/summary>
    @else = 0x05,

    /// <summary>块结束的/summary>
    end = 0x0B,

    /// <summary>无条件跳转到指定标签的/summary>
    br = 0x0C,

    /// <summary>条件跳转到指定标签的/summary>
    br_if = 0x0D,

    /// <summary>跳转表，根据索引跳转到不同标签的/summary>
    br_table = 0x0E,

    /// <summary>函数返回的/summary>
    @return = 0x0F,

    /// <summary>调用函数的/summary>
    call = 0x10,

    /// <summary>间接函数调用的/summary>
    call_indirect = 0x11,

    /// <summary>丢弃栈顶值的/summary>
    drop = 0x1A,

    /// <summary>条件选择的/summary>
    select = 0x1B,

    /// <summary>带类型条件选择的/summary>
    select_typed = 0x1C,

    #endregion

    #region 局部变量指的

    /// <summary>获取局部变量的/summary>
    local_get = 0x20,

    /// <summary>设置局部变量的/summary>
    local_set = 0x21,

    /// <summary>设置局部变量并保留栈顶值的/summary>
    local_tee = 0x22,

    /// <summary>获取全局变量的/summary>
    global_get = 0x23,

    /// <summary>设置全局变量的/summary>
    global_set = 0x24,

    #endregion

    #region 内存指令

    /// <summary>加载 32 位整数的/summary>
    i32_load = 0x28,

    /// <summary>加载 64 位整数的/summary>
    i64_load = 0x29,

    /// <summary>加载 32 位浮点数的/summary>
    f32_load = 0x2A,

    /// <summary>加载 64 位浮点数的/summary>
    f64_load = 0x2B,

    /// <summary>加载 8 位有符号整数并扩展为 32 位的/summary>
    i32_load8_s = 0x2C,

    /// <summary>加载 8 位无符号整数并扩展为 32 位的/summary>
    i32_load8_u = 0x2D,

    /// <summary>加载 16 位有符号整数并扩展为 32 位的/summary>
    i32_load16_s = 0x2E,

    /// <summary>加载 16 位无符号整数并扩展为 32 位的/summary>
    i32_load16_u = 0x2F,

    /// <summary>加载 8 位有符号整数并扩展为 64 位的/summary>
    i64_load8_s = 0x30,

    /// <summary>加载 8 位无符号整数并扩展为 64 位的/summary>
    i64_load8_u = 0x31,

    /// <summary>加载 16 位有符号整数并扩展为 64 位的/summary>
    i64_load16_s = 0x32,

    /// <summary>加载 16 位无符号整数并扩展为 64 位的/summary>
    i64_load16_u = 0x33,

    /// <summary>加载 32 位有符号整数并扩展为 64 位的/summary>
    i64_load32_s = 0x34,

    /// <summary>加载 32 位无符号整数并扩展为 64 位的/summary>
    i64_load32_u = 0x35,

    /// <summary>存储 32 位整数的/summary>
    i32_store = 0x36,

    /// <summary>存储 64 位整数的/summary>
    i64_store = 0x37,

    /// <summary>存储 32 位浮点数的/summary>
    f32_store = 0x38,

    /// <summary>存储 64 位浮点数的/summary>
    f64_store = 0x39,

    /// <summary>存储 32 位整数的的8 位的/summary>
    i32_store8 = 0x3A,

    /// <summary>存储 32 位整数的的16 位的/summary>
    i32_store16 = 0x3B,

    /// <summary>存储 64 位整数的的8 位的/summary>
    i64_store8 = 0x3C,

    /// <summary>存储 64 位整数的的16 位的/summary>
    i64_store16 = 0x3D,

    /// <summary>存储 64 位整数的的32 位的/summary>
    i64_store32 = 0x3E,

    /// <summary>当前内存大小（页数）的/summary>
    memory_size = 0x3F,

    /// <summary>增长内存（页数）的/summary>
    memory_grow = 0x40,

    #endregion

    #region 常量指令

    /// <summary>32 位整数常量的/summary>
    i32_const = 0x41,

    /// <summary>64 位整数常量的/summary>
    i64_const = 0x42,

    /// <summary>32 位浮点数常量的/summary>
    f32_const = 0x43,

    /// <summary>64 位浮点数常量的/summary>
    f64_const = 0x44,

    #endregion

    #region 比较指令

    /// <summary>32 位整数等于零比较的/summary>
    i32_eqz = 0x45,

    /// <summary>32 位整数相等比较的/summary>
    i32_eq = 0x46,

    /// <summary>32 位整数不等比较的/summary>
    i32_ne = 0x47,

    /// <summary>32 位有符号整数小于比较的/summary>
    i32_lt_s = 0x48,

    /// <summary>32 位无符号整数小于比较的/summary>
    i32_lt_u = 0x49,

    /// <summary>32 位有符号整数大于比较的/summary>
    i32_gt_s = 0x4A,

    /// <summary>32 位无符号整数大于比较的/summary>
    i32_gt_u = 0x4B,

    /// <summary>32 位有符号整数小于等于比较的/summary>
    i32_le_s = 0x4C,

    /// <summary>32 位无符号整数小于等于比较的/summary>
    i32_le_u = 0x4D,

    /// <summary>32 位有符号整数大于等于比较的/summary>
    i32_ge_s = 0x4E,

    /// <summary>32 位无符号整数大于等于比较的/summary>
    i32_ge_u = 0x4F,

    /// <summary>64 位整数等于零比较的/summary>
    i64_eqz = 0x50,

    /// <summary>64 位整数相等比较的/summary>
    i64_eq = 0x51,

    /// <summary>64 位整数不等比较的/summary>
    i64_ne = 0x52,

    /// <summary>64 位有符号整数小于比较的/summary>
    i64_lt_s = 0x53,

    /// <summary>64 位无符号整数小于比较的/summary>
    i64_lt_u = 0x54,

    /// <summary>64 位有符号整数大于比较的/summary>
    i64_gt_s = 0x55,

    /// <summary>64 位无符号整数大于比较的/summary>
    i64_gt_u = 0x56,

    /// <summary>64 位有符号整数小于等于比较的/summary>
    i64_le_s = 0x57,

    /// <summary>64 位无符号整数小于等于比较的/summary>
    i64_le_u = 0x58,

    /// <summary>64 位有符号整数大于等于比较的/summary>
    i64_ge_s = 0x59,

    /// <summary>64 位无符号整数大于等于比较的/summary>
    i64_ge_u = 0x5A,

    /// <summary>32 位浮点数相等比较的/summary>
    f32_eq = 0x5B,

    /// <summary>32 位浮点数不等比较的/summary>
    f32_ne = 0x5C,

    /// <summary>32 位浮点数小于比较的/summary>
    f32_lt = 0x5D,

    /// <summary>32 位浮点数大于比较的/summary>
    f32_gt = 0x5E,

    /// <summary>32 位浮点数小于等于比较的/summary>
    f32_le = 0x5F,

    /// <summary>32 位浮点数大于等于比较的/summary>
    f32_ge = 0x60,

    /// <summary>64 位浮点数相等比较的/summary>
    f64_eq = 0x61,

    /// <summary>64 位浮点数不等比较的/summary>
    f64_ne = 0x62,

    /// <summary>64 位浮点数小于比较的/summary>
    f64_lt = 0x63,

    /// <summary>64 位浮点数大于比较的/summary>
    f64_gt = 0x64,

    /// <summary>64 位浮点数小于等于比较的/summary>
    f64_le = 0x65,

    /// <summary>64 位浮点数大于等于比较的/summary>
    f64_ge = 0x66,

    #endregion

    #region 算术指令

    /// <summary>32 位整数加法的/summary>
    i32_add = 0x6A,

    /// <summary>32 位整数减法的/summary>
    i32_sub = 0x6B,

    /// <summary>32 位整数乘法的/summary>
    i32_mul = 0x6C,

    /// <summary>32 位有符号整数除法的/summary>
    i32_div_s = 0x6D,

    /// <summary>32 位无符号整数除法的/summary>
    i32_div_u = 0x6E,

    /// <summary>32 位有符号整数取余的/summary>
    i32_rem_s = 0x6F,

    /// <summary>32 位无符号整数取余的/summary>
    i32_rem_u = 0x70,

    /// <summary>32 位整数按位与的/summary>
    i32_and = 0x71,

    /// <summary>32 位整数按位或的/summary>
    i32_or = 0x72,

    /// <summary>32 位整数按位异或的/summary>
    i32_xor = 0x73,

    /// <summary>32 位整数左移的/summary>
    i32_shl = 0x74,

    /// <summary>32 位有符号整数右移的/summary>
    i32_shr_s = 0x75,

    /// <summary>32 位无符号整数右移的/summary>
    i32_shr_u = 0x76,

    /// <summary>32 位整数循环左移的/summary>
    i32_rotl = 0x77,

    /// <summary>32 位整数循环右移的/summary>
    i32_rotr = 0x78,

    /// <summary>64 位整数加法的/summary>
    i64_add = 0x7C,

    /// <summary>64 位整数减法的/summary>
    i64_sub = 0x7D,

    /// <summary>64 位整数乘法的/summary>
    i64_mul = 0x7E,

    /// <summary>64 位有符号整数除法的/summary>
    i64_div_s = 0x7F,

    /// <summary>64 位无符号整数除法的/summary>
    i64_div_u = 0x80,

    /// <summary>64 位有符号整数取余的/summary>
    i64_rem_s = 0x81,

    /// <summary>64 位无符号整数取余的/summary>
    i64_rem_u = 0x82,

    /// <summary>64 位整数按位与的/summary>
    i64_and = 0x83,

    /// <summary>64 位整数按位或的/summary>
    i64_or = 0x84,

    /// <summary>64 位整数按位异或的/summary>
    i64_xor = 0x85,

    /// <summary>64 位整数左移的/summary>
    i64_shl = 0x86,

    /// <summary>64 位有符号整数右移的/summary>
    i64_shr_s = 0x87,

    /// <summary>64 位无符号整数右移的/summary>
    i64_shr_u = 0x88,

    /// <summary>64 位整数循环左移的/summary>
    i64_rotl = 0x89,

    /// <summary>64 位整数循环右移的/summary>
    i64_rotr = 0x8A,

    /// <summary>32 位浮点数加法的/summary>
    f32_add = 0x92,

    /// <summary>32 位浮点数减法的/summary>
    f32_sub = 0x93,

    /// <summary>32 位浮点数乘法的/summary>
    f32_mul = 0x94,

    /// <summary>32 位浮点数除法的/summary>
    f32_div = 0x95,

    /// <summary>32 位浮点数最小值的/summary>
    f32_min = 0x96,

    /// <summary>32 位浮点数最大值的/summary>
    f32_max = 0x97,

    /// <summary>32 位浮点数取负的/summary>
    f32_neg = 0x8C,

    /// <summary>32 位浮点数平方根的/summary>
    f32_sqrt = 0x91,

    /// <summary>64 位浮点数加法的/summary>
    f64_add = 0xA0,

    /// <summary>64 位浮点数减法的/summary>
    f64_sub = 0xA1,

    /// <summary>64 位浮点数乘法的/summary>
    f64_mul = 0xA2,

    /// <summary>64 位浮点数除法的/summary>
    f64_div = 0xA3,

    /// <summary>64 位浮点数最小值的/summary>
    f64_min = 0xA4,

    /// <summary>64 位浮点数最大值的/summary>
    f64_max = 0xA5,

    /// <summary>64 位浮点数取负的/summary>
    f64_neg = 0x9A,

    /// <summary>64 位浮点数平方根的/summary>
    f64_sqrt = 0x9F,

    /// <summary>32 位整数截的32 位浮点数的/summary>
    i32_trunc_f32_s = 0xA8,

    /// <summary>32 位无符号整数截断 32 位浮点数的/summary>
    i32_trunc_f32_u = 0xA9,

    /// <summary>32 位有符号整数截断 64 位浮点数的/summary>
    i32_trunc_f64_s = 0xAA,

    /// <summary>32 位无符号整数截断 64 位浮点数的/summary>
    i32_trunc_f64_u = 0xAB,

    /// <summary>64 位有符号整数截断 32 位浮点数的/summary>
    i64_trunc_f32_s = 0xAE,

    /// <summary>64 位无符号整数截断 32 位浮点数的/summary>
    i64_trunc_f32_u = 0xAF,

    /// <summary>64 位有符号整数截断 64 位浮点数的/summary>
    i64_trunc_f64_s = 0xB0,

    /// <summary>64 位无符号整数截断 64 位浮点数的/summary>
    i64_trunc_f64_u = 0xB1,

    /// <summary>32 位有符号整数转换的32 位浮点数的/summary>
    f32_convert_i32_s = 0xB2,

    /// <summary>32 位无符号整数转换的32 位浮点数的/summary>
    f32_convert_i32_u = 0xB3,

    /// <summary>32 位有符号整数转换的64 位浮点数的/summary>
    f64_convert_i32_s = 0xB7,

    /// <summary>32 位无符号整数转换的64 位浮点数的/summary>
    f64_convert_i32_u = 0xB8,

    /// <summary>64 位有符号整数转换的32 位浮点数的/summary>
    f32_convert_i64_s = 0xB4,

    /// <summary>64 位无符号整数转换的32 位浮点数的/summary>
    f32_convert_i64_u = 0xB5,

    /// <summary>64 位有符号整数转换的64 位浮点数的/summary>
    f64_convert_i64_s = 0xB9,

    /// <summary>64 位无符号整数转换的64 位浮点数的/summary>
    f64_convert_i64_u = 0xBA,

    /// <summary>32 位整数扩展为 64 位有符号整数的/summary>
    i64_extend_i32_s = 0xAC,

    /// <summary>32 位整数扩展为 64 位无符号整数的/summary>
    i64_extend_i32_u = 0xAD,

    /// <summary>64 位整数包装为 32 位整数的/summary>
    i32_wrap_i64 = 0xA7,

    /// <summary>32 位浮点数提升的64 位浮点数的/summary>
    f64_promote_f32 = 0xBB,

    /// <summary>64 位浮点数降级的32 位浮点数的/summary>
    f32_demote_f64 = 0xB6,

    #endregion

    #region 引用指令

    /// <summary>空引用的/summary>
    ref_null = 0xD0,

    /// <summary>引用空值判断的/summary>
    ref_is_null = 0xD1,

    /// <summary>函数引用的/summary>
    ref_func = 0xD2,

    /// <summary>引用相等比较的/summary>
    ref_eq = 0xD3,

    /// <summary>断言引用非空的/summary>
    ref_as_non_null = 0xD4,

    #endregion

    #region 表指的

    /// <summary>获取表元素的/summary>
    table_get = 0x25,

    /// <summary>设置表元素的/summary>
    table_set = 0x26,

    /// <summary>表大小的/summary>
    table_size = 0xFC10,

    /// <summary>增长表的/summary>
    table_grow = 0xFC0F,

    /// <summary>填充表的/summary>
    table_fill = 0xFC11,

    /// <summary>表复制的/summary>
    table_copy = 0xFC0E,

    /// <summary>表初始化的/summary>
    table_init = 0xFC0C,

    /// <summary>删除表元素的/summary>
    table_elem_drop = 0xFC0D,

    #endregion

    #region 内存批量指令

    /// <summary>内存复制的/summary>
    memory_copy = 0xFC0A,

    /// <summary>内存填充的/summary>
    memory_fill = 0xFC0B,

    /// <summary>内存初始化的/summary>
    memory_init = 0xFC08,

    /// <summary>丢弃数据段的/summary>
    data_drop = 0xFC09,

    #endregion

    #region GC 结构体指令（前缀 0xFB的

    /// <summary>创建结构体实例的/summary>
    struct_new = 0xFB00,

    /// <summary>以默认值创建结构体实例的/summary>
    struct_new_default = 0xFB01,

    /// <summary>读取结构体字段的/summary>
    struct_get = 0xFB02,

    /// <summary>有符号读取结构体打包字段的/summary>
    struct_get_s = 0xFB03,

    /// <summary>无符号读取结构体打包字段的/summary>
    struct_get_u = 0xFB04,

    /// <summary>写入结构体字段的/summary>
    struct_set = 0xFB05,

    #endregion

    #region GC 数组指令（前缀 0xFB的

    /// <summary>创建数组实例的/summary>
    array_new = 0xFB06,

    /// <summary>以默认值创建数组实例的/summary>
    array_new_default = 0xFB07,

    /// <summary>以固定大小创建数组实例的/summary>
    array_new_fixed = 0xFB08,

    /// <summary>从数据段创建数组实例的/summary>
    array_new_data = 0xFB09,

    /// <summary>从元素段创建数组实例的/summary>
    array_new_elem = 0xFB0A,

    /// <summary>读取数组元素的/summary>
    array_get = 0xFB0B,

    /// <summary>有符号读取数组打包元素的/summary>
    array_get_s = 0xFB0C,

    /// <summary>无符号读取数组打包元素的/summary>
    array_get_u = 0xFB0D,

    /// <summary>写入数组元素的/summary>
    array_set = 0xFB0E,

    /// <summary>获取数组长度的/summary>
    array_len = 0xFB0F,

    /// <summary>填充数组的/summary>
    array_fill = 0xFB10,

    /// <summary>复制数组的/summary>
    array_copy = 0xFB11,

    #endregion

    #region GC 整数引用指令（前缀 0xFB的

    /// <summary>创建 i31 引用的/summary>
    i31_new = 0xFB20,

    /// <summary>有符号读的i31 引用的/summary>
    i31_get_s = 0xFB21,

    /// <summary>无符号读的i31 引用的/summary>
    i31_get_u = 0xFB22,

    #endregion

    #region GC 转换指令（前缀 0xFB的

    /// <summary>测试引用类型的/summary>
    ref_test = 0xFB40,

    /// <summary>强制转换引用类型的/summary>
    ref_cast = 0xFB41,

    /// <summary>分支判断引用转换的/summary>
    br_on_cast = 0xFB42,

    /// <summary>分支判断引用转换失败的/summary>
    br_on_cast_fail = 0xFB43,

    /// <summary>外部引用转为任意引用的/summary>
    extern_internalize = 0xFB50,

    /// <summary>任意引用转为外部引用的/summary>
    extern_externalize = 0xFB51,

    #endregion

    #region 带返回值的调用（前缀 0xFC的

    /// <summary>带返回值的函数调用的/summary>
    call_ref = 0xFC00,

    /// <summary>带返回值的间接函数调用的/summary>
    return_call_ref = 0xFC01,

    /// <summary>返回调用函数的/summary>
    return_call = 0x12,

    /// <summary>带返回值的返回间接调用的/summary>
    return_call_indirect = 0x13,

    #endregion

    #region Canonical ABI 指令（Component Model，前缀 0xFE的

    /// <summary>内存复制 8 位的/summary>
    memory_atomic_notify = 0xFE00,

    /// <summary>内存等待 32 位的/summary>
    memory_atomic_wait32 = 0xFE01,

    /// <summary>内存等待 64 位的/summary>
    memory_atomic_wait64 = 0xFE02,

    /// <summary>原子栅栏的/summary>
    atomic_fence = 0xFE03,

    #endregion

    #region SIMD 指令（前缀 0xFD，操作码值遵的W3C WebAssembly SIMD 规范最终版本）

    v128_load = 0xFD00,
    v128_load8_x8_s = 0xFD01,
    v128_load8_x8_u = 0xFD02,
    v128_load16_x4_s = 0xFD03,
    v128_load16_x4_u = 0xFD04,
    v128_load32_x2_s = 0xFD05,
    v128_load32_x2_u = 0xFD06,
    v128_load8_splat = 0xFD07,
    v128_load16_splat = 0xFD08,
    v128_load32_splat = 0xFD09,
    v128_load64_splat = 0xFD0A,
    v128_store = 0xFD0B,
    v128_const = 0xFD0C,
    i8_x16_shuffle = 0xFD0D,
    i8_x16_swizzle = 0xFD0E,
    i8_x16_splat = 0xFD0F,
    i16_x8_splat = 0xFD10,
    i32_x4_splat = 0xFD11,
    i64_x2_splat = 0xFD12,
    f32_x4_splat = 0xFD13,
    f64_x2_splat = 0xFD14,
    i8_x16_extract_lane_s = 0xFD15,
    i8_x16_extract_lane_u = 0xFD16,
    i8_x16_replace_lane = 0xFD17,
    i16_x8_extract_lane_s = 0xFD18,
    i16_x8_extract_lane_u = 0xFD19,
    i16_x8_replace_lane = 0xFD1A,
    i32_x4_extract_lane = 0xFD1B,
    i32_x4_replace_lane = 0xFD1C,
    i64_x2_extract_lane = 0xFD1D,
    i64_x2_replace_lane = 0xFD1E,
    f32_x4_extract_lane = 0xFD1F,
    f32_x4_replace_lane = 0xFD20,
    f64_x2_extract_lane = 0xFD21,
    f64_x2_replace_lane = 0xFD22,

    i8_x16_eq = 0xFD23,
    i8_x16_ne = 0xFD24,
    i8_x16_lt_s = 0xFD25,
    i8_x16_lt_u = 0xFD26,
    i8_x16_gt_s = 0xFD27,
    i8_x16_gt_u = 0xFD28,
    i8_x16_le_s = 0xFD29,
    i8_x16_le_u = 0xFD2A,
    i8_x16_ge_s = 0xFD2B,
    i8_x16_ge_u = 0xFD2C,
    i16_x8_eq = 0xFD2D,
    i16_x8_ne = 0xFD2E,
    i16_x8_lt_s = 0xFD2F,
    i16_x8_lt_u = 0xFD30,
    i16_x8_gt_s = 0xFD31,
    i16_x8_gt_u = 0xFD32,
    i16_x8_le_s = 0xFD33,
    i16_x8_le_u = 0xFD34,
    i16_x8_ge_s = 0xFD35,
    i16_x8_ge_u = 0xFD36,
    i32_x4_eq = 0xFD37,
    i32_x4_ne = 0xFD38,
    i32_x4_lt_s = 0xFD39,
    i32_x4_lt_u = 0xFD3A,
    i32_x4_gt_s = 0xFD3B,
    i32_x4_gt_u = 0xFD3C,
    i32_x4_le_s = 0xFD3D,
    i32_x4_le_u = 0xFD3E,
    i32_x4_ge_s = 0xFD3F,
    i32_x4_ge_u = 0xFD40,
    f32_x4_eq = 0xFD41,
    f32_x4_ne = 0xFD42,
    f32_x4_lt = 0xFD43,
    f32_x4_gt = 0xFD44,
    f32_x4_le = 0xFD45,
    f32_x4_ge = 0xFD46,
    f64_x2_eq = 0xFD47,
    f64_x2_ne = 0xFD48,
    f64_x2_lt = 0xFD49,
    f64_x2_gt = 0xFD4A,
    f64_x2_le = 0xFD4B,
    f64_x2_ge = 0xFD4C,

    v128_not = 0xFD4D,
    v128_and = 0xFD4E,
    v128_and_not = 0xFD4F,
    v128_or = 0xFD50,
    v128_xor = 0xFD51,
    v128_bit_select = 0xFD52,
    v128_any_true = 0xFD53,

    v128_load8_lane = 0xFD54,
    v128_load16_lane = 0xFD55,
    v128_load32_lane = 0xFD56,
    v128_load64_lane = 0xFD57,
    v128_store8_lane = 0xFD58,
    v128_store16_lane = 0xFD59,
    v128_store32_lane = 0xFD5A,
    v128_store64_lane = 0xFD5B,
    v128_load32_zero = 0xFD5C,
    v128_load64_zero = 0xFD5D,
    f32_x4_demote_f64_x2_zero = 0xFD5E,
    f64_x2_promote_low_f32_x4 = 0xFD5F,

    i8_x16_abs = 0xFD60,
    i8_x16_neg = 0xFD61,
    i8_x16_popcnt = 0xFD62,
    i8_x16_all_true = 0xFD63,
    i8_x16_bitmask = 0xFD64,
    i8_x16_narrow_i16_x8_s = 0xFD65,
    i8_x16_narrow_i16_x8_u = 0xFD66,
    f32_x4_ceil = 0xFD67,
    f32_x4_floor = 0xFD68,
    f32_x4_trunc = 0xFD69,
    f32_x4_nearest = 0xFD6A,
    i8_x16_shl = 0xFD6B,
    i8_x16_shr_s = 0xFD6C,
    i8_x16_shr_u = 0xFD6D,
    i8_x16_add = 0xFD6E,
    i8_x16_add_sat_s = 0xFD6F,
    i8_x16_add_sat_u = 0xFD70,
    i8_x16_sub = 0xFD71,
    i8_x16_sub_sat_s = 0xFD72,
    i8_x16_sub_sat_u = 0xFD73,
    f64_x2_ceil = 0xFD74,
    f64_x2_floor = 0xFD75,
    i8_x16_min_s = 0xFD76,
    i8_x16_min_u = 0xFD77,
    i8_x16_max_s = 0xFD78,
    i8_x16_max_u = 0xFD79,
    f64_x2_trunc = 0xFD7A,
    i8_x16_avgr_u = 0xFD7B,
    i16_x8_ext_add_pairwise_i8_x16_s = 0xFD7C,
    i16_x8_ext_add_pairwise_i8_x16_u = 0xFD7D,
    i32_x4_ext_add_pairwise_i16_x8_s = 0xFD7E,
    i32_x4_ext_add_pairwise_i16_x8_u = 0xFD7F,

    i16_x8_abs = 0xFD80,
    i16_x8_neg = 0xFD81,
    i16_x8_q15_mulr_sat_s = 0xFD82,
    i16_x8_all_true = 0xFD83,
    i16_x8_bitmask = 0xFD84,
    i16_x8_narrow_i32_x4_s = 0xFD85,
    i16_x8_narrow_i32_x4_u = 0xFD86,
    i16_x8_extend_low_i8_x16_s = 0xFD87,
    i16_x8_extend_high_i8_x16_s = 0xFD88,
    i16_x8_extend_low_i8_x16_u = 0xFD89,
    i16_x8_extend_high_i8_x16_u = 0xFD8A,
    i16_x8_shl = 0xFD8B,
    i16_x8_shr_s = 0xFD8C,
    i16_x8_shr_u = 0xFD8D,
    i16_x8_add = 0xFD8E,
    i16_x8_add_sat_s = 0xFD8F,
    i16_x8_add_sat_u = 0xFD90,
    i16_x8_sub = 0xFD91,
    i16_x8_sub_sat_s = 0xFD92,
    i16_x8_sub_sat_u = 0xFD93,
    f64_x2_nearest = 0xFD94,
    i16_x8_mul = 0xFD95,
    i16_x8_min_s = 0xFD96,
    i16_x8_min_u = 0xFD97,
    i16_x8_max_s = 0xFD98,
    i16_x8_max_u = 0xFD99,
    i16_x8_avgr_u = 0xFD9B,
    i16_x8_ext_mul_low_i8_x16_s = 0xFD9C,
    i16_x8_ext_mul_high_i8_x16_s = 0xFD9D,
    i16_x8_ext_mul_low_i8_x16_u = 0xFD9E,
    i16_x8_ext_mul_high_i8_x16_u = 0xFD9F,

    i32_x4_abs = 0xFDA0,
    i32_x4_neg = 0xFDA1,
    i32_x4_all_true = 0xFDA3,
    i32_x4_bitmask = 0xFDA4,
    i32_x4_extend_low_i16_x8_s = 0xFDA7,
    i32_x4_extend_high_i16_x8_s = 0xFDA8,
    i32_x4_extend_low_i16_x8_u = 0xFDA9,
    i32_x4_extend_high_i16_x8_u = 0xFDAA,
    i32_x4_shl = 0xFDAB,
    i32_x4_shr_s = 0xFDAC,
    i32_x4_shr_u = 0xFDAD,
    i32_x4_add = 0xFDAE,
    i32_x4_sub = 0xFDB1,
    i32_x4_mul = 0xFDB5,
    i32_x4_min_s = 0xFDB6,
    i32_x4_min_u = 0xFDB7,
    i32_x4_max_s = 0xFDB8,
    i32_x4_max_u = 0xFDB9,
    i32_x4_dot_i16_x8_s = 0xFDBA,
    i32_x4_ext_mul_low_i16_x8_s = 0xFDBC,
    i32_x4_ext_mul_high_i16_x8_s = 0xFDBD,
    i32_x4_ext_mul_low_i16_x8_u = 0xFDBE,
    i32_x4_ext_mul_high_i16_x8_u = 0xFDBF,

    i64_x2_abs = 0xFDC0,
    i64_x2_neg = 0xFDC1,
    i64_x2_all_true = 0xFDC3,
    i64_x2_bitmask = 0xFDC4,
    i64_x2_extend_low_i32_x4_s = 0xFDC7,
    i64_x2_extend_high_i32_x4_s = 0xFDC8,
    i64_x2_extend_low_i32_x4_u = 0xFDC9,
    i64_x2_extend_high_i32_x4_u = 0xFDCA,
    i64_x2_shl = 0xFDCB,
    i64_x2_shr_s = 0xFDCC,
    i64_x2_shr_u = 0xFDCD,
    i64_x2_add = 0xFDCE,
    i64_x2_sub = 0xFDD1,
    i64_x2_mul = 0xFDD5,
    i64_x2_eq = 0xFDD6,
    i64_x2_ne = 0xFDD7,
    i64_x2_lt_s = 0xFDD8,
    i64_x2_gt_s = 0xFDD9,
    i64_x2_le_s = 0xFDDA,
    i64_x2_ge_s = 0xFDDB,
    i64_x2_ext_mul_low_i32_x4_s = 0xFDDC,
    i64_x2_ext_mul_high_i32_x4_s = 0xFDDD,
    i64_x2_ext_mul_low_i32_x4_u = 0xFDDE,
    i64_x2_ext_mul_high_i32_x4_u = 0xFDDF,

    f32_x4_abs = 0xFDE0,
    f32_x4_neg = 0xFDE1,
    f32_x4_sqrt = 0xFDE3,
    f32_x4_add = 0xFDE4,
    f32_x4_sub = 0xFDE5,
    f32_x4_mul = 0xFDE6,
    f32_x4_div = 0xFDE7,
    f32_x4_min = 0xFDE8,
    f32_x4_max = 0xFDE9,
    f32_x4_pmin = 0xFDEA,
    f32_x4_pmax = 0xFDEB,
    f64_x2_abs = 0xFDEC,
    f64_x2_neg = 0xFDED,
    f64_x2_sqrt = 0xFDEF,
    f64_x2_add = 0xFDF0,
    f64_x2_sub = 0xFDF1,
    f64_x2_mul = 0xFDF2,
    f64_x2_div = 0xFDF3,
    f64_x2_min = 0xFDF4,
    f64_x2_max = 0xFDF5,
    f64_x2_pmin = 0xFDF6,
    f64_x2_pmax = 0xFDF7,
    i32_x4_trunc_sat_f32_x4_s = 0xFDF8,
    i32_x4_trunc_sat_f32_x4_u = 0xFDF9,
    f32_x4_convert_i32_x4_s = 0xFDFA,
    f32_x4_convert_i32_x4_u = 0xFDFB,
    i32_x4_trunc_sat_f64_x2_s_zero = 0xFDFC,
    i32_x4_trunc_sat_f64_x2_u_zero = 0xFDFD,
    f64_x2_convert_low_i32_x4_s = 0xFDFE,
    f64_x2_convert_low_i32_x4_u = 0xFDFF,

    #endregion

    #region 异常处理指令（Exception Handling 提案的

    /// <summary>抛出异常，参数在栈上的/summary>
    @throw = 0x08,

    /// <summary>重新抛出栈顶的异常引用的/summary>
    throw_ref = 0x0A,

    /// <summary>try_table 块，替代传统 try/catch 结构的/summary>
    try_table = 0x1F,

    #endregion
}