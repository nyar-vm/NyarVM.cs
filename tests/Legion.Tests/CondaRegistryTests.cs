using System.Net;
using Nyar.PackageRegistry;
using Nyar.PackageRegistry.Conda;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class CondaRegistryTests
{
    private const string _conda_package_json = @"{
            ""name"": ""numpy"",
            ""latest_version"": ""2.1.0"",
            ""summary"": ""数值计算库"",
            ""description"": ""多维数组与矩阵运算"",
            ""home"": ""https://numpy.org"",
            ""author"": ""NumPy Team"",
            ""license"": ""BSD-3-Clause"",
            ""files"": [
                { ""download_url"": ""https://conda.anaconda.org/conda-forge/linux-64/numpy-2.1.0.tar.bz2"" }
            ],
            ""dependencies"": {
                ""python"": "">=3.9"",
                ""openblas"": "">=0.3.0""
            }
        }";

    [Fact]
    public async Task GetPackageAsync_LatestVersion_ReturnsCorrectPackage()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://api.anaconda.org/package/conda-forge/numpy",
            HttpStatusCode.OK, _conda_package_json);

        var client = new HttpClient(handler);
        var registry = new CondaRegistry(client);

        var package = await registry.get_package("numpy", "latest");

        Assert.NotNull(package);
        Assert.Equal("numpy", package.name);
        Assert.Equal("2.1.0", package.version);
        Assert.Equal("数值计算库", package.description);
        Assert.Equal("https://numpy.org", package.homepage);
        Assert.Equal("NumPy Team", package.author);
        Assert.Equal("BSD-3-Clause", package.license);
        Assert.Contains("numpy-2.1.0.tar.bz2", package.dist_tarball);
    }

    [Fact]
    public async Task GetPackageAsync_IncludesDependencies()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://api.anaconda.org/package/conda-forge/numpy",
            HttpStatusCode.OK, _conda_package_json);

        var client = new HttpClient(handler);
        var registry = new CondaRegistry(client);

        var package = await registry.get_package("numpy", "latest");

        Assert.Equal(2, package.Dependencies.Count);
        Assert.Contains("python@>=3.9", package.Dependencies);
        Assert.Contains("openblas@>=0.3.0", package.Dependencies);
    }

    [Fact]
    public async Task GetPackageAsync_NotFound_ThrowsRegistryException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        var client = new HttpClient(handler);
        var registry = new CondaRegistry(client);

        await Assert.ThrowsAsync<RegistryException>(() => registry.get_package("nonexistent", "latest"));
    }

    [Fact]
    public async Task SearchPackagesAsync_ReturnsResults()
    {
        var searchJson = @"[
                { ""name"": ""numpy"", ""latest_version"": ""2.1.0"", ""summary"": ""数值计算"", ""home"": ""https://numpy.org"", ""author"": ""NumPy Team"", ""license"": ""BSD"" },
                { ""name"": ""scipy"", ""latest_version"": ""1.14.0"", ""summary"": ""科学计算"", ""home"": ""https://scipy.org"", ""author"": ""SciPy Team"", ""license"": ""BSD"" }
            ]";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://api.anaconda.org/search?name=sci", HttpStatusCode.OK, searchJson);

        var client = new HttpClient(handler);
        var registry = new CondaRegistry(client);

        var results = await registry.search_packages("sci");

        Assert.Equal(2, results.Count);
        Assert.Equal("numpy", results[0].Name);
        Assert.Equal("scipy", results[1].Name);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_ReturnsVersionList()
    {
        var filesJson = @"{
                ""files"": [
                    { ""version"": ""1.26.0"" },
                    { ""version"": ""2.0.0"" },
                    { ""version"": ""2.1.0"" }
                ]
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.add_route("https://api.anaconda.org/package/conda-forge/numpy",
            HttpStatusCode.OK, filesJson);

        var client = new HttpClient(handler);
        var registry = new CondaRegistry(client);

        var versions = await registry.get_package_versions("numpy");

        Assert.Equal(3, versions.Count);
        Assert.Contains("2.1.0", versions);
    }

    [Fact]
    public async Task PublishPackageAsync_RequiresAuthToken()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var registry = new CondaRegistry(client);

        var options = new PublishOptions { PackageName = "test", Version = "1.0.0" };

        await Assert.ThrowsAsync<RegistryException>(() => registry.PublishPackageAsync(options, Array.Empty<byte>()));
    }
}