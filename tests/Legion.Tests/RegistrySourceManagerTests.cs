namespace Valkyrie.PackageManager.Tests;

public class RegistrySourceManagerTests
{
    private string get_temp_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "legion-tests", "rsm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Constructor_InitializesDefaultSources()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);

        Assert.NotNull(manager.get_endpoint("npm"));
        Assert.NotNull(manager.get_endpoint("jsr"));
        Assert.NotNull(manager.get_endpoint("conda"));
        Assert.NotNull(manager.get_endpoint("maven"));
        Assert.NotNull(manager.get_endpoint("nuget"));
    }

    [Fact]
    public void DefaultSources_HaveAllFive()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);
        manager.load();

        Assert.Equal(5, manager.sources.Count);
        Assert.Equal("https://registry.npmjs.org", manager.get_endpoint("npm"));
        Assert.Equal("https://jsr.io", manager.get_endpoint("jsr"));
        Assert.Equal("https://api.anaconda.org", manager.get_endpoint("conda"));
        Assert.Equal("https://search.maven.org", manager.get_endpoint("maven"));
        Assert.Equal("https://api.nuget.org/v3", manager.get_endpoint("nuget"));
    }

    [Fact]
    public void SetEndpoint_UpdatesSource()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);

        manager.set_endpoint("npm", "https://registry.npmmirror.com");

        Assert.Equal("https://registry.npmmirror.com", manager.get_endpoint("npm"));
    }

    [Fact]
    public void RemoveEndpoint_RemovesSource()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);

        var removed = manager.remove_endpoint("jsr");

        Assert.True(removed);
        Assert.Null(manager.get_endpoint("jsr"));
    }

    [Fact]
    public void RemoveEndpoint_CaseInsensitive()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);

        var removed = manager.remove_endpoint("NPM");

        Assert.True(removed);
        Assert.Null(manager.get_endpoint("npm"));
    }

    [Fact]
    public async Task SaveAndLoad_PreservesCustomEndpoints()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);

        manager.set_endpoint("npm", "https://custom-registry.example.com");
        manager.set_endpoint("maven", "https://maven.example.com");
        manager.remove_endpoint("jsr");
        await manager.save();

        var loaded = new RegistrySourceManager(configDir);
        loaded.load();

        Assert.Equal("https://custom-registry.example.com", loaded.get_endpoint("npm"));
        Assert.Equal("https://maven.example.com", loaded.get_endpoint("maven"));
        Assert.Null(loaded.get_endpoint("jsr"));
    }

    [Fact]
    public void CreateRegistry_Npm_CreatesNpmRegistry()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);

        var registry = manager.create_registry("npm");

        Assert.IsType<NpmRegistry>(registry);
        Assert.Equal("https://registry.npmjs.org", registry.endpoint);
    }

    [Fact]
    public void CreateRegistry_WithCustomEndpoint_UsesCustomEndpoint()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);
        manager.set_endpoint("npm", "https://custom.registry.io");

        var registry = manager.create_registry("npm");

        Assert.Equal("https://custom.registry.io", registry.endpoint);
    }

    [Fact]
    public void CreateRegistry_Unknown_Throws()
    {
        var configDir = get_temp_dir();
        var manager = new RegistrySourceManager(configDir);

        Assert.Throws<ArgumentException>(() => manager.create_registry("unknown"));
    }
}