namespace Nyar.PackageManager.Security;

public class SecurityAuditResult
{
    public List<VulnerabilityReport> vulnerabilities { get; set; } = [];
    public List<LicenseInfo> licenses { get; set; } = [];
    public List<SignatureVerificationResult> signatures { get; set; } = [];
    public bool has_vulnerabilities => vulnerabilities.Count > 0;
    public bool has_license_issues => licenses.Exists(l => !l.is_compatible);
    public bool has_signature_issues => signatures.Exists(s => !s.is_valid);
}