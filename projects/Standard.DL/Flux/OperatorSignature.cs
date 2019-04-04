namespace Std.DL.Flux;

/// <summary>算子编译期元数据</summary>
public sealed class OperatorSignature
{
    /// <summary>算子名称</summary>
    public string Name { get; init; } = "";

    /// <summary>输入类型列表</summary>
    public Type[] InputTypes { get; init; } = [];

    /// <summary>输出类型</summary>
    public Type OutputType { get; init; } = typeof(object);

    /// <summary>算子参数数量</summary>
    public int Arity => InputTypes.Length;
}