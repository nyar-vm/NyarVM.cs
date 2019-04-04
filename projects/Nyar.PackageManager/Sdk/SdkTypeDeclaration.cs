namespace Nyar.PackageManager.Sdk;

/// <summary>
///     SDK 绫诲瀷澹版槑
/// </summary>
public class SdkTypeDeclaration
{
    /// <summary>
    ///     绫诲瀷鍚嶇О
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     绫诲瀷绉嶇被锛歴truct / enum / interface / trait
    /// </summary>
    public string? kind { get; set; } = "struct";

    /// <summary>
    ///     绫诲瀷鎻忚堪
    /// </summary>
    public string? description { get; set; }
}