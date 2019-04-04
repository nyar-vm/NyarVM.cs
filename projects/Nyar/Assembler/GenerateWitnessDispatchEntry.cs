namespace Nyar.Assembler;

/// <summary>
///     元编译模块级的 witness 分派绑定。
///     在统一编译模型中同时保留符号层身份与稳定 ID，供 `NyarVM` 与 AOT 后端共用。
/// </summary>
public sealed record GenerateWitnessDispatchEntry(
    string trait_name,
    int slot_index,
    string method_name,
    string target_type_name,
    string implementation_function_name)
{
    /// <summary>
    ///     规范化后的协议名称。
    /// </summary>
    public string normalized_trait_name => GenerateWitnessIdentity.normalize_symbol_name(trait_name);

    /// <summary>
    ///     规范化后的目标类型名称。
    /// </summary>
    public string normalized_target_type_name => GenerateWitnessIdentity.normalize_symbol_name(target_type_name);

    /// <summary>
    ///     规范化后的方法名称。
    /// </summary>
    public string normalized_method_name => GenerateWitnessIdentity.normalize_symbol_name(method_name);

    /// <summary>
    ///     Witness 方法身份 ID。
    /// </summary>
    public int method_id => GenerateWitnessIdentity.compute_method_id(
        normalized_trait_name,
        normalized_target_type_name,
        normalized_method_name);

    /// <summary>
    ///     目标类型身份 ID。
    /// </summary>
    public int type_id => GenerateWitnessIdentity.compute_type_id(normalized_target_type_name);

    /// <summary>
    ///     协议身份 ID。
    /// </summary>
    public int interface_id => GenerateWitnessIdentity.compute_interface_id(normalized_trait_name);

    /// <summary>
    ///     协议方法槽位。
    /// </summary>
    public int interface_method_index => slot_index;
}