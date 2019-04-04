namespace Nyar.Analyzer.Highlight;

public enum HighlightKind
{
    none,
    keyword,
    control_keyword,
    @string,
    number,
    comment,
    @operator,
    punctuation,
    identifier,
    type_identifier,
    function_identifier,
    parameter,
    property,
    field,
    variable,
    constant,
    @namespace,
    module,
    decorator,
    regex,
    escape,
    delimiter,
    interpolation
}