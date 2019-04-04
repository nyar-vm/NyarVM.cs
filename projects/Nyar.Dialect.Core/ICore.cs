using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Core;

/// <summary>
///     Core 方言的 Object Algebra 接口定义。
///     包含值、运算、数据聚合、内存、控制流和效应六类操作。
/// </summary>
[Dialect("core", may_have_children = true)]
public interface ICore<T>
{
    #region 值

    /// <summary>
    ///     字面量值
    /// </summary>
    /// <param name="value">常量值。</param>
    /// <returns>常量表达式。</returns>
    [Operator("lit")]
    Term<T> literal<TValue>(TValue value);

    #endregion

    #region 特性组

    /// <summary>
    ///     特性组，包装多个特性节点
    /// </summary>
    /// <param name="children">特性列表。</param>
    [Operator("attrgroup")]
    Term<T> attr_group(Term<T>[] children);

    #endregion

    #region 纯运算

    /// <summary>
    ///     整数加法
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>加法结果。</returns>
    [Operator("add")]
    Term<T> add(Term<T> left, Term<T> right);

    /// <summary>
    ///     整数减法
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>减法结果。</returns>
    [Operator("sub")]
    Term<T> sub(Term<T> left, Term<T> right);

    /// <summary>
    ///     整数乘法
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>乘法结果。</returns>
    [Operator("mul")]
    Term<T> mul(Term<T> left, Term<T> right);

    /// <summary>
    ///     整数除法
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>除法结果。</returns>
    [Operator("div")]
    Term<T> div(Term<T> left, Term<T> right);

    /// <summary>
    ///     整数取余
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>取余结果。</returns>
    [Operator("rem")]
    Term<T> rem(Term<T> left, Term<T> right);

    /// <summary>
    ///     整数取负
    /// </summary>
    /// <param name="operand">操作数。</param>
    /// <returns>取负结果。</returns>
    [Operator("neg")]
    Term<T> neg(Term<T> operand);

    /// <summary>
    ///     逻辑非
    /// </summary>
    /// <param name="operand">操作数。</param>
    /// <returns>逻辑非结果。</returns>
    [Operator("not")]
    Term<bool> not(Term<bool> operand);

    /// <summary>
    ///     逻辑与
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>逻辑与结果。</returns>
    [Operator("and")]
    Term<bool> and(Term<bool> left, Term<bool> right);

    /// <summary>
    ///     逻辑或
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>逻辑或结果。</returns>
    [Operator("or")]
    Term<bool> or(Term<bool> left, Term<bool> right);

    /// <summary>
    ///     比较
    /// </summary>
    /// <param name="op">比较操作符。</param>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>比较结果。</returns>
    [Operator("cmp")]
    Term<bool> cmp(CompareOp op, Term<T> left, Term<T> right);

    #endregion

    #region 数据聚合

    /// <summary>
    ///     元组构造
    /// </summary>
    /// <param name="elements">元素列表。</param>
    /// <returns>元组表达式。</returns>
    [Operator("tuple")]
    Term<T> tuple(Term<T>[] elements);

    /// <summary>
    ///     元组投影
    /// </summary>
    /// <param name="tuple">元组表达式。</param>
    /// <param name="index">投影索引。</param>
    /// <returns>投影结果。</returns>
    [Operator("project")]
    Term<T> project(Term<T> tuple, int index);

    #endregion

    #region 内存

    /// <summary>
    ///     内存分配
    /// </summary>
    /// <param name="size">分配大小。</param>
    /// <returns>分配的指针。</returns>
    [Operator("alloc")]
    Term<T> alloc(Term<T> size);

    /// <summary>
    ///     内存释放
    /// </summary>
    /// <param name="pointer">要释放的指针。</param>
    /// <returns>释放结果。</returns>
    [Operator("free")]
    Term<T> free(Term<T> pointer);

    /// <summary>
    ///     内存加载
    /// </summary>
    /// <param name="pointer">加载地址。</param>
    /// <param name="order">内存序。</param>
    /// <returns>加载的值。</returns>
    [Operator("load")]
    Term<T> load(Term<T> pointer, MemoryOrder order = MemoryOrder.none);

    /// <summary>
    ///     内存存储
    /// </summary>
    /// <param name="pointer">存储地址。</param>
    /// <param name="value">存储的值。</param>
    /// <param name="order">内存序。</param>
    /// <returns>存储结果。</returns>
    [Operator("store")]
    Term<T> store(Term<T> pointer, Term<T> value, MemoryOrder order = MemoryOrder.none);

    #endregion

    #region 控制流

