namespace Std.Data.Text.Diagnostics;

/// <summary>
///     负责将诊断视图模型转换为 pretty 输出所需的布局结构。
/// </summary>
internal static class DiagnosticLayoutEngine
{
    /// <summary>
    ///     创建 pretty 模式的最小布局。
    /// </summary>
    public static DiagnosticLayout create_pretty_layout(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        var blocks = new List<DiagnosticLayoutBlock>
        {
            create_heading_block(viewModel, options)
        };

        if (viewModel.location.has_precise_span)
        {
            blocks.Add(create_location_block(viewModel, options));

            var snippetBlock = try_create_snippet_block(viewModel, options);
            if (snippetBlock is not null)
            {
                blocks.Add(snippetBlock.Value);
            }
        }

        var metadataBlock = create_metadata_block(viewModel, options);
        if (metadataBlock is not null)
        {
            blocks.Add(metadataBlock.Value);
        }

        blocks.Add(new DiagnosticLayoutBlock(
            DiagnosticLayoutBlockKind.Spacer,
            [new DiagnosticLayoutLine(string.Empty)]));

        return new DiagnosticLayout(blocks);
    }

    private static DiagnosticLayoutBlock create_heading_block(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        return new DiagnosticLayoutBlock(
            DiagnosticLayoutBlockKind.Heading,
            [
                new DiagnosticLayoutLine(
                    DiagnosticRenderSupport.format_pretty_heading(viewModel, options),
                    DiagnosticRenderSupport.resolve_severity_color(viewModel.severity))
            ]);
    }

    private static DiagnosticLayoutBlock create_location_block(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        var symbols = DiagnosticRenderSupport.get_symbols(options);
        return new DiagnosticLayoutBlock(
            DiagnosticLayoutBlockKind.Location,
            [
                new DiagnosticLayoutLine(
                    $"  {symbols.location_prefix} {DiagnosticRenderSupport.format_location(viewModel.location)}",
                    ConsoleColor.DarkGray)
            ]);
    }

    private static DiagnosticLayoutBlock? try_create_snippet_block(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        if (viewModel.source_lines.Count == 0)
        {
            return null;
        }

        var snippetLines = order_source_lines(viewModel.source_lines);
        if (snippetLines.Count == 0)
        {
            return null;
        }

        var symbols = DiagnosticRenderSupport.get_symbols(options);
        var maxLineNumberWidth = snippetLines.Max(line => line.line_number.ToString().Length);
        var lines = new List<DiagnosticLayoutLine>
        {
            new($"   {new string(' ', maxLineNumberWidth)} {symbols.gutter}", ConsoleColor.DarkGray)
        };

        DiagnosticSourceLineViewModel? previousLine = null;
        IReadOnlyList<IReadOnlyList<DiagnosticAnnotationViewModel>> previousAnnotationLayers = [];
        foreach (var sourceLine in snippetLines)
        {
            if (previousLine is not null && sourceLine.line_number > previousLine.Value.line_number + 1)
            {
                var omittedLineCount = sourceLine.line_number - previousLine.Value.line_number - 1;
                lines.Add(new DiagnosticLayoutLine(
                    $"   {new string(' ', maxLineNumberWidth)} {symbols.gutter} ... {omittedLineCount} lines omitted",
                    ConsoleColor.DarkGray));
            }

            var lineNumber = sourceLine.line_number.ToString().PadLeft(maxLineNumberWidth);
            var gutter = new string(' ', maxLineNumberWidth);
            lines.Add(new DiagnosticLayoutLine($"   {lineNumber} {symbols.gutter} {sourceLine.text}"));

            var annotations = select_visible_annotations(viewModel.annotations, sourceLine.line_number);
            var annotationLayers = build_annotation_layers(annotations, sourceLine, previousAnnotationLayers);
            foreach (var annotationLayer in annotationLayers)
            {
                lines.Add(new DiagnosticLayoutLine(
                    $"   {gutter} {symbols.gutter} {build_marker_text(annotationLayer, sourceLine)}",
                    resolve_annotation_color(viewModel, annotationLayer)));
            }

            previousAnnotationLayers =
            [
                .. annotationLayers
                    .Select(annotationLayer => (IReadOnlyList<DiagnosticAnnotationViewModel>)[.. annotationLayer])
            ];
            previousLine = sourceLine;
        }

        return new DiagnosticLayoutBlock(
            DiagnosticLayoutBlockKind.Snippet,
            lines);
    }

