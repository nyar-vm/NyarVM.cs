namespace Valkyrie.PackageManager.Tests;

public class VendorAuthStoreTests
{
    private string get_temp_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "legion-tests", "auth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void IsLoggedIn_NoToken_ReturnsFalse()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        Assert.False(store.is_logged_in("npm"));
    }

    [Fact]
    public void SaveToken_ThenIsLoggedIn_ReturnsTrue()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("npm", "https://registry.npmjs.org", "test-token-12345");

        Assert.True(store.is_logged_in("npm"));
    }

    [Fact]
    public void GetToken_ReturnsStoredToken()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("npm", "https://registry.npmjs.org", "my-secret-token");

        var token = store.get_token("npm");
        Assert.Equal("my-secret-token", token);
    }

    [Fact]
    public void GetToken_NoToken_ReturnsNull()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        var token = store.get_token("npm");
        Assert.Null(token);
    }

    [Fact]
    public void RemoveToken_RemovesAuthInfo()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("npm", "https://registry.npmjs.org", "token");
        var removed = store.remove_token("npm");

        Assert.True(removed);
        Assert.False(store.is_logged_in("npm"));
        Assert.Null(store.get_token("npm"));
    }

    [Fact]
    public void GetAuthInfo_StoresUserAndExpiry()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);
        var expiry = DateTime.UtcNow.AddDays(30);

        store.save_token("npm", "https://registry.npmjs.org", "token", "testuser", expiry);

        var info = store.get_auth_info("npm");
        Assert.NotNull(info);
        Assert.Equal("testuser", info.current_user);
        Assert.Equal(expiry, info.expires_at);
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        var info = new VendorAuthInfo
        {
            vendor_name = "npm",
            token = "expired-token",
            expires_at = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(info.is_expired);
        Assert.False(info.is_logged_in);
    }

    [Fact]
    public void IsExpired_NotExpired_ReturnsFalse()
    {
        var info = new VendorAuthInfo
        {
            vendor_name = "npm",
            token = "valid-token",
            expires_at = DateTime.UtcNow.AddDays(30)
        };

        Assert.False(info.is_expired);
        Assert.True(info.is_logged_in);
    }

    [Fact]
    public void ObfuscateDeobfuscate_RoundTrip()
    {
        var original = "npm_abc123def456ghi789";

        var obfuscated = VendorAuthStore.obfuscate_token(original);
        var recovered = VendorAuthStore.deobfuscate_token(obfuscated);

        Assert.NotEqual(original, obfuscated);
        Assert.Equal(original, recovered);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesTokens()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("npm", "https://registry.npmjs.org", "npm-token-123");
        store.save_token("jsr", "https://jsr.io", "jsr-token-456");
        await store.save();

        var loaded = new VendorAuthStore(dir);
        loaded.load();

        Assert.True(loaded.is_logged_in("npm"));
        Assert.True(loaded.is_logged_in("jsr"));
        Assert.Equal("npm-token-123", loaded.get_token("npm"));
        Assert.Equal("jsr-token-456", loaded.get_token("jsr"));
    }

    [Fact]
    public void GetToken_ExpiredToken_ReturnsNull()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("npm", "https://registry.npmjs.org", "old-token", null, DateTime.UtcNow.AddDays(-5));

        Assert.False(store.is_logged_in("npm"));
        Assert.Null(store.get_token("npm"));
    }

    [Fact]
    public void MultipleVendors_ManagedIndependently()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("npm", "https://registry.npmjs.org", "n-token");
        store.save_token("nuget", "https://api.nuget.org/v3", "g-token");

        Assert.True(store.is_logged_in("npm"));
        Assert.True(store.is_logged_in("nuget"));

        store.remove_token("npm");

        Assert.False(store.is_logged_in("npm"));
        Assert.True(store.is_logged_in("nuget"));
    }

    [Fact]
    public void CaseInsensitive_VendorName()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("NPM", "https://registry.npmjs.org", "token");

        Assert.True(store.is_logged_in("npm"));
        Assert.Equal("token", store.get_token("NPM"));
    }

    [Fact]
    public async Task SaveAsync_OnlyWritesLoggedInVendors()
    {
        var dir = get_temp_dir();
        var store = new VendorAuthStore(dir);

        store.save_token("npm", "https://registry.npmjs.org", "active-token");
        store.save_token("jsr", "https://jsr.io", "expired-token", null, DateTime.UtcNow.AddDays(-1));
        await store.save();

        var loaded = new VendorAuthStore(dir);
        loaded.load();

        Assert.True(loaded.is_logged_in("npm"));
        Assert.False(loaded.is_logged_in("jsr"));
    }
}