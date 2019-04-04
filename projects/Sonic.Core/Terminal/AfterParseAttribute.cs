using System;

namespace Core.Terminal;

/// <summary>
///     标记命令类中的方法为解析后钩子。命令行参数解析完成后，Source Generator 将自动调用此方法，
///     可用于额外初始化或交叉字段验证。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AfterParseAttribute : Attribute
{
}