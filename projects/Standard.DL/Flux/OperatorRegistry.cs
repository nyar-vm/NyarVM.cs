namespace Std.DL.Flux;

/// <summary>全局算子注册表</summary>
public static class OperatorRegistry
{
    private static readonly Dictionary<string, OperatorSignature> _operators = new();

    /// <summary>注册算子</summary>
    public static void Register(string name, OperatorSignature signature)
    {
        _operators[name] = signature;
    }

    /// <summary>尝试获取算子</summary>
    public static bool TryGet(string name, out OperatorSignature? signature)
    {
        return _operators.TryGetValue(name, out signature);
    }
}