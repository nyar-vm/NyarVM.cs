using System.Security.Cryptography;
using System.Text;

namespace Nyar.ObjectAlgebra;

/// <summary>
///     操作符键，由方言标识符和操作符编号组成，全局唯一标识一个操作符。
/// </summary>
public readonly record struct OperatorKey(Guid dialect_id, int operator_id)
{
    /// <summary>
    ///     创建操作符键
    /// </summary>
    /// <param name="dialectId">方言 GUID。</param>
    /// <param name="operatorId">操作符编号。</param>
    public static OperatorKey create(Guid dialectId, int operatorId)
    {
        return new OperatorKey(dialectId, operatorId);
    }

    /// <summary>
    ///     从方言名称计算操作符键
    /// </summary>
    /// <param name="dialectName">方言名称。</param>
    /// <param name="operatorId">操作符编号。</param>
    public static OperatorKey from_dialect_name(string dialectName, int operatorId)
    {
        var dialectId = DialectIdHelper.compute(dialectName);
        return new OperatorKey(dialectId, operatorId);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{dialect_id.ToString("N")[..8]}:{operator_id}";
    }
}

/// <summary>
///     方言标识符辅助方法，提供基于名称的确定性 GUID 计算。
/// </summary>
public static class DialectIdHelper
{
    /// <summary>
    ///     从方言名称计算确定性 GUID（基于 MD5）
    /// </summary>
    /// <param name="name">方言名称。</param>
    /// <returns>确定性 GUID。</returns>
    public static Guid compute(string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}