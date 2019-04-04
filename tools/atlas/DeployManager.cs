namespace Atlas.CLI;

public sealed class DeployManager
{
    private readonly string _environment;
    private readonly string _package_path;
    private readonly string? _region;
    private readonly string? _version;

    public DeployManager(string environment, string? region, string? version, string packagePath)
    {
        _environment = environment;
        _region = region;
        _version = version ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        _package_path = packagePath;
    }

    public async Task<DeployResult> deploy(bool wait)
    {
        try
        {
            if (!Directory.Exists(_package_path) && !File.Exists(_package_path))
                return DeployResult.fail($"部署包不存在: {_package_path}");

            var deploymentId = Guid.NewGuid().ToString("N")[..12];

            await Task.Delay(100);

            return new DeployResult
            {
                success = true,
                version = _version!,
                deployment_id = deploymentId,
                url = $"https://{_environment}.atlas.app/{_version}"
            };
        }
        catch (Exception ex)
        {
            return DeployResult.fail(ex.Message);
        }
    }
}

public sealed class DeployResult
{
    public bool success { get; init; }
    public string version { get; init; } = string.Empty;
    public string deployment_id { get; init; } = string.Empty;
    public string? url { get; init; }
    public string? error_message { get; init; }

    public static DeployResult fail(string message)
    {
        return new DeployResult { success = false, error_message = message };
    }
}