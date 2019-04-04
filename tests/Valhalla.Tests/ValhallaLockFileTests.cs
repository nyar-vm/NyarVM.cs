namespace Valhalla.Tests;

public class ValhallaLockFileTests
{
    private string get_temp_project_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"valhalla-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Exists_新实例_返回假()
    {
        var dir = get_temp_project_dir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            Assert.False(lockFile.exists());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FilePath_正确拼接()
    {
        var lockFile = new ValhallaLockFile("/project");
        var expected = Path.Combine("/project", "protoswap.lock");
        Assert.Equal(expected, lockFile.file_path);
    }

    [Fact]
    public async Task 添加条目后保存_可重新加载()
    {
        var dir = get_temp_project_dir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            lockFile.add_or_update("test.pkg", new ValhallaLockEntry
            {
                incarnation = 3,
                publisher = "abc123",
                version = "1.2.0",
                sha256 = "abcdef",
                registry = "https://valhalla.example.com"
            });

            await lockFile.save();
            Assert.True(lockFile.exists());

            var reloaded = new ValhallaLockFile(dir);
            await reloaded.load();

            var entry = reloaded.get_entry("test.pkg");
            Assert.NotNull(entry);
            Assert.Equal(3, entry!.incarnation);
            Assert.Equal("abc123", entry.publisher);
            Assert.Equal("1.2.0", entry.version);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task 删除条目_报不满足存在()
    {
        var dir = get_temp_project_dir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            lockFile.add_or_update("test.pkg", new ValhallaLockEntry { version = "1.0.0" });
            await lockFile.save();

            lockFile.remove("test.pkg");
            await lockFile.save();

            var reloaded = new ValhallaLockFile(dir);
            await reloaded.load();

            Assert.Null(reloaded.get_entry("test.pkg"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void IsVersionLocked_版本匹配_返回真()
    {
        var lockFile = new ValhallaLockFile("/project");
        lockFile.add_or_update("test.pkg", new ValhallaLockEntry { version = "2.0.0" });

        Assert.True(lockFile.is_version_locked("test.pkg", "2.0.0"));
    }

    [Fact]
    public void IsVersionLocked_版本不匹配_返回假()
    {
        var lockFile = new ValhallaLockFile("/project");
        lockFile.add_or_update("test.pkg", new ValhallaLockEntry { version = "2.0.0" });

        Assert.False(lockFile.is_version_locked("test.pkg", "1.0.0"));
    }

    [Fact]
    public void 删除不存在条目_返回假()
    {
        var lockFile = new ValhallaLockFile("/project");
        Assert.False(lockFile.remove("nonexistent"));
    }

    [Fact]
    public async Task 多次保存_UpdatedAt更新()
    {
        var dir = get_temp_project_dir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            lockFile.add_or_update("test.pkg", new ValhallaLockEntry { version = "1.0.0" });
            await lockFile.save();

            var first = new ValhallaLockFile(dir);
            await first.load();
            var firstTime = first.get_content()!.updated_at;

            await Task.Delay(10);

            lockFile.add_or_update("test.pkg", new ValhallaLockEntry { version = "2.0.0" });
            await lockFile.save();

            var second = new ValhallaLockFile(dir);
            await second.load();
            var secondTime = second.get_content()!.updated_at;

            Assert.True(secondTime > firstTime);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}