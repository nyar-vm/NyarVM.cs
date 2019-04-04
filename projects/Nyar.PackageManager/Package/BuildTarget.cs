using Nyar.Types.Targets;

namespace Nyar.PackageManager.Package;

/// <summary>
///     构建目标定义，支持短别名和完整目标三元组
/// </summary>
[Obsolete("已过期：请使用 Nyar.Language.Valkyrie.Compiler.Targets.CanonicalTripleRegistry 替代。" +
          "旧的 TargetTriple.try_parse 体系已统一到新的 4 段式 arch-impl-spec-abi 标准。")]
public class BuildTarget
{
    /// <summary>
    ///     鐩爣鏍囪瘑锛屾敮鎸佺煭鍒悕锛堝 "nyar"锛夋垨瀹屾暣涓夊厓缁勶紙濡?"clr-unity-windows-il2cpp"锛?    ///
    /// </summary>
    public string target { get; set; } = string.Empty;

    /// <summary>
    ///     鏄惁鐢熸垚 Source Map锛?wasm.map锛?    ///
    /// </summary>
    public bool source_map { get; set; }

    /// <summary>
    ///     鏄惁鐢熸垚 TypeScript 澹版槑鏂囦欢锛?d.ts锛?    ///
    /// </summary>
    public bool type_script { get; set; }

    /// <summary>
    ///     鏄惁鐢熸垚 WAT锛圵ebAssembly Text Format锛夋枃鏈緭鍑?    ///
    /// </summary>
    public bool wat { get; set; }

    /// <summary>
    ///     鏄惁鐢熸垚 MSIL 鏂囨湰杈撳嚭
    /// </summary>
    public bool msil { get; set; }

    /// <summary>
    ///     瑙ｆ瀽鐩爣涓?CompilationTarget
    /// </summary>
    /// <param name="target">
    ///     鐩爣瀛楃涓?/param>
    ///     <returns>瑙ｆ瀽鍚庣殑缂栬瘧鐩爣锛岃В鏋愬け璐ヨ繑鍥?null</returns>
    public static CompilationTarget? resolve(string target)
    {
        if (CanonicalTarget.try_parse(target, out var triple)) return triple.to_compilation_target();

        return null;
    }
}