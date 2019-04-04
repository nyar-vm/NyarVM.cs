namespace Nyar.VM.LegacyVM.Algebra;

/// <summary>
///     Lambda 闭包，封装参数名、函数体和捕获环境。
/// </summary>
public sealed class LambdaClosure
{
    /// <summary>
    ///     参数名列表
    /// </summary>
    public string[] Parameters { get; }

    /// <summary>
    ///     函数体表达式
    /// </summary>
    public object Body { get; }

    /// <summary>
    ///     捕获的环境变量
    /// </summary>
    public Dictionary<string, object> CapturedEnvironment { get; }

    /// <summary>
    ///     创建 lambda 闭包
    /// </summary>
    /// <param name="parameters">参数名列表</param>
    /// <param name="body">函数体表达式</param>
    /// <param name="capturedEnvironment">捕获的环境变量</param>
    public LambdaClosure(string[] parameters, object body, Dictionary<string, object>? capturedEnvironment = null)
    {
        Parameters = parameters;
        Body = body;
        CapturedEnvironment = capturedEnvironment ?? new Dictionary<string, object>();
    }
}