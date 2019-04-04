using System;

namespace Core.Data;

/// <summary>
///     标记类或方法需要事务支持。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TransactionalAttribute : Attribute
{
}