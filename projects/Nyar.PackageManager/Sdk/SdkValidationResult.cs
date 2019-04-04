namespace Nyar.PackageManager.Sdk;

/// <summary>
///     SDK 瑙勮寖楠岃瘉缁撴灉
/// </summary>
public class SdkValidationResult
{
    /// <summary>
    ///     鏄惁閫氳繃楠岃瘉
    /// </summary>
    public bool is_valid { get; set; }

    /// <summary>
    ///     楠岃瘉閿欒鍒楄〃
    /// </summary>
    public List<string> errors { get; set; } = [];

    /// <summary>
    ///     楠岃瘉璀﹀憡鍒楄〃
    /// </summary>
    public List<string> warnings { get; set; } = [];
}