    /// <summary>
    ///     标签
    /// </summary>
    /// <param name="name">标签名称。</param>
    /// <returns>标签表达式。</returns>
    [Operator("label")]
    Term<T> label(string name);

    /// <summary>
    ///     条件分支
    /// </summary>
    /// <param name="condition">分支条件。</param>
    /// <param name="trueLabel">条件为真时跳转的标签。</param>
    /// <param name="falseLabel">条件为假时跳转的标签。</param>
    /// <returns>分支结果。</returns>
    [Operator("branch")]
    Term<T> branch(Term<bool> condition, Term<T> trueLabel, Term<T> falseLabel);

    /// <summary>
    ///     Phi 节点
    /// </summary>
    /// <param name="labels">分支标签列表。</param>
    /// <param name="values">分支对应值列表。</param>
    /// <returns>Phi 结果。</returns>
    [Operator("phi")]
    Term<T> phi(Term<T>[] labels, Term<T>[] values);

    /// <summary>
    ///     函数调用
    /// </summary>
    /// <param name="function">被调用的函数。</param>
    /// <param name="arguments">调用参数。</param>
    /// <returns>调用结果。</returns>
    [Operator("call")]
    Term<T> call(Term<T> function, Term<T>[] arguments);

    /// <summary>
    ///     返回
    /// </summary>
    /// <param name="value">返回值。</param>
    /// <returns>返回结果。</returns>
    [Operator("ret")]
    Term<T> ret(Term<T> value);

    #endregion

    #region 效应

    /// <summary>
    ///     执行效应
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="arguments">效应参数。</param>
    /// <returns>效应执行结果。</returns>
    [Operator("perform")]
    Term<T> perform(string effectName, Term<T>[] arguments);

    /// <summary>
    ///     处理效应
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="handler">效应处理器。</param>
    /// <param name="body">效应体。</param>
    /// <returns>效应处理结果。</returns>
    [Operator("handle")]
    Term<T> handle(string effectName, Term<T> handler, Term<T> body);

    #endregion

    #region 声明与符号

    /// <summary>
    ///     变量声明
    /// </summary>
    /// <param name="attributes">特性标注列表。</param>
    /// <param name="modifiers">修饰符列表。</param>
    /// <param name="name">声明名称。</param>
    /// <param name="type">可选类型引用。</param>
    /// <param name="value">可选初始值。</param>
    [Operator("vardecl")]
    Term<T> var_decl(Term<T>[] attributes, string[] modifiers, string name, Term<T>? type, Term<T>? value);

    /// <summary>
    ///     特性标注
    /// </summary>
    /// <param name="name">特性名称。</param>
    /// <param name="arguments">参数列表。</param>
    [Operator("attrib")]
    Term<T> attrib(string name, string[] arguments);

    /// <summary>
    ///     类型引用
    /// </summary>
    /// <param name="name">类型名称。</param>
    /// <param name="generic_args">泛型参数。</param>
    [Operator("typeref")]
    Term<T> type_ref(string name, Term<T>[] genericArgs);

    /// <summary>
    ///     泛型类型参数声明
    /// </summary>
    /// <param name="name">类型参数名。</param>
    /// <param name="constraints">类型约束列表。</param>
    /// <param name="default_type">默认类型。</param>
    [Operator("typeparam")]
    Term<T> type_param(string name, Term<T>[] constraints, Term<T>? defaultType);

    /// <summary>
    ///     泛型约束子句
    /// </summary>
    /// <param name="type_parameter_id">被约束的类型参数。</param>
    /// <param name="bounds">约束边界列表。</param>
    [Operator("tpconstraint")]
    Term<T> type_param_constraint(Term<T> typeParameterId, Term<T>[] bounds);

    /// <summary>
    ///     联合类型（A | B）
    /// </summary>
    /// <param name="members">联合成员类型列表。</param>
    [Operator("uniontype")]
    Term<T> union_type(Term<T>[] members);

    /// <summary>
    ///     交集类型（A and B）
    /// </summary>
    /// <param name="members">交集成员类型列表。</param>
    [Operator("isecttype")]
    Term<T> intersect_type(Term<T>[] members);

    /// <summary>
    ///     函数类型（micro(A, B) -> T）
    /// </summary>
    /// <param name="param_types">参数类型列表。</param>
    /// <param name="return_type">返回类型。</param>
    [Operator("functype")]
    Term<T> func_type(Term<T>[] paramTypes, Term<T> returnType);

    /// <summary>
    ///     列表类型 [T]
    /// </summary>
    /// <param name="element_type">元素类型。</param>
    [Operator("listtype")]
    Term<T> list_type(Term<T> elementType);

