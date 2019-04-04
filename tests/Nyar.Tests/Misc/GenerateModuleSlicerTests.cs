namespace Nyar.Tests.Misc;

public sealed class GenerateModuleSlicerTests
{
    [Fact]
    public void SliceByEntry_ShouldKeepQualifiedHelper_WhenEntryCallsShortName()
    {
        var module = new GenerateModule("LegionSlice");

        var helper = new GenerateFunction("legion.collect_tail_args", "object");
        helper.add_instruction(new GenerateInstruction(
            NyarHeadCode.@const,
            new GenerateOperand.Null(GenerateValueType.@object)));
        helper.add_instruction(new GenerateInstruction(NyarHeadCode.@return));
        module.add_function(helper);

        var unrelated = new GenerateFunction("legion.unrelated_helper", "void");
        unrelated.add_instruction(new GenerateInstruction(NyarHeadCode.@return));
        module.add_function(unrelated);

        var entry = new GenerateFunction("legion.legion", "void");
        entry.add_instruction(new GenerateInstruction(
            NyarHeadCode.call_static,
            new GenerateOperand.FuncRef(
                "collect_tail_args",
                new GenerateFunctionType
                {
                    parameters = [GenerateValueType.@object, GenerateValueType.i32],
                    results = [GenerateValueType.@object]
                })));
        entry.add_instruction(new GenerateInstruction(NyarHeadCode.pop));
        entry.add_instruction(new GenerateInstruction(NyarHeadCode.@return));
        module.add_function(entry);
        module.add_export(new GenerateModuleExport("main", GenerateExportKind.function, 2));

        var sliced = GenerateModuleSlicer.SliceByEntry(module, "legion.legion");
        var slicedNames = sliced.functions.Select(function => function.name).ToArray();

        Assert.Contains("legion.legion", slicedNames);
        Assert.Contains("legion.collect_tail_args", slicedNames);
        Assert.DoesNotContain("legion.unrelated_helper", slicedNames);
    }

    [Fact]
    public void SliceByEntry_ShouldResolveUniqueShortEntryName()
    {
        var module = new GenerateModule("LegionSlice");

        var helper = new GenerateFunction("legion.collect_tail_args", "object");
        helper.add_instruction(new GenerateInstruction(
            NyarHeadCode.@const,
            new GenerateOperand.Null(GenerateValueType.@object)));
        helper.add_instruction(new GenerateInstruction(NyarHeadCode.@return));
        module.add_function(helper);

        var entry = new GenerateFunction("legion.legion", "void");
        entry.add_instruction(new GenerateInstruction(
            NyarHeadCode.call_static,
            new GenerateOperand.FuncRef(
                "collect_tail_args",
                new GenerateFunctionType
                {
                    parameters = [GenerateValueType.@object, GenerateValueType.i32],
                    results = [GenerateValueType.@object]
                })));
        entry.add_instruction(new GenerateInstruction(NyarHeadCode.pop));
        entry.add_instruction(new GenerateInstruction(NyarHeadCode.@return));
        module.add_function(entry);
        module.add_export(new GenerateModuleExport("main", GenerateExportKind.function, 1));

        var sliced = GenerateModuleSlicer.SliceByEntry(module, "legion");
        var slicedNames = sliced.functions.Select(function => function.name).ToArray();

        Assert.Contains("legion.legion", slicedNames);
        Assert.Contains("legion.collect_tail_args", slicedNames);
    }
}
