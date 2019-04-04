using System;

namespace Core.Compiler.Document.Template;

/// <summary>
///     标记类关联的模板路径
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TemplateAttribute : Attribute
{
    /// <summary>
    ///     创建模板特性
    /// </summary>
    public TemplateAttribute(string path)
    {
        this.path = path;
    }

    /// <summary>
    ///     模板文件路径
    /// </summary>
    public string? path { get; }
}