    /// <summary>
    ///     定长数组类型 [T; N]
    /// </summary>
    /// <param name="element_type">元素类型。</param>
    /// <param name="size">固定长度。</param>
    [Operator("arrtype")]
    Term<T> arr_type(Term<T> elementType, int size);

    /// <summary>
    ///     类型别名
    /// </summary>
    /// <param name="alias_name">别名。</param>
    /// <param name="target">目标类型引用。</param>
    [Operator("typealias")]
    Term<T> type_alias(string aliasName, Term<T> target);

    /// <summary>
    ///     符号引用
    /// </summary>
    /// <param name="name">符号名称。</param>
    [Operator("sym")]
    Term<T> sym(string name);

    /// <summary>
    ///     导入
    /// </summary>
    /// <param name="module_name">模块名称。</param>
    /// <param name="name">符号名称。</param>
    /// <param name="parameter_types">函数参数类型列表。</param>
    /// <param name="return_type">函数返回类型。</param>
    /// <param name="alias">源语言中的函数别名。</param>
    [Operator("import")]
    Term<T> import(string moduleName, string name, string[] parameterTypes, string? returnType, string? alias);

    /// <summary>
    ///     导出
    /// </summary>
    /// <param name="name">符号名称。</param>
    /// <param name="value">导出值。</param>
    [Operator("export")]
    Term<T> export(string name, Term<T> value);

    /// <summary>
    ///     模块
    /// </summary>
    /// <param name="name">模块名称。</param>
    /// <param name="children">模块成员。</param>
    [Operator("mod")]
    Term<T> mod(string name, Term<T>[] children);

    #endregion

    #region 函数与闭包

    /// <summary>
    ///     Lambda 表达式
    /// </summary>
    /// <param name="parameters">参数列表。</param>
    /// <param name="body">函数体。</param>
    [Operator("lambda")]
    Term<T> lambda(string[] parameters, Term<T> body);

    /// <summary>
    ///     函数应用
    /// </summary>
    /// <param name="function">函数引用。</param>
    /// <param name="arguments">参数列表。</param>
    [Operator("apply")]
    Term<T> apply(Term<T> function, Term<T>[] arguments);

    /// <summary>
    ///     闭包
    /// </summary>
    /// <param name="function">函数引用。</param>
    /// <param name="captured">捕获的变量。</param>
    [Operator("closure")]
    Term<T> closure(Term<T> function, Term<T>[] captured);

    #endregion

    #region 控制流扩展

    /// <summary>
    ///     条件选择
    /// </summary>
    /// <param name="condition">条件。</param>
    /// <param name="then">真分支。</param>
    /// <param name="else">假分支。</param>
    [Operator("choice")]
    Term<T> choice(Term<T> condition, Term<T> then, Term<T> elseBranch);

    /// <summary>
    ///     状态更新
    /// </summary>
    /// <param name="key">状态键。</param>
    /// <param name="value">状态值。</param>
    [Operator("stateup")]
    Term<T> state_up(Term<T> key, Term<T> value);

    /// <summary>
    ///     生命周期
    /// </summary>
    /// <param name="init">初始化。</param>
    /// <param name="final">终结。</param>
    [Operator("lifecycle")]
    Term<T> lifecycle(Term<T> init, Term<T> final);

    /// <summary>
    ///     元数据
    /// </summary>
    /// <param name="data">元数据内容。</param>
    [Operator("meta")]
    Term<T> meta(Term<T> data);

    /// <summary>
    ///     陷阱
    /// </summary>
    /// <param name="value">陷阱值。</param>
    [Operator("trap")]
    Term<T> trap(Term<T> value);

    /// <summary>
    ///     组合
    /// </summary>
    /// <param name="first">第一个操作。</param>
    /// <param name="second">第二个操作。</param>
    [Operator("compose")]
    Term<T> compose(Term<T> first, Term<T> second);

    /// <summary>
    ///     协程恢复
    /// </summary>
    /// <param name="value">返回值。</param>
    [Operator("resume")]
    Term<T> resume(Term<T> value);

    #endregion

    #region 数据结构

    /// <summary>
    ///     数组字面量
    /// </summary>
    /// <param name="elements">数组元素。</param>
    [Operator("arraylit")]
    Term<T> array_lit(Term<T>[] elements);

    /// <summary>
    ///     范围
    /// </summary>
    /// <param name="start">起始值。</param>
    /// <param name="end">结束值。</param>
    [Operator("range")]
    Term<T> range(Term<T> start, Term<T> end);

    /// <summary>
    ///     键值对
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="value">值。</param>
    [Operator("pair")]
    Term<T> pair(Term<T> key, Term<T> value);

