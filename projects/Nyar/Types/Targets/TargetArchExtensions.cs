namespace Nyar.Types.Targets;

/// <summary>
///     TargetArch 扩展方法
/// </summary>
public static class TargetArchExtensions
{
    /// <param name="arch">目标架构。</param>
    extension(TargetArch arch)
    {
        /// <summary>
        ///     将架构枚举转换为目标三元组中的小写字符串
        /// </summary>
        /// <returns>目标三元组中的小写字符串。</returns>
        public string to_triple_string()
        {
            return arch switch
            {
                TargetArch.nyar_vm => "nyar",
                TargetArch.x86 => "x86",
                TargetArch.x86_64 => "x86_64",
                TargetArch.arm => "arm",
                TargetArch.a_arch64 => "aarch64",
                TargetArch.risc_v32 => "riscv32",
                TargetArch.risc_v64 => "riscv64",
                TargetArch.wasm32 => "wasm32",
                TargetArch.wasm64 => "wasm64",
                TargetArch.clr => "clr",
                TargetArch.jvm => "jvm",
                TargetArch.native => "native",
                TargetArch.cuda => "cuda",
                TargetArch.gcn => "gcn",
                TargetArch.spirv => "spirv",
                TargetArch.msl => "msl",
                _ => "unknown"
            };
        }

        /// <summary>
        ///     将架构枚举转换为后端家族
        /// </summary>
        /// <returns>后端家族标识符。</returns>
        public TargetBackendFamily to_backend_family()
        {
            return arch switch
            {
                TargetArch.nyar_vm => TargetBackendFamily.nyar_vm,
                TargetArch.clr => TargetBackendFamily.clr,
                TargetArch.jvm => TargetBackendFamily.jvm,
                TargetArch.wasm32 or TargetArch.wasm64 => TargetBackendFamily.wasm,
                TargetArch.spirv => TargetBackendFamily.spirv,
                TargetArch.x86_64 or TargetArch.x86 or TargetArch.a_arch64 or TargetArch.arm
                    or TargetArch.risc_v32 or TargetArch.risc_v64 or TargetArch.native => TargetBackendFamily.native,
                TargetArch.cuda or TargetArch.gcn or TargetArch.msl => TargetBackendFamily.gpu,
                _ => TargetBackendFamily.unknown
            };
        }
    }

    /// <summary>
    ///     尝试将字符串解析为架构
    /// </summary>
    /// <param name="s">要解析的字符串。</param>
    /// <param name="arch">解析成功时的架构。</param>
    /// <returns>是否解析成功。</returns>
    public static bool try_parse(string s, out TargetArch arch)
    {
        arch = default;
        var lower = s.ToLowerInvariant();

        switch (lower)
        {
            case "nyar":
            case "gnosis":
                arch = TargetArch.nyar_vm;
                return true;
            case "x86":
            case "i386":
            case "i686":
                arch = TargetArch.x86;
                return true;
            case "x86_64":
            case "amd64":
                arch = TargetArch.x86_64;
                return true;
            case "arm":
            case "armv7":
                arch = TargetArch.arm;
                return true;
            case "aarch64":
            case "arm64":
                arch = TargetArch.a_arch64;
                return true;
            case "riscv32":
                arch = TargetArch.risc_v32;
                return true;
            case "riscv64":
                arch = TargetArch.risc_v64;
                return true;
            case "wasm32":
                arch = TargetArch.wasm32;
                return true;
            case "wasm64":
                arch = TargetArch.wasm64;
                return true;
            case "clr":
                arch = TargetArch.clr;
                return true;
            case "jvm":
                arch = TargetArch.jvm;
                return true;
            case "native":
                arch = TargetArch.native;
                return true;
            case "cuda":
                arch = TargetArch.cuda;
                return true;
            case "gcn":
            case "amdgcn":
                arch = TargetArch.gcn;
                return true;
            case "spirv":
                arch = TargetArch.spirv;
                return true;
            case "msl":
                arch = TargetArch.msl;
                return true;
            default:
                return false;
        }
    }
}