namespace Core.Compiler.Verification;

/// <summary>
///     基于属性的测试接口，定义属性测试的契约
/// </summary>
public interface IPropertyTest
{
    /// <summary>
    ///     运行属性测试
    /// </summary>
    void run();
}