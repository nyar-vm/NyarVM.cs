using Std.Data.Text.Valkyrie.AST;

namespace Std.Data.Text.Valkyrie.Parser;

/// <summary>
///     AST 节点类型枚举，用于运行时按类型判别而不依赖反射
/// </summary>
/// <para>每种 AST 节点对应一个枚举值，通过 <see cref="ValkyrieNode.type" /> 属性获取</para>
public enum ValkyrieNodeType
{
    /// <summary>
    ///     未知节点类型（出错或未匹配时的默认值）
    /// </summary>
    unknown = -1,

    compilation_unit,
    component_decl,
    system_decl,
    widget_decl,
    function_decl,
    block_stmt,
    if_stmt,
    loop_statement,
    meta_expr_interpolation,
    meta_loop_stmt,
    meta_foreach_stmt,
    meta_if_stmt,
    meta_match_stmt,
    macro_ref_expr,
    meta_template_text,
    struct_decl,
    for_stmt,
    discard_stmt,
    swizzle_expr,
    using_decl,
    uniform_binding_decl,
    enum_decl,
    flags_decl,
    union_decl,
    unite_decl,
    storage_decl,
    service_decl,
    domain_decl,
    meta_decl,
    namespace_decl,
    variable_decl,
    field_decl,
    parameter_decl,
    import_decl,
    attribute_decl,
    doc_comment_decl,
    type_alias_decl,
    neural_decl,
    class_decl,
    shader_decl,
    varying_decl,
    constant_buffer_decl,
    texture_decl,
    sampler_decl,
    shader_attribute_decl,
    type_annotation,
    literal_expr,
    binary_expr,
    assignment_expr,
    member_access_expr,
    qualified_path_expr,
    identifier_node,
    unary_expr,
    lambda_expr,
    query_expr,
    term_call_expression,
    term_index_expression,
    ordinal_index_expression,
    offset_index_expression,
    block_expr,
    if_statement,
    match_stmt,
    assignment_statement,
    return_statement,
    resume_stmt,
    raise_stmt,
    yield_stmt,
    try_stmt,
    term_expression_statement,
    while_statement,
    until_statement,
    catch_stmt,
    /// <summary>
    ///     后缀 catch 表达式，如 <c>expr.catch { ... }</c>
    /// </summary>
    catch_expr,
    /// <summary>
    ///     后缀 match 表达式，如 <c>expr.match { ... }</c>
    /// </summary>
    match_expr,
    channel_conditional_decl,
    hal_decl,
    pal_decl,
    trait_decl,
    is_expr,
    as_expr,
    in_expr,
    cast_expr,
    or_pattern,
    guarded_pattern,

    /// <summary>
    ///     展开表达式，如 <c>..expr</c> 或 <c>...expr</c>
    /// </summary>
    spread_expr,

    /// <summary>
    ///     if 表达式，如 <c>if condition { then_expr } else { else_expr }</c>
    /// </summary>
    if_expr
}
