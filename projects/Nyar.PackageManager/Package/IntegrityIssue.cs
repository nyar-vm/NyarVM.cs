namespace Nyar.PackageManager.Package;

/// <summary>
///     瀹屾暣鎬ф牎楠岄棶棰樼被鍨?///
/// </summary>
public enum IntegrityIssue
{
    /// <summary>
    ///     缂哄皯瀹屾暣鎬у搱甯?    ///
    /// </summary>
    missing_integrity,

    /// <summary>
    ///     瀹夎鐩綍缂哄け
    /// </summary>
    missing_package,

    /// <summary>
    ///     鍝堝笇鍊间笉鍖归厤
    /// </summary>
    hash_mismatch
}