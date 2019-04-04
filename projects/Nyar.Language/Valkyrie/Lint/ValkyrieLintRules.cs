using Nyar.Lint;

namespace Nyar.Language.Valkyrie.Lint;

public static class ValkyrieLintRules
{
    public static LintEngine CreateDefault()
    {
        var engine = new LintEngine();
        engine.Register(new UnusedVariableRule());
        engine.Register(new LargeFunctionBodyRule());
        engine.Register(new EmptyControlBodyRule());
        return engine;
    }
}