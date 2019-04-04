namespace Valkyrie.PackageManager.Tests;

public class CredentialProviderTests
{
    private string get_temp_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "legion-tests", "cred-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    #region CondaEnvProvider

    [Fact]
    public async Task CondaEnv_AnacondaApiToken_Found()
    {
        Environment.SetEnvironmentVariable("ANACONDA_API_TOKEN", "conda-api-token");

        try
        {
            var provider = new CondaEnvProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("conda-api-token", credential.token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANACONDA_API_TOKEN", null);
        }
    }

    #endregion

    #region MavenEnvProvider

    [Fact]
    public async Task MavenEnv_MavenToken_Found()
    {
        Environment.SetEnvironmentVariable("MAVEN_TOKEN", "maven-env-token");

        try
        {
            var provider = new MavenEnvProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("maven-env-token", credential.token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MAVEN_TOKEN", null);
        }
    }

    #endregion

    #region NuGetEnvProvider

    [Fact]
    public async Task NuGetEnv_NuGetApiKey_Found()
    {
        Environment.SetEnvironmentVariable("NUGET_API_KEY", "nuget-api-key-123");

        try
        {
            var provider = new NuGetEnvProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("nuget-api-key-123", credential.token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_API_KEY", null);
        }
    }

    #endregion

    #region NpmNpmrcProvider

    [Fact]
    public async Task NpmNpmrc_AuthToken_ParsedCorrectly()
    {
        var dir = get_temp_dir();
        var npmrcPath = Path.Combine(dir, ".npmrc");
        await File.WriteAllTextAsync(npmrcPath, "//registry.npmjs.org/:_authToken=npm_test_token_123\n");

        var originalHome = Environment.GetEnvironmentVariable("USERPROFILE")
                           ?? Environment.GetEnvironmentVariable("HOME") ?? "";
        Environment.SetEnvironmentVariable("USERPROFILE", dir);

        try
        {
            var provider = new NpmNpmrcProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("npm_test_token_123", credential.token);
            Assert.Contains(".npmrc", credential.source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
            File.Delete(npmrcPath);
        }
    }

    [Fact]
    public async Task NpmNpmrc_NoFile_ReturnsNull()
    {
        var dir = get_temp_dir();
        var originalHome = Environment.GetEnvironmentVariable("USERPROFILE")
                           ?? Environment.GetEnvironmentVariable("HOME") ?? "";
        Environment.SetEnvironmentVariable("USERPROFILE", dir);

        try
        {
            var provider = new NpmNpmrcProvider();
            Assert.False(provider.is_available());

            var credential = await provider.discover();
            Assert.Null(credential);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
        }
    }

    #endregion

    #region NpmEnvProvider

    [Fact]
    public async Task NpmEnv_NodeAuthToken_Found()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", "env-npm-token-abc");

        try
        {
            var provider = new NpmEnvProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("env-npm-token-abc", credential.token);
            Assert.Contains("NODE_AUTH_TOKEN", credential.source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        }
    }

    [Fact]
    public async Task NpmEnv_NoToken_FallsThrough()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        Environment.SetEnvironmentVariable("NPM_TOKEN", null);

        var provider = new NpmEnvProvider();
        Assert.False(provider.is_available());

        var credential = await provider.discover();
        Assert.Null(credential);
    }

    #endregion

    #region JsrDenoConfigProvider

    [Fact]
    public async Task JsrDenoConfig_TokenFound()
    {
        var dir = get_temp_dir();
        var denoDir = Path.Combine(dir, "deno");
        Directory.CreateDirectory(denoDir);
        var configPath = Path.Combine(denoDir, "config.json");
        await File.WriteAllTextAsync(configPath, @"{ ""https://jsr.io"": ""jsr-config-token-xyz"", ""net.jsr"": ""alt-token"" }");

        var originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var provider = new JsrDenoConfigProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("alt-token", credential.token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }

    [Fact]
    public async Task JsrDenoConfig_NoFile_ReturnsNull()
    {
        var dir = get_temp_dir();
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var provider = new JsrDenoConfigProvider();
            Assert.False(provider.is_available());

            var credential = await provider.discover();
            Assert.Null(credential);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }

    #endregion

    #region JsrEnvProvider

    [Fact]
    public async Task JsrEnv_JsrToken_Found()
    {
        Environment.SetEnvironmentVariable("JSR_TOKEN", "jsr-env-token");

        try
        {
            var provider = new JsrEnvProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("jsr-env-token", credential.token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("JSR_TOKEN", null);
        }
    }

    [Fact]
    public async Task JsrEnv_DenoAuthTokens_Found()
    {
        Environment.SetEnvironmentVariable("DENO_AUTH_TOKENS", "npm@token1;jsr@jsr-from-deno-token;other@token3");

        try
        {
            var provider = new JsrEnvProvider();
            Assert.True(provider.is_available());

            var credential = await provider.discover();
            Assert.NotNull(credential);
            Assert.Equal("jsr-from-deno-token", credential.token);
            Assert.Contains("DENO_AUTH_TOKENS", credential.source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DENO_AUTH_TOKENS", null);
        }
    }

    #endregion

    #region CredentialDiscoveryManager

    [Fact]
    public async Task DiscoveryManager_WithEnvToken_DiscoversNpm()
    {
        Environment.SetEnvironmentVariable("DISCOVERY_NPM_TOKEN", "discovery-npm-token");

        var provider = new FakeNpmEnvProvider("discovery-npm-token");
        var manager = new CredentialDiscoveryManager();
        manager.register_provider(provider);

        try
        {
            var credential = await manager.discover("npm");

            Assert.NotNull(credential);
            Assert.Equal("discovery-npm-token", credential.token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DISCOVERY_NPM_TOKEN", null);
        }
    }

    [Fact]
    public async Task DiscoveryManager_NothingAvailable_ReturnsNull()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        Environment.SetEnvironmentVariable("NPM_TOKEN", null);
        Environment.SetEnvironmentVariable("JSR_TOKEN", null);
        Environment.SetEnvironmentVariable("DENO_AUTH_TOKENS", null);
        Environment.SetEnvironmentVariable("ANACONDA_API_TOKEN", null);
        Environment.SetEnvironmentVariable("MAVEN_TOKEN", null);
        Environment.SetEnvironmentVariable("NUGET_API_KEY", null);

        var dir = get_temp_dir();
        var originalHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";

        Environment.SetEnvironmentVariable("USERPROFILE", dir);
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var manager = new CredentialDiscoveryManager();
            manager.register_builtin_providers();

            var credential = await manager.discover("npm");

            Assert.Null(credential);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }

    [Fact]
    public void DiscoveryManager_GetAvailableProviders_EmptyWhenNone()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        Environment.SetEnvironmentVariable("NPM_TOKEN", null);
        Environment.SetEnvironmentVariable("MAVEN_TOKEN", null);

        var dir = get_temp_dir();
        var originalHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";

        Environment.SetEnvironmentVariable("USERPROFILE", dir);

        try
        {
            var manager = new CredentialDiscoveryManager();
            manager.register_builtin_providers();

            var available = manager.get_available_providers("maven");

            Assert.Empty(available);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
        }
    }

    [Fact]
    public void DiscoveryManager_ProviderPriority_OrderedByPriority()
    {
        var manager = new CredentialDiscoveryManager();
        manager.register_builtin_providers();

        var npmProviders = manager.get_available_providers("npm");

        if (npmProviders.Count >= 2)
            for (var i = 1; i < npmProviders.Count; i++)
                Assert.True(npmProviders[i - 1].priority <= npmProviders[i].priority);
    }

    #endregion
}