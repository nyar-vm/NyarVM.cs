using Nyar.Analyzer.Semantic;

namespace Std.Data.Text.Awsl.Semantic;

/// <summary>
///     AWSL 类型检查器实现
/// </summary>
public sealed class AwslTypeCheckerImpl : TypeChecker
{
    public AwslTypeCheckerImpl()
    {
        bridge = new AwslSemanticBridge();
    }

    public AwslSemanticBridge bridge { get; }

    public SemanticModel check_widget(AwslParseResult parseResult, string? filePath = null)
    {
        return bridge.build_semantic_model(parseResult, filePath ?? "");
    }

    public IType infer_property_type(AwslProperty prop)
    {
        var declaredType = bridge.convert_widget_property_type(prop.type_name);

        if (declaredType is AutoType or UnknownType &&
            prop.default_value_kind != AwslValueKind.none)
            return bridge.infer_value_kind_type(prop.default_value_kind);

        return declaredType;
    }

    public override IType infer_type(ISymbol symbol)
    {
        throw new NotImplementedException();
    }

    public override IType infer_type_of_expression(object node)
    {
        throw new NotImplementedException();
    }

    public override bool check_type(IType expected, IType actual, out SemanticDiagnostic? diagnostic)
    {
        throw new NotImplementedException();
    }

    public override IReadOnlyList<SemanticDiagnostic> check_all_types(SemanticModel model)
    {
        throw new NotImplementedException();
    }
}