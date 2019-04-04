using System;

namespace Core.Compiler.Transpilation;

/// <summary>
///     标记类型可转译到指定的目标语言，允许在同一类型上多次使用
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class TranspileToAttribute : Attribute
{
    /// <summary>
    ///     初始化转译目标语言特性
    /// </summary>
    /// <param name="targets">目标语言参数列表</param>
    public TranspileToAttribute(params TargetLanguage[] targets)
    {
        this.targets = targets;
    }

    /// <summary>
    ///     获取转译的目标语言列表
    /// </summary>
    public TargetLanguage[] targets { get; }
}