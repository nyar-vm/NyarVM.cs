using Nyar.PackageRegistry;
using Nyar.PackageRegistry.Conda;
using Nyar.PackageRegistry.Jsr;
using Nyar.PackageRegistry.Maven;
using Nyar.PackageRegistry.Npm;
using Nyar.PackageRegistry.Nuget;

namespace Nyar.PackageManager.Auth;

/// <summary>
///     Vendor 管理器，统一管理注册表源的增删查和登录认证
/// </summary>
public class VendorManager
{
    private readonly VendorAuthStore _auth_store;
    private readonly Dictionary<string, IRegistry> _registries;
    private readonly RegistrySourceManager _source_manager;

    /// <summary>
    ///     创建 Vendor 管理器
    /// </summary>
    /// <param name="sourceManager">注册表源管理器</param>
    /// <param name="authStore">认证令牌存储</param>
    /// <param name="registries">已注册的注册表适配器字典</param>
    public VendorManager(RegistrySourceManager sourceManager, VendorAuthStore authStore,
        Dictionary<string, IRegistry> registries)
    {
        _source_manager = sourceManager;
        _auth_store = authStore;
        _registries = registries;
        credential_discovery = new CredentialDiscoveryManager();
        credential_discovery.register_builtin_providers();
    }

    /// <summary>
    ///     获取凭据发现管理器
    /// </summary>
    public CredentialDiscoveryManager credential_discovery { get; }

    /// <summary>
    ///     加载认证信息
    /// </summary>
    public void load()
    {
        _auth_store.load();
    }

    /// <summary>
    ///     保存认证信息
    /// </summary>
    public async Task save()
    {
        await _auth_store.save();
    }

    #region Vendor 管理

    /// <summary>
    ///     添加 Vendor（注册表源）
    /// </summary>
    /// <param name="name">Vendor 名称</param>
    /// <param name="endpoint">端点地址</param>
    public void add_vendor(string name, string endpoint)
    {
        _source_manager.set_endpoint(name, endpoint);
    }

    /// <summary>
    ///     移除 Vendor（注册表源及其认证信息）
    /// </summary>
    /// <param name="name">Vendor 名称</param>
    /// <returns>是否成功移除</returns>
    public bool remove_vendor(string name)
    {
        _auth_store.remove_token(name);
        return _source_manager.remove_endpoint(name);
    }

    /// <summary>
    ///     列出所有已配置的 Vendor 及其状态
    /// </summary>
    /// <returns>Vendor 状态列表</returns>
    public List<VendorStatus> list_vendors()
    {
        var result = new List<VendorStatus>();

        foreach (var (name, endpoint) in _source_manager.sources)
        {
            var authInfo = _auth_store.get_auth_info(name);
            var availableProviders = credential_discovery.get_available_providers(name);

            result.Add(new VendorStatus
            {
                name = name,
                endpoint = endpoint,
                is_logged_in = authInfo?.is_logged_in ?? false,
                current_user = authInfo?.current_user,
                logged_in_at = authInfo?.logged_in_at,
                expires_at = authInfo?.expires_at,
                has_official_credentials = availableProviders.Count > 0,
                available_credential_sources =
                [
                    .. availableProviders
                        .Select(p => p.provider_name)
                ]
            });
        }

        return result;
    }

    /// <summary>
    ///     获取指定 Vendor 的状态
    /// </summary>
    /// <param name="name">Vendor 名称</param>
    /// <returns>Vendor 状态，未找到返回 null</returns>
    public VendorStatus? get_vendor_status(string name)
    {
        var endpoint = _source_manager.get_endpoint(name);

        if (endpoint is null) return null;

        var authInfo = _auth_store.get_auth_info(name);
        var availableProviders = credential_discovery.get_available_providers(name);

        return new VendorStatus
        {
            name = name,
            endpoint = endpoint,
            is_logged_in = authInfo?.is_logged_in ?? false,
            current_user = authInfo?.current_user,
            logged_in_at = authInfo?.logged_in_at,
            expires_at = authInfo?.expires_at,
            has_official_credentials = availableProviders.Count > 0,
            available_credential_sources =
            [
                .. availableProviders
                    .Select(p => p.provider_name)
            ]
        };
    }

    #endregion

    #region 登录

    /// <summary>
    ///     登录到指定 Vendor
    ///     如果 token 为空，自动尝试从官方工具配置中发现凭据
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <param name="token">认证令牌，为 null 时自动尝试发现</param>
    /// <returns>登录结果</returns>
    public async Task<VendorLoginResult> login(string vendorName, string? token = null)
    {
        var endpoint = _source_manager.get_endpoint(vendorName);

        if (endpoint is null) return VendorLoginResult.fail($"Vendor '{vendorName}' 未配置，请先使用 'legion vendor add' 添加");

        var registry = get_registry(vendorName, endpoint);

        if (registry is null) return VendorLoginResult.fail($"不支持的 Vendor 类型：'{vendorName}'");

        if (string.IsNullOrWhiteSpace(token))
        {
            var discoveryResult = await try_discover_and_login(vendorName, endpoint, registry);

            if (discoveryResult is not null) return discoveryResult;

            var availableProviders = credential_discovery.get_available_providers(vendorName);

            if (availableProviders.Count > 0)
            {
                var sources = string.Join("、", availableProviders.Select(p => p.provider_name));
                return VendorLoginResult.fail(
                    $"未提供令牌，且从官方工具（{sources}）自动获取失败，请手动传入 --token");
            }

            return VendorLoginResult.fail(
                "未提供认证令牌且无可用的官方工具凭据，请手动传入 --token");
        }

        return await verify_and_save_token(vendorName, endpoint, registry, token, "手动输入");
    }

