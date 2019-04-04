using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

public static class DeclarationNodeExtensions
{
    public static TermParameterItem create_term_parameter(
        IdentifierNode name,
        TypeNode? boundType = null,
        TermNode? defaultTerm = null)
    {
        return new TermParameterItem(name, boundType, defaultTerm);
    }

    public static TermParameterList create_term_parameter_list(IReadOnlyList<TermParameterItem> items)
    {
        return new TermParameterList
        {
            items = items
        };
    }

    public static TermParameterList create_single_term_parameter_list(
        IdentifierNode name,
        TypeNode? boundType = null,
        TermNode? defaultTerm = null)
    {
        return create_term_parameter_list(
        [
            create_term_parameter(name, boundType, defaultTerm)
        ]);
    }

    public static TypeParameterItem create_type_parameter(
        IdentifierNode name,
        TypeNode? boundType = null,
        TypeNode? defaultType = null)
    {
        return new TypeParameterItem(name, boundType, defaultType);
    }

    public static TypeParameterList create_type_parameter_list(IReadOnlyList<TypeParameterItem> items)
    {
        return new TypeParameterList
        {
            items = items
        };
    }

    public static TypeParameterList create_single_type_parameter_list(
        IdentifierNode name,
        TypeNode? boundType = null,
        TypeNode? defaultType = null)
    {
        return create_type_parameter_list(
        [
            create_type_parameter(name, boundType, defaultType)
        ]);
    }
}