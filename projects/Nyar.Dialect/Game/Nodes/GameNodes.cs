using Nyar.IR.Intent;

namespace Nyar.Dialect.Game.Nodes;

#region 行为树节点

[AlgebraNode]
public sealed partial record Sequence(IReadOnlyList<Id> children) : AlgebraNode;

[AlgebraNode]
public sealed partial record Selector(IReadOnlyList<Id> children) : AlgebraNode;

[AlgebraNode]
public sealed partial record Parallel(IReadOnlyList<Id> children, ParallelPolicy policy) : AlgebraNode;

public enum ParallelPolicy
{
    AllSuccess,
    AnySuccess,
    Majority
}

[AlgebraNode]
public sealed partial record Decorator(DecoratorType type, Id child, Id? parameter) : AlgebraNode;

public enum DecoratorType
{
    Invert,
    Repeat,
    UntilFail,
    Cooldown,
    Timeout
}

[AlgebraNode]
public sealed partial record Condition(Id predicate) : AlgebraNode;

[AlgebraNode]
public sealed partial record Action(string actionName, IReadOnlyDictionary<string, Id> parameters) : AlgebraNode;

#endregion

#region 决策逻辑节点

[AlgebraNode]
public sealed partial record UtilityScore(Id context, Id scorerFunction) : AlgebraNode;

[AlgebraNode]
public sealed partial record UtilitySelect(IReadOnlyList<Id> options) : AlgebraNode;

[AlgebraNode]
public sealed partial record RuleMatch(Id condition, Id action) : AlgebraNode;

[AlgebraNode]
public sealed partial record RuleSet(IReadOnlyList<Id> rules, ConflictResolution resolution) : AlgebraNode;

public enum ConflictResolution
{
    Priority,
    Recency,
    Specificity,
    Utility
}

#endregion

#region 游戏状态节点

[AlgebraNode]
public sealed partial record StateQuery(string entityId, string propertyPath) : AlgebraNode;

[AlgebraNode]
public sealed partial record StateUpdate(string entityId, string propertyPath, Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record EventTrigger(string eventName, IReadOnlyDictionary<string, Id> payload) : AlgebraNode;

[AlgebraNode]
public sealed partial record EventListen(string eventPattern, Id handler) : AlgebraNode;

#endregion

#region 导航与感知节点

[AlgebraNode]
public sealed partial record PathFind(Id start, Id goal, Id? constraints) : AlgebraNode;

[AlgebraNode]
public sealed partial record PerceptionQuery(string entityId, PerceptionType type, float radius) : AlgebraNode;

public enum PerceptionType
{
    Visual,
    Auditory,
    Proximity,
    Memory
}

#endregion

#region ECS 实体组件系统节点

[AlgebraNode]
public sealed partial record SpawnEntity(Id? archetype) : AlgebraNode;

[AlgebraNode]
public sealed partial record DestroyEntity(Id entity) : AlgebraNode;

[AlgebraNode]
public sealed partial record AddComponent(Id entity, string componentType, IReadOnlyDictionary<string, Id> fields)
    : AlgebraNode;

[AlgebraNode]
public sealed partial record GetComponent(Id entity, string componentType, string fieldName) : AlgebraNode;

[AlgebraNode]
public sealed partial record SetComponent(Id entity, string componentType, string fieldName, Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record RemoveComponent(Id entity, string componentType) : AlgebraNode;

[AlgebraNode]
public sealed partial record HasComponent(Id entity, string componentType) : AlgebraNode;

[AlgebraNode]
public sealed partial record DefineComponent(string componentName, IReadOnlyList<ComponentFieldDef> fields) : AlgebraNode;

public sealed record ComponentFieldDef(string Name, string TypeName, Id? DefaultValue);

[AlgebraNode]
public sealed partial record QueryEntities(QuerySpec spec) : AlgebraNode;

public sealed record QuerySpec(
    IReadOnlyList<string> All,
    IReadOnlyList<string> Any,
    IReadOnlyList<string> None,
    bool Changed);

[AlgebraNode]
public sealed partial record DefineSystem(
    string systemName,
    QuerySpec query,
    SystemPhase phase,
    IReadOnlyList<string> before,
    IReadOnlyList<string> after) : AlgebraNode;

public enum SystemPhase
{
    Initialization,
    PreUpdate,
    Update,
    PostUpdate,
    PreRender,
    Render,
    PostRender,
    Cleanup
}

[AlgebraNode]
public sealed partial record WorldUpdate(Id deltaTime) : AlgebraNode;

#endregion

#region PAL/HAL 平台能力节点

/// <summary>
///     使用平台能力节点
///     HAL.use::&lt;T&gt;() 编译期检查，PAL.use::&lt;T&gt;() 运行期查询
/// </summary>
/// <param name="CapabilityType">能力类型（HAL 或 PAL）。</param>
/// <param name="InterfaceName">能力接口名称（如 IRenderer、ILoginService）。</param>
/// <param name="Arguments">调用参数。</param>
[AlgebraNode]
public sealed partial record UseCapability(
    CapabilityDomain capabilityType,
    string interfaceName,
    IReadOnlyList<Id> arguments) : AlgebraNode;

/// <summary>
///     能力域：HAL 编译期绑定，PAL 运行期查询
/// </summary>
public enum CapabilityDomain
{
    /// <summary>
    ///     编译期绑定，总是存在
    /// </summary>
    HAL,

    /// <summary>
    ///     运行期查询，可能缺失
    /// </summary>
    PAL
}

/// <summary>
///     硬件信息节点
///     HAL.hardware 静态属性，编译期注入
/// </summary>
[AlgebraNode]
public sealed partial record HardwareInfo : AlgebraNode;

/// <summary>
///     平台信息节点
///     PAL.platform 静态属性，运行期确定
/// </summary>
[AlgebraNode]
public sealed partial record PlatformInfo : AlgebraNode;

/// <summary>
///     Channel 条件守卫节点
///     #channel(steam) { ... } 编译期根据 Channel 宏过滤
/// </summary>
/// <param name="ChannelName">渠道名称（如 Steam、WeChat）。</param>
/// <param name="Body">守卫体。</param>
[AlgebraNode]
public sealed partial record ChannelGuard(string channelName, Id body) : AlgebraNode;

/// <summary>
///     插件元信息声明节点
///     @plugin(name, version, channels: [...]) 编译期提取
/// </summary>
/// <param name="PluginName">插件名称。</param>
/// <param name="Version">版本号。</param>
/// <param name="ChannelFilter">渠道过滤器（空列表表示所有渠道）。</param>
/// <param name="EntryPoints">入口点列表。</param>
[AlgebraNode]
public sealed partial record PluginDecl(
    string pluginName,
    string version,
    IReadOnlyList<string> channelFilter,
    IReadOnlyList<string> entryPoints) : AlgebraNode;

#endregion