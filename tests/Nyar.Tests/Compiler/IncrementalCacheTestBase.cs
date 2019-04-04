using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Nyar.Tests.Compiler;

internal abstract class IncrementalCacheTestBase : IDisposable
{
    protected const string DefaultTriple = "nyar-unknown-windows";
    protected const string WasmTriple = "wasm32-unknown-browser-wasm";

    private readonly IncrementalCacheTestContext _context;

    protected IncrementalCacheTestBase()
    {
        _context = new IncrementalCacheTestContext();
    }

    protected IncrementalCacheTestContext context => _context;

    public void Dispose()
    {
        _context.Dispose();
    }

    protected NyarDatabaseCompilationCache create_cache()
    {
        return _context.create_cache();
    }
}
