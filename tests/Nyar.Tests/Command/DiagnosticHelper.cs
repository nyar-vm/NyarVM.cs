using Std.Data.Text.Diagnostics;

namespace Nyar.Tests.Command;

public static class DiagnosticHelper
{
    public static string FormatDiagnostics(DiagnosticSink diagnostics)
    {
        var diags = diagnostics.messages;
        if (diags.Count == 0) return "无诊断信息";

        var lines = new List<string>();
        foreach (var d in diags) lines.Add($"  [{d.severity}] {d.message}");

        return string.Join("\n", lines);
    }
}
