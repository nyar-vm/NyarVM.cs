using System;

namespace Core.Compiler.Document;

/// <summary>
///     标记类、方法或属性的文档示例代码
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class DocExampleAttribute : Attribute
{
    /// <summary>
    ///     初始化文档示例特性
    /// </summary>
    /// <param name="code">示例代码</param>
    public DocExampleAttribute(string code)
    {
        this.code = code;
    }

    /// <summary>
    ///     示例代码
    /// </summary>
    public string code { get; }
}