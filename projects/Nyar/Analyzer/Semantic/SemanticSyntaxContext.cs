using Std.Data.Text.Syntax;

namespace Nyar.Analyzer.Semantic;

public class SemanticSyntaxContext : ISyntaxContext
{
    private readonly Dictionary<string, int> _operator_precedence;
    private readonly HashSet<string> _type_names;

    public SemanticSyntaxContext()
    {
        _type_names = [];
        _operator_precedence = new Dictionary<string, int>();
    }

    public IReadOnlySet<string> type_names => _type_names;
    public IReadOnlyDictionary<string, int> operator_precedence => _operator_precedence;

    public bool is_type_name(string name)
    {
        return _type_names.Contains(name);
    }

    public void register_type_name(string name)
    {
        _type_names.Add(name);
    }

    public void unregister_type_name(string name)
    {
        _type_names.Remove(name);
    }

    public int? get_operator_precedence(string op)
    {
        return _operator_precedence.TryGetValue(op, out var prec) ? prec : null;
    }

    public void set_operator_precedence(string op, int precedence)
    {
        _operator_precedence[op] = precedence;
    }
}