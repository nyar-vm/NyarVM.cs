using System;
using Core.Compiler.Token;

namespace Core.Compiler;

/// <summary>
///     标记类或方法编译到指定的目标架构
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CompileToAttribute : Attribute
{
    /// <summary>
    ///     初始化编译目标架构特性
    /// </summary>
    /// <param name="target">目标架构</param>
    public CompileToAttribute(TargetArchitecture target)
    {
        this.target = target;
    }

    /// <summary>
    ///     获取目标架构
    /// </summary>
    public TargetArchitecture target { get; }
}