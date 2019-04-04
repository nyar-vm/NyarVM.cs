using Nyar.Dialect.Game.Nodes;
using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Game;

/// <summary>
///     Game 方言的 OA 接口定义。
///     该接口声明行为树、游戏状态、ECS 与平台能力相关操作。
/// </summary>
[Dialect("game")]
public interface IGame<T>
{
    /// <summary>
    ///     顺序节点。
    /// </summary>
    [Operator("sequence")]
    Term<T> Sequence(IReadOnlyList<Term<T>> children);

    /// <summary>
    ///     选择节点。
    /// </summary>
    [Operator("selector")]
    Term<T> Selector(IReadOnlyList<Term<T>> children);

    /// <summary>
    ///     并行节点。
    /// </summary>
    [Operator("parallel")]
    Term<T> Parallel(IReadOnlyList<Term<T>> children, ParallelPolicy policy);

    /// <summary>
    ///     装饰器节点。
    /// </summary>
    [Operator("decorator")]
    Term<T> Decorator(DecoratorType type, Term<T> child, Term<T>? parameter);

    /// <summary>
    ///     条件节点。
    /// </summary>
    [Operator("condition")]
    Term<T> Condition(Term<T> predicate);

    /// <summary>
    ///     动作节点。
    /// </summary>
    [Operator("action")]
    Term<T> Action(string actionName, IReadOnlyDictionary<string, Term<T>> parameters);

    /// <summary>
    ///     效用评分。
    /// </summary>
    [Operator("utility_score")]
    Term<T> UtilityScore(Term<T> context, Term<T> scorerFunction);

    /// <summary>
    ///     效用选择。
    /// </summary>
    [Operator("utility_select")]
    Term<T> UtilitySelect(IReadOnlyList<Term<T>> options);

    /// <summary>
    ///     规则匹配。
    /// </summary>
    [Operator("rule_match")]
    Term<T> RuleMatch(Term<T> condition, Term<T> action);

    /// <summary>
    ///     规则集。
    /// </summary>
    [Operator("rule_set")]
    Term<T> RuleSet(IReadOnlyList<Term<T>> rules, ConflictResolution resolution);

    /// <summary>
    ///     状态查询。
    /// </summary>
    [Operator("state_query")]
    Term<T> StateQuery(string entityId, string propertyPath);

    /// <summary>
    ///     状态更新。
    /// </summary>
    [Operator("state_update")]
    Term<T> StateUpdate(string entityId, string propertyPath, Term<T> value);

    /// <summary>
    ///     事件触发。
    /// </summary>
    [Operator("event_trigger")]
    Term<T> EventTrigger(string eventName, IReadOnlyDictionary<string, Term<T>> payload);

    /// <summary>
    ///     事件监听。
    /// </summary>
    [Operator("event_listen")]
    Term<T> EventListen(string eventPattern, Term<T> handler);

    /// <summary>
    ///     路径查找。
    /// </summary>
    [Operator("path_find")]
    Term<T> PathFind(Term<T> start, Term<T> goal, Term<T>? constraints);

    /// <summary>
    ///     感知查询。
    /// </summary>
    [Operator("perception_query")]
    Term<T> PerceptionQuery(string entityId, PerceptionType type, float radius);

    /// <summary>
    ///     生成实体。
    /// </summary>
    [Operator("spawn_entity")]
    Term<T> SpawnEntity(Term<T>? archetype);

    /// <summary>
    ///     销毁实体。
    /// </summary>
    [Operator("destroy_entity")]
    Term<T> DestroyEntity(Term<T> entity);

    /// <summary>
    ///     添加组件。
    /// </summary>
    [Operator("add_component")]
    Term<T> AddComponent(Term<T> entity, string componentType, IReadOnlyDictionary<string, Term<T>> fields);

    /// <summary>
    ///     获取组件字段。
    /// </summary>
    [Operator("get_component")]
    Term<T> GetComponent(Term<T> entity, string componentType, string fieldName);

    /// <summary>
    ///     设置组件字段。
    /// </summary>
    [Operator("set_component")]
    Term<T> SetComponent(Term<T> entity, string componentType, string fieldName, Term<T> value);

    /// <summary>
    ///     删除组件。
    /// </summary>
    [Operator("remove_component")]
    Term<T> RemoveComponent(Term<T> entity, string componentType);

    /// <summary>
    ///     检查组件。
    /// </summary>
    [Operator("has_component")]
    Term<T> HasComponent(Term<T> entity, string componentType);

    /// <summary>
    ///     定义组件。
    /// </summary>
    [Operator("define_component")]
    Term<T> DefineComponent(string componentName, IReadOnlyList<ComponentFieldDef> fields);

    /// <summary>
    ///     查询实体。
    /// </summary>
    [Operator("query_entities")]
    Term<T> QueryEntities(QuerySpec spec);

    /// <summary>
    ///     定义系统。
    /// </summary>
    [Operator("define_system")]
    Term<T> DefineSystem(
        string systemName,
        QuerySpec query,
        SystemPhase phase,
        IReadOnlyList<string> before,
        IReadOnlyList<string> after);

    /// <summary>
    ///     世界更新。
    /// </summary>
    [Operator("world_update")]
    Term<T> WorldUpdate(Term<T> deltaTime);

    /// <summary>
    ///     使用平台能力。
    /// </summary>
    [Operator("use_capability")]
    Term<T> UseCapability(CapabilityDomain capabilityType, string interfaceName, IReadOnlyList<Term<T>> arguments);

    /// <summary>
    ///     硬件信息。
    /// </summary>
    [Operator("hardware_info")]
    Term<T> HardwareInfo();

    /// <summary>
    ///     平台信息。
    /// </summary>
    [Operator("platform_info")]
    Term<T> PlatformInfo();

    /// <summary>
    ///     渠道守卫。
    /// </summary>
    [Operator("channel_guard")]
    Term<T> ChannelGuard(string channelName, Term<T> body);

    /// <summary>
    ///     插件声明。
    /// </summary>
    [Operator("plugin_decl")]
    Term<T> PluginDecl(string pluginName, string version, IReadOnlyList<string> channelFilter,
        IReadOnlyList<string> entryPoints);
}