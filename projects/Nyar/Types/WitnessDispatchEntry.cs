namespace Nyar.Types;

/// <summary>
///     Witness 分派条目，描述编译期生成的接口分派信息
///     由 BytecodeGenerator 在发射 CallWitness 指令时生成
/// </summary>
public sealed class WitnessDispatchEntry
{
    /// <summary>
    ///     初始化 WitnessDispatchEntry
    /// </summary>
    /// <param name="methodId">方法 ID。</param>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="methodName">方法名称。</param>
    /// <param name="functionIndex">目标函数索引。</param>
    /// <param name="interfaceId">接口 ID（可选）。</param>
    /// <param name="interfaceMethodIndex">接口方法索引（可选）。</param>
    public WitnessDispatchEntry(int methodId, int typeId, string methodName, int functionIndex, int interfaceId = 0,
        int interfaceMethodIndex = 0)
    {
        method_id = methodId;
        type_id = typeId;
        method_name = methodName;
        function_index = functionIndex;
        interface_id = interfaceId;
        interface_method_index = interfaceMethodIndex;
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
    ///     目标函数索引
    /// </summary>
    public int function_index { get; set; }

    /// <summary>
    ///     接口 ID
    /// </summary>
    public int interface_id { get; }

    /// <summary>
    ///     接口方法索引
    /// </summary>
    public int interface_method_index { get; }
}