namespace Std.Data.Text.GraphQL;

public enum GqlDirectiveLocation
{
    query,
    mutation,
    subscription,
    field,
    fragment_definition,
    fragment_spread,
    inline_fragment,
    variable_definition,
    schema,
    scalar,
    @object,
    field_definition,
    argument_definition,
    @interface,
    union,
    @enum,
    enum_value,
    input_object,
    input_field_definition
}