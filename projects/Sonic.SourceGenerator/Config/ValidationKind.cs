namespace Sonic.Data.Generator.Config;

/// <summary>
///     验证特性的种类。
/// </summary>
internal enum ValidationKind
{
    /// <summary>
    ///     [Required] 特性。
    /// </summary>
    required = 0,

    /// <summary>
    ///     [Range] 特性。
    /// </summary>
    range = 1,

    /// <summary>
    ///     [Regex] 特性。
    /// </summary>
    regex = 2,

    /// <summary>
    ///     [CollectionNotEmpty] 特性。
    /// </summary>
    collection_not_empty = 3,

    /// <summary>
    ///     [Uri] 特性。
    /// </summary>
    uri = 4,

    /// <summary>
    ///     [Enum] 特性。
    /// </summary>
    @enum = 5
}