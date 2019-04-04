namespace Valkyrie.PackageManager.Tests;

/// <summary>
///     测试用可控制凭据提供器
/// </summary>
public class FakeCredentialProvider : ICredentialProvider
{
    private readonly string _source;
    private readonly string? _token;

    public FakeCredentialProvider(string vendorName, string? token, string source)
    {
        vendor_name = vendorName;
        _token = token;
        _source = source;
    }

    public string provider_name => "测试凭据源";
    public string vendor_name { get; }

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
            source = _source
        });
    }
}