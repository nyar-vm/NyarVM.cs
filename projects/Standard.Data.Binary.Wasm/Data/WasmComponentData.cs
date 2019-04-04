namespace Std.Data.Binary.Wasm.Data;

/// <summary>
///     WASM 组件顶级数据结构，表的Component Model 二进制格式的
///     Component 的Core Module 的主要区别：
///     1. 版本号为 0x0A (WASI p2) 而非 0x01
///     2. 支持嵌套组件/模块
///     3. 支持 Canonical ABI 的lift/lower 操作
/// </summary>
public sealed class WasmComponentData
{
    /// <summary>
    ///     Component 中的类型定义，包括模块类型、实例类型、函数类型等的
    /// </summary>
    public IReadOnlyList<WasmComponentType> types { get; init; } = [];

    /// <summary>
    ///     Component 的导入项（导入核心模块、导入实例、导入函数等）的
    /// </summary>
    public IReadOnlyList<WasmComponentImport> imports { get; init; } = [];

    /// <summary>
    ///     Component 的嵌套组件定义的
    /// </summary>
    public IReadOnlyList<WasmComponentNestedComponent> nested_components { get; init; } = [];

    /// <summary>
    ///     Component 的导出项的
    /// </summary>
    public IReadOnlyList<WasmComponentExport> exports { get; init; } = [];
}

/// <summary>
///     Component 类型定义抽象基类的
/// </summary>
public abstract record WasmComponentType;

/// <summary>
///     模块类型定义，描述一个核的WASM 模块的类型签名的
///     包含该模块的类型段索引列表以及导的导出签名的
/// </summary>
public sealed record WasmComponentModuleType : WasmComponentType
{
    /// <summary>
    ///     模块内部核心类型的索引列表的
    /// </summary>
    public IReadOnlyList<uint> core_type_indices { get; init; } = [];
}

/// <summary>
///     实例类型定义，描述一个导入或导出的实例的类型的
/// </summary>
public sealed record WasmComponentInstanceType : WasmComponentType
{
    /// <summary>
    ///     实例的导出项类型列表的
    /// </summary>
    public IReadOnlyList<(string Name, WasmComponentType Type)> export_types { get; init; } = [];
}

/// <summary>
///     组件函数类型定义，描述参数和结果类型（支的WIT 类型）的
/// </summary>
public sealed record WasmComponentFuncType : WasmComponentType
{
    /// <summary>
    ///     函数参数列表，每个参数为 (名称, 类型索引)的
    /// </summary>
    public IReadOnlyList<(string Name, uint TypeIndex)> parameters { get; init; } = [];

    /// <summary>
    ///     函数返回类型索引列表的
    /// </summary>
    public IReadOnlyList<uint> results { get; init; } = [];
}

/// <summary>
///     Component 导入项，对应 component 格式中的 import 段的
/// </summary>
public sealed class WasmComponentImport
{
    /// <summary>
    ///     导入源模块名（外部依赖的组件名）的
    /// </summary>
    public string module_name { get; init; } = string.Empty;

    /// <summary>
    ///     导入项在源模块中的名称的
    /// </summary>
    public string field_name { get; init; } = string.Empty;

    /// <summary>
    ///     导入项的类型索引，指的Types 列表的
    /// </summary>
    public uint type_index { get; init; }
}

/// <summary>
///     Component 导出项，对应 component 格式中的 export 段的
/// </summary>
public sealed class WasmComponentExport
{
    /// <summary>
    ///     导出名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     导出项的排序索引，指向组件内的定义的
    /// </summary>
    public uint sort_index { get; init; }

    /// <summary>
    ///     导出项的类型索引的
    /// </summary>
    public uint type_index { get; init; }
}

/// <summary>
///     Component 嵌套组件，component 可以包含内部核心模块或子组件的
/// </summary>
public sealed class WasmComponentNestedComponent
{
    /// <summary>
    ///     嵌套的核的WASM 模块数据（如果嵌套的是模块）的
    /// </summary>
    public WasmModuleData? core_module { get; init; }

    /// <summary>
    ///     嵌套的子组件数据（如果嵌套的是组件）的
    /// </summary>
    public WasmComponentData? sub_component { get; init; }
}

/// <summary>
///     WASM Component 版本标识的
/// </summary>
public static class WasmComponentVersion
{
    /// <summary>
    ///     WASI Preview 2 Component Model 版本号（兼容 WIT 类型系统）的
    /// </summary>
    public const uint version = 0x0A;

    /// <summary>
    ///     WASM Sonic.Core (MVP) 版本号的
    /// </summary>
    public const uint core_version = 0x01;
}