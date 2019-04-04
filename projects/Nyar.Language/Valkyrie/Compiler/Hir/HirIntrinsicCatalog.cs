using System.Collections.Frozen;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     intrinsic 目录。
///     当前仅收录 `JVM/WASM/CLR` 共享的最小基础能力。
/// </summary>
public static class HirIntrinsicCatalog
{
    private static readonly FrozenDictionary<string, HirIntrinsicSymbol> _known =
        new[]
            {
                "i32.add", "i32.sub", "i32.mul", "i32.div", "i32.rem", "i32.eq", "i32.lt",
                "i32.and", "i32.or", "i32.xor", "i32.shl", "i32.shr",
                "i64.add", "i64.sub", "i64.mul", "i64.div", "i64.rem", "i64.eq", "i64.lt",
                "i64.and", "i64.or", "i64.xor", "i64.shl", "i64.shr",
                "f64.add", "f64.sub", "f64.mul", "f64.div", "f64.eq", "f64.lt"
            }
            .Select(name => new HirIntrinsicSymbol(name))
            .ToFrozenDictionary(symbol => symbol.name, StringComparer.Ordinal);

    public static bool try_resolve(string name, out HirIntrinsicSymbol symbol)
    {
        return _known.TryGetValue(name, out symbol!);
    }
}