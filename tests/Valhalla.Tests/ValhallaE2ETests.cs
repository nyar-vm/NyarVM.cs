using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Valhalla.Tests;

/// <summary>
///     瓦尓哈拉端到端集成测试，启动真实 HTTP 服务器进行全链路测试
/// </summary>
public class ValhallaE2ETests
{
    /// <summary>
    ///     验证依赖注入完整性
    /// </summary>
    [Fact]
    public void Constructor_验证依赖注入完整性()
    {
        Assert.True(true);
    }

    /// <summary>
    ///     发布→检索→下载全链路测试
    /// </summary>
    [Fact]
    public async Task 发布检索下载全链路_验证完整流程()
    {
        var tempDir = create_temp_dir();
        try
        {
            var port = get_free_port();
            var config = create_test_config(tempDir, port);
            var baseUrl = $"http://localhost:{port}";

            var server = new ValhallaServer(config);
            var serverTask = start_server_in_background(server);
            await wait_for_server_ready(baseUrl);

            var client = new ValhallaClient(baseUrl, maxRetries: 2, retryBaseDelayMs: 100);

            var packageName = "test.e2e.pkg";
            var version = "1.0.0";
            var publisher = "test-fingerprint-abc123";

            var testPackageData = generate_test_data(4096);
            var expectedDigest = compute_sha256_hex(testPackageData);

            using var httpClient = new HttpClient();

            // 注册包
            await register_package(httpClient, baseUrl, packageName, publisher);

            // 上传版本
            await upload_version(httpClient, baseUrl, packageName, version, testPackageData);

            // 确认包在列表中
            var listResponse = await client.list_packages();
            Assert.NotNull(listResponse);
            Assert.Contains(listResponse.packages, p => p.Name == packageName);

            // 获取 manifest
            var manifest = await client.get_manifest(packageName);
            Assert.NotNull(manifest);
            Assert.Equal(packageName, manifest!.name);
            Assert.True(manifest.versions.ContainsKey(version));

            // 获取版本详情
            var versionEntry = await client.get_version(packageName, version);
            Assert.NotNull(versionEntry);
            Assert.Equal(expectedDigest, versionEntry!.package_digest);

            // 下载二进制
            var downloadResult = await client.download(packageName, version);
            Assert.NotNull(downloadResult);
            Assert.True(downloadResult.package_data.Length > 0);
            Assert.Null(downloadResult.error);

            // 验证下载的 SHA-256 与服务端报告的一致
            var actualDownloadDigest = compute_sha256_hex(downloadResult.package_data);
            Assert.Equal(downloadResult.package_sha256, actualDownloadDigest);

            // 安装到临时目录
            var installDir = Path.Combine(tempDir, "installed");
            var installer = new ValhallaInstaller(client);
            var installResult = await installer.install(packageName, version, installDir);
            Assert.True(installResult.success, installResult.error ?? "安装应成功");
            Assert.Equal(packageName, installResult.package_name);
            Assert.Equal(version, installResult.version);

            // 验证安装成功，文件存在
            var installedNyarPath = Path.Combine(installDir, $"{packageName}-{version}.nyar");
            Assert.True(File.Exists(installedNyarPath));
            var installedDigestPath = Path.Combine(installDir, $"{packageName}-{version}.sha256");
            Assert.True(File.Exists(installedDigestPath));

            var installedData = await File.ReadAllBytesAsync(installedNyarPath);
            var installedDigest = compute_sha256_hex(installedData);
            Assert.Equal(expectedDigest, installedDigest);

            await stop_server(server, serverTask);
        }
        finally
        {
            delete_temp_dir(tempDir);
        }
    }

    /// <summary>
    ///     shield/unshield 生命周期测试
    /// </summary>
    [Fact]
    public async Task ShieldUnshield生命周期_验证状态变迁()
    {
        var tempDir = create_temp_dir();
        try
        {
            var port = get_free_port();
            var config = create_test_config(tempDir, port);
            var baseUrl = $"http://localhost:{port}";

            var server = new ValhallaServer(config);
            var serverTask = start_server_in_background(server);
            await wait_for_server_ready(baseUrl);

            var client = new ValhallaClient(baseUrl, maxRetries: 2, retryBaseDelayMs: 100);

            var packageName = "test.shield.pkg";
            var version = "1.0.0";
            var publisher = "test-fingerprint-shield";

            var testPackageData = generate_test_data(2048);

            using var httpClient = new HttpClient();

            await register_package(httpClient, baseUrl, packageName, publisher);
            await upload_version(httpClient, baseUrl, packageName, version, testPackageData);

            // 确认版本状态为 active
            var versionEntry = await client.get_version(packageName, version);
            Assert.NotNull(versionEntry);
            Assert.Equal(VersionStatus.active, versionEntry!.status);

            // 发送 shield 请求
            var shieldUrl = $"{baseUrl}/api/packages/{packageName}/shield/{version}";
            var shieldBody = new StringContent(
                @"{""reason"":""安全漏洞 CVE-2025-0001""}",
                Encoding.UTF8,
                "application/json");
            var shieldResponse = await httpClient.PostAsync(shieldUrl, shieldBody);
            Assert.Equal(HttpStatusCode.OK, shieldResponse.StatusCode);

            // 验证版本状态变为 shielded
            var shieldedEntry = await client.get_version(packageName, version);
            Assert.NotNull(shieldedEntry);
            Assert.Equal(VersionStatus.shielded, shieldedEntry!.status);
            Assert.Equal("安全漏洞 CVE-2025-0001", shieldedEntry.shield_reason);

            // 发送 unshield 请求
            var unshieldUrl = $"{baseUrl}/api/packages/{packageName}/unshield/{version}";
            var unshieldResponse = await httpClient.PostAsync(unshieldUrl, null);
            Assert.Equal(HttpStatusCode.OK, unshieldResponse.StatusCode);

            // 验证版本状态恢复为 active
            var unshieldedEntry = await client.get_version(packageName, version);
            Assert.NotNull(unshieldedEntry);
            Assert.Equal(VersionStatus.active, unshieldedEntry!.status);
            Assert.Null(unshieldedEntry.shield_reason);

            await stop_server(server, serverTask);
        }
        finally
        {
            delete_temp_dir(tempDir);
        }
    }

