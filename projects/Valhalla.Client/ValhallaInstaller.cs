namespace Valhalla.Client;

/// <summary>
///     瓦尓哈拉安装器，负责下载、SHA-256 验证、写入磁盘
/// </summary>
public class ValhallaInstaller
{
    private readonly ValhallaClient _client;

    /// <summary>
    ///     创建安装器
    /// </summary>
    /// <param name="client">瓦尓哈拉 HTTP 客户端</param>
    public ValhallaInstaller(ValhallaClient client)
    {
        _client = client;
    }

    /// <summary>
    ///     对照锁文件校验下载结果的安全属性
    /// </summary>
    /// <param name="lockFile">锁文件</param>
    /// <param name="manifest">远程包清单</param>
    /// <param name="downloadResult">下载结果</param>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    /// <returns>校验结果，若校验失败则包含错误信息</returns>
    public (bool Passed, string? Error) validate_against_lock(
        ValhallaLockFile lockFile,
        PackageManifest manifest,
        ValhallaDownloadResult downloadResult,
        string packageName,
        string version)
    {
        var lockedEntry = lockFile.get_entry(packageName);

        if (lockedEntry is not null)
        {
            if (manifest.incarnation > lockedEntry.incarnation) return (false, "包已被 PURGE 后重新注册，请检查安全通告");

            var manifestPublisher = manifest.publisher.ToLowerInvariant();
            var lockedPublisher = lockedEntry.publisher.ToLowerInvariant();
            if (!string.Equals(manifestPublisher, lockedPublisher, StringComparison.Ordinal))
                return (false, "发布者身份已变更，可能存在安全风险");
        }

        var actualDigest = ValhallaDigest.compute(downloadResult.package_data);

        if (lockedEntry is not null && lockedEntry.version == version)
        {
            var lockedShaLower = lockedEntry.sha256.ToLowerInvariant();
            if (actualDigest.hex_string != lockedShaLower)
                return (false, $"SHA-256 与锁文件不匹配：锁记录 {lockedShaLower}，实际 {actualDigest.hex_string}");
        }

        if (!string.IsNullOrEmpty(downloadResult.package_sha256))
        {
            var serverShaLower = downloadResult.package_sha256.ToLowerInvariant();
            if (actualDigest.hex_string != serverShaLower)
                return (false, $"SHA-256 与服务端声明不匹配：服务端声明 {serverShaLower}，实际 {actualDigest.hex_string}");
        }

        return (true, null);
    }

    /// <summary>
    ///     下载并验证包，写入目标目录
    /// </summary>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    /// <param name="targetDirectory">目标安装目录</param>
    /// <param name="ct">取消令牌</param>
    public async Task<InstallResult> install(
        string packageName,
        string version,
        string targetDirectory,
        CancellationToken ct = default)
    {
        try
        {
            var download = await _client.download(packageName, version, ct);

            var actualDigest = ValhallaDigest.compute(download.package_data);

            if (!string.IsNullOrEmpty(download.package_sha256))
            {
                var expectedLower = download.package_sha256.ToLowerInvariant();
                if (actualDigest.hex_string != expectedLower)
                    return InstallResult.fail(packageName, version,
                        $"SHA-256 不匹配：期望 {expectedLower}，实际 {actualDigest.hex_string}");
            }

            // 验证源码 SHA-256（如果有）
            string? actualSourceSha256 = null;
            if (download.source_data is not null && !string.IsNullOrEmpty(download.source_sha256))
            {
                var sourceDigest = ValhallaDigest.compute(download.source_data);
                actualSourceSha256 = sourceDigest.hex_string;

                var expectedSourceLower = download.source_sha256.ToLowerInvariant();
                if (sourceDigest.hex_string != expectedSourceLower)
                    return InstallResult.fail(packageName, version,
                        $"源码 SHA-256 不匹配：期望 {expectedSourceLower}，实际 {sourceDigest.hex_string}");
            }

            // 对照锁文件校验安全属性
            var lockFile = new ValhallaLockFile(targetDirectory);
            if (lockFile.exists())
            {
                await lockFile.load();

                var manifest = await _client.get_manifest(packageName, ct);
                if (manifest is not null)
                {
                    var (passed, error) = validate_against_lock(lockFile, manifest, download, packageName, version);
                    if (!passed) return InstallResult.fail(packageName, version, error!);
                }
            }

            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);

            // 写入 .nyar 文件
            var packagePath = Path.Combine(targetDirectory, $"{packageName}-{version}.nyar");
            await File.WriteAllBytesAsync(packagePath, download.package_data, ct);

            // 写入源码包（如果有）
            if (download.source_data is not null)
            {
                var sourcePath = Path.Combine(targetDirectory, $"{packageName}-{version}.src.tar.gz");
                await File.WriteAllBytesAsync(sourcePath, download.source_data, ct);
            }

            // 写入 SHA-256 承诺文件
            var digestPath = Path.Combine(targetDirectory, $"{packageName}-{version}.sha256");
            await File.WriteAllTextAsync(digestPath,
                $"{actualDigest.hex_string}  .nyar\n{actualSourceSha256 ?? "N/A"}  source\n", ct);

            return InstallResult.succeed(packageName, version, actualDigest.hex_string, actualSourceSha256);
        }
        catch (Exception ex)
        {
            return InstallResult.fail(packageName, version, ex.Message);
        }
    }
}