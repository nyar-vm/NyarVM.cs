namespace Nyar.Assembler;

/// <summary>
///     代码生成中间表示的操作数判别联合。
///     替代 Operand + OperandKind enum + Type string 的组合，
///     通过 pattern match 实现类型安全的操作数访问。
/// </summary>
public abstract record GenerateOperand
{
    /// <summary>
    ///     32 位整数立即数
    /// </summary>
    public sealed record I32(int value) : GenerateOperand;

    /// <summary>
    ///     64 位整数立即数
    /// </summary>
    public sealed record I64(long value) : GenerateOperand;


    /// <summary>
    ///     32 位浮点立即数
    /// </summary>
    public sealed record F32(float value) : GenerateOperand;


    /// <summary>
    ///     64 位浮点立即数
    /// </summary>
    public sealed record F64(double value) : GenerateOperand;


    /// <summary>
    ///     字符串常量
    /// </summary>
    public sealed record Str(string value) : GenerateOperand;


    /// <summary>
    ///     局部变量引用
    /// </summary>
    public sealed record Local(int index, GenerateValueType type) : GenerateOperand;


    /// <summary>
    ///     参数引用
    /// </summary>
    public sealed record Param(int index, GenerateValueType type) : GenerateOperand;


    /// <summary>
    ///     标签（用于跳转目标）
    /// </summary>
    public sealed record Label(string name) : GenerateOperand;


    /// <summary>
    ///     函数引用
    /// </summary>
    public sealed record FuncRef(string name, GenerateFunctionType signature) : GenerateOperand;


    /// <summary>
    ///     常量池引用
    /// </summary>
    public sealed record Const(int pool_index, GenerateValueType type) : GenerateOperand;


    /// <summary>
    ///     空值
    /// </summary>
    public sealed record Null(GenerateValueType type) : GenerateOperand;
}