    /// <summary>
    ///     从官方工具刷新凭据（已有存储令牌时刷新）
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>登录结果</returns>
    public async Task<VendorLoginResult> refresh_from_official_tool(string vendorName)
    {
        var endpoint = _source_manager.get_endpoint(vendorName);

        if (endpoint is null) return VendorLoginResult.fail($"Vendor '{vendorName}' 未配置");

        var registry = get_registry(vendorName, endpoint);

        if (registry is null) return VendorLoginResult.fail($"不支持的 Vendor 类型：'{vendorName}'");

        var result = await try_discover_and_login(vendorName, endpoint, registry);

        return result ?? VendorLoginResult.fail(
            $"未找到 '{vendorName}' 的官方工具凭据，请手动登录");
    }

    /// <summary>
    ///     退出指定 Vendor 的登录
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>是否成功登出</returns>
    public async Task<VendorLoginResult> logout(string vendorName)
    {
        if (!_auth_store.is_logged_in(vendorName)) return VendorLoginResult.fail($"未登录到 '{vendorName}'");

        _auth_store.remove_token(vendorName);
        await _auth_store.save();

        return VendorLoginResult.ok(vendorName, string.Empty, null);
    }

    /// <summary>
    ///     获取已登录 Vendor 的用户信息
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>用户信息</returns>
    public VendorLoginResult who_am_i(string vendorName)
    {
        var authInfo = _auth_store.get_auth_info(vendorName);

        if (authInfo is null || !authInfo.is_logged_in)
            return VendorLoginResult.fail($"未登录到 '{vendorName}'，请先使用 'legion vendor login {vendorName}' 登录");

        return VendorLoginResult.ok(
            vendorName,
            authInfo.current_user ?? "未知用户",
            authInfo.expires_at);
    }

    /// <summary>
    ///     获取 Vendor 的认证令牌（用于自动注入到发布流程）
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>认证令牌，未登录返回 null</returns>
    public string? get_token(string vendorName)
    {
        return _auth_store.get_token(vendorName);
    }

    /// <summary>
    ///     检查是否已登录指定 Vendor
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>是否已登录</returns>
    public bool is_logged_in(string vendorName)
    {
        return _auth_store.is_logged_in(vendorName);
    }

    #endregion

    #region 私有方法

    private async Task<VendorLoginResult?> try_discover_and_login(
        string vendorName, string endpoint, IRegistry registry)
    {
        var credential = await credential_discovery.discover(vendorName);

        if (credential is null || string.IsNullOrEmpty(credential.token)) return null;

        try
        {
            var result = await verify_and_save_token(
                vendorName, endpoint, registry,
                credential.token,
                credential.source);

            if (result.success) return result;

            return VendorLoginResult.fail(
                $"官方工具凭据（{credential.source}）中的令牌验证失败：{result.error_message}");
        }
        catch (Exception ex)
        {
            return VendorLoginResult.fail(
                $"官方工具凭据（{credential.source}）验证异常：{ex.Message}");
        }
    }

    private async Task<VendorLoginResult> verify_and_save_token(
        string vendorName, string endpoint, IRegistry registry,
        string token, string source)
    {
        var verifyResult = await registry.verify_token(token);

        if (!verifyResult.valid) return VendorLoginResult.fail($"令牌验证失败：{verifyResult.error_message ?? "未知错误"}");

        _auth_store.save_token(
            vendorName,
            endpoint,
            token,
            verifyResult.username,
            verifyResult.expires_at);

        await _auth_store.save();

        return new VendorLoginResult
        {
            success = true,
            vendor_name = vendorName,
            username = verifyResult.username ?? "未知用户",
            expires_at = verifyResult.expires_at,
            credential_source = source
        };
    }

    private IRegistry? get_registry(string vendorName, string endpoint)
    {
        if (_registries.TryGetValue(vendorName, out var registry)) return registry;

        return vendorName.ToLowerInvariant() switch
        {
            "npm" => new NpmRegistry { endpoint = endpoint },
            "jsr" => new JsrRegistry { endpoint = endpoint },
            "conda" => new CondaRegistry { endpoint = endpoint },
            "maven" => new MavenRegistry { endpoint = endpoint },
            "nuget" => new NuGetRegistry { endpoint = endpoint },
            _ => null
        };
    }

    #endregion
}