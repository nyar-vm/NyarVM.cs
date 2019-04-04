namespace Core.Compiler;

/// <summary>
///     重写规则接口，定义中间表示的模式匹配与变换逻辑
/// </summary>
public interface IRewriteRule
{
    /// <summary>
    ///     判断中间表示是否匹配该重写规则
    /// </summary>
    /// <param name="ir">待匹配的中间表示</param>
    /// <returns>匹配成功返回 true，否则返回 false</returns>
    bool match(IIntermediateRepresentation ir);

    /// <summary>
    ///     对匹配成功的中间表示应用重写变换
    /// </summary>
    /// <param name="ir">待重写的中间表示</param>
    /// <returns>重写后的中间表示</returns>
    IIntermediateRepresentation apply(IIntermediateRepresentation ir);
}