namespace Hermes.YYDB.Query;

public abstract class QueryExpression
{
    protected QueryExpression(string targetTypeName)
    {
        TargetTypeName = targetTypeName;
    }

    public abstract string QueryKind { get; }
    public string TargetTypeName { get; }
}