    private static DiagnosticLayoutBlock? create_metadata_block(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        var lines = DiagnosticRenderSupport.enumerate_hints(viewModel, options)
            .Select(hint => new DiagnosticLayoutLine(
                DiagnosticRenderSupport.format_pretty_metadata(options, "hint", hint),
                ConsoleColor.DarkGray))
            .ToList();

        var relatedInformationItems = DiagnosticRenderSupport.enumerate_related_information(viewModel);
        if (relatedInformationItems.Length > 0)
        {
            lines.Add(new DiagnosticLayoutLine(
                DiagnosticRenderSupport.format_pretty_related_heading(options),
                ConsoleColor.DarkGray));
            lines.AddRange(relatedInformationItems.Select(relatedInformation => new DiagnosticLayoutLine(
                DiagnosticRenderSupport.format_pretty_related_item(relatedInformation),
                ConsoleColor.DarkGray)));
        }

        if (lines.Count == 0)
        {
            return null;
        }

        return new DiagnosticLayoutBlock(DiagnosticLayoutBlockKind.Metadata, lines);
    }

    private static List<DiagnosticAnnotationViewModel> select_visible_annotations(
        IReadOnlyList<DiagnosticAnnotationViewModel> annotations,
        int lineNumber)
    {
        return
        [
            .. annotations
                .Where(annotation => annotation.start_line <= lineNumber && annotation.end_line >= lineNumber)
                .OrderBy(annotation => annotation.kind == DiagnosticAnnotationKind.Primary ? 0 : 1)
                .ThenBy(annotation => annotation.start_line)
                .ThenBy(annotation => annotation.start_column)
                .ThenBy(annotation => annotation.end_line)
                .ThenBy(annotation => annotation.end_column)
                .ThenBy(annotation => annotation.label, StringComparer.Ordinal)
        ];
    }

    private static List<DiagnosticSourceLineViewModel> order_source_lines(
        IReadOnlyList<DiagnosticSourceLineViewModel> sourceLines)
    {
        return
        [
            .. sourceLines
                .OrderBy(sourceLine => sourceLine.line_number)
        ];
    }

