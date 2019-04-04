using System.Collections.Immutable;
using Nyar.EGraph;

namespace Nyar.IR.Intent;

/// <summary>
///     OA Intent 抽象基类，表示编译器中间表示中的所有意图操作。
///     OA 方言节点通过 SourceGenerator 从此类派生。
/// </summary>
/// <remarks>
///     ⛔ 此类型已冻结，禁止新增子类。
///     新的方言操作符应通过 IOperatorDescriptor + ENode 的开放模型注册，
///     而不是继承 AlgebraNode 创建新的节点类。
///     详见 .trae/specs/oa-full-pipeline/spec.md。
/// </remarks>
[Obsolete("AlgebraNode 已冻结，禁止新增子类。请使用 ENode + IOperatorDescriptor 开放模型")]
public abstract partial record AlgebraNode : ILanguage<AlgebraNode>
{
    #region ILanguage<AlgebraNode> 接口实现

    /// <summary>
    ///     获取节点的所有子节点标识符，默认返回空列表
    /// </summary>
    public virtual IReadOnlyList<Id> child_ids()
    {
        return [];
    }

    /// <summary>
    ///     对节点的所有子节点标识符应用映射函数，默认返回自身
    /// </summary>
    public virtual AlgebraNode map_children(Func<Id, Id> f)
    {
        return this;
    }

    #endregion

    #region 模块与导出

    /// <summary>
    ///     模块节点，表示一个完整的编译单元
    /// </summary>
    /// <param name="name">模块名称</param>
    /// <param name="children">模块成员列表</param>
    [AlgebraNode]
    public sealed partial record Module(string name, ImmutableArray<Id> children) : AlgebraNode;

    /// <summary>
    ///     导出节点，表示模块的导出项
    /// </summary>
    /// <param name="name">导出名称</param>
    /// <param name="value">导出值</param>
    [AlgebraNode]
    public sealed partial record Export(string name, Id value) : AlgebraNode;

    /// <summary>
    ///     Lambda 表达式节点
    /// </summary>
    /// <param name="parameters">参数名称列表</param>
    /// <param name="body">函数体</param>
    [AlgebraNode]
    public sealed partial record Lambda(ImmutableArray<string> parameters, Id body) : AlgebraNode;

    #endregion

    #region 对象与字段操作

    /// <summary>
    ///     创建新对象节点
    /// </summary>
    /// <param name="type_name">类型名称</param>
    /// <param name="field_count">字段数量</param>
    [AlgebraNode]
    public sealed partial record NewObject(string type_name, int field_count) : AlgebraNode;

    /// <summary>
    ///     获取字段节点
    /// </summary>
    /// <param name="object">目标对象</param>
    /// <param name="field_name">字段名称</param>
    [AlgebraNode]
    public sealed partial record GetField(Id @object, string field_name) : AlgebraNode;

    /// <summary>
    ///     设置字段节点
    /// </summary>
    /// <param name="object">目标对象</param>
    /// <param name="field_name">字段名称</param>
    /// <param name="value">要设置的值</param>
    [AlgebraNode]
    public sealed partial record SetField(Id @object, string field_name, Id value) : AlgebraNode;

    /// <summary>
    ///     获取索引节点
    /// </summary>
    /// <param name="object">目标对象</param>
    /// <param name="index">索引值</param>
    [AlgebraNode]
    public sealed partial record GetIndex(Id @object, Id index) : AlgebraNode;

    /// <summary>
    ///     设置索引节点
    /// </summary>
    /// <param name="object">目标对象</param>
    /// <param name="index">索引值</param>
    /// <param name="value">要设置的值</param>
    [AlgebraNode]
    public sealed partial record SetIndex(Id @object, Id index, Id value) : AlgebraNode;

    #endregion

    #region 控制流

    /// <summary>
    ///     顺序执行节点
    /// </summary>
    /// <param name="children">语句列表</param>
    [AlgebraNode]
    public sealed partial record Seq(ImmutableArray<Id> children) : AlgebraNode;

    /// <summary>
    ///     循环节点
    /// </summary>
    /// <param name="count">循环计数/条件</param>
    /// <param name="body">循环体</param>
    [AlgebraNode]
    public sealed partial record Repeat(Id count, Id body) : AlgebraNode;

    /// <summary>
    ///     返回节点
    /// </summary>
    /// <param name="value">返回值</param>
    [AlgebraNode]
    public sealed partial record Return(Id value) : AlgebraNode;

    /// <summary>
    ///     跳出当前循环。
    /// </summary>
    [AlgebraNode]
    public sealed partial record Break : AlgebraNode;

