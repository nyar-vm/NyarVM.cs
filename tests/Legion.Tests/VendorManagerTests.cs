using System.Net;

namespace Valkyrie.PackageManager.Tests;

public class VendorManagerTests
{
    private string get_temp_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "legion-tests", "vendor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private VendorManager create_manager(string configDir)
    {
        var sourceManager = new RegistrySourceManager(configDir);
        var authStore = new VendorAuthStore(configDir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(),
            ["jsr"] = new JsrRegistry(),
            ["conda"] = new CondaRegistry(),
            ["maven"] = new MavenRegistry(),
            ["nuget"] = new NuGetRegistry()
        };

        return new VendorManager(sourceManager, authStore, registries);
    }

    [Fact]
    public void AddVendor_AddsNewVendor()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        manager.add_vendor("custom", "https://custom.registry.io");

        var status = manager.get_vendor_status("custom");
        Assert.NotNull(status);
        Assert.Equal("https://custom.registry.io", status.endpoint);
    }

    [Fact]
    public void RemoveVendor_RemovesExistingVendor()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        var removed = manager.remove_vendor("jsr");

        Assert.True(removed);
        Assert.Null(manager.get_vendor_status("jsr"));
    }

    [Fact]
    public void ListVendors_ReturnsAllConfiguredVendors()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        var vendors = manager.list_vendors();

        Assert.Equal(5, vendors.Count);

        foreach (var v in vendors) Assert.False(v.is_logged_in);
    }

    [Fact]
    public async Task LoginAsync_WithValidToken_ReturnsSuccess()
    {
        var dir = get_temp_dir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""testuser"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        var result = await manager.login("npm", "valid-test-token");

        Assert.True(result.success);
        Assert.Equal("testuser", result.username);
        Assert.Equal("手动输入", result.credential_source);
        Assert.True(manager.is_logged_in("npm"));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidToken_ReturnsFailure()
    {
        var dir = get_temp_dir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.Unauthorized, @"{}");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        var result = await manager.login("npm", "invalid-token");

        Assert.False(result.success);
        Assert.Contains("验证失败", result.error_message);
        Assert.False(manager.is_logged_in("npm"));
    }

    [Fact]
    public async Task LoginAsync_WithoutToken_AttemptsAutoDiscovery()
    {
        var dir = get_temp_dir();

        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""auto-user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);

        var fakeProvider = new FakeCredentialProvider("npm", "auto-token", "mock-npmrc");
        manager.credential_discovery.register_provider(fakeProvider);

        var result = await manager.login("npm");

        Assert.True(result.success);
        Assert.Equal("auto-user", result.username);
        Assert.Contains("mock-npmrc", result.credential_source);
    }

    [Fact]
    public async Task LoginAsync_UnknownVendor_ReturnsFailure()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        var result = await manager.login("unknown", "token");

        Assert.False(result.success);
        Assert.Contains("未配置", result.error_message);
    }

    [Fact]
    public async Task LoginAsync_EmptyToken_AutoDiscoveryFallback()
    {
        var dir = get_temp_dir();

        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""fallback-user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);

        var fakeProvider = new FakeCredentialProvider("npm", "fallback-token", "mock-fallback-npmrc");
        manager.credential_discovery.register_provider(fakeProvider);

        var result = await manager.login("npm", "");

        Assert.True(result.success);
        Assert.Equal("fallback-user", result.username);
        Assert.Contains("mock-fallback-npmrc", result.credential_source);
    }

    [Fact]
    public async Task LogoutAsync_LoggedInVendor_Success()
    {
        var dir = get_temp_dir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.login("npm", "token");

        var result = await manager.logout("npm");

        Assert.True(result.success);
        Assert.False(manager.is_logged_in("npm"));
    }

    [Fact]
    public async Task LogoutAsync_NotLoggedIn_ReturnsFailure()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        var result = await manager.logout("npm");

        Assert.False(result.success);
        Assert.Contains("未登录", result.error_message);
    }

    [Fact]
    public async Task WhoAmI_LoggedIn_ReturnsUserInfo()
    {
        var dir = get_temp_dir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""myuser"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.login("npm", "token");

        var result = manager.who_am_i("npm");

        Assert.True(result.success);
        Assert.Equal("myuser", result.username);
    }

    [Fact]
    public void WhoAmI_NotLoggedIn_ReturnsFailure()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        var result = manager.who_am_i("npm");

        Assert.False(result.success);
        Assert.Contains("未登录", result.error_message);
    }

    [Fact]
    public void GetVendorStatus_ReturnsCorrectStatus()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        var status = manager.get_vendor_status("npm");

        Assert.NotNull(status);
        Assert.Equal("npm", status.name);
        Assert.Equal("https://registry.npmjs.org", status.endpoint);
        Assert.False(status.is_logged_in);
    }

    [Fact]
    public async Task GetToken_ReturnsStoredTokenForPublish()
    {
        var dir = get_temp_dir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""pubuser"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.login("npm", "publish-token-xyz");

        Assert.Equal("publish-token-xyz", manager.get_token("npm"));
    }

    [Fact]
    public void IsLoggedIn_NoLogin_ReturnsFalse()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        Assert.False(manager.is_logged_in("npm"));
    }

    [Fact]
    public async Task RemoveVendor_AlsoRemovesAuthToken()
    {
        var dir = get_temp_dir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""u"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.login("npm", "token");
        Assert.True(manager.is_logged_in("npm"));

        manager.remove_vendor("npm");

        Assert.False(manager.is_logged_in("npm"));
        Assert.Null(manager.get_vendor_status("npm"));
    }

    [Fact]
    public void CredentialDiscovery_IsInitialized()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        Assert.NotNull(manager.credential_discovery);
    }

    [Fact]
    public async Task RefreshFromOfficialTool_WithNpmrc_ReturnsSuccess()
    {
        var dir = get_temp_dir();

        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""refresh-user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);

        var fakeProvider = new FakeCredentialProvider("npm", "refresh-token", "npmrc-mock");
        manager.credential_discovery.register_provider(fakeProvider);

        var result = await manager.refresh_from_official_tool("npm");

        Assert.True(result.success);
        Assert.Equal("refresh-user", result.username);
        Assert.Contains("npmrc-mock", result.credential_source);
    }

    [Fact]
    public void VendorStatus_IncludesCredentialsInfo()
    {
        var dir = get_temp_dir();
        var manager = create_manager(dir);

        var originalHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";
        Environment.SetEnvironmentVariable("USERPROFILE", dir);
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var status = manager.get_vendor_status("npm");

            Assert.NotNull(status);
            Assert.Equal("npm", status.name);
            Assert.False(status.has_official_credentials);
            Assert.Empty(status.available_credential_sources);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }
}