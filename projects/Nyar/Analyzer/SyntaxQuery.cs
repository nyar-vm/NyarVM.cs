using TextSpan = Std.Data.Text.Syntax.TextSpan;

namespace Nyar.Analyzer;

public static class SyntaxQuery
{
    public static IEnumerable<(int Kind, TextSpan Span)> descendants_of_kind(
        IReadOnlyList<(int Kind, TextSpan Span, int ChildCount, int StartIndex)> nodes,
        int startIndex,
        int targetKind)
    {
        if (startIndex < 0 || startIndex >= nodes.Count) yield break;

        var stack = new Stack<int>();
        stack.Push(startIndex);

        while (stack.Count > 0)
        {
            var idx = stack.Pop();
            var (kind, span, childCount, childStart) = nodes[idx];

            if (kind == targetKind) yield return (kind, span);

            for (var i = 0; i < childCount; i++)
            {
                var childIdx = childStart + i;
                if (childIdx < nodes.Count) stack.Push(childIdx);
            }
        }
    }

    public static IEnumerable<(int Kind, TextSpan Span)> descendants(
        IReadOnlyList<(int Kind, TextSpan Span, int ChildCount, int StartIndex)> nodes,
        int startIndex)
    {
        if (startIndex < 0 || startIndex >= nodes.Count) yield break;

        var stack = new Stack<int>();
        stack.Push(startIndex);

        while (stack.Count > 0)
        {
            var idx = stack.Pop();
            var (kind, span, childCount, childStart) = nodes[idx];

            yield return (kind, span);

            for (var i = 0; i < childCount; i++)
            {
                var childIdx = childStart + i;
                if (childIdx < nodes.Count) stack.Push(childIdx);
            }
        }
    }

    public static (int Kind, TextSpan Span)? find_token_at_offset(
        IReadOnlyList<(int Kind, TextSpan Span)> tokens,
        int offset)
    {
        var left = 0;
        var right = tokens.Count - 1;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var token = tokens[mid];

            if (token.Span.contains(offset)) return token;

            if (offset < token.Span.start)
                right = mid - 1;
            else
                left = mid + 1;
        }

        return null;
    }

    public static IEnumerable<(int Kind, TextSpan Span)> tokens_in_range(
        IReadOnlyList<(int Kind, TextSpan Span)> tokens,
        TextSpan range)
    {
        foreach (var token in tokens)
            if (token.Span.overlaps_with(range))
                yield return token;
    }
}