    /// <summary>
    ///     跳过当前循环剩余部分并继续下一轮。
    /// </summary>
    [AlgebraNode]
    public sealed partial record Continue : AlgebraNode;

    /// <summary>
    ///     抛出效应节点
    /// </summary>
    /// <param name="op">效应操作</param>
    /// <param name="resumeType">恢复类型</param>
    [AlgebraNode]
    public sealed partial record Raise(Id op, Id resume_type) : AlgebraNode;

    /// <summary>
    ///     恢复效应节点
    /// </summary>
    /// <param name="value">恢复值</param>
    [AlgebraNode]
    public sealed partial record Resume(Id value) : AlgebraNode;

    /// <summary>
    ///     Try 表达式节点
    /// </summary>
    /// <param name="captureEffects">捕获的效应</param>
    /// <param name="body">Try 体</param>
    [AlgebraNode]
    public sealed partial record Try(Id capture_effects, Id body) : AlgebraNode;

    /// <summary>
    ///     Catch 表达式节点
    /// </summary>
    /// <param name="value">捕获值</param>
    [AlgebraNode]
    public sealed partial record Catch(Id value) : AlgebraNode;

    /// <summary>
    ///     变量声明节点
    /// </summary>
    /// <param name="annotations">注解列表</param>
    /// <param name="modifiers">修饰符列表</param>
    /// <param name="name">变量名称</param>
    /// <param name="type">变量类型（可为空）</param>
    /// <param name="value">初始值（可为空）</param>
    [AlgebraNode]
    public sealed partial record VarDecl(
        ImmutableArray<Id> annotations,
        ImmutableArray<string> modifiers,
        string name,
        Id? type = null,
        Id? value = null) : AlgebraNode;

    /// <summary>
    ///     条件选择节点（if-then-else），将 AST 级 Pattern 匹配降级为简单控制流
    /// </summary>
    /// <param name="condition">条件表达式</param>
    /// <param name="then">条件为真时的分支</param>
    /// <param name="else">条件为假时的分支</param>
    [AlgebraNode]
    public sealed partial record Choice(Id condition, Id then, Id @else) : AlgebraNode;

    #endregion

    #region 常量与字面量

    /// <summary>
    ///     整数常量节点
    /// </summary>
    /// <param name="value">常量值</param>
    [AlgebraNode]
    public sealed partial record Constant(long value) : AlgebraNode;

    /// <summary>
    ///     浮点常量节点
    /// </summary>
    /// <param name="value">常量值</param>
    [AlgebraNode]
    public sealed partial record FloatConstant(double value) : AlgebraNode;

    /// <summary>
    ///     字符串常量节点
    /// </summary>
    /// <param name="value">常量值</param>
    [AlgebraNode]
    public sealed partial record StringConstant(string value) : AlgebraNode;

    /// <summary>
    ///     布尔常量节点
    /// </summary>
    /// <param name="value">常量值</param>
    [AlgebraNode]
    public sealed partial record BooleanConstant(bool value) : AlgebraNode;

    /// <summary>
    ///     空值节点
    /// </summary>
    [AlgebraNode]
    public sealed partial record None : AlgebraNode;

    /// <summary>
    ///     符号引用节点
    /// </summary>
    /// <param name="name">符号名称</param>
    [AlgebraNode]
    public sealed partial record Symbol(string name) : AlgebraNode;

    #endregion

    #region 类型与运算

    /// <summary>
    ///     类型转换节点
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <param name="target_type">目标类型</param>
    [AlgebraNode]
    public sealed partial record Cast(Id value, Id target_type) : AlgebraNode;

    /// <summary>
    ///     类型引用节点
    /// </summary>
    /// <param name="name">类型名称</param>
    /// <param name="type_args">类型参数列表</param>
    [AlgebraNode]
    public sealed partial record TypeRef(string name, ImmutableArray<Id> type_args) : AlgebraNode;

    #endregion

    #region 状态与集合

    /// <summary>
    ///     状态更新节点
    /// </summary>
    /// <param name="target">目标引用</param>
    /// <param name="value">新值</param>
    [AlgebraNode]
    public sealed partial record StateUpdate(Id target, Id value) : AlgebraNode;

    /// <summary>
    ///     数组字面量节点
    /// </summary>
    /// <param name="elements">元素列表</param>
    [AlgebraNode]
    public sealed partial record ArrayLiteral(ImmutableArray<Id> elements) : AlgebraNode;

    #endregion
}
