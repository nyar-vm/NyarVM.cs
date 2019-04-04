namespace Core.Security.Authorization;

/// <summary>
///     IPolicyEvaluator 接口
/// </summary>
public interface IPolicyEvaluator<T>
{
    /// <summary>
    ///     评估策略是否满足
    /// </summary>
    bool evaluate(T context);
}