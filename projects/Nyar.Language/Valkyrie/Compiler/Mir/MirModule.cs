using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Types.Externals;

namespace Nyar.Language.Valkyrie.Compiler.Mir;

/// <summary>
///     MIR 模块，封装 EGraph + Oa 及 witness 绑定
/// </summary>
public sealed class MirModule
{
    private readonly Dictionary<string, IReadOnlyList<HirAttribute>> _attributes = new(StringComparer.Ordinal);
    private readonly List<HirDataDeriveRequest> _dataDeriveRequests = [];
    private readonly Dictionary<string, EffectSet> _effectSets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<ExternalImport>> _externalImportLinks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<ExternalImport>> _typeExternalImportLinks = new(StringComparer.Ordinal);
    private readonly HashSet<string> _exportedFunctions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _intrinsics = new(StringComparer.Ordinal);

    private readonly Dictionary<string, IReadOnlyList<HirTypeRef>> _parameterTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HirTypeRef> _returnTypes = new(StringComparer.Ordinal);

    /// <summary>
    ///     EGraph 图
    /// </summary>
    public readonly EGraph<AlgebraNode> graph;

    /// <summary>
    ///     模块名称
    /// </summary>
    public readonly string name;

    /// <summary>
    ///     当前目标后端名称。
    /// </summary>
    public readonly string target_backend;

    /// <summary>
    ///     witness 分派绑定列表
    /// </summary>
    public readonly List<MirWitnessDispatchBinding> witness_bindings = [];

    /// <summary>
    ///     提取结果（优化后的 Oa 表达式树）
    /// </summary>
    public MirTree? extraction_result;

    /// <summary>
    ///     优化统计
    /// </summary>
    public OptimizationStats? optimization_stats;

    /// <summary>
    ///     根节点 Id（可选）
    /// </summary>
    public Id? root;

    /// <summary>
    ///     初始化 MIR 模块
    /// </summary>
    /// <param name="name">模块名称</param>
    /// <param name="graph">EGraph 图</param>
    public MirModule(string name, EGraph<AlgebraNode> graph, string targetBackend)
    {
        this.name = name;
        this.graph = graph;
        target_backend = targetBackend;
    }

    /// <summary>
    ///     兼容未显式指定目标后端的构造方式。
    /// </summary>
    public MirModule(string name, EGraph<AlgebraNode> graph)
        : this(name, graph, string.Empty)
    {
    }

    /// <summary>
    ///     兼容测试中直接指定根节点的构造方式。
    /// </summary>
    public MirModule(string name, EGraph<AlgebraNode> graph, Id root)
        : this(name, graph, string.Empty)
    {
        this.root = root;
    }

    /// <summary>
    ///     添加 witness 分派绑定
    /// </summary>
    /// <param name="binding">witness 绑定</param>
    public void add_witness_binding(MirWitnessDispatchBinding binding)
    {
        witness_bindings.Add(binding);
    }

    /// <summary>
    ///     设置函数的参数类型
    /// </summary>
    public void set_parameter_types(string functionName, IReadOnlyList<HirTypeRef> parameterTypes)
    {
        _parameterTypes[functionName] = parameterTypes;
    }

    /// <summary>
    ///     设置函数的返回类型
    /// </summary>
    public void set_return_type(string functionName, HirTypeRef returnType)
    {
        _returnTypes[functionName] = returnType;
    }

    /// <summary>
    ///     设置函数的属性注解
    /// </summary>
    public void set_function_attributes(string functionName, IReadOnlyList<HirAttribute> attributes)
    {
        _attributes[functionName] = attributes;
    }

    /// <summary>
    ///     设置当前模块的 `[data]` 派生请求。
    ///     这里保留的是“结构类型 <-> 中性结构化容器”的请求元数据，
    ///     不绑定具体容器实现，也不绑定文本格式。
    /// </summary>
    public void set_data_derive_requests(IReadOnlyList<HirDataDeriveRequest> requests)
    {
        _dataDeriveRequests.Clear();
        _dataDeriveRequests.AddRange(requests);
    }

    /// <summary>
    ///     设置函数的外部导入链接。
    /// </summary>
    public void set_function_external_import_links(
        string functionName,
        IReadOnlyList<ExternalImport> externalImportLinks)
    {
        _externalImportLinks[functionName] = externalImportLinks;
    }

    /// <summary>
    ///     设置类型的外部导入链接。
    /// </summary>
    public void set_type_external_import_links(
        string typeName,
        IReadOnlyList<ExternalImport> externalImportLinks)
    {
        _typeExternalImportLinks[typeName] = externalImportLinks;
    }

