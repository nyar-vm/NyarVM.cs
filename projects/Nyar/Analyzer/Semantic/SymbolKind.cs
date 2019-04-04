namespace Nyar.Analyzer.Semantic;

public enum SymbolKind
{
    @namespace,
    module,
    @class,
    @interface,
    @enum,
    function,
    method,
    property,
    field,
    variable,
    parameter,
    type_parameter,
    type_alias,
    import,
    export,
    constant,
    constructor,
    destructor,
    @operator,
    @event,
    @delegate
}