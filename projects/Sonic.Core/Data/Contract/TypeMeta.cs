using System.Collections.Immutable;

namespace Core.Data.Contract;

/// <summary>
///     类型元数据，描述类型的名称及其字段信息。
/// </summary>
public sealed class TypeMeta
{
    /// <summary>
    ///     初始化类型元数据。
    /// </summary>
    /// <param name="typeName">类型名称。</param>
    /// <param name="fields">字段元数据列表。</param>
    public TypeMeta(string typeName, ImmutableArray<FieldMeta> fields)
    {
        type_name = typeName;
        this.fields = fields;
    }

    /// <summary>
    ///     类型名称。
    /// </summary>
    public string type_name { get; }

    /// <summary>
    ///     类型的字段元数据列表。
    /// </summary>
    public ImmutableArray<FieldMeta> fields { get; }

    /// <summary>
    ///     字段元数据，描述字段的名称、类型和必填性。
    /// </summary>
    public sealed class FieldMeta
    {
        /// <summary>
        ///     初始化字段元数据。
        /// </summary>
        /// <param name="name">字段名称。</param>
        /// <param name="fieldType">字段类型名称。</param>
        /// <param name="isRequired">字段是否为必填项。</param>
        public FieldMeta(string name, string fieldType, bool isRequired)
        {
            this.name = name;
            field_type = fieldType;
            is_required = isRequired;
        }

        /// <summary>
        ///     字段名称。
        /// </summary>
        public string name { get; }

        /// <summary>
        ///     字段类型名称。
        /// </summary>
        public string field_type { get; }

        /// <summary>
        ///     字段是否为必填项。
        /// </summary>
        public bool is_required { get; }
    }
}