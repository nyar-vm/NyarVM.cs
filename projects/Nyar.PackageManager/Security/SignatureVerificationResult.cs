namespace Nyar.PackageManager.Security;

public class SignatureVerificationResult
{
    public string package_name { get; set; } = string.Empty;
    public string version { get; set; } = string.Empty;
    public bool is_valid { get; set; }
    public string? signer { get; set; }
    public string? error { get; set; }
}