    /// <summary>
    ///     表
    /// </summary>
    /// <param name="entries">表项。</param>
    [Operator("table")]
    Term<T> table(Term<T>[] entries);

    /// <summary>
    ///     获取序数索引
    /// </summary>
    /// <param name="object">对象。</param>
    /// <param name="index">序数索引。</param>
    [Operator("getordinalidx")]
    Term<T> get_ordinal_idx(Term<T> obj, Term<T> index);

    /// <summary>
    ///     设置序数索引
    /// </summary>
    /// <param name="object">对象。</param>
    /// <param name="index">序数索引。</param>
    /// <param name="value">值。</param>
    [Operator("setordinalidx")]
    Term<T> set_ordinal_idx(Term<T> obj, Term<T> index, Term<T> value);

    /// <summary>
    ///     获取偏移索引
    /// </summary>
    /// <param name="object">对象。</param>
    /// <param name="index">偏移索引。</param>
    [Operator("getoffsetidx")]
    Term<T> get_offset_idx(Term<T> obj, Term<T> index);

    /// <summary>
    ///     设置偏移索引
    /// </summary>
    /// <param name="object">对象。</param>
    /// <param name="index">偏移索引。</param>
    /// <param name="value">值。</param>
    [Operator("setoffsetidx")]
    Term<T> set_offset_idx(Term<T> obj, Term<T> index, Term<T> value);

    #endregion

    #region 对象模型

    /// <summary>
    ///     继承声明
    /// </summary>
    /// <param name="field_name">基类字段名。</param>
    /// <param name="base_type">基类类型引用。</param>
    /// <param name="modifiers">修饰词列表。</param>
    /// <param name="attributes">属性列表。</param>
    [Operator("inherit")]
    Term<T> inherit(string fieldName, Term<T> baseType, string[] modifiers, Term<T>[] attributes);

    /// <summary>
    ///     类定义
    /// </summary>
    /// <param name="name">类名。</param>
    /// <param name="inheritances">继承列表。</param>
    /// <param name="fields">字段列表。</param>
    /// <param name="body">类体。</param>
    [Operator("classdef")]
    Term<T> class_def(string name, Term<T>[] inheritances, Term<T>[] fields, Term<T> body);

    #endregion

    #region 模式匹配

    /// <summary>
    ///     模式匹配表达式
    /// </summary>
    /// <param name="value">匹配目标值。</param>
    /// <param name="arms">匹配分支列表。</param>
    [Operator("match")]
    Term<T> match(Term<T> value, Term<T>[] arms);

    /// <summary>
    ///     模式匹配分支
    /// </summary>
    /// <param name="pattern">匹配模式。</param>
    /// <param name="body">分支体。</param>
    /// <param name="guard">可选守卫条件。</param>
    [Operator("matcharm")]
    Term<T> match_arm(Term<T> pattern, Term<T> body, Term<T>? guard);

    /// <summary>
    ///     错误捕获表达式
    /// </summary>
    /// <param name="value">可能出错的值。</param>
    /// <param name="arms">捕获分支列表。</param>
    [Operator("catch")]
    Term<T> @catch(Term<T> value, Term<T>[] arms);

    /// <summary>
    ///     错误捕获分支
    /// </summary>
    /// <param name="pattern">错误模式。</param>
    /// <param name="body">处理体。</param>
    [Operator("catcharm")]
    Term<T> catch_arm(Term<T> pattern, Term<T> body);

    /// <summary>
    ///     字面量模式
    /// </summary>
    /// <param name="value">字面量值。</param>
    [Operator("litpat")]
    Term<T> lit_pattern(Term<T> value);

    /// <summary>
    ///     变量绑定模式
    /// </summary>
    /// <param name="name">绑定变量名。</param>
    /// <param name="type">可选类型标注。</param>
    [Operator("varpat")]
    Term<T> var_pattern(string name, Term<T>? type);

    /// <summary>
    ///     构造器模式
    /// </summary>
    /// <param name="constructor_name">构造器名称。</param>
    /// <param name="inner_patterns">内部子模式列表。</param>
    [Operator("ctorpat")]
    Term<T> ctor_pattern(string constructorName, Term<T>[] innerPatterns);

    /// <summary>
    ///     对象字段模式
    /// </summary>
    /// <param name="name">字段名。</param>
    /// <param name="pattern">字段对应的子模式。</param>
    [Operator("objpatfield")]
    Term<T> obj_pat_field(string name, Term<T> pattern);

    /// <summary>
    ///     对象模式
    /// </summary>
    /// <param name="type_name">对象或变体名称。</param>
    /// <param name="fields">带字段名的子模式列表。</param>
    [Operator("objpat")]
    Term<T> obj_pattern(string typeName, Term<T>[] fields);

