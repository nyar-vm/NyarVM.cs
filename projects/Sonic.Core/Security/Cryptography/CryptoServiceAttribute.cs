using System;

namespace Core.Security.Cryptography;

/// <summary>
///     标记加密服务类
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CryptoServiceAttribute : Attribute
{
}