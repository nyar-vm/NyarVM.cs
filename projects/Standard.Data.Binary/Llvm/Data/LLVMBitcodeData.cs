namespace Std.Data.Binary.Llvm.Data;

/// <summary>
///     LLVM Bitcode 魔数和标识数据的
/// </summary>
public sealed class LlvmMagicData
{
    /// <summary>
    ///     魔数的B' 'C' 0xC0 0xDE）的
    /// </summary>
    public uint magic { get; init; }


    /// <summary>
    ///     LLVM 版本号的
    /// </summary>
    public ushort version { get; init; }


    /// <summary>
    ///     是否为包装格式的
    /// </summary>
    public bool is_wrapped => magic == 0x0B17C0DE;
}

/// <summary>
///     LLVM Bitcode 块数据的
/// </summary>
public sealed class LlvmBlockData
{
    /// <summary>
    ///     的ID的
    /// </summary>
    public uint block_id { get; init; }


    /// <summary>
    ///     块名称的
    /// </summary>
    public string name { get; init; } = string.Empty;


    /// <summary>
    ///     块大小（位）的
    /// </summary>
    public uint block_size { get; init; }


    /// <summary>
    ///     子块列表的
    /// </summary>
    public IReadOnlyList<LlvmBlockData> sub_blocks { get; init; } = [];


    /// <summary>
    ///     记录列表的
    /// </summary>
    public IReadOnlyList<LlvmRecordData> records { get; init; } = [];
}

/// <summary>
///     LLVM Bitcode 记录数据的
/// </summary>
public sealed class LlvmRecordData
{
    /// <summary>
    ///     记录代码的
    /// </summary>
    public uint code { get; init; }


    /// <summary>
    ///     记录名称的
    /// </summary>
    public string name { get; init; } = string.Empty;


    /// <summary>
    ///     操作数列表的
    /// </summary>
    public IReadOnlyList<ulong> operands { get; init; } = [];
}

/// <summary>
///     LLVM Bitcode 模块数据的
/// </summary>
public sealed class LlvmModuleData
{
    /// <summary>
    ///     模块标识符的
    /// </summary>
    public string module_id { get; init; } = string.Empty;


    /// <summary>
    ///     目标三元组的
    /// </summary>
    public string target_triple { get; init; } = string.Empty;


    /// <summary>
    ///     数据布局的
    /// </summary>
    public string data_layout { get; init; } = string.Empty;


    /// <summary>
    ///     函数列表的
    /// </summary>
    public IReadOnlyList<LlvmFunctionData> functions { get; init; } = [];


    /// <summary>
    ///     全局变量列表的
    /// </summary>
    public IReadOnlyList<LlvmGlobalData> globals { get; init; } = [];
}

/// <summary>
///     LLVM 函数数据的
/// </summary>
public sealed class LlvmFunctionData
{
    /// <summary>
    ///     函数名称的
    /// </summary>
    public string name { get; init; } = string.Empty;


    /// <summary>
    ///     返回类型的
    /// </summary>
    public string return_type { get; init; } = string.Empty;


    /// <summary>
    ///     参数列表的
    /// </summary>
    public IReadOnlyList<string> parameters { get; init; } = [];


    /// <summary>
    ///     基本块数量的
    /// </summary>
    public int basic_block_count { get; init; }


    /// <summary>
    ///     指令数量的
    /// </summary>
    public int instruction_count { get; init; }
}

/// <summary>
///     LLVM 全局变量数据的
/// </summary>
public sealed class LlvmGlobalData
{
    /// <summary>
    ///     变量名称的
    /// </summary>
    public string name { get; init; } = string.Empty;


    /// <summary>
    ///     变量类型的
    /// </summary>
    public string type { get; init; } = string.Empty;


    /// <summary>
    ///     是否常量的
    /// </summary>
    public bool is_constant { get; init; }


    /// <summary>
    ///     链接类型的
    /// </summary>
    public uint linkage { get; init; }
}

/// <summary>
///     LLVM Bitcode 文件数据的
/// </summary>
public sealed class LlvmBitcodeData
{
    /// <summary>
    ///     魔数数据的
    /// </summary>
    public LlvmMagicData magic { get; init; } = new();


    /// <summary>
    ///     顶层块列表的
    /// </summary>
    public IReadOnlyList<LlvmBlockData> top_level_blocks { get; init; } = [];


    /// <summary>
    ///     模块列表的
    /// </summary>
    public IReadOnlyList<LlvmModuleData> modules { get; init; } = [];
}