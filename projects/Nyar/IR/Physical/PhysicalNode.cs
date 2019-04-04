using System.Collections.Immutable;
using Nyar.IR.Intent;

namespace Nyar.IR.Physical;

/// <summary>
///     物理层节点基类 - 表达"在哪里执行"的寄存器分配、内存地址和统一分派
/// </summary>
public abstract partial record PhysicalNode : AlgebraNode
{
    #region 空间层

    /// <summary>
    ///     管道
    /// </summary>
    /// <param name="input">输入。</param>
    /// <param name="output">输出。</param>
    [AlgebraNode]
    public sealed partial record Pipe(Id input, Id output) : PhysicalNode;

    /// <summary>
    ///     寄存器
    /// </summary>
    /// <param name="value">寄存器值。</param>
    [AlgebraNode]
    public sealed partial record Reg(Id value) : PhysicalNode;

    #endregion

    #region 资源管理

    /// <summary>
    ///     克隆资源
    /// </summary>
    /// <param name="value">目标资源。</param>
    [AlgebraNode]
    public sealed partial record ResourceClone(Id value) : PhysicalNode;

    /// <summary>
    ///     释放资源
    /// </summary>
    /// <param name="value">目标资源。</param>
    [AlgebraNode]
    public sealed partial record ResourceDrop(Id value) : PhysicalNode;

    #endregion

    #region 指针操作

    /// <summary>
    ///     取地址
    /// </summary>
    /// <param name="value">目标值。</param>
    [AlgebraNode]
    public sealed partial record AddrOf(Id value) : PhysicalNode;

    /// <summary>
    ///     解引用
    /// </summary>
    /// <param name="pointer">指针。</param>
    [AlgebraNode]
    public sealed partial record Deref(Id pointer) : PhysicalNode;

    /// <summary>
    ///     指针偏移
    /// </summary>
    /// <param name="pointer">基指针。</param>
    /// <param name="offset">偏移量。</param>
    [AlgebraNode]
    public sealed partial record PtrOffset(Id pointer, Id offset) : PhysicalNode;

    #endregion

    #region 效应相关

    /// <summary>
    ///     效应约束
    /// </summary>
    /// <param name="effect">效应名称。</param>
    [AlgebraNode]
    public sealed partial record EffectConstraint(string effect) : PhysicalNode;

    /// <summary>
    ///     效应行，表示一组效应的并集（如 {Pure | IO | State}）
    /// </summary>
    /// <param name="effects">效应集合，每个元素指向 EffectConstraint 或 EffectType 节点。</param>
    [AlgebraNode]
    public sealed partial record EffectRow(ImmutableArray<Id> effects) : PhysicalNode;

    /// <summary>
    ///     效应执行操作，在效应上下文中触发一个效应（如 perform Log("hello")）
    ///     对应代数效应系统中的"唤起效应"语义
    /// </summary>
    /// <param name="effect_name">效应名称。</param>
    /// <param name="payload">效应携带的数据。</param>
    [AlgebraNode]
    public sealed partial record EffectPerform(string effect_name, Id payload) : PhysicalNode;

    /// <summary>
    ///     效应处理器，捕获并处理效应操作（如 handle { perform Log(msg) => resume(unit) }）
    /// </summary>
    /// <param name="body">被处理的代码体。</param>
    /// <param name="effect_arms">效应处理分支列表。</param>
    [AlgebraNode]
    public sealed partial record EffectHandle(Id body, ImmutableArray<Id> effect_arms) : PhysicalNode;

    /// <summary>
    ///     效应处理分支，定义对特定效应的处理逻辑（如 perform Log(msg) => { println(msg); resume(unit) }）
    /// </summary>
    /// <param name="effect_name">要处理的效应名称。</param>
    /// <param name="payload_variable">效应数据绑定变量名。</param>
    /// <param name="resume_variable">恢复续延绑定的变量名（可选）。</param>
    /// <param name="handler_body">处理逻辑体。</param>
    [AlgebraNode]
    public sealed partial record EffectArm(
        string effect_name,
        string payload_variable,
        string? resume_variable,
        Id handler_body) : PhysicalNode;

    #endregion

    #region 统一对象模型分派