    private static string build_marker_text(
        IReadOnlyList<DiagnosticAnnotationViewModel> annotations,
        DiagnosticSourceLineViewModel sourceLine)
    {
        if (annotations.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        var cursor = 0;
        foreach (var annotation in annotations)
        {
            var indent = get_caret_indent(annotation, sourceLine);
            var width = get_caret_width(annotation, sourceLine, indent);
            var marker = resolve_marker(annotation);
            if (indent > cursor)
            {
                builder.Append(' ', indent - cursor);
                cursor = indent;
            }

            builder.Append(marker, width);
            cursor += width;

            if (should_render_annotation_label(annotation, sourceLine.line_number))
            {
                builder.Append(' ');
                builder.Append(annotation.label);
                cursor += annotation.label.Length + 1;
            }
        }

        return builder.ToString();
    }

    private static int get_caret_indent(
        DiagnosticAnnotationViewModel annotation,
        DiagnosticSourceLineViewModel sourceLine)
    {
        return annotation.start_line == sourceLine.line_number
            ? annotation.caret_indent
            : 0;
    }

    private static int get_caret_width(
        DiagnosticAnnotationViewModel annotation,
        DiagnosticSourceLineViewModel sourceLine,
        int indent)
    {
        if (annotation.is_single_line)
        {
            return annotation.caret_width;
        }

        var visibleWidth = System.Math.Max(sourceLine.text.Length, 1);
        if (annotation.start_line == sourceLine.line_number)
        {
            return System.Math.Max(visibleWidth - indent, 1);
        }

        if (annotation.end_line == sourceLine.line_number)
        {
            return annotation.end_column > 1
                ? System.Math.Max(annotation.end_column - 1, 1)
                : 1;
        }

        if (should_use_compact_secondary_continuation(annotation, sourceLine))
        {
            return System.Math.Min(visibleWidth, 3);
        }

        return visibleWidth;
    }

    private static bool should_render_annotation_label(
        DiagnosticAnnotationViewModel annotation,
        int lineNumber)
    {
        return !string.IsNullOrWhiteSpace(annotation.label)
               && (annotation.is_single_line || annotation.end_line == lineNumber);
    }

    private static bool should_use_compact_secondary_continuation(
        DiagnosticAnnotationViewModel annotation,
        DiagnosticSourceLineViewModel sourceLine)
    {
        return annotation.kind == DiagnosticAnnotationKind.Secondary
               && !annotation.is_single_line
               && annotation.start_line < sourceLine.line_number
               && annotation.end_line > sourceLine.line_number;
    }

    private static List<List<DiagnosticAnnotationViewModel>> build_annotation_layers(
        IReadOnlyList<DiagnosticAnnotationViewModel> annotations,
        DiagnosticSourceLineViewModel sourceLine,
        IReadOnlyList<IReadOnlyList<DiagnosticAnnotationViewModel>> previousLayers)
    {
        var layers = new List<List<DiagnosticAnnotationViewModel>>();
        var secondaryAnnotations = new List<DiagnosticAnnotationViewModel>();
        foreach (var annotation in annotations)
        {
            if (annotation.kind == DiagnosticAnnotationKind.Primary)
            {
                assign_annotation_to_layers(layers, annotation, sourceLine);
                continue;
            }

            secondaryAnnotations.Add(annotation);
        }

        var pendingAnnotations = new List<DiagnosticAnnotationViewModel>(secondaryAnnotations);
        foreach (var annotation in secondaryAnnotations)
        {
            var preferredLayerIndex = find_previous_layer_index(previousLayers, annotation);
            if (preferredLayerIndex < 0)
            {
                continue;
            }

            assign_annotation_to_layers(layers, annotation, sourceLine, preferredLayerIndex);
            pendingAnnotations.Remove(annotation);
        }

        foreach (var annotation in pendingAnnotations)
        {
            assign_annotation_to_layers(layers, annotation, sourceLine);
        }

        foreach (var layer in layers)
        {
            layer.Sort((left, right) => get_caret_indent(left, sourceLine).CompareTo(get_caret_indent(right, sourceLine)));
        }

        return layers;
    }

    private static void assign_annotation_to_layers(
        List<List<DiagnosticAnnotationViewModel>> layers,
        DiagnosticAnnotationViewModel annotation,
        DiagnosticSourceLineViewModel sourceLine,
        int preferredStartIndex = 0)
    {
        for (var layerIndex = preferredStartIndex; layerIndex < layers.Count; layerIndex++)
        {
            if (!can_assign_annotation(layers[layerIndex], annotation, sourceLine))
            {
                continue;
            }

            layers[layerIndex].Add(annotation);
            return;
        }

        for (var layerIndex = 0; layerIndex < preferredStartIndex && layerIndex < layers.Count; layerIndex++)
        {
            if (!can_assign_annotation(layers[layerIndex], annotation, sourceLine))
            {
                continue;
            }

            layers[layerIndex].Add(annotation);
            return;
        }

        layers.Add([annotation]);
    }

    private static int find_previous_layer_index(
        IReadOnlyList<IReadOnlyList<DiagnosticAnnotationViewModel>> previousLayers,
        DiagnosticAnnotationViewModel annotation)
    {
        for (var layerIndex = 0; layerIndex < previousLayers.Count; layerIndex++)
        {
            if (previousLayers[layerIndex].Contains(annotation))
            {
                return layerIndex;
            }
        }

        return -1;
    }

    private static bool can_assign_annotation(
        IReadOnlyList<DiagnosticAnnotationViewModel> layer,
        DiagnosticAnnotationViewModel annotation,
        DiagnosticSourceLineViewModel sourceLine)
    {
        var range = get_visible_range(annotation, sourceLine);
        return layer.All(existing => !ranges_overlap(
            range,
            get_visible_range(existing, sourceLine)));
    }

    private static (int start, int end) get_visible_range(
        DiagnosticAnnotationViewModel annotation,
        DiagnosticSourceLineViewModel sourceLine)
    {
        var indent = get_caret_indent(annotation, sourceLine);
        var width = get_caret_width(annotation, sourceLine, indent);
        return (indent, indent + width);
    }

    private static bool ranges_overlap((int start, int end) left, (int start, int end) right)
    {
        return left.start < right.end && right.start < left.end;
    }

    private static char resolve_marker(DiagnosticAnnotationViewModel annotation)
    {
        return annotation.kind == DiagnosticAnnotationKind.Primary ? '^' : '-';
    }

    private static ConsoleColor resolve_annotation_color(
        DiagnosticViewModel viewModel,
        IReadOnlyList<DiagnosticAnnotationViewModel> annotations)
    {
        return annotations.Any(annotation => annotation.kind == DiagnosticAnnotationKind.Primary)
            ? DiagnosticRenderSupport.resolve_severity_color(viewModel.severity)
            : ConsoleColor.DarkGray;
    }
}
