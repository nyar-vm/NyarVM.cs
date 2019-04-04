using System;

namespace Core.Compiler.FFI;

/// <summary>
///     标记类为外部函数接口绑定，指定关联的原生库名称
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class FfiAttribute : Attribute
{
    /// <summary>
    ///     初始化外部函数接口特性
    /// </summary>
    /// <param name="libraryName">原生库名称</param>
    public FfiAttribute(string libraryName)
    {
        library_name = libraryName;
    }

    /// <summary>
    ///     获取原生库的名称
    /// </summary>
    public string library_name { get; }
}