    /// <summary>
    ///     统一调用节点，根据 DispatchKind 选择分派策略。
    ///     JIT 去虚拟化可在同一节点上改写 DispatchKind 而不改变节点类型。
    /// </summary>
    /// <param name="dispatch">分派策略。</param>
    /// <param name="target">调用目标（Static 时为函数 ID，Witness/Dynamic 时为接收者 ID）。</param>
    /// <param name="arguments">参数列表。</param>
    /// <param name="witness">见证表 ID（Dynamic 分派时使用）。</param>
    /// <param name="method_index">方法槽索引（Static 分派时使用）。</param>
    /// <param name="method_name">方法名称（Dynamic 分派时使用）。</param>
    /// <param name="inline_cache_version">内联缓存版本号（JIT 去虚拟化时使用）。</param>
    [AlgebraNode]
    public sealed partial record Call(
        DispatchKind dispatch,
        Id target,
        ImmutableArray<Id> arguments,
        Id? witness = null,
        int? method_index = null,
        string? method_name = null,
        uint? inline_cache_version = null)
        : PhysicalNode;

    /// <summary>
    ///     统一字段访问节点，根据 DispatchKind 选择分派策略。
    ///     支持 JIT 去虚拟化在同一节点上改写 DispatchKind。
    /// </summary>
    /// <param name="dispatch">分派策略。</param>
    /// <param name="object">目标对象。</param>
    /// <param name="field_index">字段槽索引（Static/Witness 分派时使用，Dynamic 时可为 0）。</param>
    /// <param name="field_name">字段名称（Dynamic 分派时使用）。</param>
    /// <param name="witness">见证表 ID（Witness 分派时使用）。</param>
    /// <param name="inline_cache_version">内联缓存版本号（JIT 去虚拟化时使用）。</param>
    [AlgebraNode]
    public sealed partial record Access(
        DispatchKind dispatch,
        Id @object,
        int field_index,
        string? field_name = null,
        Id? witness = null,
        uint? inline_cache_version = null) : PhysicalNode;

    #endregion

    #region SQL 物理算子

    /// <summary>
    ///     全表扫描
    /// </summary>
    /// <param name="table_name">表名。</param>
    [AlgebraNode]
    public sealed partial record Scan(string table_name) : PhysicalNode;

    /// <summary>
    ///     排序
    /// </summary>
    /// <param name="order_spec">排序规格（含列和方向）。</param>
    /// <param name="source">数据源。</param>
    [AlgebraNode]
    public sealed partial record SqlSort(Id order_spec, Id source) : PhysicalNode;

    /// <summary>
    ///     LIMIT/OFFSET
    /// </summary>
    /// <param name="count">返回行数。</param>
    /// <param name="offset">偏移行数。</param>
    /// <param name="source">数据源。</param>
    [AlgebraNode]
    public sealed partial record SqlLimit(int count, int offset, Id source) : PhysicalNode;

    /// <summary>
    ///     列投影
    /// </summary>
    /// <param name="columns_spec">列规格。</param>
    /// <param name="source">数据源。</param>
    [AlgebraNode]
    public sealed partial record SqlProject(Id columns_spec, Id source) : PhysicalNode;

    /// <summary>
    ///     聚合（GROUP BY + 聚合函数）
    /// </summary>
    /// <param name="group_spec">分组列规格。</param>
    /// <param name="agg_spec">聚合规格。</param>
    /// <param name="source">数据源。</param>
    [AlgebraNode]
    public sealed partial record SqlAggregate(Id group_spec, Id agg_spec, Id source) : PhysicalNode;

    /// <summary>
    ///     SQL 连接操作，携带连接类型信息
    /// </summary>
    /// <param name="join_type">连接类型（0=Inner, 1=Left, 2=Right, 3=Full）。</param>
    /// <param name="left">左数据源。</param>
    /// <param name="right">右数据源。</param>
    /// <param name="condition">连接条件。</param>
    [AlgebraNode]
    public sealed partial record SqlJoin(int join_type, Id left, Id right, Id condition) : PhysicalNode;

    /// <summary>
    ///     SQL 子查询谓词，用于 IN/EXISTS/NOT IN/NOT EXISTS 子查询
    /// </summary>
    /// <param name="subquery_kind">子查询类型（0=In, 1=NotIn, 2=Exists, 3=NotExists）。</param>
    /// <param name="left">左表达式（IN 时为列引用，EXISTS 时为空）。</param>
    /// <param name="right">子查询数据源。</param>
    [AlgebraNode]
    public sealed partial record SqlSubqueryPred(int subquery_kind, Id left, Id right) : PhysicalNode;

    #endregion

    #region Oa 接口实现覆盖

    /// <inheritdoc />
    public override IReadOnlyList<Id> child_ids()
    {
        return [];
    }

    /// <inheritdoc />
    public override PhysicalNode map_children(Func<Id, Id> f)
    {
        return this;
    }

    #endregion
}