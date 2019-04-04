namespace Nyar.Assembler;

/// <summary>
///     代码生成中间表示的函数类型描述
/// </summary>
public sealed record GenerateFunctionType
{
    /// <summary>
    ///     参数类型列表
    /// </summary>
    public IReadOnlyList<GenerateValueType> parameters { get; init; } = [];

    /// <summary>
    ///     返回值类型列表
    /// </summary>
    public IReadOnlyList<GenerateValueType> results { get; init; } = [];
}