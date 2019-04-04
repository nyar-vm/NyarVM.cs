namespace Std.Binary.Attributes;

/// <summary>
///     标记代数联合类型，指定判别字段和可能的子类型�?///
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class AlgebraicUnionAttribute : Attribute
{
    /// <summary>
    ///     存储类型标签的字段名�?    ///
    /// </summary>
    public string discriminator_field { get; set; } = "";

    /// <summary>
    ///     可能的子类型列表�?    ///
    /// </summary>
    public Type[] cases { get; set; } = [];
}