    /// <summary>
    ///     设置函数的效应签名
    /// </summary>
    public void set_effect_set(string functionName, EffectSet effectSet)
    {
        _effectSets[functionName] = effectSet;
    }

    /// <summary>
    ///     设置函数的内建名称
    /// </summary>
    public void set_function_intrinsic(string functionName, string intrinsicName)
    {
        _intrinsics[functionName] = intrinsicName;
    }

    /// <summary>
    ///     标记函数为导出
    /// </summary>
    public void add_export_function(string functionName)
    {
        _exportedFunctions.Add(functionName);
    }

    /// <summary>
    ///     判断函数是否为导出函数
    /// </summary>
    public bool is_exported_function(string functionName)
    {
        return _exportedFunctions.Contains(functionName);
    }

    /// <summary>
    ///     尝试获取函数的参数类型
    /// </summary>
    public bool try_get_parameter_types(string functionName, out IReadOnlyList<HirTypeRef>? parameterTypes)
    {
        return _parameterTypes.TryGetValue(functionName, out parameterTypes);
    }

    /// <summary>
    ///     尝试获取函数的返回类型
    /// </summary>
    public bool try_get_return_type(string functionName, out HirTypeRef returnType)
    {
        return _returnTypes.TryGetValue(functionName, out returnType!);
    }

    /// <summary>
    ///     尝试获取函数的属性注解
    /// </summary>
    public bool try_get_function_attributes(string functionName, out IReadOnlyList<HirAttribute> attributes)
    {
        return _attributes.TryGetValue(functionName, out attributes!);
    }

    /// <summary>
    ///     尝试获取函数的外部导入链接。
    /// </summary>
    public bool try_get_function_external_import_links(
        string functionName,
        out IReadOnlyList<ExternalImport> externalImportLinks)
    {
        return _externalImportLinks.TryGetValue(functionName, out externalImportLinks!);
    }

    /// <summary>
    ///     尝试获取类型的外部导入链接。
    /// </summary>
    public bool try_get_type_external_import_links(
        string typeName,
        out IReadOnlyList<ExternalImport> externalImportLinks)
    {
        return _typeExternalImportLinks.TryGetValue(typeName, out externalImportLinks!);
    }

    /// <summary>
    ///     枚举所有类型级外部导入链接。
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, IReadOnlyList<ExternalImport>>> enumerate_type_external_import_links()
    {
        return [.. _typeExternalImportLinks];
    }

    /// <summary>
    ///     枚举所有具有元数据（属性、参数类型、返回类型等）的函数名。
    ///     包括有函数体的普通函数和无函数体的外部导入声明。
    /// </summary>
    public IReadOnlyList<string> enumerate_function_names()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in _attributes.Keys) names.Add(name);
        foreach (var name in _parameterTypes.Keys) names.Add(name);
        foreach (var name in _returnTypes.Keys) names.Add(name);
        foreach (var name in _externalImportLinks.Keys) names.Add(name);
        return [.. names];
    }

    /// <summary>
    ///     枚举当前模块携带的 `[data]` 派生请求。
    /// </summary>
    public IReadOnlyList<HirDataDeriveRequest> enumerate_data_derive_requests()
    {
        return [.. _dataDeriveRequests];
    }



    /// <summary>
    ///     尝试获取函数的效应签名
    /// </summary>
    public bool try_get_effect_set(string functionName, out EffectSet effectSet)
    {
        return _effectSets.TryGetValue(functionName, out effectSet!);
    }

    /// <summary>
    ///     尝试获取函数的内建名称
    /// </summary>
    public bool try_get_function_intrinsic(string functionName, out string intrinsicName)
    {
        return _intrinsics.TryGetValue(functionName, out intrinsicName!);
    }

    /// <summary>
    ///     尝试获取函数的完整签名（参数类型和返回类型）
    /// </summary>
    public bool try_get_function_signature(string functionName, out IReadOnlyList<HirTypeRef> parameterTypeNames,
        out HirTypeRef returnTypeName)
    {
        if (_parameterTypes.TryGetValue(functionName, out var parameterTypes) &&
            _returnTypes.TryGetValue(functionName, out var returnType))
        {
            parameterTypeNames = parameterTypes;
            returnTypeName = returnType;
            return true;
        }

        parameterTypeNames = [];
        returnTypeName = null!;
        return false;
    }
}
