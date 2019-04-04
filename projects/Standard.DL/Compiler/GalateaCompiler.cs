using Std.DL.Execution;
using Std.DL.Flux;
using Std.DL.Topos;

namespace Std.DL.Compiler;

/// <summary>
///     Galatea 编译器 —— 将模型定义编译为可执行的 CompiledGraph
/// </summary>
public static class GalateaCompiler
{
    /// <summary>
    ///     使用 GraphBuilder 编译模型（适用新 API）
    /// </summary>
    /// <param name="name">计算图名称</param>
    /// <param name="configure">配置构建器</param>
    /// <returns>编译后的计算图</returns>
    public static CompiledGraph Compile(string name, Action<GraphBuilder> configure)
    {
        var builder = new GraphBuilder(name);
        configure(builder);
        return builder.Build();
    }

    /// <summary>
    ///     编译拓扑蓝图（旧 API 兼容，直接返回拓扑）
    /// </summary>
    /// <param name="topology">待编译的拓扑蓝图</param>
    /// <returns>原始拓扑</returns>
    public static ITopology Compile(ITopology topology)
    {
        return topology;
    }
}

/// <summary>
///     计算图构建器 —— 声明式构建 CompiledGraph
/// </summary>
public class GraphBuilder
{
    private readonly List<string> _inputNames = [];
    private readonly string _name;
    private readonly List<GraphOperation> _operations = [];
    private readonly List<string> _outputNames = [];

    /// <summary>
    ///     创建计算图构建器
    /// </summary>
    /// <param name="name">计算图名称</param>
    public GraphBuilder(string name)
    {
        _name = name;
    }

    /// <summary>
    ///     声明计算图的输入
    /// </summary>
    /// <param name="name">输入名称</param>
    public void Input(string name)
    {
        _inputNames.Add(name);
    }

    /// <summary>
    ///     添加一个操作
    /// </summary>
    /// <param name="name">操作名称</param>
    /// <param name="opType">操作类型</param>
    /// <param name="config">操作配置</param>
    /// <param name="inputNames">输入名称列表</param>
    public void Operation(string name, string opType, Dictionary<string, object> config, params string[] inputNames)
    {
        _operations.Add(new GraphOperation
        {
            Name = name,
            OpType = opType,
            Config = config,
            InputNames = inputNames,
            Parameters = []
        });
    }

    /// <summary>
    ///     添加一个带参数的操作
    /// </summary>
    /// <param name="name">操作名称</param>
    /// <param name="opType">操作类型</param>
    /// <param name="config">操作配置</param>
    /// <param name="parameters">参数张量列表</param>
    /// <param name="inputNames">输入名称列表</param>
    public void ParameterizedOp(
        string name, string opType, Dictionary<string, object> config,
        ArrayND[] parameters, params string[] inputNames)
    {
        _operations.Add(new GraphOperation
        {
            Name = name,
            OpType = opType,
            Config = config,
            InputNames = inputNames,
            Parameters = parameters
        });
    }

    /// <summary>
    ///     声明计算图的输出
    /// </summary>
    /// <param name="name">输出对应的操作名称</param>
    public void Output(string name)
    {
        _outputNames.Add(name);
    }

    /// <summary>
    ///     构建 CompiledGraph
    /// </summary>
    public CompiledGraph Build()
    {
        return new CompiledGraph
        {
            Id = Guid.NewGuid().ToString(),
            InputNames = _inputNames.AsReadOnly(),
            OutputNames = _outputNames.AsReadOnly(),
            Operations = _operations
        };
    }
}