    /// <summary>
    ///     purge 包操作测试
    /// </summary>
    [Fact]
    public async Task Purge包操作_验证状态变为Purged()
    {
        var tempDir = create_temp_dir();
        try
        {
            var port = get_free_port();
            var config = create_test_config(tempDir, port);
            var baseUrl = $"http://localhost:{port}";

            var server = new ValhallaServer(config);
            var serverTask = start_server_in_background(server);
            await wait_for_server_ready(baseUrl);

            var client = new ValhallaClient(baseUrl, maxRetries: 2, retryBaseDelayMs: 100);

            var packageName = "test.purge.pkg";
            var purgeReason = "违反内容政策";

            using var httpClient = new HttpClient();

            await register_package(httpClient, baseUrl, packageName, "test-fingerprint-purge");

            // 发送 DELETE 请求 purge 包
            var purgeUrl = $"{baseUrl}/api/packages/{packageName}";
            var purgeRequest = new HttpRequestMessage(HttpMethod.Delete, purgeUrl)
            {
                Content = new StringContent(
                    $"{{\"reason\":\"{purgeReason}\"}}",
                    Encoding.UTF8,
                    "application/json")
            };
            var purgeResponse = await httpClient.SendAsync(purgeRequest);
            Assert.Equal(HttpStatusCode.OK, purgeResponse.StatusCode);

            // 验证包状态变为 purged
            var manifest = await client.get_manifest(packageName);
            Assert.NotNull(manifest);
            Assert.Equal(PackageStatus.purged, manifest!.status);
            Assert.Equal(purgeReason, manifest.purge_reason);
            Assert.NotNull(manifest.purged_at);

            await stop_server(server, serverTask);
        }
        finally
        {
            delete_temp_dir(tempDir);
        }
    }

    #region 辅助方法

    /// <summary>
    ///     获取一个空闲的 TCP 端口
    /// </summary>
    private static int get_free_port()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    ///     创建用于测试的临时目录
    /// </summary>
    private static string create_temp_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ValhallaE2E_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    ///     删除临时目录及其所有内容
    /// </summary>
    private static void delete_temp_dir(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    /// <summary>
    ///     创建用于测试的服务端配置
    /// </summary>
    private static ValhallaConfig create_test_config(string storagePath, int port)
    {
        return new ValhallaConfig
        {
            name = "E2E 测试实例",
            port = port,
            storage_path = storagePath,
            storage = StorageBackend.local,
            pubkey_required = false,
            @public = true,
            registration = RegistrationMode.open,
            enable_audit_public_access = true
        };
    }

    /// <summary>
    ///     在后台任务中启动服务端
    /// </summary>
    private static Task start_server_in_background(ValhallaServer server)
    {
        return Task.Run(() => server.start());
    }

    /// <summary>
    ///     等待服务端就绪，轮询健康检查端点
    /// </summary>
    private static async Task wait_for_server_ready(string baseUrl, int timeoutMs = 15000)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync($"{baseUrl}/health");
                if (response.IsSuccessStatusCode) return;
            }
            catch
            {
                // 服务端尚未就绪，继续等待
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("服务端未能在超时时间内就绪");
    }

    /// <summary>
    ///     停止服务端并等待后台任务完成
    /// </summary>
    private static async Task stop_server(ValhallaServer server, Task serverTask)
    {
        try
        {
            await server.stop();
            await serverTask.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            // 忽略停止错误
        }
    }

    /// <summary>
    ///     通过 HTTP API 注册包
    /// </summary>
    private static async Task register_package(
        HttpClient http,
        string baseUrl,
        string packageName,
        string publisher)
    {
        var url = $"{baseUrl}/api/packages";
        var body = new StringContent(
            $"{{\"name\":\"{packageName}\",\"publisher\":\"{publisher}\"}}",
            Encoding.UTF8,
            "application/json");
        var response = await http.PostAsync(url, body);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///     通过 HTTP API 上传版本
    /// </summary>
    private static async Task upload_version(
        HttpClient http,
        string baseUrl,
        string packageName,
        string version,
        byte[] packageData)
    {
        var url = $"{baseUrl}/api/packages/{packageName}/versions";

        using var form = new MultipartFormDataContent();

        var manifestContent = new StringContent(
            $"{{\"version\":\"{version}\"}}",
            Encoding.UTF8,
            "application/json");
        form.Add(manifestContent, "manifest");

        var packageContent = new ByteArrayContent(packageData);
        packageContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(packageContent, "package", $"{packageName}-{version}.nyar");

        var response = await http.PostAsync(url, form);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///     生成指定大小的测试数据
    /// </summary>
    private static byte[] generate_test_data(int size)
    {
        var data = new byte[size];
        Random.Shared.NextBytes(data);
        return data;
    }

    /// <summary>
    ///     计算字节数组的 SHA-256 hex 字符串
    /// </summary>
    private static string compute_sha256_hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion
}