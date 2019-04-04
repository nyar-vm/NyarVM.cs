using System.Runtime.InteropServices;

namespace Nyar.Types.Targets;

/// <summary>
///     目标三元组，描述编译目标的架构、厂商、操作系统和 ABI
/// </summary>
public readonly struct CanonicalTarget : IEquatable<CanonicalTarget>
{
    #region 属性

    /// <summary>
    ///     目标架构
    /// </summary>
    public TargetArch arch { get; init; }

    /// <summary>
    ///     运行时提供者/目标平台厂商
    /// </summary>
    public TargetVendor vendor { get; init; }

    /// <summary>
    ///     操作系统
    /// </summary>
    public TargetSpecification specification { get; init; }

    /// <summary>
    ///     Application Binary Interface（可选）
    /// </summary>
    public TargetAbi? abi { get; init; }

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化目标三元组
    /// </summary>
    /// <param name="arch">目标架构。</param>
    /// <param name="vendor">运行时提供者/目标平台厂商。</param>
    /// <param name="specification">操作系统。</param>
    /// <param name="abi">Application Binary Interface（可选）。</param>
    public CanonicalTarget(TargetArch arch, TargetVendor vendor, TargetSpecification specification,
        TargetAbi? abi = null)
    {
        this.arch = arch;
        this.vendor = vendor;
        this.specification = specification;
        this.abi = abi;
    }

    #endregion

    #region 架构判断属性

    /// <summary>
    ///     判断目标架构是否为 64 位
    /// </summary>
    public bool is64_bit => arch switch
    {
        TargetArch.x86_64 => true,
        TargetArch.a_arch64 => true,
        TargetArch.risc_v64 => true,
        TargetArch.wasm64 => true,
        TargetArch.clr => true,
        TargetArch.jvm => true,
        TargetArch.nyar_vm => true,
        TargetArch.native => true,
        TargetArch.cuda => true,
        TargetArch.gcn => true,
        TargetArch.spirv => true,
        TargetArch.msl => true,
        _ => false
    };

    /// <summary>
    ///     获取目标架构的指针大小（字节）
    /// </summary>
    public int pointer_size => is64_bit ? 8 : 4;

    /// <summary>
    ///     判断目标架构是否为虚拟机架构
    /// </summary>
    public bool is_virtual_machine => arch is TargetArch.nyar_vm or TargetArch.clr or TargetArch.jvm
        or TargetArch.wasm32 or TargetArch.wasm64;

    /// <summary>
    ///     判断目标架构是否为 GPU 架构
    /// </summary>
    public bool is_gpu => arch is TargetArch.cuda or TargetArch.gcn or TargetArch.spirv or TargetArch.msl;

    #endregion

    #region 别名注册表

    /// <summary>
    ///     目标三元组别名注册表，键为别名，值为对应的三元组字符串
    /// </summary>
    private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nyar"] = "nyar-unknown-unknown-managed",
        ["gnosis"] = "gnosis-unknown-native",
        ["wasm"] = "wasm32-unknown-browser-wasm",
        ["wasi"] = "wasm32-unknown-wasi-wasip1",
        ["wasip1"] = "wasm32-unknown-wasi-wasip1",
        ["wasip2"] = "wasm32-unknown-wasi-wasip2",
        ["clr"] = "clr-microsoft-unknown-managed",
        ["jvm"] = "jvm-openjdk-unknown-managed",
        ["node"] = "wasm32-node-unknown-wasm",
        ["deno"] = "wasm32-deno-unknown-wasm",
        ["bun"] = "wasm32-bun-unknown-wasm"
    };

    #endregion

    #region 解析方法

    /// <summary>
    ///     将字符串解析为目标三元组，解析失败时抛出异常
    /// </summary>
    /// <param name="s">要解析的字符串。</param>
    /// <returns>解析得到的目标三元组。</returns>
    /// <exception cref="FormatException">字符串无法解析为有效的目标三元组</exception>
    public static CanonicalTarget parse(string s)
    {
        if (try_parse(s, out var result)) return result;

        throw new FormatException($"无法将 \"{s}\" 解析为有效的 {nameof(CanonicalTarget)}");
    }

    /// <summary>
    ///     尝试将字符串解析为目标三元组
    /// </summary>
    /// <param name="s">要解析的字符串。</param>
    /// <param name="result">解析成功时的目标三元组。</param>
    /// <returns>是否解析成功。</returns>
    public static bool try_parse(string s, out CanonicalTarget result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(s)) return false;

        if (_aliases.TryGetValue(s, out var aliasTarget)) return try_parse_triple(aliasTarget, out result);

        if (string.Equals(s, "native", StringComparison.OrdinalIgnoreCase))
        {
            result = get_native_triple();
            return true;
        }

        return try_parse_triple(s, out result);
    }

    /// <summary>
    ///     尝试将三元组字符串解析为目标三元组
    /// </summary>
    /// <param name="s">三元组字符串，格式为 arch-vendor-os[-abi]。</param>
    /// <param name="result">解析成功时的目标三元组。</param>
    /// <returns>是否解析成功。</returns>
    private static bool try_parse_triple(string s, out CanonicalTarget result)
    {
        result = default;

        var segments = s.Split('-');
        if (segments.Length is < 3 or > 4) return false;

        if (!TargetArchExtensions.try_parse(segments[0], out var arch)) return false;

        if (!TargetVendorExtensions.try_parse(segments[1], out var vendor)) return false;

        if (!TargetSpecificationExtensions.try_parse(segments[2], out var os)) return false;

        TargetAbi? abi = null;
        if (segments.Length == 4)
        {
            if (string.Equals(segments[3], "native", StringComparison.OrdinalIgnoreCase))
            {
                abi = null;
            }
            else if (string.Equals(segments[3], "managed", StringComparison.OrdinalIgnoreCase))
            {
                abi = arch switch
                {
                    TargetArch.nyar_vm => TargetAbi.managed,
                    TargetArch.clr => TargetAbi.clr,
                    TargetArch.jvm => TargetAbi.jvm,
                    _ => null
                };

                if (abi is null) return false;
            }
            else if (TargetAbiExtensions.try_parse(segments[3], out var parsedAbi))
            {
                abi = parsedAbi;
            }
            else
            {
                return false;
            }
        }

        result = new CanonicalTarget(arch, vendor, os, abi);
        return true;
    }

    /// <summary>
    ///     获取当前平台的原生目标三元组
    /// </summary>
    /// <returns>当前平台的原生目标三元组。</returns>
    private static CanonicalTarget get_native_triple()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new CanonicalTarget(TargetArch.x86_64, TargetVendor.pc, TargetSpecification.windows,
                TargetAbi.microsoft_x64);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new CanonicalTarget(TargetArch.a_arch64, TargetVendor.apple, TargetSpecification.mac_os,
                TargetAbi.system_v);

        return new CanonicalTarget(TargetArch.x86_64, TargetVendor.unknown, TargetSpecification.linux,
            TargetAbi.system_v);
    }

    #endregion

    #region 格式化方法

    /// <summary>
    ///     将目标三元组格式化为字符串，格式为 arch-vendor-os[-abi]
    /// </summary>
    /// <returns>格式化后的三元组字符串。</returns>
    public override string ToString()
    {
        var archStr = arch.to_triple_string();
        var vendorStr = vendor.to_triple_string();
        var osStr = specification.to_triple_string();

        if (arch is TargetArch.nyar_vm or TargetArch.clr or TargetArch.jvm)
        {
            var managedAbi = abi ?? get_default_abi(arch);
            var managedSuffix = managedAbi switch
            {
                TargetAbi.managed => "managed",
                TargetAbi.clr => "managed",
                TargetAbi.jvm => "managed",
                _ => managedAbi.to_triple_string()
            };
            return $"{archStr}-{vendorStr}-{osStr}-{managedSuffix}";
        }

        var abiSuffix = "";
        if (abi.HasValue) abiSuffix = $"-{abi.Value.to_triple_string()}";

        return $"{archStr}-{vendorStr}-{osStr}{abiSuffix}";
    }

    /// <summary>
    ///     将目标三元组转换为短别名，如果匹配别名注册表则返回别名，否则返回完整三元组字符串
    /// </summary>
    /// <returns>短别名或完整三元组字符串。</returns>
    public string to_short_alias()
    {
        var str = ToString();

        foreach (var (alias, target) in _aliases)
            if (string.Equals(target, str, StringComparison.OrdinalIgnoreCase))
                return alias;

        var nativeTriple = get_native_triple();

        if (string.Equals(nativeTriple.ToString(), str, StringComparison.OrdinalIgnoreCase)) return "native";

        return str;
    }

    #endregion

    #region 转换方法

    /// <summary>
    ///     将目标三元组转换为编译目标
    /// </summary>
    /// <returns>对应的编译目标。</returns>
    public CompilationTarget to_compilation_target()
    {
        var abi = this.abi ?? get_default_abi(arch);

        var target = new CompilationTarget
        {
            arch = arch,
            vendor = vendor,
            os = specification,
            abi = abi
        };
        return target;
    }

    /// <summary>
    ///     从编译目标创建目标三元组
    /// </summary>
    /// <param name="target">编译目标。</param>
    /// <returns>对应的目标三元组。</returns>
    public static CanonicalTarget from_compilation_target(CompilationTarget target)
    {
        return new CanonicalTarget(
            target.arch,
            target.vendor,
            target.os,
            target.abi
        );
    }

    /// <summary>
    ///     根据架构获取默认 ABI
    /// </summary>
    /// <param name="arch">目标架构。</param>
    /// <returns>默认的 ABI。</returns>
    private static TargetAbi get_default_abi(TargetArch arch)
    {
        return arch switch
        {
            TargetArch.nyar_vm => TargetAbi.managed,
            TargetArch.clr => TargetAbi.clr,
            TargetArch.jvm => TargetAbi.jvm,
            TargetArch.wasm32 => TargetAbi.web_assembly,
            TargetArch.wasm64 => TargetAbi.web_assembly,
            _ => default
        };
    }

    /// <summary>
    ///     根据架构和操作系统推断 API
    /// </summary>
    /// <param name="arch">目标架构。</param>
    /// <param name="os">操作系统。</param>
    /// <param name="abi">Application Binary Interface。</param>
    /// <returns>推断的 API。</returns>
    private static TargetApi infer_api(TargetArch arch, TargetSpecification os, TargetAbi abi)
    {
        if (arch == TargetArch.clr) return TargetApi.net;

        if (arch == TargetArch.jvm) return TargetApi.java;

        if (arch == TargetArch.wasm32 && os == TargetSpecification.web) return TargetApi.web;

        if (arch == TargetArch.wasm32 && os == TargetSpecification.linux) return TargetApi.posix;

        if (arch == TargetArch.native && os == TargetSpecification.windows) return TargetApi.windows;

        if (arch == TargetArch.native && os == TargetSpecification.linux) return TargetApi.posix;

        if (arch == TargetArch.x86_64 && os == TargetSpecification.windows) return TargetApi.windows;

        if (arch == TargetArch.x86_64 && os == TargetSpecification.linux) return TargetApi.posix;

        return TargetApi.none;
    }

    #endregion

    #region 目标配置派生

    /// <summary>
    ///     从当前三元组枚举派生目标配置
    /// </summary>
    /// <param name="targetMode">可选的目标模式覆盖，为 null 时使用默认 Prod</param>
    /// <returns>目标策略配置</returns>
    public TargetProfile to_profile(TargetMode? targetMode = null)
    {
        var resolvedAbi = abi ?? get_default_abi(arch);
        return new TargetProfile
        {
            canonical_triple = ToString(),
            target_mode = targetMode ?? TargetMode.prod,
            backend_family = arch.to_backend_family(),
            host_kind = derive_host_kind(),
            host_flavor = derive_host_flavor(),
            abi = resolvedAbi,
            capability_tags = derive_capability_tags(),
            entry_policy = derive_entry_policy(),
            artifact_policy = derive_artifact_policy()
        };
    }

    /// <summary>
    ///     从厂商和操作系统枚举派生宿主类型
    /// </summary>
    /// <returns>宿主类型标识符</returns>
    private TargetHostKind derive_host_kind()
    {
        if (arch == TargetArch.nyar_vm) return TargetHostKind.nyar_vm;

        if (arch == TargetArch.clr) return TargetHostKind.dotnet;

        if (arch == TargetArch.jvm) return TargetHostKind.jvm;

        if (arch is TargetArch.cuda or TargetArch.gcn or TargetArch.spirv or TargetArch.msl) return TargetHostKind.gpu;

        switch (vendor)
        {
            case TargetVendor.node:
            case TargetVendor.deno:
            case TargetVendor.bun:
                return TargetHostKind.java_script;
        }

        switch (specification)
        {
            case TargetSpecification.windows:
            case TargetSpecification.linux:
            case TargetSpecification.mac_os:
            case TargetSpecification.android:
            case TargetSpecification.ios:
                return TargetHostKind.native;
            case TargetSpecification.web:
                return TargetHostKind.browser;
        }

        return abi switch
        {
            TargetAbi.wasi_p1 => TargetHostKind.wasi,
            TargetAbi.wasi_p2 => TargetHostKind.wasi,
            _ => TargetHostKind.unknown
        };
    }

    /// <summary>
    ///     派生宿主风味。
    ///     `host_kind` 只表达大类，具体生态差异下沉到开放字符串。
    /// </summary>
    private string derive_host_flavor()
    {
        if (arch == TargetArch.nyar_vm) return "nyarvm";
        if (arch == TargetArch.clr) return "dotnet";
        if (arch == TargetArch.jvm) return specification == TargetSpecification.android ? "android-art" : "openjdk";
        if (arch is TargetArch.cuda or TargetArch.gcn or TargetArch.spirv or TargetArch.msl) return "shader";

        return vendor switch
        {
            TargetVendor.node => "node",
            TargetVendor.deno => "deno",
            TargetVendor.bun => "bun",
            _ => specification switch
            {
                TargetSpecification.web => "web-standard",
                TargetSpecification.wasi => abi == TargetAbi.wasi_p2 ? "wasi-preview2" : "wasi-preview1",
                TargetSpecification.windows => "win32",
                TargetSpecification.linux => "linux-gnu",
                TargetSpecification.mac_os => "apple-darwin",
                TargetSpecification.android => "android-native",
                TargetSpecification.ios => "ios-native",
                _ => "default"
            }
        };
    }

    /// <summary>
    ///     派生默认能力标签。
    ///     非标准宿主可在发行版层追加能力，而不需要再拆新的目标枚举。
    /// </summary>
    private string[] derive_capability_tags()
    {
        if (arch == TargetArch.nyar_vm) return ["vm", "module-loader"];
        if (arch == TargetArch.clr) return ["managed", "reflection", "filesystem"];
        if (arch == TargetArch.jvm)
            return specification == TargetSpecification.android
                ? ["managed", "android-lifecycle", "asset-loader"]
                : ["managed", "reflection", "filesystem"];
        if (arch is TargetArch.cuda or TargetArch.gcn or TargetArch.spirv or TargetArch.msl)
            return ["shader", "gpu"];

        if (vendor is TargetVendor.node or TargetVendor.deno or TargetVendor.bun)
            return ["javascript", "esmodule", "filesystem", "timers"];

        return specification switch
        {
            TargetSpecification.web => ["javascript", "dom", "canvas", "fetch", "esmodule"],
            TargetSpecification.wasi => ["wasi", "filesystem", "cli"],
            TargetSpecification.windows => ["native", "filesystem", "process"],
            TargetSpecification.linux => ["native", "filesystem", "process"],
            TargetSpecification.mac_os => ["native", "filesystem", "process"],
            TargetSpecification.android => ["native", "mobile-lifecycle", "asset-loader"],
            TargetSpecification.ios => ["native", "mobile-lifecycle", "bundle-resource"],
            _ => []
        };
    }

    /// <summary>
    ///     从架构、厂商和操作系统枚举派生入口策略
    /// </summary>
    /// <returns>入口策略配置</returns>
    private EntryPolicy derive_entry_policy()
    {
        if (abi is TargetAbi.wasi_p1 or TargetAbi.wasi_p2)
            return new EntryPolicy
            {
                default_entry = "_start",
                wrap_strategy = WrapStrategy.direct,
                generate_wrapper = false
            };

        if (arch == TargetArch.clr)
            return new EntryPolicy
            {
                default_entry = "Main",
                wrap_strategy = WrapStrategy.wrapper,
                generate_wrapper = true
            };

        if (arch == TargetArch.jvm)
            return new EntryPolicy
            {
                default_entry = "main",
                wrap_strategy = WrapStrategy.wrapper,
                generate_wrapper = true
            };

        if (specification == TargetSpecification.web)
            return new EntryPolicy
            {
                default_entry = "main",
                wrap_strategy = WrapStrategy.hosted,
                generate_wrapper = false
            };

        if (vendor is TargetVendor.node or TargetVendor.deno or TargetVendor.bun)
            return new EntryPolicy
            {
                default_entry = "main",
                wrap_strategy = WrapStrategy.hosted,
                generate_wrapper = false
            };

        return new EntryPolicy
        {
            default_entry = "main",
            wrap_strategy = WrapStrategy.direct,
            generate_wrapper = false
        };
    }

    /// <summary>
    ///     从架构和操作系统枚举派生产物策略
    /// </summary>
    /// <returns>产物策略配置</returns>
    private ArtifactPolicy derive_artifact_policy()
    {
        if (arch == TargetArch.nyar_vm)
            return new ArtifactPolicy
            {
                primary_extension = ".nyar",
                default_publish_format = "bundle",
                supported_publish_formats = ["bundle"],
                required_adaptors = ["std:nyarvm"],
                generate_launch_scripts = false
            };

        if (arch is TargetArch.wasm32 or TargetArch.wasm64)
            return new ArtifactPolicy
            {
                primary_extension = ".wasm",
                default_publish_format = abi == TargetAbi.wasi_p2 ? "wasm-component" : "wasm-module",
                supported_publish_formats = abi == TargetAbi.wasi_p2
                    ? ["wasm-component", "oci"]
                    : ["wasm-module", "web-app", "extension", "mini-game"],
                required_adaptors = abi == TargetAbi.wasi_p2
                    ? ["std:wasi"]
                    : vendor switch
                    {
                        TargetVendor.node => ["std:node"],
                        TargetVendor.deno => ["std:deno"],
                        TargetVendor.bun => ["std:bun"],
                        _ => ["std:web"]
                    },
                generate_launch_scripts = false
            };

        if (arch == TargetArch.jvm)
            return new ArtifactPolicy
            {
                primary_extension = ".jar",
                default_publish_format = "jar",
                supported_publish_formats = specification == TargetSpecification.android
                    ? ["apk", "aab", "jar"]
                    : ["jar", "jlink-image"],
                requires_code_signing = specification == TargetSpecification.android,
                supports_store_distribution = specification == TargetSpecification.android,
                generate_launch_scripts = true
            };

        if (arch == TargetArch.clr)
            return new ArtifactPolicy
            {
                primary_extension = ".exe",
                default_publish_format = specification switch
                {
                    TargetSpecification.windows => "msix",
                    TargetSpecification.android => "apk",
                    TargetSpecification.ios => "ipa",
                    _ => "directory"
                },
                supported_publish_formats = specification switch
                {
                    TargetSpecification.windows => ["directory", "zip", "msix", "single-file"],
                    TargetSpecification.android => ["apk", "aab"],
                    TargetSpecification.ios => ["ipa", "app-bundle"],
                    TargetSpecification.mac_os => ["app-bundle", "pkg", "directory"],
                    _ => ["directory", "tar", "single-file"]
                },
                required_adaptors = specification switch
                {
                    TargetSpecification.android => ["std:android"],
                    TargetSpecification.ios => ["std:ios"],
                    _ => ["std:dotnet"]
                },
                generate_runtime_config = true,
                generate_debug_symbols = true,
                generate_launch_scripts = true,
                generate_xml_doc = true,
                requires_code_signing = specification is TargetSpecification.windows
                    or TargetSpecification.android
                    or TargetSpecification.ios
                    or TargetSpecification.mac_os,
                supports_store_distribution = specification is TargetSpecification.windows
                    or TargetSpecification.android
                    or TargetSpecification.ios
            };

        if (arch == TargetArch.spirv)
            return new ArtifactPolicy
            {
                primary_extension = ".spv",
                default_publish_format = "shader-module",
                supported_publish_formats = ["shader-module", "asset-pack"],
                required_adaptors = ["std:shader"],
                generate_launch_scripts = false
            };

        if (arch is TargetArch.x86_64 or TargetArch.x86 or TargetArch.a_arch64 or TargetArch.arm
            or TargetArch.risc_v32 or TargetArch.risc_v64 or TargetArch.native)
            return new ArtifactPolicy
            {
                primary_extension = specification == TargetSpecification.windows ? ".exe" : string.Empty,
                default_publish_format = specification switch
                {
                    TargetSpecification.windows => "msix",
                    TargetSpecification.android => "apk",
                    TargetSpecification.ios => "ipa",
                    TargetSpecification.mac_os => "app-bundle",
                    _ => "directory"
                },
                supported_publish_formats = specification switch
                {
                    TargetSpecification.windows => ["directory", "zip", "msix", "single-file"],
                    TargetSpecification.android => ["apk", "aab", "aab-split"],
                    TargetSpecification.ios => ["ipa", "app-bundle"],
                    TargetSpecification.mac_os => ["app-bundle", "pkg", "directory"],
                    _ => ["directory", "tar", "deb", "rpm", "appimage"]
                },
                required_adaptors = specification switch
                {
                    TargetSpecification.android => ["std:android"],
                    TargetSpecification.ios => ["std:ios"],
                    _ => ["std:native"]
                },
                generate_debug_symbols = true,
                generate_launch_scripts = false,
                requires_code_signing = specification is TargetSpecification.windows
                    or TargetSpecification.android
                    or TargetSpecification.ios
                    or TargetSpecification.mac_os,
                supports_store_distribution = specification is TargetSpecification.windows
                    or TargetSpecification.android
                    or TargetSpecification.ios
            };

        return new ArtifactPolicy
        {
            primary_extension = string.Empty,
            supported_publish_formats = ["directory"],
            required_adaptors = [],
            generate_launch_scripts = false
        };
    }

    #endregion

    #region 相等性

    /// <summary>
    ///     判断是否与另一个目标三元组相等
    /// </summary>
    /// <param name="other">另一个目标三元组。</param>
    /// <returns>是否相等。</returns>
    public bool Equals(CanonicalTarget other)
    {
        return arch == other.arch
               && vendor == other.vendor
               && specification == other.specification
               && abi == other.abi;
    }

    /// <summary>
    ///     判断是否与另一个对象相等
    /// </summary>
    /// <param name="obj">另一个对象。</param>
    /// <returns>是否相等。</returns>
    public override bool Equals(object? obj)
    {
        return obj is CanonicalTarget other && Equals(other);
    }

    /// <summary>
    ///     获取哈希码
    /// </summary>
    /// <returns>哈希码。</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(arch, vendor, specification, abi);
    }

    /// <summary>
    ///     判断两个目标三元组是否相等
    /// </summary>
    /// <param name="left">左侧目标三元组。</param>
    /// <param name="right">右侧目标三元组。</param>
    /// <returns>是否相等。</returns>
    public static bool operator ==(CanonicalTarget left, CanonicalTarget right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     判断两个目标三元组是否不相等
    /// </summary>
    /// <param name="left">左侧目标三元组。</param>
    /// <param name="right">右侧目标三元组。</param>
    /// <returns>是否不相等。</returns>
    public static bool operator !=(CanonicalTarget left, CanonicalTarget right)
    {
        return !left.Equals(right);
    }

    #endregion
}
