using System.Net;

namespace Valhalla.Tests;

public class ValhallaInstallerTests
{
    private const string _base_url = "https://valhalla.test";

    [Fact]
    public async Task InstallAsync_成功下载验证并写入()
    {
        var packageData = new byte[512];
        new Random(42).NextBytes(packageData);
        var packageDigest = ValhallaDigest.compute(packageData);

        // 构建下载响应：4B长度 + 包数据 + 4B零长度(无源码)
        var responseData = new MemoryStream();
        responseData.Write(BitConverter.GetBytes(packageData.Length));
        responseData.Write(packageData);
        responseData.Write(BitConverter.GetBytes(0));
        var responseBytes = responseData.ToArray();

        var handler = new TestHttpMessageHandler();
        handler.add_get_bytes("/api/packages/test.pkg/versions/1.0.0/download",
            HttpStatusCode.OK, responseBytes, new Dictionary<string, string>
            {
                ["X-Content-SHA256"] = packageDigest.hex_string,
                ["Content-Type"] = "application/octet-stream"
            });

        using var http = new HttpClient(handler);
        var client = new ValhallaClient(_base_url, http);

        var tempDir = Path.Combine(Path.GetTempPath(), $"valhalla-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var installer = new ValhallaInstaller(client);
            var result = await installer.install("test.pkg", "1.0.0", tempDir);

            Assert.True(result.success, result.error ?? "安装应成功");
            Assert.Equal(packageDigest.hex_string, result.sha256);

            var packagePath = Path.Combine(tempDir, "test.pkg-1.0.0.nyar");
            Assert.True(File.Exists(packagePath));

            var writtenData = await File.ReadAllBytesAsync(packagePath);
            Assert.Equal(packageData, writtenData);

            var digestPath = Path.Combine(tempDir, "test.pkg-1.0.0.sha256");
            Assert.True(File.Exists(digestPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InstallAsync_SHA256不匹配_失败()
    {
        var packageData = new byte[256];
        new Random(42).NextBytes(packageData);

        var responseData = new MemoryStream();
        responseData.Write(BitConverter.GetBytes(packageData.Length));
        responseData.Write(packageData);
        responseData.Write(BitConverter.GetBytes(0));
        var responseBytes = responseData.ToArray();

        var handler = new TestHttpMessageHandler();
        handler.add_get_bytes("/api/packages/bad.pkg/versions/1.0.0/download",
            HttpStatusCode.OK, responseBytes, new Dictionary<string, string>
            {
                ["X-Content-SHA256"] = "0000000000000000000000000000000000000000000000000000000000000000",
                ["Content-Type"] = "application/octet-stream"
            });

        using var http = new HttpClient(handler);
        var client = new ValhallaClient(_base_url, http);

        var tempDir = Path.Combine(Path.GetTempPath(), $"valhalla-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var installer = new ValhallaInstaller(client);
            var result = await installer.install("bad.pkg", "1.0.0", tempDir);

            Assert.False(result.success);
            Assert.Contains("SHA-256 不匹配", result.error);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}