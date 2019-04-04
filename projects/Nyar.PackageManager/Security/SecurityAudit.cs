using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nyar.PackageManager.Security;

/// <summary>
///     安全审计引擎 — 漏洞扫描、许可证合规、签名验证、完整性校验
/// </summary>
public class SecurityAudit
{
    #region 构造函数

    public SecurityAudit()
    {
        _cache_dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".valkyrie", "cache");

        if (!Directory.Exists(_cache_dir)) Directory.CreateDirectory(_cache_dir);

        _vuln_cache = load_vulnerability_cache();
    }

    #endregion

    #region 许可证检查

    private Task<List<LicenseInfo>> check_license(PackageRegistry.Package package)
    {
        var licenses = new List<LicenseInfo>
        {
            new()
            {
                package_name = package.name,
                version = package.version,
                license = !string.IsNullOrEmpty(package.license) ? package.license : "unknown",
                is_compatible = !string.IsNullOrEmpty(package.license) && is_license_compatible(package.license),
                is_restricted = !string.IsNullOrEmpty(package.license) && is_license_restricted(package.license)
            }
        };

        return Task.FromResult(licenses);
    }

    #endregion

    #region 版本比较辅助

    private static bool is_higher_version(string a, string b)
    {
        return string.Compare(a, b, StringComparison.Ordinal) > 0;
    }

    #endregion

    #region 签名验证

    private bool verify_signature_with_embedded_key(byte[] data, byte[] signature)
    {
        var trustedKeysDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".valkyrie", "trusted-keys");

        if (!Directory.Exists(trustedKeysDir)) return false;

        foreach (var keyFile in Directory.GetFiles(trustedKeysDir, "*.pub"))
            try
            {
                var publicKeyBytes = File.ReadAllBytes(keyFile);
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                if (rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) return true;
            }
            catch (CryptographicException)
            {
            }

        return false;
    }

    #endregion

    #region 常量

    private static readonly HashSet<string> _compatible_licenses =
        new((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase)
        {
            "MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "0BSD",
            "ISC", "Unlicense", "CC0-1.0", "WTFPL", "Zlib"
        };

    private static readonly HashSet<string> _restricted_licenses =
        new((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase)
        {
            "GPL-2.0-only", "GPL-2.0-or-later", "GPL-3.0-only", "GPL-3.0-or-later",
            "AGPL-3.0-only", "AGPL-3.0-or-later", "SSPL-1.0", "BUSL-1.1"
        };

    private static readonly HttpClient _http_client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string _osv_api_endpoint = "https://api.osv.dev/v1/query";
    private const string _vuln_cache_file_name = "vulnerability-cache.json";

    #endregion

    #region 字段

    private readonly string _cache_dir;
    private readonly Dictionary<string, List<VulnerabilityReport>> _vuln_cache;
    private readonly object _cache_lock = new();

    #endregion

    #region 公开 API

    /// <summary>
    ///     审计单个包
    /// </summary>
    public async Task<SecurityAuditResult> audit_package(PackageRegistry.Package package)
    {
        var result = new SecurityAuditResult();

        result.vulnerabilities = await scan_vulnerabilities(package);
        result.licenses = await check_license(package);

        return result;
    }

    /// <summary>
    ///     审计依赖列表
    /// </summary>
    public async Task<SecurityAuditResult> audit_dependencies(List<PackageRegistry.Package> packages)
    {
        var result = new SecurityAuditResult();

        foreach (var package in packages)
        {
            var vulns = await scan_vulnerabilities(package);
            result.vulnerabilities.AddRange(vulns);

            var licenses = await check_license(package);
            result.licenses.AddRange(licenses);
        }

        return result;
    }

    /// <summary>
    ///     审计依赖列表，返回可修复的版本建议
    /// </summary>
    public Dictionary<string, string> get_fix_suggestions(List<VulnerabilityReport> vulnerabilities)
    {
        var suggestions = new Dictionary<string, string>();

        foreach (var vuln in vulnerabilities.Where(v => v.is_fixable))
        {
            var key = $"{vuln.package_name}@{vuln.version}";

            if (!suggestions.ContainsKey(key) || is_higher_version(vuln.fixed_version!, suggestions[key]))
                suggestions[key] = vuln.fixed_version!;
        }

        return suggestions;
    }

    /// <summary>
    ///     CRC64 完整性校验
    /// </summary>
    public bool verify_integrity(string packagePath, string expectedHash)
    {
        if (!File.Exists(packagePath) && !Directory.Exists(packagePath)) return false;

        string actualHash;

        if (File.Exists(packagePath))
            actualHash = compute_file_hash(packagePath);
        else
            actualHash = compute_directory_hash(packagePath);

        if (expectedHash.StartsWith("sha512-")) return actualHash == expectedHash;

        if (expectedHash.StartsWith("sha256-"))
        {
            var sha256Hash = File.Exists(packagePath)
                ? compute_file_hash_sha256(packagePath)
                : compute_directory_hash_sha256(packagePath);
            return sha256Hash == expectedHash;
        }

        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     RSA 签名验证
    /// </summary>
    public SignatureVerificationResult verify_signature(string packagePath, string? publicKeyPath = null)
    {
        var result = new SignatureVerificationResult();

        var signaturePath = packagePath + ".sig";
        if (!File.Exists(signaturePath))
        {
            result.is_valid = false;
            result.error = "未找到签名文件";
            return result;
        }

        if (publicKeyPath is not null && !File.Exists(publicKeyPath))
        {
            result.is_valid = false;
            result.error = "未找到公钥文件";
            return result;
        }

        try
        {
            var signatureBytes = File.ReadAllBytes(signaturePath);
            var dataBytes = File.Exists(packagePath)
                ? File.ReadAllBytes(packagePath)
                : compute_directory_hash_raw(packagePath);

            if (publicKeyPath is not null)
            {
                var publicKeyBytes = File.ReadAllBytes(publicKeyPath);

                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                result.is_valid = rsa.VerifyData(
                    dataBytes, signatureBytes, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            else
            {
                result.is_valid = verify_signature_with_embedded_key(dataBytes, signatureBytes);
            }

            result.signer = result.is_valid ? "verified" : "unverified";
        }
        catch (CryptographicException ex)
        {
            result.is_valid = false;
            result.error = $"签名验证失败: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    ///     Ed25519 签名验证（Valhalla 注册中心包签名）
    /// </summary>
    /// <param name="data">待验证的原始数据</param>
    /// <param name="signature">64 字节 Ed25519 签名</param>
    /// <param name="publicKey">32 字节 Ed25519 公钥</param>
    public static SignatureVerificationResult verify_ed25519_signature(byte[] data, byte[] signature, byte[] publicKey)
    {
        var result = new SignatureVerificationResult();

        if (publicKey.Length != 32)
        {
            result.is_valid = false;
            result.error = "Ed25519 公钥长度无效，应为 32 字节";
            return result;
        }

        if (signature.Length != 64)
        {
            result.is_valid = false;
            result.error = "Ed25519 签名长度无效，应为 64 字节";
            return result;
        }

        try
        {
            var nsecType = Type.GetType(
                "NSec.Cryptography.SignatureAlgorithm, NSec.Cryptography");

            if (nsecType is null)
            {
                result.is_valid = false;
                result.error = "NSec.Cryptography 未安装，无法进行 Ed25519 签名验证。请安装 NSec.Cryptography 包";
                return result;
            }

            var algorithmProp = nsecType.GetProperty("Ed25519",
                BindingFlags.Public | BindingFlags.Static);

            if (algorithmProp is null)
            {
                result.is_valid = false;
                result.error = "无法获取 Ed25519 算法实例";
                return result;
            }

            var algorithm = algorithmProp.GetValue(null);

            var publicKeyType = Type.GetType(
                "NSec.Cryptography.PublicKey, NSec.Cryptography");

            var importMethod = publicKeyType?.GetMethod("Import",
                BindingFlags.Public | BindingFlags.Static,
                [
                    nsecType,
                    typeof(byte[]),
                    Type.GetType("NSec.Cryptography.KeyBlobFormat, NSec.Cryptography")!
                ]);

            var keyBlobFormat = Type.GetType(
                "NSec.Cryptography.KeyBlobFormat, NSec.Cryptography");

            var rawFormat = keyBlobFormat?.GetField("RawPublicKey", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);

            var publicKeyObj = importMethod?.Invoke(null,
                [algorithm, publicKey, rawFormat]);

            if (publicKeyObj is null)
            {
                result.is_valid = false;
                result.error = "无法导入 Ed25519 公钥";
                return result;
            }

            var verifyMethod = nsecType.GetMethod("Verify",
                BindingFlags.Public | BindingFlags.Instance);

            var verifyResult = (bool)verifyMethod!.Invoke(algorithm,
                [publicKeyObj, data, signature])!;

            result.is_valid = verifyResult;
            result.signer = verifyResult
                ? $"ed25519:{Convert.ToHexStringLower(publicKey)}"
                : "unverified";
        }
        catch (Exception ex)
        {
            result.is_valid = false;
            result.error = $"Ed25519 签名验证异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    ///     检查许可证是否兼容
    /// </summary>
    public bool is_license_compatible(string license)
    {
        return _compatible_licenses.Contains(license);
    }

    /// <summary>
    ///     检查许可证是否受限
    /// </summary>
    public bool is_license_restricted(string license)
    {
        return _restricted_licenses.Contains(license);
    }

    /// <summary>
    ///     计算单个文件的 SHA256 哈希
    /// </summary>
    public static string compute_sha256_hash(string filePath)
    {
        return compute_file_hash_sha256(filePath);
    }

    /// <summary>
    ///     计算目录的 SHA256 哈希
    /// </summary>
    public static string compute_sha256_hash_directory(string directoryPath)
    {
        return compute_directory_hash_sha256(directoryPath);
    }

    #endregion

    #region 本地漏洞缓存

    /// <summary>
    ///     从本地缓存文件加载漏洞数据
    /// </summary>
    private Dictionary<string, List<VulnerabilityReport>> load_vulnerability_cache()
    {
        var cachePath = Path.Combine(_cache_dir, _vuln_cache_file_name);

        if (!File.Exists(cachePath)) return new Dictionary<string, List<VulnerabilityReport>>();

        try
        {
            var json = File.ReadAllText(cachePath);
            var cache = JsonSerializer.Deserialize<Dictionary<string, List<VulnerabilityReport>>>(json);

            if (cache is null) return new Dictionary<string, List<VulnerabilityReport>>();

            var expiredKeys = new List<string>();
            var cacheMaxAge = TimeSpan.FromDays(1);
            var now = DateTime.UtcNow;

            foreach (var kvp in cache)
            foreach (var vuln in kvp.Value)
                if (vuln.cache_timestamp is DateTime ts && now - ts > cacheMaxAge)
                {
                    expiredKeys.Add(kvp.Key);
                    break;
                }

            foreach (var key in expiredKeys) cache.Remove(key);

            return cache;
        }
        catch
        {
            return new Dictionary<string, List<VulnerabilityReport>>();
        }
    }

    /// <summary>
    ///     保存漏洞数据到本地缓存
    /// </summary>
    private void save_vulnerability_cache()
    {
        lock (_cache_lock)
        {
            try
            {
                var cachePath = Path.Combine(_cache_dir, _vuln_cache_file_name);
                var json = JsonSerializer.Serialize(_vuln_cache,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(cachePath, json);
            }
            catch
            {
                // 缓存写入失败不影响主流程
            }
        }
    }

    /// <summary>
    ///     获取缓存中的漏洞数据
    /// </summary>
    private List<VulnerabilityReport>? get_cached_vulnerabilities(string packageName, string version)
    {
        lock (_cache_lock)
        {
            var key = $"{packageName}@{version}";
            return _vuln_cache.GetValueOrDefault(key);
        }
    }

    /// <summary>
    ///     缓存漏洞数据
    /// </summary>
    private void cache_vulnerabilities(string packageName, string version, List<VulnerabilityReport> vulns)
    {
        lock (_cache_lock)
        {
            var key = $"{packageName}@{version}";

            foreach (var vuln in vulns) vuln.cache_timestamp = DateTime.UtcNow;

            _vuln_cache[key] = vulns;
        }

        save_vulnerability_cache();
    }

    #endregion

    #region 漏洞扫描

    private async Task<List<VulnerabilityReport>> scan_vulnerabilities(PackageRegistry.Package package)
    {
        var cached = get_cached_vulnerabilities(package.name, package.version);
        if (cached is not null) return cached;

        var reports = new List<VulnerabilityReport>();

        try
        {
            var queryPayload = new
            {
                package = new
                {
                    package.name, package.version
                }
            };

            var jsonPayload = JsonSerializer.Serialize(queryPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _http_client.PostAsync(_osv_api_endpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                cache_vulnerabilities(package.name, package.version, reports);
                return reports;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("vulns", out var vulnsArray))
            {
                cache_vulnerabilities(package.name, package.version, reports);
                return reports;
            }

            foreach (var vuln in vulnsArray.EnumerateArray())
            {
                var report = new VulnerabilityReport
                {
                    package_name = package.name,
                    version = package.version,
                    vulnerability_id = vuln.TryGetProperty("id", out var id) ? id.GetString() : null
                };

                if (vuln.TryGetProperty("summary", out var summary)) report.title = summary.GetString() ?? string.Empty;

                if (vuln.TryGetProperty("aliases", out var aliases) &&
                    aliases.ValueKind == JsonValueKind.Array)
                {
                    var cveId = aliases.EnumerateArray()
                        .Select(a => a.GetString())
                        .FirstOrDefault(a => a is not null && a.StartsWith("CVE-"));

                    report.vulnerability_id ??= cveId;
                }

                if (vuln.TryGetProperty("database_specific", out var dbSpecific))
                {
                    if (dbSpecific.TryGetProperty("severity", out var severity))
                        report.severity = severity.GetString() ?? "unknown";

                    if (dbSpecific.TryGetProperty("cwe_id", out var cwe)) report.cwe_id = cwe.GetString();
                }

                if (vuln.TryGetProperty("severity", out var severityArray) &&
                    severityArray.ValueKind == JsonValueKind.Array)
                {
                    var firstSeverity = severityArray.EnumerateArray().FirstOrDefault();
                    if (firstSeverity.TryGetProperty("score", out var score))
                        report.severity = score.GetString() ?? "unknown";
                }

                parse_fixed_version(vuln, report);
                parse_cvss_score(vuln, report);

                if (vuln.TryGetProperty("references", out var refs) &&
                    refs.ValueKind == JsonValueKind.Array)
                {
                    var firstRef = refs.EnumerateArray().FirstOrDefault();
                    if (firstRef.TryGetProperty("url", out var url)) report.url = url.GetString();
                }

                reports.Add(report);
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (HttpRequestException)
        {
        }
        catch (JsonException)
        {
        }

        cache_vulnerabilities(package.name, package.version, reports);
        return reports;
    }

    /// <summary>
    ///     从 OSV 响应中解析修复版本
    /// </summary>
    private static void parse_fixed_version(JsonElement vuln, VulnerabilityReport report)
    {
        if (!vuln.TryGetProperty("affected", out var affected) ||
            affected.ValueKind != JsonValueKind.Array)
            return;

        foreach (var entry in affected.EnumerateArray())
        {
            if (!entry.TryGetProperty("ranges", out var ranges) ||
                ranges.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var range in ranges.EnumerateArray())
            {
                if (!range.TryGetProperty("type", out var rangeType) ||
                    rangeType.GetString() != "ECOSYSTEM")
                    continue;

                if (range.TryGetProperty("events", out var events) &&
                    events.ValueKind == JsonValueKind.Array)
                    foreach (var evt in events.EnumerateArray())
                        if (evt.TryGetProperty("fixed", out var @fixed))
                        {
                            report.fixed_version = @fixed.GetString();
                            return;
                        }
            }
        }
    }

    /// <summary>
    ///     从 OSV 响应中解析 CVSS 评分
    /// </summary>
    private static void parse_cvss_score(JsonElement vuln, VulnerabilityReport report)
    {
        if (!vuln.TryGetProperty("severity", out var sevs) ||
            sevs.ValueKind != JsonValueKind.Array)
            return;

        foreach (var sev in sevs.EnumerateArray())
        {
            if (!sev.TryGetProperty("type", out var sevType) ||
                sevType.GetString() != "CVSS_V3")
                continue;

            if (sev.TryGetProperty("score", out var score) &&
                score.GetString() is string scoreStr &&
                double.TryParse(scoreStr, out var scoreVal))
            {
                report.cvss_score = scoreVal;
                return;
            }
        }
    }

    #endregion

    #region 哈希计算

    private static string compute_file_hash(string filePath)
    {
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha512.ComputeHash(stream);

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    private static string compute_directory_hash(string directoryPath)
    {
        using var sha512 = SHA512.Create();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            var relativePath = Path.GetRelativePath(directoryPath, filePath);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha512.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var fileStream = File.OpenRead(filePath);
            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                sha512.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha512.TransformFinalBlock([], 0, 0);
        var hash = sha512.Hash!;

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    private static string compute_file_hash_sha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);

        return $"sha256-{Convert.ToBase64String(hash)}";
    }

    private static string compute_directory_hash_sha256(string directoryPath)
    {
        using var sha256 = SHA256.Create();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            var relativePath = Path.GetRelativePath(directoryPath, filePath);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var fileStream = File.OpenRead(filePath);
            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock([], 0, 0);
        var hash = sha256.Hash!;

        return $"sha256-{Convert.ToBase64String(hash)}";
    }

    private static byte[] compute_directory_hash_raw(string directoryPath)
    {
        using var sha256 = SHA256.Create();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            var relativePath = Path.GetRelativePath(directoryPath, filePath);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var fileStream = File.OpenRead(filePath);
            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock([], 0, 0);
        return sha256.Hash!;
    }

    #endregion
}