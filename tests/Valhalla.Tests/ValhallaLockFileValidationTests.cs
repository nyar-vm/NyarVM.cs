namespace Valhalla.Tests;

public class ValhallaLockFileValidationTests
{
    private const string _test_package_name = "test.pkg";
    private const string _test_version = "1.0.0";
    private const string _publisher_a = "aaaa1111bbbb2222cccc3333dddd4444eeee5555ffff6666gggg7777hhhh";
    private const string _publisher_b = "zzzz9999yyyy8888xxxx7777wwww6666vvvv5555uuuu4444tttt3333ssss";

    private static (byte[] PackageData, ValhallaDigest Digest) create_test_package_data()
    {
        var data = new byte[256];
        new Random(42).NextBytes(data);
        var digest = ValhallaDigest.compute(data);
        return (data, digest);
    }

    private static ValhallaLockFile create_lock_file(string publisher, int incarnation, string sha256)
    {
        var lockFile = new ValhallaLockFile("/test/project");
        lockFile.add_or_update(_test_package_name, new ValhallaLockEntry
        {
            publisher = publisher,
            incarnation = incarnation,
            version = _test_version,
            sha256 = sha256,
            registry = "https://valhalla.example.com"
        });
        return lockFile;
    }

    private static PackageManifest create_manifest(string publisher, int incarnation)
    {
        return new PackageManifest
        {
            name = _test_package_name,
            incarnation = incarnation,
            publisher = publisher
        };
    }

    [Fact]
    public void Incarnation不匹配_远程更大_校验失败()
    {
        var (packageData, _) = create_test_package_data();
        var lockFile = create_lock_file(_publisher_a, 1, "any");
        var manifest = create_manifest(_publisher_a, 3);

        var downloadResult = new ValhallaDownloadResult
        {
            package_name = _test_package_name,
            version = _test_version,
            package_data = packageData
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.validate_against_lock(
            lockFile, manifest, downloadResult, _test_package_name, _test_version);

        Assert.False(passed);
        Assert.Contains("PURGE", error);
    }

    [Fact]
    public void Publisher不匹配_校验失败()
    {
        var (packageData, _) = create_test_package_data();
        var lockFile = create_lock_file(_publisher_a, 1, "any");

        // 远程 manifest 中 publisher 与锁文件不同
        var manifest = create_manifest(_publisher_b, 1);

        var downloadResult = new ValhallaDownloadResult
        {
            package_name = _test_package_name,
            version = _test_version,
            package_data = packageData
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.validate_against_lock(
            lockFile, manifest, downloadResult, _test_package_name, _test_version);

        Assert.False(passed);
        Assert.Contains("发布者", error);
    }

    [Fact]
    public void SHA256不匹配_校验失败()
    {
        var (packageData, digest) = create_test_package_data();
        var wrongSha256 = "0000000000000000000000000000000000000000000000000000000000000000";
        var lockFile = create_lock_file(_publisher_a, 1, wrongSha256);
        var manifest = create_manifest(_publisher_a, 1);

        var downloadResult = new ValhallaDownloadResult
        {
            package_name = _test_package_name,
            version = _test_version,
            package_data = packageData,
            package_sha256 = digest.hex_string
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.validate_against_lock(
            lockFile, manifest, downloadResult, _test_package_name, _test_version);

        Assert.False(passed);
        Assert.Contains("SHA-256", error);
        Assert.Contains("锁文件", error);
    }

    [Fact]
    public void 所有匹配_校验通过()
    {
        var (packageData, digest) = create_test_package_data();
        var lockFile = create_lock_file(_publisher_a, 1, digest.hex_string);
        var manifest = create_manifest(_publisher_a, 1);

        var downloadResult = new ValhallaDownloadResult
        {
            package_name = _test_package_name,
            version = _test_version,
            package_data = packageData,
            package_sha256 = digest.hex_string
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.validate_against_lock(
            lockFile, manifest, downloadResult, _test_package_name, _test_version);

        Assert.True(passed);
        Assert.Null(error);
    }
}