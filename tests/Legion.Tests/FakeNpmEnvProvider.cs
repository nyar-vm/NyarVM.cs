namespace Valkyrie.PackageManager.Tests;

/// <summary>
///     测试用伪造 npm 环境变量提供器
/// </summary>
public class FakeNpmEnvProvider : ICredentialProvider
{
    private readonly string _token;

    public FakeNpmEnvProvider(string token)
    {
        _token = token;
    }

    public string provider_name => "测试 npm 环境变量";
    public string vendor_name => "npm";
    public int priority => 5;

    public bool is_available()
    {
        return !string.IsNullOrEmpty(_token);
    }

    public Task<DiscoveredCredential?> discover()
    {
        if (string.IsNullOrEmpty(_token)) return Task.FromResult<DiscoveredCredential?>(null);

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            token = _token,
            source = "测试 npm 环境变量"
        });
    }
}