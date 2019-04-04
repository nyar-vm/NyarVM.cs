using System.Text;
using System.Text.Json;

namespace Std.DL.Data;

/// <summary>
///     松散包格式 .glpx 的读写器
/// </summary>
public static class LoosePack
{
    private const int DefaultChunkSize = 640;

    /// <summary>
    ///     从 raw reader 回调创建 .glpx 目录
    /// </summary>
    /// <param name="outputDir">输出目录（如 mnist.glpx/）</param>
    /// <param name="schema">数据集 Schema</param>
    /// <param name="splitNames">分片名列表（如 train, test）</param>
    /// <param name="recordSource">回调：(splitIndex, recordIndex) → (bytes, 是否继续)</param>
    /// <param name="splitCounts">每个分片的记录数</param>
    public static void Write(string outputDir, DatasetSchema schema, string[] splitNames,
        Func<int, int, (byte[] Data, bool HasMore)> recordSource, int[] splitCounts)
    {
        Directory.CreateDirectory(outputDir);

        var indexJson = BuildIndexJson(schema, splitNames, splitCounts);
        File.WriteAllText(Path.Combine(outputDir, "index.json5"), indexJson, Encoding.UTF8);

        for (var si = 0; si < splitNames.Length; si++)
        {
            var splitPath = Path.Combine(outputDir, splitNames[si]);
            Directory.CreateDirectory(splitPath);

            var chunks = new List<object>();
            var chunkIdx = 0;
            var globalIdx = 0;
            var total = splitCounts[si];

            while (globalIdx < total)
            {
                var chunkEnd = System.Math.Min(globalIdx + DefaultChunkSize, total);
                var chunkPath = Path.Combine(splitPath, $"chunk_{chunkIdx:D5}.data");

                using var fs = new FileStream(chunkPath, FileMode.Create);
                using var bw = new BinaryWriter(fs);

                for (var i = globalIdx; i < chunkEnd; i++)
                {
                    var (data, _) = recordSource(si, i);
                    bw.Write(data.Length);
                    bw.Write(data);
                }

                chunks.Add(new
                    { start = globalIdx, end = chunkEnd, path = Path.GetRelativePath(outputDir, chunkPath) });
                globalIdx = chunkEnd;
                chunkIdx++;
            }

            var chunksJson = JsonSerializer.Serialize(chunks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(splitPath, "chunks.json5"), chunksJson, Encoding.UTF8);
        }
    }

    private static string BuildIndexJson(DatasetSchema schema, string[] splitNames, int[] splitCounts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  name: \"{schema.Name}\",");
        sb.AppendLine($"  version: \"{schema.Version}\",");
        if (schema.Source is not null) sb.AppendLine($"  source: \"{schema.Source}\",");

        sb.AppendLine($"  created: \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",");
        sb.AppendLine("  schema: {");
        sb.AppendLine("    inputs: [");
        for (var i = 0; i < schema.Inputs.Count; i++)
        {
            var f = schema.Inputs[i];
            sb.Append($"      {{ name: \"{f.Name}\", type: {f.TypeCode}, shape: [{string.Join(", ", f.Shape)}] }}");
            sb.AppendLine(i < schema.Inputs.Count - 1 ? "," : "");
        }

        sb.AppendLine("    ],");
        sb.AppendLine("    outputs: [");
        for (var i = 0; i < schema.Outputs.Count; i++)
        {
            var f = schema.Outputs[i];
            sb.Append($"      {{ name: \"{f.Name}\", type: {f.TypeCode}, shape: [{string.Join(", ", f.Shape)}] }}");
            sb.AppendLine(i < schema.Outputs.Count - 1 ? "," : "");
        }

        sb.AppendLine("    ]");
        sb.AppendLine("  },");
        sb.AppendLine("  splits: {");
        for (var i = 0; i < splitNames.Length; i++)
        {
            sb.Append($"    {splitNames[i]}: {{ count: {splitCounts[i]}, path: \"{splitNames[i]}/\" }}");
            sb.AppendLine(i < splitNames.Length - 1 ? "," : "");
        }

        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }
}