namespace Core.Math;

/// <summary>
///     浮点数接口，表示具有近似相等比较能力的浮点类型。
/// </summary>
/// <typeparam name="T">实现此接口的类型本身。</typeparam>
public interface IFloat<T> : IApproximate<T> where T : IFloat<T>
{
}