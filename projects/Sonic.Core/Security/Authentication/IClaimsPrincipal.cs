using System.Collections.Generic;

namespace Core.Security.Authentication;

/// <summary>
///     声明主体接口
/// </summary>
public interface IClaimsPrincipal
{
    /// <summary>
    ///     主体拥有的声明集合
    /// </summary>
    IEnumerable<IClaim> claims { get; }
}