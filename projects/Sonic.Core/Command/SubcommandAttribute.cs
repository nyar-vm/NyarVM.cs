using System;

namespace Core.Command;

/// <summary>
///     子命令特性，标注父命令中用于路由子命令的属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SubcommandAttribute : Attribute
{
}