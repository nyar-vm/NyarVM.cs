namespace Std.Data.Text.Syntax;

public delegate void RefParseAction<TLanguage, TContext>(ref ParseContext<TLanguage, TContext> ctx)
    where TLanguage : Language
    where TContext : ISyntaxContext;