    /// <summary>
    ///     通配符模式
    /// </summary>
    [Operator("wildpat")]
    Term<T> wild_pattern();

    /// <summary>
    ///     或模式
    /// </summary>
    /// <param name="alternatives">备选模式列表。</param>
    [Operator("orpat")]
    Term<T> or_pattern(Term<T>[] alternatives);

    /// <summary>
    ///     守卫条件
    /// </summary>
    /// <param name="pattern">主模式。</param>
    /// <param name="condition">守卫条件表达式。</param>
    [Operator("guardpat")]
    Term<T> guard_pattern(Term<T> pattern, Term<T> condition);

    /// <summary>
    ///     类型测试模式
    /// </summary>
    /// <param name="type_name">目标类型名。</param>
    [Operator("typetestpat")]
    Term<T> type_test_pattern(string typeName);

    #endregion

    #region 类型转换

    /// <summary>
    ///     显式类型转换
    /// </summary>
    /// <param name="value">待转换的值。</param>
    /// <param name="target_type">目标类型。</param>
    [Operator("cast")]
    Term<T> cast(Term<T> value, Term<T> targetType);

    /// <summary>
    ///     运行时类型检查
    /// </summary>
    /// <param name="value">待检查的值。</param>
    /// <param name="target_type">目标类型。</param>
    /// <returns>如果值属于目标类型则返回 true。</returns>
    [Operator("typecheck")]
    Term<bool> type_check(Term<T> value, Term<T> targetType);

    /// <summary>
    ///     安全类型转换（as? 的降级目标）
    /// </summary>
    /// <param name="value">待转换的值。</param>
    /// <param name="target_type">目标类型。</param>
    /// <returns>转换成功返回目标类型的值，失败返回 null/None。</returns>
    [Operator("asconvert")]
    Term<T> as_convert(Term<T> value, Term<T> targetType);

    /// <summary>
    ///     Row 类型结构上转
    /// </summary>
    /// <param name="value">待上转的值。</param>
    /// <param name="target_row_type">目标 row 类型。</param>
    /// <param name="kept_fields">保留的字段名列表。</param>
    /// <returns>上转后的值。</returns>
    [Operator("rowupcast")]
    Term<T> row_upcast(Term<T> value, Term<T> targetRowType, string[] keptFields);

    #endregion

    #region 数据操作

    /// <summary>
    ///     数据映射
    /// </summary>
    /// <param name="function">映射函数。</param>
    /// <param name="data">数据源。</param>
    [Operator("map")]
    Term<T> map(Term<T> function, Term<T> data);

    /// <summary>
    ///     数据过滤
    /// </summary>
    /// <param name="predicate">谓词。</param>
    /// <param name="data">数据源。</param>
    [Operator("filter")]
    Term<T> filter(Term<T> predicate, Term<T> data);

    /// <summary>
    ///     数据归约
    /// </summary>
    /// <param name="function">归约函数。</param>
    /// <param name="initial">初始值。</param>
    /// <param name="data">数据源。</param>
    [Operator("reduce")]
    Term<T> reduce(Term<T> function, Term<T> initial, Term<T> data);

    #endregion

    #region 循环降级

    /// <summary>
    ///     循环过滤
    /// </summary>
    /// <param name="predicate">谓词。</param>
    /// <param name="data">数据源。</param>
    [Operator("loopfilter")]
    Term<T> loop_filter(Term<T> predicate, Term<T> data);

    /// <summary>
    ///     循环映射
    /// </summary>
    /// <param name="function">映射函数。</param>
    /// <param name="data">数据源。</param>
    [Operator("loopmap")]
    Term<T> loop_map(Term<T> function, Term<T> data);

    /// <summary>
    ///     循环归约
    /// </summary>
    /// <param name="function">归约函数。</param>
    /// <param name="initial">初始值。</param>
    /// <param name="data">数据源。</param>
    [Operator("loopreduce")]
    Term<T> loop_reduce(Term<T> function, Term<T> initial, Term<T> data);

    #endregion

    #region 顺序与迭代

    /// <summary>
    ///     顺序组合，依次执行一组操作
    /// </summary>
    /// <param name="children">操作列表。</param>
    [Operator("seq")]
    Term<T> seq(Term<T>[] children);

    /// <summary>
    ///     重复执行，当条件为真时循环执行体
    /// </summary>
    /// <param name="condition">循环条件。</param>
    /// <param name="body">循环体。</param>
    [Operator("repeat")]
    Term<T> repeat(Term<T> condition, Term<T> body);

    #endregion
}