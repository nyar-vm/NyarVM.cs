namespace Valkyrie.PackageManager.Tests;

public class PackageCacheTests
{
    private string get_temp_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "legion-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void AddPackage_NewPackage_AddedToIndex()
    {
        var cacheDir = get_temp_dir();
        var cache = new PackageCache(cacheDir);

        cache.add_package("test-pkg", "1.0.0", "/some/path");

        Assert.True(cache.has_package("test-pkg", "1.0.0"));
    }

    [Fact]
    public void RemovePackage_RemovesFromIndex()
    {
        var cacheDir = get_temp_dir();
        var cache = new PackageCache(cacheDir);

        cache.add_package("test-pkg", "1.0.0", cacheDir);
        cache.RemovePackage("test-pkg", "1.0.0");

        Assert.False(cache.has_package("test-pkg", "1.0.0"));
    }

    [Fact]
    public async Task SaveAndLoad_PreservesData()
    {
        var cacheDir = get_temp_dir();
        var cache = new PackageCache(cacheDir);

        cache.add_package("pkg-a", "1.0.0", "/path/to/a");
        cache.add_package("pkg-b", "2.0.0", "/path/to/b");
        await cache.save();

        var cache2 = new PackageCache(cacheDir);
        cache2.Load();

        Assert.True(cache2.has_package("pkg-a", "1.0.0"));
        Assert.True(cache2.has_package("pkg-b", "2.0.0"));
    }

    [Fact]
    public void Verify_AllFilesExist_ReturnsTrue()
    {
        var cacheDir = get_temp_dir();
        var pkgDir = Path.Combine(cacheDir, "test-pkg");
        Directory.CreateDirectory(pkgDir);
        File.WriteAllText(Path.Combine(pkgDir, "test.txt"), "hello");

        var cache = new PackageCache(cacheDir);
        cache.add_package("test-pkg", "1.0.0", pkgDir);

        Assert.True(cache.Verify());
    }

    [Fact]
    public void Verify_FileMissing_ReturnsFalse()
    {
        var cacheDir = get_temp_dir();
        var cache = new PackageCache(cacheDir);

        cache.add_package("test-pkg", "1.0.0", "/nonexistent/path/12345");

        Assert.False(cache.Verify());
    }

    [Fact]
    public void Clean_RemovesAllEntries()
    {
        var cacheDir = get_temp_dir();
        var cache = new PackageCache(cacheDir);

        cache.add_package("pkg-a", "1.0.0", "/path/a");
        cache.add_package("pkg-b", "2.0.0", "/path/b");
        cache.Clean();

        Assert.False(cache.has_package("pkg-a", "1.0.0"));
        Assert.False(cache.has_package("pkg-b", "2.0.0"));
    }

    [Fact]
    public void List_ReturnsAllCached()
    {
        var cacheDir = get_temp_dir();
        var pkgADir = Path.Combine(cacheDir, "pkg-a");
        var pkgBDir = Path.Combine(cacheDir, "pkg-b");
        Directory.CreateDirectory(pkgADir);
        Directory.CreateDirectory(pkgBDir);
        File.WriteAllText(Path.Combine(pkgADir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(pkgBDir, "b.txt"), "b");

        var cache = new PackageCache(cacheDir);
        cache.add_package("pkg-a", "1.0.0", pkgADir);
        cache.add_package("pkg-b", "2.0.0", pkgBDir);

        var list = cache.list();

        Assert.Equal(2, list.Count);
        Assert.Contains(list, e => e.PackageName == "pkg-a" && e.Version == "1.0.0");
        Assert.Contains(list, e => e.PackageName == "pkg-b" && e.Version == "2.0.0");
    }

    [Fact]
    public void GetCacheDir_ReturnsCachedPath()
    {
        var cacheDir = get_temp_dir();
        var pkgDir = Path.Combine(cacheDir, "my-pkg");
        Directory.CreateDirectory(pkgDir);
        File.WriteAllText(Path.Combine(pkgDir, "data.txt"), "data");

        var cache = new PackageCache(cacheDir);
        cache.add_package("my-pkg", "3.0.0", pkgDir);

        var result = cache.get_cache_dir("my-pkg", "3.0.0");

        Assert.Equal(pkgDir, result);
    }
}